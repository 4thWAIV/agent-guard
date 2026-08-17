// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a static (non-const) field or property typed <c>ISystemServices</c> or any of its service types — the
/// owned boundary interfaces derived from the container plus <c>System.TimeProvider</c> — declared outside the
/// composition point. AG0017 only stops re-calling <c>SystemServices.Create()</c>; nothing stopped stashing the result
/// (or one service) in a static and reading it ambiently, which is the service-locator shortcut constructor injection
/// forbids (ag0024-no-static-service-holder). Services arrive by constructor injection; the only place a service type
/// may sit in a static is the composition point (the <c>Program</c> method or the test <c>SystemServicesBuilder</c>).
/// A constant is exempt because a <c>const</c> cannot hold a service instance. The service-type set is the one derived
/// from <c>ISystemServices</c> by <see cref="BoundaryServices.Resolve"/>, captured once per compilation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoStaticServiceHolderAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0024";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A static field or property must not hold a service type outside the composition point",
        messageFormat: "Static holder '{0}' is typed as the ISystemServices container or one of its service types; receive the service through the constructor instead of stashing it in a static",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A static (non-const) field or property typed ISystemServices or one of its service types (the owned boundary interfaces derived from the container plus System.TimeProvider) is a build error outside the composition point. Reading a service ambiently off a static is the service-locator shortcut constructor injection forbids; services arrive through the constructor.");

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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // Derive the service-type set from ISystemServices once per compilation, then capture it for the per-symbol
        // callbacks — no hand-maintained list (derive-service-set-from-isystemservices).
        DerivedServices services = BoundaryServices.Resolve(context.Compilation);
        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, services), SymbolKind.Field);
        context.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, services), SymbolKind.Property);
    }

    private static void AnalyzeField(SymbolAnalysisContext context, DerivedServices services)
    {
        var field = (IFieldSymbol)context.Symbol;

        // A const cannot hold a service instance; only a static, non-const field is a holder.
        if (!field.IsStatic || field.IsConst)
        {
            return;
        }

        Report(context, services, field, field.Type as INamedTypeSymbol);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, DerivedServices services)
    {
        var property = (IPropertySymbol)context.Symbol;

        if (!property.IsStatic)
        {
            return;
        }

        Report(context, services, property, property.Type as INamedTypeSymbol);
    }

    private static void Report(
        SymbolAnalysisContext context, DerivedServices services, ISymbol holder, INamedTypeSymbol? holderType)
    {
        // The composition point (the Program method / the SystemServicesBuilder) is the one place a service type may
        // sit in a static; everywhere else is RED. Walk the holder's enclosing types with the same Encloses form the
        // three sibling composition-point rules use (AG0017/AG0031/AG0015), so a static nested inside the composition
        // point is exempt too, not only one whose immediate containing type is the composition point.
        if (CompositionPoint.Encloses(holder))
        {
            return;
        }

        if (services.IsServiceType(holderType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, holder.Locations[0], holder.Name));
        }
    }
}
