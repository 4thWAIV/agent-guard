// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0105: an [UnmanagedCallersOnly] callback body must be a single try statement whose catch handles every managed
/// exception (a bare catch or catch (Exception)) and returns a native error code, never letting an exception unwind
/// across the native frame.
/// </summary>
public class NativeCallbackBodyMustBeGuardedAnalyzerTests
{
    // The COM-visible completed-handler interface — the shape WinRT invokes back through a COM-callable wrapper (the
    // Windows Hello IAsyncOperationCompletedHandler). A method IMPLEMENTING one of its members is a native callback whose
    // body AG0105 governs exactly as it governs an [UnmanagedCallersOnly] body (the coverage refactor's AG0105 extension).
    private const string ComCompletedHandlerInterface = """
        using System.Runtime.InteropServices;
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        internal interface IAsyncCompletedHandler
        {
            void Invoke(int status);
        }
        """;

    [Fact]
    public async Task UnguardedBody_IsReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    return value + 1;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task ExpressionBody_IsReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value) => value + 1;
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
    }

    [Fact]
    public async Task NarrowCatch_IsReported()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (InvalidOperationException) { return -1; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
    }

    [Fact]
    public async Task SingleTryCatchException_IsNotReported()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (Exception) { return -1; }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task LeakySpecificCatch_AheadOfCompliantCatchAll_IsReported()
    {
        // ROUND 6 FIX A: a leaky specific catch that rethrows AHEAD of a compliant catch-all is a legal
        // more-derived-before-Exception catch order, but the InvalidOperationException it rethrows unwinds across the
        // native frame before the catch-all can turn it into a native error code. The body must be EXACTLY ONE catch
        // that is the compliant catch-all, so this two-catch body is reported even though its second catch is clean.
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (InvalidOperationException) { throw; }
                    catch (Exception) { return -1; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CatchException_ThatRethrows_IsReported()
    {
        // FIX 1: a bare rethrow re-raises the caught exception across the native frame, so a catch-all of System
        // Exception that rethrows does not guard the boundary and is reported.
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (Exception) { throw; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CatchException_ThatThrowsNew_IsReported()
    {
        // FIX 1: throwing a fresh exception still unwinds past the native frame, so a catch-all of System Exception
        // that raises a new exception is reported too.
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (Exception ex) { throw new InvalidOperationException("boom", ex); }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
    }

    [Fact]
    public async Task CompliantCatch_WithThrowingFinally_IsReported()
    {
        // ROUND 5 FIX 2: a compliant catch-all of System.Exception does not save a finally that throws — the finally's
        // throw still unwinds across the native frame, so the callback is reported even though its catch is clean.
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (Exception) { return -1; }
                    finally { throw new InvalidOperationException("boom"); }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CompliantCatch_WithNonThrowingFinally_IsNotReported()
    {
        // ROUND 5 FIX 2 (scope): a finally that does NOT throw leaves no escape path, so a compliant catch-all plus a
        // benign finally stays clean — the fix targets only a throwing finally.
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (Exception) { return -1; }
                    finally { System.GC.KeepAlive(value); }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task BareCatch_IsNotReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch { return -1; }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task BareFilteredCatch_IsReported()
    {
        // ROUND 7 FIX A: a declaration-less FILTERED catch — a bare catch carrying a when-clause, legal C# — is NOT
        // catch-everything: the runtime filter can decline the exception and let it unwind across the native frame. The
        // reordered filter check runs BEFORE the bare-catch shortcut, so this declaration-less catch is reported. Matrix
        // cell: Declaration=none, Filter=yes.
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch when (value > 0) { return -1; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task FilteredCatchException_IsReported()
    {
        // ROUND 7 FIX A: a when-clause on a System.Exception catch can still decline at runtime, so even a typed catch of
        // System.Exception does not catch everything when it carries a filter. Matrix cell: Declaration=Exception,
        // Filter=yes.
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (Exception) when (value > 0) { return -1; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task FilteredNarrowCatch_IsReported()
    {
        // ROUND 7 FIX A: a narrow catch carrying a `when` filter is reported for both reasons — narrower than Exception
        // AND filtered. It pins the last cell of the Declaration x Filter x Type matrix. Matrix cell:
        // Declaration=InvalidOperationException, Filter=yes.
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                [UnmanagedCallersOnly]
                internal static int Reply(int value)
                {
                    try { return value; }
                    catch (InvalidOperationException) when (value > 0) { return -1; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task OrdinaryMethodWithoutAttribute_IsNotReported()
    {
        const string source = """
            internal static class Ordinary
            {
                internal static int Reply(int value) => value + 1;
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task StaticLocalFunctionCallback_WithUnguardedBody_IsReported()
    {
        // ROUND 8 FIX (SOLID): C# permits [UnmanagedCallersOnly] on a static local function too (CS8896), so a callback
        // authored as a LocalFunctionStatementSyntax with an UNGUARDED body must be gated. Under the old method-only
        // registration this would slip through ungated; the added LocalFunctionStatement registration is what fires it.
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                internal static void Register()
                {
                    [UnmanagedCallersOnly]
                    static int Reply(int value)
                    {
                        return value + 1;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task UserDefinedUnmanagedCallersOnlyInDifferentNamespace_IsNotReported()
    {
        // ROUND 9 (attribute-identity): a user-declared attribute ALSO named UnmanagedCallersOnly in a DIFFERENT
        // namespace is not the BCL [UnmanagedCallersOnly]. The rule now matches by RESOLVED type, so this attribute
        // resolves to Custom.UnmanagedCallersOnlyAttribute, is not the interop attribute, and the unguarded body is
        // NOT reported — the false positive the bare syntactic name would have raised is closed.
        const string source = """
            namespace Custom
            {
                using System;

                [AttributeUsage(AttributeTargets.Method)]
                public sealed class UnmanagedCallersOnlyAttribute : Attribute
                {
                }

                internal static class Callbacks
                {
                    [UnmanagedCallersOnly]
                    internal static int Reply(int value)
                    {
                        return value + 1;
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task MalformedUserDefinedUnmanagedCallersOnly_WithRequiredArgOmitted_IsNotReported()
    {
        // ROUND 10 (SOLID): the parallel case that pins the GetTypeInfo branch of AttributeIdentity.ResolveAttributeType.
        // Custom.UnmanagedCallersOnlyAttribute in a DIFFERENT namespace declares a REQUIRED constructor parameter; applied
        // here with that argument OMITTED, GetSymbolInfo fails with OverloadResolutionFailure (no constructor symbol)
        // while GetTypeInfo STILL binds the user's type. The resolver recovers Custom.UnmanagedCallersOnlyAttribute, so
        // IsAnyOf reports it is not the BCL [UnmanagedCallersOnly] and this unguarded body is NOT reported. Without the
        // GetTypeInfo branch it would fall through to the namespace-blind syntactic fallback and gate a non-callback — a
        // false positive — so this fixture keeps a future reader from deleting the branch as dead.
        const string source = """
            namespace Custom
            {
                using System;

                [AttributeUsage(AttributeTargets.Method)]
                public sealed class UnmanagedCallersOnlyAttribute : Attribute
                {
                    public UnmanagedCallersOnlyAttribute(string name)
                    {
                    }
                }

                internal static class Callbacks
                {
                    [UnmanagedCallersOnly]
                    internal static int Reply(int value)
                    {
                        return value + 1;
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task StaticLocalFunctionCallback_WithSingleTryCatchException_IsNotReported()
    {
        // ROUND 8 FIX (SOLID): the local-function path routes through the SAME shared guard core, so a compliant static
        // local function callback whose whole body is a single try statement whose catch handles System.Exception and
        // returns an error code is NOT reported — proving the added path shares the body logic and does not over-report.
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Callbacks
            {
                internal static void Register()
                {
                    [UnmanagedCallersOnly]
                    static int Reply(int value)
                    {
                        try { return value; }
                        catch (Exception) { return -1; }
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task ComCompletedHandler_UnguardedInvoke_IsReported()
    {
        // The Windows-COM completed-handler shape: a method implementing a [ComVisible(true)] interface member is a
        // native callback. An unguarded body lets a managed exception unwind across the native invocation, so it is
        // reported the same as an unguarded [UnmanagedCallersOnly] body.
        string source = ComCompletedHandlerInterface + """

            internal sealed class Handler : IAsyncCompletedHandler
            {
                public void Invoke(int status)
                {
                    System.Console.WriteLine(status);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.Windows"));
        Assert.Equal("AG0105", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task ComCompletedHandler_GuardedInvoke_IsNotReported()
    {
        // A completed handler whose whole body is one try that catches every managed exception — the shape the real
        // AsyncCompletedHandler uses — complies, proving the extension shares the body check and does not over-report
        // (this is why the rule is preventive against the existing Windows handler).
        string source = ComCompletedHandlerInterface + """

            internal sealed class Handler : IAsyncCompletedHandler
            {
                public void Invoke(int status)
                {
                    try { System.Console.WriteLine(status); }
                    catch (System.Exception) { }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.Windows"));
    }

    [Fact]
    public async Task ComInvisibleInterface_UnguardedInvoke_IsNotReported()
    {
        // A [ComVisible(false)] interface is not exposed to native COM, so its implementer is not a native callback — an
        // ordinary managed method, not this rule's concern.
        string source = """
            using System.Runtime.InteropServices;
            [ComVisible(false)]
            internal interface IPlainHandler
            {
                void Invoke(int status);
            }

            internal sealed class Handler : IPlainHandler
            {
                public void Invoke(int status)
                {
                    System.Console.WriteLine(status);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.Windows"));
    }

    [Fact]
    public async Task PlainInterface_UnguardedImplementation_IsNotReported()
    {
        // A method implementing an ordinary (non-COM) interface is not a native callback — no ComVisible attribute, so
        // native code never calls it back through a wrapper.
        string source = """
            internal interface IHandler
            {
                void Invoke(int status);
            }

            internal sealed class Handler : IHandler
            {
                public void Invoke(int status)
                {
                    System.Console.WriteLine(status);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackBodyMustBeGuardedAnalyzer>(source, "AgentGuard.CrossPlatform.Windows"));
    }
}
