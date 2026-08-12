// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw wall-clock read anywhere in the codebase — <c>DateTime.Now</c>/<c>UtcNow</c>/<c>Today</c>,
/// <c>DateTimeOffset.Now</c>/<c>UtcNow</c>, a <c>Stopwatch</c>, or <c>Environment.TickCount</c> — and a direct
/// <c>TimeProvider</c> acquisition (<c>TimeProvider.System</c>, or any static member of <c>System.TimeProvider</c>
/// that hands back a <c>TimeProvider</c>). The raw wall-clock reads are banned everywhere; the direct acquisition is
/// legal only at the one composition point — the <c>Program</c> method or the test <c>SystemServicesBuilder</c> —
/// because that is where the clock is wired into <c>ISystemServices</c> (timeprovider-on-the-container). Everywhere
/// else reads time off the injected clock <c>ISystemServices.Clock</c>, so tests control it. An instance call on an
/// already-injected clock (<c>clock.GetUtcNow()</c>) is not an acquisition and is never caught; constructing or
/// comparing <c>DateTime</c>/<c>DateTimeOffset</c> values remains legal.
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
        title: "Time must be read through the injected clock on ISystemServices",
        messageFormat: "Clock access '{0}' bypasses the injected clock; read time off ISystemServices.Clock — a raw wall-clock read is banned everywhere and a direct TimeProvider acquisition only at the composition point",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A raw wall-clock read (DateTime.Now/UtcNow/Today, DateTimeOffset.Now/UtcNow, Stopwatch, Environment.TickCount) is banned everywhere, and a direct TimeProvider acquisition (TimeProvider.System, or any static System.TimeProvider member returning a TimeProvider) is legal only at the one SystemServices.Create() composition point and the test SystemServicesBuilder. Everywhere else reads time through the injected clock off ISystemServices.Clock, so tests control the clock.");

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
        // A direct TimeProvider acquisition (TimeProvider.System) is legal only at the one composition point and the
        // test builder — that is where the clock is wired into ISystemServices; everywhere else reads it off
        // ISystemServices.Clock (timeprovider-on-the-container, AG0015 tightened from preventive to active).
        if (IsClockAcquisition(member, type))
        {
            if (!CompositionPoint.Encloses(context.ContainingSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
            }

            return;
        }

        // A raw wall-clock read is banned everywhere, with no composition exemption.
        if (IsBanned(member, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsClockAcquisition(ISymbol member, INamedTypeSymbol type)
    {
        // A static acquisition of a TimeProvider from System.TimeProvider — TimeProvider.System, and any future static
        // member on that type that hands back a TimeProvider — is a direct acquisition. An instance call on an
        // already-injected clock (clock.GetUtcNow()) is not static, so it is never caught here; and reading
        // ISystemServices.Clock is a member on the container, not on TimeProvider, so it is not caught either.
        return member.IsStatic
            && WellKnownType.Is(type, KnownNamespaces.System, BoundaryServices.TimeProviderName)
            && WellKnownType.Is(
                MemberValueType(member) as INamedTypeSymbol, KnownNamespaces.System, BoundaryServices.TimeProviderName);
    }

    private static ITypeSymbol? MemberValueType(ISymbol member)
    {
        return member switch
        {
            IPropertySymbol property => property.Type,
            IMethodSymbol method => method.ReturnType,
            IFieldSymbol field => field.Type,
            _ => null,
        };
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
