// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single source of truth for how the <c>System.IO</c> filesystem surface is partitioned between the three
/// rules that share it, so no member falls through every rule (allowed everywhere) or trips two at once. The
/// OS-uniform filesystem rule (AG0011) owns the family below except the members the other two claim; the
/// environment rule (AG0012) claims the current-directory members; and the OS-divergent rule (AG0101) claims the
/// symlink and Unix-mode members. Constructing a <c>FileInfo</c>/<c>DirectoryInfo</c>/<c>FileSystemInfo</c> is
/// inert — it opens no handle and reads nothing until a member is touched — so it is not a boundary call for
/// either rule; the OS access is the member that is read, and that member is what each rule catches. (This is
/// what lets the same construction stand both in the Boundaries directory-enumerator adapter and in the
/// CrossPlatform link-target reader.) Both AG0011 and AG0101 read these sets, so the boundary between them is
/// defined once.
/// </summary>
internal static class FilesystemMembers
{
    /// <summary>
    /// The <c>System.IO</c> types that carry both OS-uniform members (which route to AG0011) and OS-divergent
    /// members (which route to AG0101): <c>File</c>, <c>Directory</c>, <c>FileInfo</c>, <c>DirectoryInfo</c>, and
    /// their <c>FileSystemInfo</c> base (where <c>LinkTarget</c> and <c>UnixFileMode</c> are actually declared).
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> Family = ImmutableArray.Create(
        (KnownNamespaces.SystemIO, "File"),
        (KnownNamespaces.SystemIO, "Directory"),
        (KnownNamespaces.SystemIO, "FileInfo"),
        (KnownNamespaces.SystemIO, "DirectoryInfo"),
        (KnownNamespaces.SystemIO, "FileSystemInfo"));

    /// <summary>
    /// The <c>*Info</c> types whose <em>construction</em> is inert — it opens no handle and performs no OS access
    /// until a member is touched — so the bare <c>new</c> is flagged by neither AG0011 nor AG0101. The member that
    /// is later read carries the boundary meaning and is what the rules catch.
    /// </summary>
    private static readonly ImmutableArray<(string Namespace, string Name)> InfoTypes = ImmutableArray.Create(
        (KnownNamespaces.SystemIO, "FileInfo"),
        (KnownNamespaces.SystemIO, "DirectoryInfo"),
        (KnownNamespaces.SystemIO, "FileSystemInfo"));

    /// <summary>
    /// The OS-divergent member names on the filesystem family that route to <c>IPlatformFileSystem</c> (AG0101),
    /// not to the OS-uniform filesystem interfaces (AG0011): the symlink members and the Unix-mode members.
    /// </summary>
    private static readonly ImmutableHashSet<string> OsDivergentMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "LinkTarget",
        "UnixFileMode",
        "CreateSymbolicLink",
        "ResolveLinkTarget",
        "SetUnixFileMode",
        "GetUnixFileMode");

    /// <summary>
    /// The environment member names on <c>Directory</c> that route to <c>IEnvironment</c> (AG0012), not to the
    /// filesystem interfaces (AG0011): the current-directory getter and setter.
    /// </summary>
    private static readonly ImmutableHashSet<string> CurrentDirectoryMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "GetCurrentDirectory",
        "SetCurrentDirectory");

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/> is one of the OS-divergent symlink or Unix-mode
    /// members that AG0101 owns.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <returns><see langword="true"/> when the member name is an OS-divergent member name.</returns>
    internal static bool IsOsDivergentMember(ISymbol member) => OsDivergentMemberNames.Contains(member.Name);

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/> is a current-directory member on
    /// <c>Directory</c> that AG0012 owns.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <returns><see langword="true"/> when the member name is a current-directory member name.</returns>
    internal static bool IsCurrentDirectoryMember(ISymbol member) => CurrentDirectoryMemberNames.Contains(member.Name);

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/> is a constructor of one of the <c>*Info</c> types
    /// whose construction is inert, so neither filesystem rule flags the bare <c>new</c>.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <param name="declaringType">The type that declares the member.</param>
    /// <returns><see langword="true"/> when the member is an inert <c>*Info</c> constructor.</returns>
    internal static bool IsInertInfoConstruction(ISymbol member, INamedTypeSymbol declaringType)
    {
        return member is IMethodSymbol { MethodKind: MethodKind.Constructor }
            && WellKnownType.IsAnyOf(declaringType, InfoTypes);
    }
}
