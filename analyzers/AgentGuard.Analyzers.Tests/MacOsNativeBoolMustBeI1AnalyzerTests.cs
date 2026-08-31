// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0103: in the macOS implementation library, every bool return and parameter of a P/Invoke must be marshalled as
/// UnmanagedType.I1 (the one-byte Darwin bool). Scoped to the macOS assembly; the Windows 4-byte BOOL is correct there.
/// </summary>
public class MacOsNativeBoolMustBeI1AnalyzerTests
{
    private const string BoolReturnSource = """
        using System.Runtime.InteropServices;
        internal static class Native
        {
            [DllImport("libSystem")]
            internal static extern bool Evaluate();
        }
        """;

    [Fact]
    public async Task BoolReturnWithoutMarshalAs_InMacOs_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<MacOsNativeBoolMustBeI1Analyzer>(BoolReturnSource, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0103", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task BoolParameterWithoutMarshalAs_InMacOs_IsReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Native
            {
                [DllImport("libSystem")]
                internal static extern int Evaluate(bool interactive);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<MacOsNativeBoolMustBeI1Analyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0103", diagnostic.Id);
    }

    [Fact]
    public async Task BoolReturnMarshalledAsI1_InMacOs_IsNotReported()
    {
        const string source = """
            using System.Runtime.InteropServices;
            internal static class Native
            {
                [DllImport("libSystem")]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool Evaluate();
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<MacOsNativeBoolMustBeI1Analyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task BoolReturnWithoutMarshalAs_InWindows_IsNotReported()
    {
        // Scoped to the macOS assembly: the Windows 4-byte UnmanagedType.Bool is correct, so this rule does not fire
        // in the Windows library.
        Assert.Empty(await AnalyzerRunner.RunAsync<MacOsNativeBoolMustBeI1Analyzer>(BoolReturnSource, "AgentGuard.CrossPlatform.Windows"));
    }
}
