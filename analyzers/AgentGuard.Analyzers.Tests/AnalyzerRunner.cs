// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Runs an analyzer over a piece of source and returns the diagnostics it produced. It makes no
/// pass-or-fail decision of its own; each test asserts on the returned diagnostics itself.
/// </summary>
internal static class AnalyzerRunner
{
    /// <summary>
    /// Compiles <paramref name="source"/> against the net9.0 reference assemblies, runs
    /// <typeparamref name="TAnalyzer"/> over it, and returns exactly the diagnostics the analyzer produced. The
    /// assembly name is configurable because some rules (native interop, OS branching) turn on the assembly the
    /// code is compiled into.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer to run.</typeparam>
    /// <param name="source">The C# source to analyze.</param>
    /// <param name="assemblyName">The assembly name to compile the source into.</param>
    /// <returns>The diagnostics the analyzer reported.</returns>
    internal static async Task<ImmutableArray<Diagnostic>> RunAsync<TAnalyzer>(string source, string assemblyName = "AnalyzerUnderTest")
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        ImmutableArray<MetadataReference> references =
            await ReferenceAssemblies.Net.Net90.ResolveAsync(LanguageNames.CSharp, CancellationToken.None)
                .ConfigureAwait(false);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()));

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the exact source text a diagnostic points at, so a test can assert its location.
    /// </summary>
    /// <param name="source">The source that was analyzed.</param>
    /// <param name="diagnostic">The diagnostic to locate.</param>
    /// <returns>The source substring the diagnostic spans.</returns>
    internal static string SpanText(string source, Diagnostic diagnostic)
    {
        var span = diagnostic.Location.SourceSpan;
        return source.Substring(span.Start, span.Length);
    }
}
