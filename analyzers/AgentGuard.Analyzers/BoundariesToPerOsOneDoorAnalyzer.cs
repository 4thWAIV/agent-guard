// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a call from <c>AgentGuard.Boundaries</c> into a per-OS implementation assembly
/// (<c>.MacOS</c>/<c>.Linux</c>/<c>.Windows</c>) that is not <c>Platform.Create()</c>. The per-OS
/// <c>InternalsVisibleTo</c> grants (platform-create-internal) otherwise expose every internal member to Boundaries;
/// AG0023 pins only the core CrossPlatform assembly, so this rule pins the per-OS door
/// (ag0029-boundaries-to-per-os-one-door). The one legal call is the static <c>Platform.Create()</c> that
/// <c>SystemServices.Create()</c> uses to obtain the OS-divergent <c>IPlatformFileSystem</c>; any other
/// Boundaries → per-OS call is a build error.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BoundariesToPerOsOneDoorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0029";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Boundaries may call a per-OS assembly only through Platform.Create()",
        messageFormat: "Call from AgentGuard.Boundaries into per-OS member '{0}' is not Platform.Create(); the one legal door into a per-OS assembly is Platform.Create()",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A call from AgentGuard.Boundaries into a per-OS implementation assembly (.MacOS/.Linux/.Windows) is legal only as Platform.Create(). The per-OS InternalsVisibleTo grants otherwise expose every internal member; the single door is the static Platform.Create() that SystemServices.Create() uses to obtain the OS-divergent IPlatformFileSystem.");

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
        // gate-then-scan setup is shared with AG0023 through MemberUseScanner.RegisterForAssembly; only Inspect (the
        // one legal door) is this rule's own.
        context.RegisterCompilationStartAction(
            context => MemberUseScanner.RegisterForAssembly(context, BoundaryAssembly.Name, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        // Only a call reaching into a per-OS implementation assembly is in scope; a call into the core CrossPlatform
        // assembly is AG0023's, and a call within Boundaries is neither rule's.
        if (!CrossPlatformBoundary.IsPerOsImplementationAssembly(type.ContainingAssembly?.Name))
        {
            return;
        }

        // The one legal door: the static Platform.Create() factory, matched by full type identity in PlatformFactory,
        // never a bare name. Any other Boundaries → per-OS call is a build error.
        if (PlatformFactory.Is(member, type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
    }
}
