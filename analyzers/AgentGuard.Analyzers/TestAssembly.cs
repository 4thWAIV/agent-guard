// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// Identifies the test assemblies — those whose name ends in <c>.Tests</c> — and the one test-only assembly whose
/// types they alone may use, <c>AgentGuard.TestHelpers</c>. The shipped guard must never be built against the fake
/// container the test helpers construct, so a reference to a <c>AgentGuard.TestHelpers</c> type from any assembly
/// that is not a test assembly is a build error (AG0018). This is the assembly-name gate for that rule, the same
/// shape as <see cref="CrossPlatformBoundary.IsCrossPlatformLibrary"/>.
/// </summary>
internal static class TestAssembly
{
    /// <summary>
    /// The assembly name of the test-only helpers assembly, referenced only by test projects and never shipped.
    /// The single owner of this value.
    /// </summary>
    internal const string TestHelpersName = "AgentGuard.TestHelpers";

    private const string TestAssemblySuffix = ".Tests";

    /// <summary>
    /// Gets a value indicating whether <paramref name="compilation"/> is a test assembly — one whose name ends in
    /// <c>.Tests</c> — which is the only kind of assembly permitted to reference <c>AgentGuard.TestHelpers</c>.
    /// </summary>
    /// <param name="compilation">The compilation under analysis.</param>
    /// <returns><see langword="true"/> when the assembly name ends in <c>.Tests</c>; otherwise
    /// <see langword="false"/>.</returns>
    internal static bool IsTestAssembly(Compilation compilation)
    {
        string? assemblyName = compilation.AssemblyName;
        return assemblyName is not null && assemblyName.EndsWith(TestAssemblySuffix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="symbol"/> is declared in the <c>AgentGuard.TestHelpers</c>
    /// assembly — the test-only helpers whose types shipping code may not reference.
    /// </summary>
    /// <param name="symbol">The referenced symbol to test.</param>
    /// <returns><see langword="true"/> when the symbol comes from <c>AgentGuard.TestHelpers</c>; otherwise
    /// <see langword="false"/>.</returns>
    internal static bool IsFromTestHelpers(ISymbol symbol)
    {
        return symbol.ContainingAssembly is not null
            && string.Equals(symbol.ContainingAssembly.Name, TestHelpersName, StringComparison.Ordinal);
    }
}
