// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a timeout introduced inside the presence layer — a class implementing
/// <c>AgentGuard.Abstractions.Contracts.IPresenceCheck</c>, OR one of the per-OS FLOW-port owners one level below it
/// (<c>ILocalAuthentication</c>/<c>IWindowsUserPresence</c>/<c>IPolkitAuthority</c>), OR one of the native-OPS owners
/// one layer below that (<c>IObjCRuntime</c>/<c>IWindowsHelloNativeOps</c>/<c>ICredentialPromptNativeOps</c>). The
/// approval gate owns the ONE 60-second bound around the presence check (via <c>Task.Delay(timeout, services.Clock, ct)</c>
/// and <c>Task.WhenAny</c>); neither a presence impl, nor its flow port, nor a native-ops class may wrap its own timeout
/// around the native/D-Bus call — a second bound would race the gate's, break the single fail-closed guarantee, and (for
/// the Linux polkit path) cancel <c>CheckAuthorization</c> mid-prompt. Constructing a <c>CancellationTokenSource</c>,
/// calling <c>CancelAfter</c>, awaiting <c>Task.Delay</c>, or starting a <c>Timer</c>/<c>PeriodicTimer</c> anywhere in
/// that layer is a build error. The impl, its port, and the native-ops class still receive and honor the gate's
/// <c>CancellationToken</c>; they just may not mint their own timeout. Preventive; no native-ops owner exists yet.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoTimeoutInPresenceImplAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0107";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A presence implementation or its per-OS native port must not introduce its own timeout",
        messageFormat: "Timeout '{0}' is created inside the presence layer (a presence impl or its per-OS native port); the approval gate owns the single 60-second bound — neither may wrap its own timeout around the native or D-Bus call",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class implementing IPresenceCheck, or one of the per-OS native-port owners below it (ILocalAuthentication/IWindowsUserPresence/IPolkitAuthority), must not construct a CancellationTokenSource, call CancelAfter, await Task.Delay, or start a Timer/PeriodicTimer: the approval gate owns the one 60-second fail-closed bound around the presence check, and a second timeout inside the impl or its port would race it and break the single-timeout guarantee. The impl and port honor the gate's injected CancellationToken but mint no timeout of their own.");

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
        // Only inside the presence layer: a class implementing IPresenceCheck, OR one of the per-OS FLOW-port owners
        // one level below it (ILocalAuthentication/IWindowsUserPresence/IPolkitAuthority), OR one of the native-OPS
        // owners one layer below THAT (IObjCRuntime/IWindowsHelloNativeOps/ICredentialPromptNativeOps). A port or
        // native-ops implementer that wraps its own timeout around the native/D-Bus call must be caught too — the gate
        // owns the one bound at every layer. Elsewhere, AG0038 governs bounded waits.
        if (!OwnerClass.Implements(OwnerClass.EnclosingType(context), PresenceContracts.PresenceCheck)
            && !PresenceContracts.IsInAnyPresencePortOwner(context)
            && !PresenceContracts.IsInNativeOpsOwner(context))
        {
            return;
        }

        if (IsTimeoutIntroduction(member, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsTimeoutIntroduction(ISymbol member, INamedTypeSymbol type)
    {
        // The bounded-timeout members recognized through the shared TimeoutMembers detector: any
        // CancellationTokenSource construction (even the TimeProvider overload) or its CancelAfter — the impl may not
        // mint or arm its own cancellation timeout — a Task.Delay or Task.WaitAsync wait (both shared with AG0038), and
        // a TimeProvider.CreateTimer. CreateTimer IS the injected-clock path AG0038 mandates as correct, so AG0038 never
        // flags it; but a presence impl must create no timeout at all, even off the injected clock, so AG0107 does.
        if (TimeoutMembers.IsCancellationTokenSourceConstructor(member, type)
            || TimeoutMembers.IsCancelAfter(member, type)
            || TimeoutMembers.IsTaskDelay(member, type)
            || TimeoutMembers.IsTaskWaitAsync(member, type)
            || TimeoutMembers.IsTimeProviderCreateTimer(member, type))
        {
            return true;
        }

        // A timer-driven bound — this rule's own, not a member AG0038 shares.
        return WellKnownType.Is(type, KnownNamespaces.SystemThreading, "Timer")
            || WellKnownType.Is(type, KnownNamespaces.SystemThreading, "PeriodicTimer");
    }
}
