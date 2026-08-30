// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// Recognizes a native-interop (P/Invoke) method — one declared with <c>[DllImport]</c> or <c>[LibraryImport]</c>.
/// AG0008 confines the P/Invoke <em>declaration</em> to the per-OS platform libraries; this recognizes a
/// <em>call</em> to such a method, so AG0101 can keep the raw syscall site itself inside those libraries too.
/// </summary>
internal static class PInvoke
{
    /// <summary>The simple name of <c>System.Runtime.InteropServices.DllImportAttribute</c>. The single owner of
    /// this literal; <see cref="InteropOnlyInCrossPlatformLibrariesAnalyzer"/> references it rather than redeclaring
    /// it.</summary>
    internal const string DllImportAttributeName = "DllImportAttribute";

    /// <summary>The simple name of <c>System.Runtime.InteropServices.LibraryImportAttribute</c>. The single owner of
    /// this literal; <see cref="InteropOnlyInCrossPlatformLibrariesAnalyzer"/> references it rather than redeclaring
    /// it.</summary>
    internal const string LibraryImportAttributeName = "LibraryImportAttribute";

    /// <summary>
    /// The (namespace, name) identities of the two native-interop attributes — <c>DllImportAttribute</c> and
    /// <c>LibraryImportAttribute</c>, both in <see cref="KnownNamespaces.SystemRuntimeInteropServices"/>. The single
    /// owner of "which attributes are the interop attributes": <see cref="IsInteropAttribute"/> consumes it against a
    /// resolved <see cref="AttributeData.AttributeClass"/>, and <see cref="InteropOnlyInCrossPlatformLibrariesAnalyzer"/>
    /// consumes the same list against an <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax"/> through
    /// <see cref="AttributeIdentity.IsAnyOf"/>,
    /// so neither spells the set a second time.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> InteropAttributes =
        ImmutableArray.Create(
            (KnownNamespaces.SystemRuntimeInteropServices, DllImportAttributeName),
            (KnownNamespaces.SystemRuntimeInteropServices, LibraryImportAttributeName));

    /// <summary>
    /// Gets a value indicating whether <paramref name="method"/> is a native-interop method — an <c>extern</c>
    /// <c>[DllImport]</c> method (which carries marshalling data) or a <c>[LibraryImport]</c> partial method.
    /// </summary>
    /// <param name="method">The method to test.</param>
    /// <returns><see langword="true"/> when the method is a P/Invoke declaration.</returns>
    internal static bool IsPInvoke(IMethodSymbol method)
    {
        return method.GetDllImportData() is not null || method.GetAttributes().Any(IsInteropAttribute);
    }

    private static bool IsInteropAttribute(AttributeData attribute)
    {
        return WellKnownType.IsAnyOf(attribute.AttributeClass, InteropAttributes);
    }
}
