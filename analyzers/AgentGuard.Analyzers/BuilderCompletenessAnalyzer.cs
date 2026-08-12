// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a service property on the <c>ISystemServices</c> container that has no matching <c>With(T)</c> or
/// <c>Wrap(Func&lt;T,T&gt;)</c> overload on the test <c>SystemServicesBuilder</c>. The builder is the single approved way
/// a test constructs the container, so it must stay complete as the container grows: every service the container
/// exposes must be substitutable through <c>With(...)</c> and wrappable through <c>Wrap(...)</c>
/// (builder-completeness-ag0019, tightened to require BOTH by the all-abstractions ruling). Add a service property to
/// the container without its builder overloads and this fires — a build error until the overloads are added, so the
/// builder can never silently fall behind the container. The rule runs only in the assembly that DEFINES the builder
/// (<c>AgentGuard.TestHelpers</c>): a <c>.Tests</c> project that merely references the builder is not re-checked. The
/// container type and each property type are matched by full name (namespace + name, LESSON 1) — the container and the
/// service parameter types by the exact symbol identity of the container property's type, and <c>Func&lt;,&gt;</c> by
/// <see cref="WellKnownType"/> — never a bare name.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BuilderCompletenessAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0019";

    private const string Category = "AgentGuard.Architecture";
    private const string BuilderTypeName = "SystemServicesBuilder";

    // Compose the metadata names from the single owners of each part rather than re-spelling the literals: the
    // container is AgentGuard.Abstractions.Contracts.ISystemServices (KnownNamespaces.AgentGuardAbstractionsContracts +
    // BoundaryServices.ContainerName — the same two owners SystemServicesCreateOnlyAtCompositionAnalyzer matches the
    // container by) and the builder is AgentGuard.TestHelpers.SystemServicesBuilder (TestAssembly.TestHelpersName +
    // the builder type name).
    private const string ContainerMetadataName =
        KnownNamespaces.AgentGuardAbstractionsContracts + "." + BoundaryServices.ContainerName;

    private const string BuilderMetadataName =
        TestAssembly.TestHelpersName + "." + BuilderTypeName;

    private const string WithMethodName = "With";
    private const string WrapMethodName = "Wrap";
    private const string FuncTypeName = "Func";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "The test SystemServicesBuilder must stay complete with the ISystemServices container",
        messageFormat: "Service '{0}' ({1}) on ISystemServices has no matching '{2}' on SystemServicesBuilder; add the '{2}' overload so the builder stays complete with the container",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every service property on ISystemServices must have a matching With(T) and Wrap(Func<T,T>) overload on the test SystemServicesBuilder. Adding a service to the container without its builder overloads is a build error until they are added, so the one approved test-construction path can never silently fall behind the container.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedRules;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        // The rule checks only the assembly that DEFINES the builder (AgentGuard.TestHelpers). GetTypeByMetadataName is
        // safe here — unlike the multi-forwarded BCL types WellKnownType guards against, SystemServicesBuilder and
        // ISystemServices are first-party types defined in exactly one assembly each, so there is no cross-assembly
        // ambiguity. Absent either type — the builder here, or the container from a referenced AgentGuard.Abstractions —
        // the rule is preventive and simply does not fire (nothing can fall behind a container that is not there yet).
        INamedTypeSymbol? builder = context.Compilation.GetTypeByMetadataName(BuilderMetadataName);
        if (builder is null
            || !SymbolEqualityComparer.Default.Equals(builder.ContainingAssembly, context.Compilation.Assembly))
        {
            return;
        }

        INamedTypeSymbol? container = context.Compilation.GetTypeByMetadataName(ContainerMetadataName);
        if (container is null)
        {
            return;
        }

        foreach (IPropertySymbol service in container.GetMembers().OfType<IPropertySymbol>())
        {
            ITypeSymbol serviceType = service.Type;

            if (!HasSubstituteOverload(builder, WithMethodName, serviceType, IsServiceParameter))
            {
                Report(context, builder, service.Name, serviceType, WithMethodName + "(" + serviceType.Name + ")");
            }

            if (!HasSubstituteOverload(builder, WrapMethodName, serviceType, IsFuncOfServiceParameter))
            {
                Report(
                    context,
                    builder,
                    service.Name,
                    serviceType,
                    WrapMethodName + "(Func<" + serviceType.Name + ", " + serviceType.Name + ">)");
            }
        }
    }

    private static bool HasSubstituteOverload(
        INamedTypeSymbol builder,
        string methodName,
        ITypeSymbol serviceType,
        System.Func<ITypeSymbol, ITypeSymbol, bool> parameterMatches)
    {
        return builder.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Any(method => method.Parameters.Length == 1 && parameterMatches(method.Parameters[0].Type, serviceType));
    }

    private static bool IsServiceParameter(ITypeSymbol parameterType, ITypeSymbol serviceType)
    {
        // The With(T) parameter is the service type itself, matched by exact symbol identity — the strongest possible
        // identity check, stronger than namespace + name.
        return SymbolEqualityComparer.Default.Equals(parameterType, serviceType);
    }

    private static bool IsFuncOfServiceParameter(ITypeSymbol parameterType, ITypeSymbol serviceType)
    {
        // The Wrap(Func<T,T>) parameter is a System.Func with both type arguments the service type. Func is matched by
        // full name (namespace + name) through WellKnownType; the two type arguments by exact symbol identity.
        return parameterType is INamedTypeSymbol { TypeArguments.Length: 2 } func
            && WellKnownType.Is(func, KnownNamespaces.System, FuncTypeName)
            && SymbolEqualityComparer.Default.Equals(func.TypeArguments[0], serviceType)
            && SymbolEqualityComparer.Default.Equals(func.TypeArguments[1], serviceType);
    }

    private static void Report(
        CompilationAnalysisContext context,
        INamedTypeSymbol builder,
        string serviceName,
        ITypeSymbol serviceType,
        string missingOverload)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(Rule, builder.Locations[0], serviceName, serviceType.Name, missingOverload));
    }
}
