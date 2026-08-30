// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a bounded wait or timeout that does not flow through the injected clock (<c>TimeProvider</c>), so the one
/// 60-second presence timeout stays testable by advancing a fake clock with no real wait (timeout-uses-timeprovider).
/// A blocking sleep (<c>Thread.Sleep</c>) is banned everywhere — it has no <c>TimeProvider</c> form and cannot be
/// virtualized. A <c>Task.Delay</c>, <c>Task.WaitAsync</c>, or <c>CancellationTokenSource</c> that carries a timeout
/// must take the overload with a <c>System.TimeProvider</c> parameter (for example
/// <c>Task.Delay(delay, services.Clock, ct)</c> and <c>new CancellationTokenSource(delay, services.Clock)</c>); the
/// no-<c>TimeProvider</c> overloads bind the real system clock and make the wait un-fakeable. This complements AG0015,
/// which bans raw wall-clock READS; this rule governs bounded WAITS. Reading a delay off an already-injected clock is
/// never caught. This rule is preventive against the presence gate's timeout; the codebase has no bounded wait today.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TimeoutMustUseTimeProviderAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0038";

    private const string Category = "AgentGuard.Architecture";
    private const string TimeProviderName = "TimeProvider";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A bounded wait or timeout must flow through the injected TimeProvider",
        messageFormat: "Bounded wait '{0}' does not flow through the injected clock; use the TimeProvider overload (pass ISystemServices.Clock) so the timeout is testable — Thread.Sleep is banned outright",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A bounded wait or timeout must be virtualizable through the injected clock so the 60-second presence timeout is proven by advancing a fake TimeProvider with no real wait. Thread.Sleep is banned everywhere (it has no TimeProvider form); Task.Delay, Task.WaitAsync, and a CancellationTokenSource carrying a timeout must take the overload with a System.TimeProvider parameter and be passed ISystemServices.Clock. This complements AG0015, which bans raw wall-clock reads; this rule governs bounded waits.");

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
        if (member is not IMethodSymbol method)
        {
            return;
        }

        if (IsBanned(method, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsBanned(IMethodSymbol method, INamedTypeSymbol type)
    {
        // Thread.Sleep — a blocking sleep with no TimeProvider form, banned everywhere. This rule's own; not a
        // bounded-timeout member AG0107 shares.
        if (WellKnownType.Is(type, KnownNamespaces.SystemThreading, "Thread")
            && string.Equals(method.Name, "Sleep", StringComparison.Ordinal))
        {
            return true;
        }

        // Task.Delay / Task.WaitAsync (the shared TimeoutMembers detector) — must take the TimeProvider overload.
        if (TimeoutMembers.IsTaskDelay(method, type) || TimeoutMembers.IsTaskWaitAsync(method, type))
        {
            return !HasTimeProviderParameter(method);
        }

        // CancellationTokenSource (the shared detector) — the timeout-carrying constructors (any non-empty parameter
        // list, which are all the delay overloads) must take the TimeProvider overload. The parameterless ctor carries
        // no timeout.
        return TimeoutMembers.IsCancellationTokenSourceConstructor(method, type)
            && !method.Parameters.IsEmpty
            && !HasTimeProviderParameter(method);
    }

    private static bool HasTimeProviderParameter(IMethodSymbol method)
    {
        return method.Parameters.Any(
            parameter => WellKnownType.Is(parameter.Type as INamedTypeSymbol, KnownNamespaces.System, TimeProviderName));
    }
}
