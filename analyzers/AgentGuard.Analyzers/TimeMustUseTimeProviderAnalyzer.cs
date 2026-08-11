// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw wall-clock read anywhere in the codebase — <c>DateTime.Now</c>/<c>UtcNow</c>/<c>Today</c>,
/// <c>DateTimeOffset.Now</c>/<c>UtcNow</c>, a <c>Stopwatch</c>, or <c>Environment.TickCount</c>. Time is always
/// read through an injected <c>TimeProvider</c>, which is already threaded everywhere, so tests control the clock.
/// There is no assembly where a raw clock read is allowed. Constructing or comparing <c>DateTime</c> /
/// <c>DateTimeOffset</c> values remains legal; only reading the ambient clock is banned.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TimeMustUseTimeProviderAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0015";

    private const string Category = "AgentGuard.Architecture";

    private static readonly ImmutableHashSet<string> DateTimeClockMembers = ImmutableHashSet.Create(
        StringComparer.Ordinal, "Now", "UtcNow", "Today");

    private static readonly ImmutableHashSet<string> DateTimeOffsetClockMembers = ImmutableHashSet.Create(
        StringComparer.Ordinal, "Now", "UtcNow");

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Time must be read through an injected TimeProvider",
        messageFormat: "Raw clock read '{0}' is not allowed anywhere; read time through the injected TimeProvider so tests control the clock",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A raw wall-clock read (DateTime.Now/UtcNow/Today, DateTimeOffset.Now/UtcNow, Stopwatch, Environment.TickCount) is not allowed anywhere. Time is read through an injected TimeProvider, already threaded everywhere, so tests control the clock.");

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
        if (IsBanned(member, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsBanned(ISymbol member, INamedTypeSymbol type)
    {
        if (WellKnownType.Is(type, KnownNamespaces.System, "DateTime"))
        {
            return DateTimeClockMembers.Contains(member.Name);
        }

        if (WellKnownType.Is(type, KnownNamespaces.System, "DateTimeOffset"))
        {
            return DateTimeOffsetClockMembers.Contains(member.Name);
        }

        if (WellKnownType.Is(type, KnownNamespaces.System, "Environment"))
        {
            return TimeMembers.IsEnvironmentClockMember(member);
        }

        // Stopwatch — any use is a wall-clock read.
        return WellKnownType.Is(type, KnownNamespaces.SystemDiagnostics, "Stopwatch");
    }
}
