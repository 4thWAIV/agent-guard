// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports where the test <c>SystemServicesBuilder</c> fails to mirror the <c>ISystemServices</c> container at every
/// nesting level (ag0019-recursive-nested-completeness, builder-mirrors-container-nesting). The builder is the single
/// approved way a test constructs the container, and every service must be substitutable AND wrappable through it, so
/// it must mirror the container's shape: a DIRECT leaf service — a <c>Contracts</c>-interface accessor, or the
/// <c>TimeProvider</c> clock — on a container node requires a <c>With(T)</c> and a <c>Wrap(Func&lt;T,T&gt;)</c> on the
/// builder scope for that node; a NESTED container accessor (for example <c>FileSystem</c>) requires a public
/// zero-parameter navigator named <c>On&lt;AccessorMemberName&gt;()</c> (for example <c>OnFileSystem()</c>) whose
/// return type is the sub-builder scope, which must recursively satisfy the same check to arbitrary depth. A missing
/// navigator is reported ONCE at that node, not once per leaf beneath it. The walk is the ONE container walk
/// <see cref="BoundaryServices.ResolveTree"/> performs, so the builder is enforced against the identical structure
/// AG0034 guards and the identical leaf set AG0025/AG0031 read. The rule runs only in the assembly that DEFINES the
/// builder (<c>AgentGuard.TestHelpers</c>): a <c>.Tests</c> project that merely references the builder is not
/// re-checked. Service parameter types are matched by full name (namespace + name) through <see cref="WellKnownType"/>,
/// and <c>Func&lt;,&gt;</c> by <see cref="WellKnownType"/> — never a bare name. It is preventive today — the builder is
/// built in the IMPLEMENT pass — so it does not fire until the builder exists.
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

    // The builder is AgentGuard.TestHelpers.SystemServicesBuilder (TestAssembly.TestHelpersName + the builder type
    // name) — composed from the single owner of each part rather than re-spelling the literal.
    private const string BuilderMetadataName =
        TestAssembly.TestHelpersName + "." + BuilderTypeName;

    private const string WithMethodName = "With";
    private const string WrapMethodName = "Wrap";
    private const string NavigatorPrefix = "On";
    private const string FuncTypeName = "Func";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "The test SystemServicesBuilder must mirror the ISystemServices container at every nesting level",
        messageFormat: "'{0}' on {1} has no matching '{2}' on builder scope '{3}'; add it so SystemServicesBuilder mirrors the container at every level",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The test SystemServicesBuilder must mirror the ISystemServices container at every nesting level: each direct leaf service (a Contracts-interface accessor or the TimeProvider clock) on a container node needs a With(T) and a Wrap(Func<T,T>) on the builder scope for that node, and each nested container accessor needs a public zero-parameter On<AccessorMemberName>() navigator whose return type recursively satisfies the same check. A leaf parked on the wrong scope, or a missing navigator, is a build error until it is added, so the one approved test-construction path can never silently fall behind the container.",
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
        // safe here — SystemServicesBuilder is a first-party type defined in exactly one assembly, so there is no
        // cross-assembly ambiguity. Absent the builder — here, or the container from a referenced AgentGuard.Abstractions
        // — the rule is preventive and simply does not fire (nothing can fall behind a container that is not there yet).
        INamedTypeSymbol? builder = context.Compilation.GetTypeByMetadataName(BuilderMetadataName);
        if (builder is null
            || !WellKnownType.IsDeclaredInCompilation(builder, context.Compilation))
        {
            return;
        }

        ContainerNode? root = BoundaryServices.ResolveTree(context.Compilation);
        if (root is null)
        {
            return;
        }

        CheckScope(context, builder, root);
    }

    // Check one (builder scope, container node) pair, then recurse into each nested container through its navigator.
    private static void CheckScope(CompilationAnalysisContext context, INamedTypeSymbol scope, ContainerNode node)
    {
        foreach (ServiceAccessor accessor in node.Accessors)
        {
            if (accessor.Kind == ServiceAccessorKind.Container)
            {
                CheckNestedContainer(context, scope, node.Interface, accessor);
            }
            else
            {
                CheckLeaf(context, scope, node.Interface, accessor.Target);
            }
        }
    }

    // A direct leaf service (a Contracts-interface accessor or the TimeProvider clock) on this node requires a With(T)
    // and a Wrap(Func<T,T>) on the current builder scope, T matched by namespace + name.
    private static void CheckLeaf(
        CompilationAnalysisContext context, INamedTypeSymbol scope, INamedTypeSymbol container, INamedTypeSymbol target)
    {
        (string Namespace, string Name) identity = (target.ContainingNamespace.ToDisplayString(), target.Name);

        if (!HasSubstituteOverload(scope, WithMethodName, parameter => IsServiceParameter(parameter, identity)))
        {
            Report(context, scope, target.Name, container.Name, WithMethodName + "(" + target.Name + ")");
        }

        if (!HasSubstituteOverload(scope, WrapMethodName, parameter => IsFuncOfServiceParameter(parameter, identity)))
        {
            Report(
                context,
                scope,
                target.Name,
                container.Name,
                WrapMethodName + "(Func<" + target.Name + ", " + target.Name + ">)");
        }
    }

    // A nested container accessor requires a public zero-parameter On<AccessorMemberName>() navigator whose return type
    // is the sub-builder scope; recurse into that scope against the child node. A missing navigator is reported ONCE
    // here (not once per leaf beneath it), and the recursion stops because there is no sub-builder to descend into.
    private static void CheckNestedContainer(
        CompilationAnalysisContext context, INamedTypeSymbol scope, INamedTypeSymbol container, ServiceAccessor accessor)
    {
        string navigatorName = NavigatorPrefix + accessor.MemberName;

        INamedTypeSymbol? subBuilder = scope.GetMembers(navigatorName)
            .OfType<IMethodSymbol>()
            .Where(method => method.MethodKind == MethodKind.Ordinary
                && method.DeclaredAccessibility == Accessibility.Public
                && method.Parameters.IsEmpty)
            .Select(method => method.ReturnType as INamedTypeSymbol)
            .FirstOrDefault(returnType => returnType is not null);

        if (subBuilder is null)
        {
            Report(context, scope, accessor.MemberName, container.Name, navigatorName + "()");
            return;
        }

        if (accessor.Child is not null)
        {
            CheckScope(context, subBuilder, accessor.Child);
        }
    }

    private static bool HasSubstituteOverload(
        INamedTypeSymbol scope, string methodName, System.Func<ITypeSymbol, bool> parameterMatches)
    {
        return scope.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Any(method => method.Parameters.Length == 1 && parameterMatches(method.Parameters[0].Type));
    }

    private static bool IsServiceParameter(ITypeSymbol parameterType, (string Namespace, string Name) target)
    {
        // The With(T) parameter is the service type itself, matched by full name (namespace + name) through
        // WellKnownType — never a bare name.
        return WellKnownType.Is(parameterType as INamedTypeSymbol, target.Namespace, target.Name);
    }

    private static bool IsFuncOfServiceParameter(ITypeSymbol parameterType, (string Namespace, string Name) target)
    {
        // The Wrap(Func<T,T>) parameter is a System.Func with both type arguments the service type. Func is matched by
        // full name (namespace + name) through WellKnownType; each type argument the same way.
        return parameterType is INamedTypeSymbol { TypeArguments.Length: 2 } func
            && WellKnownType.Is(func, KnownNamespaces.System, FuncTypeName)
            && WellKnownType.Is(func.TypeArguments[0] as INamedTypeSymbol, target.Namespace, target.Name)
            && WellKnownType.Is(func.TypeArguments[1] as INamedTypeSymbol, target.Namespace, target.Name);
    }

    private static void Report(
        CompilationAnalysisContext context,
        INamedTypeSymbol scope,
        string memberName,
        string containerName,
        string missingMember)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(Rule, scope.Locations[0], memberName, containerName, missingMember, scope.Name));
    }
}
