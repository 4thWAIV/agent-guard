// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0102: a P/Invoke signature must not use object, dynamic, an unconstrained generic type parameter, or a variadic
/// (__arglist) parameter list — each leaves the marshaller without a fixed native layout.
/// </summary>
public class PInvokeSignatureMustBeBlittableAnalyzerTests
{
    [Fact]
    public async Task ObjectParameter_IsReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Native
            {
                [DllImport("libc")]
                internal static extern int Send(object payload);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PInvokeSignatureMustBeBlittableAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0102", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task ObjectReturn_IsReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Native
            {
                [DllImport("libc")]
                internal static extern object Get();
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PInvokeSignatureMustBeBlittableAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0102", diagnostic.Id);
    }

    [Fact]
    public async Task VariadicParameterList_IsReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Native
            {
                [DllImport("libc")]
                internal static extern int Printf(string format, __arglist);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PInvokeSignatureMustBeBlittableAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0102", diagnostic.Id);
    }

    [Fact]
    public async Task BlittableSignature_IsNotReported()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            internal static class Native
            {
                [DllImport("libc")]
                internal static extern long PathConf(IntPtr path, int name);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PInvokeSignatureMustBeBlittableAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task NonPInvokeMethodWithObjectParameter_IsNotReported()
    {
        const string source = """
            internal static class Ordinary
            {
                internal static int Send(object payload) => 0;
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PInvokeSignatureMustBeBlittableAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }
}
