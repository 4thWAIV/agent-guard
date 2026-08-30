// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports async orchestration inside a per-OS native-OPS owner — a class implementing <c>IObjCRuntime</c> (macOS
/// assembly) or one of the Windows native-ops interfaces (<c>IWindowsHelloNativeOps</c>/<c>ICredentialPromptNativeOps</c>).
/// The coverage refactor keeps the task/completion wiring — the <c>TaskCompletionSource</c>, the <c>GCHandle</c>, the
/// cancel-invalidate registration, the <c>await</c> of the native reply — in the fake-testable orchestrator (the flow
/// port), and leaves the native-ops class a set of SYNCHRONOUS raw primitives the orchestrator composes. These
/// async-orchestration shapes are a build error here: an <c>await</c> anywhere in the owner; an asynchronous
/// <c>await using</c> (which produces no <c>IAwaitOperation</c> node — the await folds into the using operation as
/// <c>IsAsynchronous</c> — so the bare <c>await</c> check alone would miss it); an asynchronous <c>await foreach</c>
/// (which likewise carries no <c>IAwaitOperation</c> node — it is an <c>IForEachLoopOperation</c> with
/// <c>IsAsynchronous</c> set, and AG0106's loop ban catches it only incidentally, so this rule catches it independently);
/// ANY call whose resolved return type is a <c>System.Threading.Tasks.Task</c>/<c>Task&lt;T&gt;</c>/<c>ValueTask</c>/
/// <c>ValueTask&lt;T&gt;</c> (a Task materializing in a pure-synchronous native-ops body means async work was spawned —
/// the return-type CATEGORY, which subsumes <c>Task.Run</c>, <c>Task.Factory.StartNew</c>, <c>Task.ContinueWith</c>,
/// <c>Task.Delay</c>, and the whole long tail without enumerating method names); and any use of
/// <c>System.Threading.Tasks.TaskCompletionSource</c> (its construction or any member — the task-completion state the
/// orchestrator owns). An <c>async</c> method necessarily either contains an
/// <c>await</c>/<c>await using</c>/<c>await foreach</c> (caught here) or is a CS1998 compiler warning that this
/// repository's <c>TreatWarningsAsErrors</c> fence already rejects, so the <c>async</c> keyword needs no separate check —
/// the shapes above fully cover "no async / task-completion state." This rule constrains only the CONTENTS of a
/// native-ops implementer; the class stays behind its interface and DI-mockable. Preventive; no native-ops owner exists
/// yet, so it compiles clean and its RED is proven by a fixture.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoAsyncOrchestrationInNativeOpsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0115";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A native-ops implementer must contain no async orchestration",
        messageFormat: "Async orchestration '{0}' is inside a native-ops implementer; the await and the TaskCompletionSource wiring belong in the fake-testable orchestrator (the flow port), not the thin synchronous native-ops class",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class implementing a per-OS native-ops interface (IObjCRuntime, IWindowsHelloNativeOps, ICredentialPromptNativeOps) must expose synchronous raw primitives; the async flow — the await of the native reply and the TaskCompletionSource / GCHandle / cancel-invalidate wiring — belongs in the fake-testable orchestrator (the flow port). An await anywhere in the owner, an asynchronous await using or await foreach (each produces no IAwaitOperation node, so the bare await check would miss it), any call whose resolved return type is a Task/Task<T>/ValueTask/ValueTask<T> (the return-type category that subsumes Task.Run, Task.Factory.StartNew, Task.ContinueWith, Task.Delay, and the rest without enumerating method names), or any use of System.Threading.Tasks.TaskCompletionSource (its construction or any member), is a build error. An async method either awaits (caught here) or is a CS1998 the TreatWarningsAsErrors fence rejects, so the async keyword needs no separate check. The native-ops class stays behind its interface and DI-mockable; this rule constrains only its implementation contents.");

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
        // The native-ops owners live only in the macOS and Windows per-OS assemblies; the shared gate-then-register
        // helper (owned once next to PresenceContracts.InMacOsOrWindows) skips every other compilation.
        PresenceContracts.RegisterInMacOsOrWindows(context, Register);
    }

    private static void Register(CompilationStartAnalysisContext context)
    {
        // The TaskCompletionSource use (construction and any member) rides the shared member-use scanner; any CALL that
        // materializes a Task/Task<T>/ValueTask/ValueTask<T> is caught by its return-type CATEGORY on the invocation
        // operation (subsuming every Task.Run/Factory.StartNew/ContinueWith/Delay/... spelling without enumerating
        // method names); the await, the `await using`, and the `await foreach` are operation kinds the scanner does not
        // surface, so each is a separate operation action.
        MemberUseScanner.Register(context, InspectMemberUse);
        context.RegisterOperationAction(InspectInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(InspectAwait, OperationKind.Await);
        context.RegisterOperationAction(InspectAwaitUsing, OperationKind.Using, OperationKind.UsingDeclaration);
        context.RegisterOperationAction(InspectAwaitForEach, OperationKind.Loop);
    }

    private static void InspectMemberUse(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        // TaskCompletionSource (non-generic) and TaskCompletionSource<TResult> are both named "TaskCompletionSource" in
        // System.Threading.Tasks, so the one name match catches its construction (an object creation, not a call, so the
        // invocation return-type check below would miss it) and every member use — reading .Task and the bool-returning
        // TrySetResult alike (neither surfaces a Task-typed call result).
        if (WellKnownType.Is(type, KnownNamespaces.SystemThreadingTasks, "TaskCompletionSource"))
        {
            NativeOpsOperationScan.ReportIfOwner(context, Rule, MemberUseScanner.Describe(member, type));
        }
    }

    private static void InspectInvocation(OperationAnalysisContext context)
    {
        // Any call whose resolved return type is a Task/Task<T>/ValueTask/ValueTask<T> materializes async work — in a
        // pure-synchronous native-ops body that means the orchestration was spawned here instead of in the fake-testable
        // flow port. Detecting the return-type CATEGORY subsumes Task.Run, Task.Factory.StartNew, Task.ContinueWith,
        // Task.Delay, and the whole long tail without enumerating method names (the SOLID fix). A constructor is an
        // object creation, not an invocation, so `new Task(...)` never reaches this action; TaskCompletionSource's
        // construction and members ride InspectMemberUse instead.
        var invocation = (IInvocationOperation)context.Operation;
        if (!ReturnsTaskLike(invocation.TargetMethod.ReturnType))
        {
            return;
        }

        NativeOpsOperationScan.ReportIfOwner(
            context, Rule, MemberUseScanner.Describe(invocation.TargetMethod, invocation.TargetMethod.ContainingType));
    }

    private static bool ReturnsTaskLike(ITypeSymbol returnType)
    {
        // Task and Task<TResult> share the simple name "Task"; ValueTask and ValueTask<TResult> share "ValueTask" — all
        // in System.Threading.Tasks. A constructed generic (Task<int>) carries the same Name and namespace as its
        // definition, so the two (namespace, name) matches cover all four shapes; a bool/IntPtr/void return matches
        // neither and is left alone (the real synchronous raw-call shape).
        return returnType is INamedTypeSymbol named
            && (WellKnownType.Is(named, KnownNamespaces.SystemThreadingTasks, "Task")
                || WellKnownType.Is(named, KnownNamespaces.SystemThreadingTasks, "ValueTask"));
    }

    private static void InspectAwait(OperationAnalysisContext context)
    {
        NativeOpsOperationScan.ReportIfOwner(context, Rule, "await");
    }

    private static void InspectAwaitForEach(OperationAnalysisContext context)
    {
        // OperationKind.Loop covers for/foreach/while/do; only an asynchronous foreach is async orchestration. An
        // `await foreach` produces an IForEachLoopOperation with IsAsynchronous set and no IAwaitOperation node, so the
        // bare await check misses it and AG0106's loop ban catches it only incidentally. A plain synchronous foreach —
        // and every non-foreach loop — is AG0106's concern, not this rule's, so it is left alone; this mirrors
        // InspectAwaitUsing's IsAsynchronous filter.
        if (context.Operation is not IForEachLoopOperation forEach || !forEach.IsAsynchronous)
        {
            return;
        }

        NativeOpsOperationScan.ReportIfOwner(context, Rule, "await foreach");
    }

    private static void InspectAwaitUsing(OperationAnalysisContext context)
    {
        // `await using` (both the statement form `await using (...) { }` and the declaration form `await using var x =`)
        // produces no IAwaitOperation node — the await is folded into the using operation as IsAsynchronous — and it is
        // not a loop, so neither InspectAwait nor AG0106 would catch it. Only an asynchronous using is the orchestrator's
        // async-disposal state; a synchronous `using` is ordinary scoped cleanup and is left alone.
        if (!IsAsynchronousUsing(context.Operation))
        {
            return;
        }

        NativeOpsOperationScan.ReportIfOwner(context, Rule, "await using");
    }

    private static bool IsAsynchronousUsing(IOperation operation)
    {
        return operation switch
        {
            IUsingOperation usingOperation => usingOperation.IsAsynchronous,
            IUsingDeclarationOperation usingDeclaration => usingDeclaration.IsAsynchronous,
            _ => false,
        };
    }
}
