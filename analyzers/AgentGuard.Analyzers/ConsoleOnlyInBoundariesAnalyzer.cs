// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw <c>System.Console</c> use — a call such as <c>Console.WriteLine</c> or a read of <c>Console.Out</c>,
/// <c>Console.Error</c>, or <c>Console.In</c> — made outside the <c>AgentGuard.Boundaries</c> adapter assembly.
/// Every other assembly writes and reads the console through <c>IConsole</c> pulled off <c>ISystemServices</c>,
/// which mirrors <c>System.Console</c>'s shape, so console output is captured in tests instead of hitting the real
/// streams. The raw <c>System.Console</c> lives only in the <c>Boundaries</c> console adapter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsoleOnlyInBoundariesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0016";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Raw System.Console use must live only in AgentGuard.Boundaries",
        messageFormat: "Raw console use '{0}' is outside AgentGuard.Boundaries; write and read the console through IConsole pulled off ISystemServices",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A use of System.Console is allowed only in the AgentGuard.Boundaries adapter assembly. Every other assembly writes and reads the console through IConsole on ISystemServices so console output is captured in tests instead of hitting the real streams.");

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
        context.RegisterCompilationStartAction(
            context => MemberUseScanner.RegisterUnless(context, BoundaryAssembly.IsBoundariesLibrary, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (WellKnownType.Is(type, KnownNamespaces.System, "Console"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }
}
