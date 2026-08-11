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
        ImmutableArray<MetadataReference> references = await ResolveReferencesAsync().ConfigureAwait(false);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await RunAnalyzerAsync<TAnalyzer>(compilation).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles <paramref name="referenceSource"/> into a separate assembly named
    /// <paramref name="referenceAssemblyName"/>, references it from a compilation of <paramref name="source"/> named
    /// <paramref name="assemblyName"/>, runs <typeparamref name="TAnalyzer"/> over the latter, and returns its
    /// diagnostics. This is how the assembly-crossing rules are exercised — a rule that turns on the assembly a
    /// referenced type lives in (the test helpers, the boundaries container) needs that type to come from a real
    /// second assembly, not the compilation under test.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer to run.</typeparam>
    /// <param name="source">The C# source to analyze.</param>
    /// <param name="assemblyName">The assembly name to compile the source under analysis into.</param>
    /// <param name="referenceSource">The C# source of the referenced assembly.</param>
    /// <param name="referenceAssemblyName">The assembly name of the referenced assembly.</param>
    /// <returns>The diagnostics the analyzer reported for the source under analysis.</returns>
    internal static async Task<ImmutableArray<Diagnostic>> RunWithReferenceAsync<TAnalyzer>(
        string source, string assemblyName, string referenceSource, string referenceAssemblyName)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        ImmutableArray<MetadataReference> references = await ResolveReferencesAsync().ConfigureAwait(false);

        CSharpCompilation referenceCompilation = CSharpCompilation.Create(
            assemblyName: referenceAssemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(referenceSource) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references.Add(referenceCompilation.ToMetadataReference()),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await RunAnalyzerAsync<TAnalyzer>(compilation).ConfigureAwait(false);
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

    /// <summary>
    /// Resolves the net9.0 reference assemblies every compilation in this runner is built against.
    /// </summary>
    /// <returns>The resolved metadata references.</returns>
    private static async Task<ImmutableArray<MetadataReference>> ResolveReferencesAsync()
    {
        return await ReferenceAssemblies.Net.Net90.ResolveAsync(LanguageNames.CSharp, CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Runs <typeparamref name="TAnalyzer"/> over <paramref name="compilation"/> and returns exactly the
    /// diagnostics it produced.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer to run.</typeparam>
    /// <param name="compilation">The compilation to analyze.</param>
    /// <returns>The diagnostics the analyzer reported.</returns>
    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync<TAnalyzer>(CSharpCompilation compilation)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()));

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }
}
