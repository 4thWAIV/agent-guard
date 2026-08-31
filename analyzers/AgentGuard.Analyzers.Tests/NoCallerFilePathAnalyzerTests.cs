// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0039 (no-caller-file-path): the <c>System.Runtime.CompilerServices.CallerFilePathAttribute</c> applied to a
/// parameter is a build error, repo-wide (production and test), because it captures a compile-time source path that a
/// deterministic CI build rewrites to <c>/_/...</c>; the base directory is reached through
/// <c>IEnvironment.GetBaseDirectory()</c> instead. Scoped to exactly <c>CallerFilePathAttribute</c> — its
/// <c>CallerMemberName</c>/<c>CallerLineNumber</c>/<c>CallerArgumentExpression</c> siblings leak no path and never
/// fire — and matched by resolved type, so a same-named attribute in another namespace is left alone.
/// </summary>
public class NoCallerFilePathAnalyzerTests
{
    [Fact]
    public async Task CallerFilePathOnParameter_IsReported()
    {
        const string source = """
            using System.Runtime.CompilerServices;

            namespace App
            {
                public static class C
                {
                    public static string Where([CallerFilePath] string here = "") => here;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoCallerFilePathAnalyzer>(source));

        Assert.Equal("AG0039", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CallerFilePathFullyQualified_IsReported()
    {
        // The qualified spelling resolves to the same BCL type, so the ban does not depend on a using directive.
        const string source = """
            namespace App
            {
                public static class C
                {
                    public static string Where(
                        [System.Runtime.CompilerServices.CallerFilePath] string here = "") => here;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoCallerFilePathAnalyzer>(source));

        Assert.Equal("AG0039", diagnostic.Id);
    }

    [Fact]
    public async Task CallerFilePathInProductionAssembly_IsReported()
    {
        // The ban is repo-wide with no owner exemption: it fires the same in a production (non-test) assembly.
        const string source = """
            using System.Runtime.CompilerServices;

            namespace App
            {
                public static class C
                {
                    public static string Where([CallerFilePath] string here = "") => here;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoCallerFilePathAnalyzer>(source, "AgentGuard.Engine"));

        Assert.Equal("AG0039", diagnostic.Id);
    }

    [Fact]
    public async Task CallerInfoSiblings_AreNotReported()
    {
        // CallerMemberName / CallerLineNumber / CallerArgumentExpression leak no path; the rule is scoped to
        // CallerFilePathAttribute alone, so none of the siblings fire.
        const string source = """
            using System.Runtime.CompilerServices;

            namespace App
            {
                public static class C
                {
                    public static string Log(
                        object value,
                        [CallerMemberName] string member = "",
                        [CallerLineNumber] int line = 0,
                        [CallerArgumentExpression("value")] string expression = "") => member;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoCallerFilePathAnalyzer>(source));
    }

    [Fact]
    public async Task SameNamedAttributeInAnotherNamespace_IsNotReported()
    {
        // A user-declared CallerFilePath attribute in App (not System.Runtime.CompilerServices) resolves to the user's
        // type; matched by resolved type, the rule leaves it alone — no false positive on a same-named attribute.
        const string source = """
            namespace App
            {
                [System.AttributeUsage(System.AttributeTargets.Parameter)]
                public sealed class CallerFilePathAttribute : System.Attribute
                {
                }

                public static class C
                {
                    public static string Where([CallerFilePath] string here = "") => here;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoCallerFilePathAnalyzer>(source));
    }

    [Fact]
    public async Task ParameterWithoutCallerInfo_IsNotReported()
    {
        const string source = """
            namespace App
            {
                public static class C
                {
                    public static string Where(string here) => here;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoCallerFilePathAnalyzer>(source));
    }
}
