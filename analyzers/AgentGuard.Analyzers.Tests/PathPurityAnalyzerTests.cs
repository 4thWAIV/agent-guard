// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class PathPurityAnalyzerTests
{
    [Fact]
    public async Task GetTempPath_IsReported()
    {
        // Impure: GetTempPath reads process/OS state, so it is banned everywhere (default-deny) and routed through
        // IEnvironment.
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Temp() => Path.GetTempPath();
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("GetTempPath", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SingleArgumentGetFullPath_IsReported()
    {
        // The single-argument GetFullPath resolves a relative path against the current directory — impure — so it is
        // banned; only the two-argument overload is pure (AG0012's clause folded in here).
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Full(string path) => Path.GetFullPath(path);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Contains("GetFullPath", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeparatorField_IsReported()
    {
        // The raw separator FIELD is not on the pure allowlist; reading it goes behind IPlatformFileSystem. The shared
        // scanner sees field reads (scanner-sees-field-reads), so a static-readonly field read is caught.
        const string source = """
            using System.IO;

            public class Sample
            {
                public char Separator() => Path.DirectorySeparatorChar;
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Contains("DirectorySeparatorChar", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PureCombine_IsNotReported()
    {
        // Combine is a pure function of its arguments — legal everywhere, no abstraction required.
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Join(string a, string b) => Path.Combine(a, b);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
    }

    [Fact]
    public async Task TwoArgumentGetFullPath_IsNotReported()
    {
        // The two-argument GetFullPath(path, basePath) is pure — it takes the base explicitly rather than reading the
        // current directory — so it stays legal.
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Full(string path, string basePath) => Path.GetFullPath(path, basePath);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
    }
}
