// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Runs an analyzer over a piece of source and returns the diagnostics it produced. Each test asserts on the
/// returned diagnostics itself; the runner's one judgement of its own is that every compilation it builds must
/// produce exactly the compiler errors the caller declared, which defaults to none.
/// <para>
/// An unexpected compiler error does not necessarily stop an analyzer from reporting — a rule that reads syntax, or
/// one whose symbols still bind, can report over code the compiler rejects. What it does mean is that the fixture is
/// not the code the test author believed they were testing, so neither an assertion that a diagnostic appeared nor
/// one that none did can be trusted. A fixture that is invalid ON PURPOSE is legitimate and its analyzer result is
/// meaningful: such a call site declares the exact errors it expects through
/// <c>expectedCompilerErrors</c>, and the runner still collects and returns the analyzer's diagnostics for it.
/// </para>
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
    /// <param name="expectedCompilerErrors">The compiler error identifiers this fixture is expected to produce, one
    /// entry per expected occurrence. Empty means the fixture must compile cleanly.</param>
    /// <returns>The diagnostics the analyzer reported.</returns>
    internal static async Task<ImmutableArray<Diagnostic>> RunAsync<TAnalyzer>(
        string source,
        string assemblyName = "AnalyzerUnderTest",
        params string[] expectedCompilerErrors)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        ImmutableArray<MetadataReference> references = await ResolveReferencesAsync().ConfigureAwait(false);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await RunAnalyzerAsync<TAnalyzer>(compilation, expectedCompilerErrors).ConfigureAwait(false);
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
    /// <param name="expectedCompilerErrors">The compiler error identifiers the source under analysis is expected to
    /// produce, one entry per expected occurrence. Empty means it must compile cleanly. The referenced assembly is
    /// always required to compile cleanly; a fake the test author cannot compile is never a deliberate scenario.</param>
    /// <returns>The diagnostics the analyzer reported for the source under analysis.</returns>
    internal static async Task<ImmutableArray<Diagnostic>> RunWithReferenceAsync<TAnalyzer>(
        string source,
        string assemblyName,
        string referenceSource,
        string referenceAssemblyName,
        params string[] expectedCompilerErrors)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        ImmutableArray<MetadataReference> references = await ResolveReferencesAsync().ConfigureAwait(false);

        CSharpCompilation referenceCompilation = CSharpCompilation.Create(
            assemblyName: referenceAssemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(referenceSource) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ThrowOnUnexpectedCompilerErrors(referenceCompilation, referenceAssemblyName, Array.Empty<string>());

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references.Add(referenceCompilation.ToMetadataReference()),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await RunAnalyzerAsync<TAnalyzer>(compilation, expectedCompilerErrors).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles <paramref name="source"/> against the net9.0 reference assemblies and returns the raw
    /// <see cref="Compilation"/>, so a test can drive an internal helper (for example
    /// <c>BoundaryServices.Resolve</c>) directly rather than only through an analyzer's diagnostics.
    /// </summary>
    /// <param name="source">The C# source to compile.</param>
    /// <param name="assemblyName">The assembly name to compile the source into.</param>
    /// <param name="expectedCompilerErrors">The compiler error identifiers this fixture is expected to produce, one
    /// entry per expected occurrence. Empty means the fixture must compile cleanly.</param>
    /// <returns>The compilation.</returns>
    internal static async Task<Compilation> CompileAsync(
        string source,
        string assemblyName = "AnalyzerUnderTest",
        params string[] expectedCompilerErrors)
    {
        ImmutableArray<MetadataReference> references = await ResolveReferencesAsync().ConfigureAwait(false);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ThrowOnUnexpectedCompilerErrors(compilation, assemblyName, expectedCompilerErrors);
        return compilation;
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
    /// <param name="expectedCompilerErrors">The compiler error identifiers the compilation is expected to produce,
    /// one entry per expected occurrence.</param>
    /// <returns>The diagnostics the analyzer reported.</returns>
    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync<TAnalyzer>(
        CSharpCompilation compilation, string[] expectedCompilerErrors)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        ThrowOnUnexpectedCompilerErrors(
            compilation, compilation.AssemblyName ?? "<unnamed>", expectedCompilerErrors);

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()));

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Throws unless the compiler errors <paramref name="compilation"/> produced are exactly the ones
    /// <paramref name="expectedCompilerErrors"/> declares, compared by identifier and by how many times each appears.
    /// A missing expected error, an undeclared error, and a declared error that appears the wrong number of times all
    /// fail.
    /// <para>
    /// The point is not that a compiler error silences the analyzer — it often does not — but that a fixture which
    /// does not compile the way its author intended is not the code under test, so neither an assertion that a
    /// diagnostic appeared nor one that none did means anything. A fixture that is invalid on purpose declares its
    /// errors here and its analyzer result stays meaningful. The inaccessibility error (CS0122) is the one that bites
    /// silently: a fixture reaching an internal member of a referenced fake without an InternalsVisibleTo grant
    /// reports nothing and reads exactly like a clean accept.
    /// </para>
    /// </summary>
    /// <param name="compilation">The compilation to check.</param>
    /// <param name="assemblyName">The assembly name to name in the failure message.</param>
    /// <param name="expectedCompilerErrors">The expected error identifiers, one entry per expected occurrence.</param>
    private static void ThrowOnUnexpectedCompilerErrors(
        Compilation compilation, string assemblyName, string[] expectedCompilerErrors)
    {
        ImmutableArray<Diagnostic> actualErrors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        Dictionary<string, int> actual = Tally(actualErrors.Select(diagnostic => diagnostic.Id));
        Dictionary<string, int> expected = Tally(expectedCompilerErrors);

        var problems = new List<string>();
        foreach (string id in actual.Keys.Union(expected.Keys, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
        {
            int actualCount = actual.TryGetValue(id, out int a) ? a : 0;
            int expectedCount = expected.TryGetValue(id, out int e) ? e : 0;
            if (actualCount != expectedCount)
            {
                problems.Add(Describe(id, expectedCount, actualCount));
            }
        }

        if (problems.Count == 0)
        {
            return;
        }

        string messages = string.Join(
            System.Environment.NewLine,
            actualErrors.Select(
                diagnostic => "    " + diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture)));

        throw new InvalidOperationException(
            "The test fixture compiled into '" + assemblyName
            + "' did not produce the compiler errors this test declares, so it is not the code under test:"
            + System.Environment.NewLine + string.Join(System.Environment.NewLine, problems.Select(problem => "  " + problem))
            + (messages.Length == 0
                ? System.Environment.NewLine + "  The compilation reported no errors."
                : System.Environment.NewLine + "  Errors reported:" + System.Environment.NewLine + messages));
    }

    /// <summary>
    /// Describes one identifier whose expected and actual occurrence counts differ.
    /// </summary>
    /// <param name="id">The compiler error identifier.</param>
    /// <param name="expectedCount">How many occurrences the test declared.</param>
    /// <param name="actualCount">How many occurrences the compilation produced.</param>
    /// <returns>The one-line description.</returns>
    private static string Describe(string id, int expectedCount, int actualCount)
    {
        if (expectedCount == 0)
        {
            return id + ": not declared, but reported " + actualCount.ToString(CultureInfo.InvariantCulture)
                + " time(s)";
        }

        string declared = id + ": declared " + expectedCount.ToString(CultureInfo.InvariantCulture);

        if (actualCount == 0)
        {
            return declared + " time(s), but not reported";
        }

        return declared + " time(s), but reported " + actualCount.ToString(CultureInfo.InvariantCulture)
            + " time(s)";
    }

    /// <summary>
    /// Counts how many times each identifier appears.
    /// </summary>
    /// <param name="ids">The identifiers to count.</param>
    /// <returns>A map from identifier to occurrence count.</returns>
    private static Dictionary<string, int> Tally(IEnumerable<string> ids)
    {
        var tally = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string id in ids)
        {
            tally[id] = tally.TryGetValue(id, out int count) ? count + 1 : 1;
        }

        return tally;
    }
}
