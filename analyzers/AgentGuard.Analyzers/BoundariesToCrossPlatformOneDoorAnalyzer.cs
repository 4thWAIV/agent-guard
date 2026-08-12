// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a call from <c>AgentGuard.Boundaries</c> into the core <c>AgentGuard.CrossPlatform</c> assembly that is
/// not to the one CrossPlatform adapter-factory. <c>AgentGuard.CrossPlatform</c> exposes exactly ONE factory that
/// produces its owned adapters (the file-op adapters and <c>GuidFactory</c> that live there per
/// owners-live-at-lowest-consumer), and <c>SystemServices.Create()</c> calls only that one function; the adapters
/// stay <c>internal</c> with private constructors and an <c>InternalsVisibleTo</c> grant lets Boundaries reach the
/// factory, so this rule pins Boundaries to that single entry point — any other Boundaries → CrossPlatform-core call
/// is a build error (boundaries-calls-one-crossplatform-factory). The factory type name is established by this rule
/// (Rule-Driven Development): the <c>CrossPlatformAdapters</c> type in the <c>AgentGuard.CrossPlatform</c> namespace.
/// The per-OS <c>.MacOS</c>/<c>.Linux</c>/<c>.Windows</c> assemblies are AG0029's door, not this one; the relocated
/// <c>IPlatformFileSystem</c>/<c>IPlatformServices</c> live in <c>AgentGuard.Abstractions</c>, a different assembly,
/// so they are not caught here.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BoundariesToCrossPlatformOneDoorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0023";

    private const string Category = "AgentGuard.Architecture";

    // The one allowed door into the core AgentGuard.CrossPlatform assembly: the adapter-factory type. Its name is
    // established by this rule (Rule-Driven Development — the rules are written first and the implementation conforms),
    // mirroring how SystemServices (Boundaries) and Platform (per-OS) are named. Matched by full type identity.
    private const string AdapterFactoryTypeName = "CrossPlatformAdapters";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Boundaries may call the core CrossPlatform assembly only through the one adapter-factory",
        messageFormat: "Call from AgentGuard.Boundaries into CrossPlatform-core member '{0}' is not the one adapter-factory (CrossPlatformAdapters); call only that single CrossPlatform factory",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A call from AgentGuard.Boundaries into the core AgentGuard.CrossPlatform assembly is legal only to the one adapter-factory (the CrossPlatformAdapters type), which produces the file-op adapters and GuidFactory that live in CrossPlatform. The adapters stay internal with private constructors; SystemServices.Create() calls only that one factory, and any other Boundaries → CrossPlatform-core call is a build error.");

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

        // Gated to the AgentGuard.Boundaries compilation — the rule is about what Boundaries reaches into. The
        // gate-then-scan setup is shared with AG0029 through MemberUseScanner.RegisterForAssembly; only Inspect (the
        // one legal door) is this rule's own.
        context.RegisterCompilationStartAction(
            context => MemberUseScanner.RegisterForAssembly(context, BoundaryAssembly.Name, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        // Only a call into the CORE AgentGuard.CrossPlatform assembly is in scope. A per-OS assembly is AG0029's; a
        // call within Boundaries, or into any other assembly (including the relocated interfaces now in
        // AgentGuard.Abstractions), is neither rule's.
        if (!string.Equals(type.ContainingAssembly?.Name, CrossPlatformBoundary.RootName, StringComparison.Ordinal))
        {
            return;
        }

        if (WellKnownType.Is(type, CrossPlatformBoundary.RootName, AdapterFactoryTypeName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
    }
}
