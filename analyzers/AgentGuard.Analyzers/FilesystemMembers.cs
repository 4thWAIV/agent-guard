// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single source of truth for how the <c>System.IO</c> <c>File</c>/<c>Directory</c> member surface is
/// partitioned between the rules that share it, so no member falls through every rule (allowed everywhere) or trips
/// two at once. The consolidated owner rule (AG0011) owns the split below through
/// <see cref="OwningInterfaceFor"/> except the members another rule claims; that rule also routes the
/// current-directory members to the environment owner and the OS-divergent static members to AG0101 by reading
/// <see cref="IsCurrentDirectoryMember"/> and <see cref="IsOsDivergentMember"/> here. The OS-divergent rule (AG0101)
/// reads the same OS-divergent set and fires on it. The <c>FileInfo</c>/<c>DirectoryInfo</c>/<c>FileSystemInfo</c>
/// wrappers are owned wholesale in <see cref="OwnedPrimitives"/> (fileinfo-abstraction-stays-in-ag0011), so their
/// construction and instance members are no longer partitioned here.
/// </summary>
internal static class FilesystemMembers
{
    /// <summary>
    /// The two OS-uniform static filesystem types whose members are split per owner — <c>File</c> and
    /// <c>Directory</c>. AG0101 reads this set to scope its OS-divergent static-member check to these two types; the
    /// <c>*Info</c> types are owned wholesale in <see cref="OwnedPrimitives"/>, not here.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> FileAndDirectory = ImmutableArray.Create(
        (KnownNamespaces.SystemIO, "File"),
        (KnownNamespaces.SystemIO, "Directory"));

    /// <summary>
    /// The stream, watcher, and drive <c>System.IO</c> types — <c>DriveInfo</c>, <c>FileStream</c>,
    /// <c>StreamReader</c>, <c>StreamWriter</c>, <c>FileSystemWatcher</c>. They have zero usage in the tree today and
    /// no member on any of the four filesystem interfaces, so they get NO owning interface: they are banned
    /// everywhere (the same "nowhere — grow an interface first" pattern as AG0013's forbidden <c>Process</c>), rather
    /// than lumped under an owner they do not belong to. Named once here so AG0011's filesystem-type gate still
    /// detects them and the member→owner partition below can return no owner for them.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> StreamWatcherAndDriveTypes = ImmutableArray.Create(
        (KnownNamespaces.SystemIO, "DriveInfo"),
        (KnownNamespaces.SystemIO, "FileStream"),
        (KnownNamespaces.SystemIO, "StreamReader"),
        (KnownNamespaces.SystemIO, "StreamWriter"),
        (KnownNamespaces.SystemIO, "FileSystemWatcher"));

    // The four OS-uniform filesystem interfaces in AgentGuard.Abstractions.Contracts, each the single owner of the members
    // mapped to it below (one-owner-class-per-primitive). Held as one-element sets so AG0011 can pass the resolved
    // owner straight to OwnerClass.IsOwner without allocating a new array per analyzed operation.
    private static readonly ImmutableArray<(string Namespace, string Name)> FileReaderOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IFileReader"));

    private static readonly ImmutableArray<(string Namespace, string Name)> DirectoryEnumeratorOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IDirectoryEnumerator"));

    private static readonly ImmutableArray<(string Namespace, string Name)> FileWriterOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IFileWriter"));

    private static readonly ImmutableArray<(string Namespace, string Name)> DirectoryWriterOwner =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IDirectoryWriter"));

    // No owning interface: the raw call is banned everywhere and stays a build error until an interface member is
    // deliberately grown for it. Reused wherever one side of a shared name (or a whole shared name) has no interface
    // member to route to, so the empty set is not re-spelled per entry.
    private static readonly ImmutableArray<(string Namespace, string Name)> NoOwner =
        ImmutableArray<(string Namespace, string Name)>.Empty;

    // EVERY static member NAME that System.IO.File and System.IO.Directory BOTH declare, mapped — in ONE place — to the
    // single owner of each side, so the declaring type (File/FileInfo vs Directory/DirectoryInfo) resolves the owner
    // and no shared name is left in a flat name set to pick up the wrong one. Each side maps to its
    // IFileReader/IFileWriter (File) or IDirectoryEnumerator/IDirectoryWriter (Directory) member when one exists, else
    // NoOwner (banned everywhere on that side). The OS-divergent shared names (CreateSymbolicLink, ResolveLinkTarget)
    // and the current-directory shared names (Get/SetCurrentDirectory) are NOT here: they are carved out to AG0101 and
    // AG0012 respectively before OwningInterfaceFor is ever called. The timestamp getters/setters that have no
    // interface member on either side are mapped to (NoOwner, NoOwner): they stay RED if ever called, and — because
    // none are called in the live tree — compile clean today; if one is ever called it must be raised as an
    // unmapped-but-used complete-the-set question, never given a near-miss owner here.
    private static readonly ImmutableDictionary<string, SharedNameOwner> SharedNameOwners =
        new Dictionary<string, SharedNameOwner>(StringComparer.Ordinal)
        {
            // File.Exists -> IFileReader ; Directory.Exists -> IDirectoryEnumerator.
            ["Exists"] = new(FileReaderOwner, DirectoryEnumeratorOwner),

            // File.Delete -> IFileWriter.DeleteFile ; Directory.Delete -> IDirectoryWriter.DeleteDirectory.
            ["Delete"] = new(FileWriterOwner, DirectoryWriterOwner),

            // File.Move -> IFileWriter.Move ; Directory.Move -> NoOwner (IDirectoryWriter has no move/rename member),
            // so a directory-side Move is banned everywhere until IDirectoryWriter is deliberately grown and the
            // contract amended (complete-the-set-not-a-test-fake) — never mapped to a DirectoryWriter it cannot back.
            ["Move"] = new(FileWriterOwner, NoOwner),

            // File.GetLastWriteTimeUtc -> IFileReader.GetLastWriteTimeUtc (the contract completes the set on
            // IFileReader) ; Directory.GetLastWriteTimeUtc -> IDirectoryEnumerator.GetLastWriteTimeUtc.
            ["GetLastWriteTimeUtc"] = new(FileReaderOwner, DirectoryEnumeratorOwner),

            // File.SetLastWriteTimeUtc -> NoOwner (IFileWriter has no timestamp setter and File.SetLastWriteTimeUtc is
            // unused) ; Directory.SetLastWriteTimeUtc -> IDirectoryWriter.SetLastWriteTimeUtc.
            ["SetLastWriteTimeUtc"] = new(NoOwner, DirectoryWriterOwner),

            // The remaining timestamp getters/setters — creation and access on both sides, plus the non-Utc last-write
            // forms — have no interface member on either File or Directory, so both sides are NoOwner (banned
            // everywhere). None are called in the live tree; if one ever is, it is an unmapped-but-used complete-the-set
            // question for the human, not something to give a near-miss owner.
            ["GetCreationTime"] = new(NoOwner, NoOwner),
            ["GetCreationTimeUtc"] = new(NoOwner, NoOwner),
            ["GetLastAccessTime"] = new(NoOwner, NoOwner),
            ["GetLastAccessTimeUtc"] = new(NoOwner, NoOwner),
            ["GetLastWriteTime"] = new(NoOwner, NoOwner),
            ["SetCreationTime"] = new(NoOwner, NoOwner),
            ["SetCreationTimeUtc"] = new(NoOwner, NoOwner),
            ["SetLastAccessTime"] = new(NoOwner, NoOwner),
            ["SetLastAccessTimeUtc"] = new(NoOwner, NoOwner),
            ["SetLastWriteTime"] = new(NoOwner, NoOwner),
        }.ToImmutableDictionary(StringComparer.Ordinal);

    // The unambiguous member names, each belonging to exactly one interface regardless of the declaring type — every
    // name here is declared on exactly ONE of File/FileInfo or Directory/DirectoryInfo, never both. EVERY name shared
    // between System.IO.File and System.IO.Directory (Exists, Delete, Move, and the whole timestamp getter/setter
    // family GetCreationTime[Utc]/GetLastAccessTime[Utc]/GetLastWriteTime[Utc]/Set…) is deliberately kept OUT of these
    // flat sets and disambiguated by the declaring type in OwningInterfaceFor (SharedNameOwners), so no shared name can
    // silently pick up the wrong owner by falling into a flat set. File.GetAttributes is an IFileReader member (File
    // only — Directory has no GetAttributes, so it is unambiguous and stays here): the contract completed the set by
    // adding FileAttributes GetAttributes(string) to IFileReader for the Windows symlink-kind read
    // (complete-the-set-not-a-test-fake), so it is exempt in FileReader and RED everywhere else. Reading a
    // FileAttributes enum value (e.g. `& FileAttributes.Directory`) stays legal — an enum value read as data
    // (legal-edges), which MemberUseScanner never surfaces here.
    private static readonly ImmutableHashSet<string> FileReaderMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ReadAllBytes",
        "ReadAllBytesAsync",
        "ReadAllText",
        "ReadAllTextAsync",
        "OpenRead",
        "GetAttributes");

    private static readonly ImmutableHashSet<string> DirectoryEnumeratorMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "EnumerateFiles",
        "EnumerateDirectories",
        "EnumerateFileSystemInfos",
        "GetFileSystemInfos");

    private static readonly ImmutableHashSet<string> FileWriterMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "WriteAllBytes",
        "WriteAllBytesAsync",
        "WriteAllText",
        "Copy",
        "OpenWrite");

    private static readonly ImmutableHashSet<string> DirectoryWriterMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "CreateDirectory");

    /// <summary>
    /// The OS-divergent STATIC member names on <c>File</c>/<c>Directory</c> that route to <c>IPlatformFileSystem</c>
    /// (AG0101), not to the OS-uniform filesystem interfaces (AG0011): the static symlink members and the static
    /// Unix-mode members. The instance <c>LinkTarget</c> and <c>UnixFileMode</c> members are declared on the
    /// <c>*Info</c> types, which are owned wholesale in <see cref="OwnedPrimitives"/>
    /// (fileinfo-abstraction-stays-in-ag0011), so they are NOT in this static set.
    /// </summary>
    private static readonly ImmutableHashSet<string> OsDivergentMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
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
    /// Gets a value indicating whether <paramref name="member"/> is one of the OS-divergent STATIC symlink or
    /// Unix-mode members on <c>File</c>/<c>Directory</c> that AG0101 owns.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <returns><see langword="true"/> when the member name is an OS-divergent static member name.</returns>
    internal static bool IsOsDivergentMember(ISymbol member) => OsDivergentMemberNames.Contains(member.Name);

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/> is a current-directory member on
    /// <c>Directory</c> that the environment owner (<c>IEnvironment</c>) owns.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <returns><see langword="true"/> when the member name is a current-directory member name.</returns>
    internal static bool IsCurrentDirectoryMember(ISymbol member) => CurrentDirectoryMemberNames.Contains(member.Name);

    /// <summary>
    /// Resolves the single owning interface for a banned OS-uniform filesystem member — the one interface whose
    /// implementing class may make this raw call (one-owner-class-per-primitive). A member maps to exactly one of
    /// <c>IFileReader</c>, <c>IDirectoryEnumerator</c>, <c>IFileWriter</c>, or <c>IDirectoryWriter</c>, so a class
    /// implementing a different one of the four is not exempt for it (a class implementing only <c>IFileWriter</c>
    /// is still RED on <c>File.Exists</c>, which is <c>IFileReader</c>'s). Every member NAME shared between
    /// <c>File</c> and <c>Directory</c> is disambiguated by the declaring type through the one
    /// <see cref="SharedNameOwners"/> table before any flat set is consulted, so a shared name never picks up the
    /// wrong owner. The result is returned as a one-element set AG0011 hands straight to
    /// <see cref="OwnerClass.IsOwner"/>.
    /// </summary>
    /// <param name="member">The referenced filesystem member.</param>
    /// <param name="type">The type that declares the member.</param>
    /// <returns>The one owning interface as a single-element set, or <see cref="ImmutableArray{T}.Empty"/> for a
    /// member with no single owner (the stream/drive/watcher types, and the shared names with no interface member on
    /// the referenced side, such as a directory-side <c>Move</c> or any creation/access-time getter/setter) — which is
    /// therefore never exempt and stays a build error everywhere until an interface is grown for it.</returns>
    internal static ImmutableArray<(string Namespace, string Name)> OwningInterfaceFor(ISymbol member, INamedTypeSymbol type)
    {
        // The stream, watcher, and drive types have zero usage and no interface member, so they get NO owner: return
        // an empty set, which makes them RED everywhere (banned, not lumped under IFileWriter). Returned explicitly so
        // no stream member name could ever collide with a mapped name below.
        if (WellKnownType.IsAnyOf(type, StreamWatcherAndDriveTypes))
        {
            return NoOwner;
        }

        // ONE general type-disambiguation pass over EVERY member name shared between File and Directory (Exists,
        // Delete, Move, and the whole timestamp getter/setter family). The declaring type decides the side
        // (Directory => the directory owner, File => the file owner); each side is the owning interface's member when
        // one exists, else NoOwner (banned everywhere on that side). Run BEFORE the flat sets so no shared name can
        // fall into a flat set and pick up the wrong owner. Only File and Directory ever reach here — the *Info types
        // are owned wholesale in OwnedPrimitives and routed through ResolveInfoTypes before this method is called — so
        // the side test checks Directory only.
        if (SharedNameOwners.TryGetValue(member.Name, out SharedNameOwner sharedOwners))
        {
            bool directorySide = WellKnownType.Is(type, KnownNamespaces.SystemIO, "Directory");
            return directorySide ? sharedOwners.DirectorySide : sharedOwners.FileSide;
        }

        if (FileReaderMemberNames.Contains(member.Name))
        {
            return FileReaderOwner;
        }

        if (DirectoryEnumeratorMemberNames.Contains(member.Name))
        {
            return DirectoryEnumeratorOwner;
        }

        if (FileWriterMemberNames.Contains(member.Name))
        {
            return FileWriterOwner;
        }

        if (DirectoryWriterMemberNames.Contains(member.Name))
        {
            return DirectoryWriterOwner;
        }

        return NoOwner;
    }

    /// <summary>
    /// The two owners of a member NAME shared between <c>System.IO.File</c> and <c>System.IO.Directory</c>: the
    /// interface that owns the File/FileInfo-side call and the interface that owns the Directory/DirectoryInfo-side
    /// call. Either may be <see cref="NoOwner"/>, meaning that side has no interface member and the raw call is banned
    /// everywhere on that side. A plain readonly struct with getter-only properties (not a record) so it needs no
    /// <c>IsExternalInit</c> polyfill on this netstandard2.0 analyzer project.
    /// </summary>
    private readonly struct SharedNameOwner
    {
        internal SharedNameOwner(
            ImmutableArray<(string Namespace, string Name)> fileSide,
            ImmutableArray<(string Namespace, string Name)> directorySide)
        {
            FileSide = fileSide;
            DirectorySide = directorySide;
        }

        internal ImmutableArray<(string Namespace, string Name)> FileSide { get; }

        internal ImmutableArray<(string Namespace, string Name)> DirectorySide { get; }
    }
}
