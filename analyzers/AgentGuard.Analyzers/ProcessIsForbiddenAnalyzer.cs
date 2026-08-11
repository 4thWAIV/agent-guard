// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a use of <c>System.Diagnostics.Process</c> or <c>ProcessStartInfo</c> anywhere in the codebase. The
/// guard launches no subprocess today, and process launching is not one of the primitives the boundary framework
/// wraps, so there is no assembly where a raw <c>Process</c> is allowed. If a process capability is ever needed, an
/// owned interface must be grown for it first rather than reaching for the raw type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProcessIsForbiddenAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0013";

    private const string Category = "AgentGuard.Architecture";

    private static readonly ImmutableArray<(string Namespace, string Name)> ProcessTypes = ImmutableArray.Create(
        (KnownNamespaces.SystemDiagnostics, "Process"),
        (KnownNamespaces.SystemDiagnostics, "ProcessStartInfo"));

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Process launching is not allowed",
        messageFormat: "Process launching '{0}' is not allowed anywhere; the guard launches no subprocess, so grow an owned interface first if a process capability is ever needed",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A use of System.Diagnostics.Process or ProcessStartInfo is not allowed anywhere in the codebase. The guard launches no subprocess and process launching is not a wrapped primitive; grow an owned interface first if the capability is ever needed.");

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
        context.RegisterCompilationStartAction(context => MemberUseScanner.Register(context, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (WellKnownType.IsAnyOf(type, ProcessTypes))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }
}
