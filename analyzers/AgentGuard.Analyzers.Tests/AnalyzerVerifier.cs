// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Runs a Roslyn analyzer test against the net9.0 reference assemblies the AgentGuard projects
/// target, so a test source may use the same C# language features as the product code.
/// </summary>
internal static class AnalyzerVerifier
{
    /// <summary>
    /// Verifies that <paramref name="source"/> produces exactly the diagnostics marked in it.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer under test.</typeparam>
    /// <param name="source">Test source, with expected diagnostics in <c>{|ID:span|}</c> markup.</param>
    /// <returns>A task that completes when verification finishes.</returns>
    internal static Task VerifyAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        return test.RunAsync();
    }
}
