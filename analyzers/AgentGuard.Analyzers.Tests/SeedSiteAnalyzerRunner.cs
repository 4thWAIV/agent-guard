// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Wraps a consumer body in a seed-site preamble and runs the seed-site analyzer over it — spelled once for both
/// seed-site rule test classes (<c>NoLiteralFakeRootAnalyzerTests</c> for AG0035 and
/// <c>NoLiteralCaseModeSeedAnalyzerTests</c> for AG0036) instead of the byte-identical wrapper hand-copied into each.
/// The consumer body is embedded in a <c>Consumer</c> class under an <c>App</c> namespace, appended to the caller's
/// <paramref name="preamble"/> (which differs per rule — AG0035 adds the <c>FakeEnvironment</c> stand-in), and compiled
/// into an assembly named <c>AgentGuard.TestHelpers</c> so the stand-in seed types carry the declaring-assembly
/// identity the analyzers pin on (<c>WellKnownType.IsInAssembly</c>), exactly as GuardedConstructionAnalyzerTests does
/// for AG0027.
/// </summary>
internal static class SeedSiteAnalyzerRunner
{
    /// <summary>
    /// Builds the seed-site source from <paramref name="preamble"/> and <paramref name="consumerBody"/> and runs
    /// <typeparamref name="TAnalyzer"/> over it, returning exactly the diagnostics it produced.
    /// </summary>
    /// <typeparam name="TAnalyzer">The seed-site analyzer to run.</typeparam>
    /// <param name="preamble">The rule's stand-in seed types, prepended to the generated consumer.</param>
    /// <param name="consumerBody">The C# member body each test supplies, embedded in the <c>Consumer</c> class.</param>
    /// <returns>The diagnostics the analyzer reported.</returns>
    internal static Task<ImmutableArray<Diagnostic>> RunAsync<TAnalyzer>(string preamble, string consumerBody)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        string source = preamble
            + "\n\nnamespace App\n{\n    using AgentGuard.TestHelpers;\n    public class Consumer\n    {\n        "
            + consumerBody
            + "\n    }\n}\n";
        return AnalyzerRunner.RunAsync<TAnalyzer>(source, "AgentGuard.TestHelpers");
    }
}
