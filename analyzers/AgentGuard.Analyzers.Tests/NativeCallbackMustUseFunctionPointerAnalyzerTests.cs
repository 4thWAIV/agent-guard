// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0104: a native callback must be a static [UnmanagedCallersOnly] method reached by &amp;Method, never a marshalled
/// delegate pointer (Marshal.GetFunctionPointerForDelegate / GetDelegateForFunctionPointer).
/// </summary>
public class NativeCallbackMustUseFunctionPointerAnalyzerTests
{
    [Fact]
    public async Task GetFunctionPointerForDelegate_IsReported()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Binding
            {
                internal static IntPtr Bind(Action callback) => Marshal.GetFunctionPointerForDelegate(callback);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackMustUseFunctionPointerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0104", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task GetDelegateForFunctionPointer_IsReported()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Binding
            {
                internal static Action Bind(IntPtr pointer) => Marshal.GetDelegateForFunctionPointer<Action>(pointer);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativeCallbackMustUseFunctionPointerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0104", diagnostic.Id);
    }

    [Fact]
    public async Task OtherMarshalMember_IsNotReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Binding
            {
                internal static int LastError() => Marshal.GetLastPInvokeError();
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativeCallbackMustUseFunctionPointerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }
}
