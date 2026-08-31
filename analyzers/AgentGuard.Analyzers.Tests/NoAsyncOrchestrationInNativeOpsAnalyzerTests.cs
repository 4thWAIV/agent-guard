// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0115 (no-async-orchestration-in-native-ops): a class implementing a per-OS native-ops interface (IObjCRuntime and
/// the two Windows native-ops interfaces) must expose synchronous raw primitives — no await and no
/// TaskCompletionSource. The async flow (the await of the native reply, the TaskCompletionSource / GCHandle wiring)
/// belongs in the fake-testable orchestrator. The native-ops owner interface stub is the shared owner
/// SharedAnalyzerSources.ObjCRuntimeNativeOps; the implementer sits in a second MacOS namespace block. Preventive against
/// production; its RED is proven here by violating fixtures.
/// </summary>
public class NoAsyncOrchestrationInNativeOpsAnalyzerTests
{
    private const string NativeOps = SharedAnalyzerSources.ObjCRuntimeNativeOps;

    [Fact]
    public async Task AwaitInNativeOps_IsReported()
    {
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal async Task<int> Wait(Task<int> work) => await work;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task AwaitUsingInNativeOps_IsReported()
    {
        // `await using` produces no IAwaitOperation node — the await folds into the using operation as IsAsynchronous —
        // and it is not a loop, so the bare await check (and AG0106) would miss it. The asynchronous-using check catches
        // it. This is the SOLID-2 gap the fix closes.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal async Task Use(IAsyncDisposable resource)
                    {
                        await using (resource) { }
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task AwaitUsingDeclarationInNativeOps_IsReported()
    {
        // The declaration form `await using var x = ...;` is IUsingDeclarationOperation (not IUsingOperation); it too is
        // asynchronous disposal state and is caught.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal async Task Use(IAsyncDisposable resource)
                    {
                        await using var scope = resource;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
    }

    [Fact]
    public async Task AwaitForEachInNativeOps_IsReported()
    {
        // `await foreach` produces an IForEachLoopOperation with IsAsynchronous set — no IAwaitOperation node — so the
        // bare await check misses it, and AG0106's loop ban catches it only incidentally. The asynchronous-foreach
        // check catches it independently. This is the await-foreach gap the fix closes.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Collections.Generic;
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal async Task Consume(IAsyncEnumerable<int> items)
                    {
                        await foreach (int item in items) { }
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task SynchronousForEachInNativeOps_IsNotReported()
    {
        // A plain synchronous `foreach` is ordinary control flow — AG0106's concern, not this rule's. It is NOT async
        // orchestration, so AG0115 leaves it alone; this pins the IsAsynchronous filter on the foreach check (only an
        // asynchronous foreach is caught here).
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Sum(int[] values)
                    {
                        int total = 0;
                        foreach (int value in values) { total += value; }
                        return total;
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task SynchronousUsingInNativeOps_IsNotReported()
    {
        // A synchronous `using` is ordinary scoped cleanup, not async orchestration, so it is left alone — only an
        // asynchronous using is the orchestrator's async-disposal state.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal void Use(IDisposable resource)
                    {
                        using (resource) { }
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task TaskCompletionSourceConstructionInNativeOps_IsReported()
    {
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal void Make()
                    {
                        _ = new TaskCompletionSource<int>();
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
    }

    [Fact]
    public async Task TaskCompletionSourceMemberUseInNativeOps_IsReported()
    {
        // Any TaskCompletionSource member — reading .Task, calling TrySetResult — is the task-completion state the
        // orchestrator owns, so it is caught wherever the source came from.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal void Complete(TaskCompletionSource<int> tcs) => tcs.TrySetResult(1);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
    }

    [Fact]
    public async Task TaskRunInNativeOps_IsReported()
    {
        // Task.Run offloads work to the thread pool and hands back a Task — the async orchestration the flow port owns,
        // not a synchronous raw primitive. It is caught by the return-type CATEGORY (its result is Task<int>), not a
        // Task.Run name match, so the whole Task-factory long tail is covered without enumerating method names.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal Task<int> Offload() => Task.Run(() => 1);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
    }

    [Fact]
    public async Task TaskFactoryStartNewInNativeOps_IsReported()
    {
        // Task.Factory.StartNew is a different method name from Task.Run but the same async orchestration — its result is
        // a Task<int>, so the return-type category catches it with no per-name special-casing (the SOLID fix).
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal Task<int> Offload() => Task.Factory.StartNew(() => 1);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
    }

    [Fact]
    public async Task TaskContinueWithInNativeOps_IsReported()
    {
        // Task.ContinueWith chains a continuation and returns a Task — async orchestration the flow port owns. It is
        // caught by the return-type category (result Task), no ContinueWith name match needed.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal Task Chain(Task work) => work.ContinueWith(_ => { });
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
    }

    [Fact]
    public async Task TaskDelayInNativeOps_IsReported()
    {
        // Task.Delay is representative of the long tail — not a thread-pool offload, but its result is still a Task, so
        // the return-type category flags it. An enumeration of Run/StartNew/ContinueWith would have missed this.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal Task Wait() => Task.Delay(1);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
    }

    [Fact]
    public async Task ValueTaskReturningCallInNativeOps_IsReported()
    {
        // The category spans ValueTask/ValueTask<T> as well as Task/Task<T>: a call whose result is a ValueTask<int> is
        // the same spawned async work, so it is flagged even though no Task type appears.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal ValueTask<int> Fetch() => Load();
                    private static ValueTask<int> Load() => new ValueTask<int>(1);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0115", diagnostic.Id);
    }

    [Fact]
    public async Task SynchronousNativeOps_IsNotReported()
    {
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal IntPtr Call(IntPtr self) => self;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task SynchronousRawCallShapesInNativeOps_AreNotReported()
    {
        // The real raw-call shape: synchronous primitives returning IntPtr / bool / void, and a plain call to one of
        // them (Probe -> Ready, a bool-returning call). None materializes a Task or ValueTask, so the return-type
        // category leaves them all alone — proving the fix flags the CATEGORY, not every call.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal IntPtr Send(IntPtr self) => self;
                    internal bool Ready() => true;
                    internal void Release(IntPtr handle) { }
                    internal bool Probe() => Ready();
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task AsyncOutsideNativeOps_IsNotReported()
    {
        // A class that does NOT implement a native-ops interface is not this rule's concern — the orchestrator IS async.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Threading.Tasks;
                internal sealed class Orchestrator
                {
                    internal async Task<int> Wait(Task<int> work) => await work;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoAsyncOrchestrationInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }
}
