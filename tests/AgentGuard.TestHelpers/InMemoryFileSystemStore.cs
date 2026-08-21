// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The ONE shared copy-on-write overlay behind the filesystem and platform fakes (copy-on-write-simulator-design): a
/// normalized-absolute-path node map (file / directory / symlink / tombstone, plus a per-node inaccessible flag) layered
/// over an optional real read-only base. Every write and every created symlink lands in the overlay, so the real base is
/// never mutated. Per filesystem call the resolution chain is three layers, in order: a per-path <see cref="PathHandler"/>
/// the test installed through <c>OnFileSystem().Handle(path, ...)</c>, then this in-memory overlay, then the real base.
/// It owns the ONE segment-by-segment multi-hop symlink resolver both fakes call, the owned path comparer (default
/// <see cref="StringComparer.Ordinal"/>, injectable), and the <c>CreateTempSubdirectory</c> uniqueness counter. It is
/// <c>internal</c> to <c>AgentGuard.TestHelpers</c> with no <c>InternalsVisibleTo</c> to the <c>.Tests</c> projects, and
/// is constructed only inside <see cref="SystemServicesBuilder"/> (AG0027), so both fakes are guaranteed the SAME overlay
/// instance by construction — a symlink created via the platform fake resolves for the filesystem fake.
/// </summary>
internal sealed class InMemoryFileSystemStore
{
    private const int MaxSymlinkHops = 40;

    private static readonly DateTimeOffset DefaultTimestamp = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Dictionary<string, OverlayNode> _nodes;
    private readonly Dictionary<string, PathHandler> _handlers;
    private readonly IEqualityComparer<string> _comparer;
    private readonly IFileReader? _baseReader;
    private readonly IDirectoryEnumerator? _baseEnumerator;
    private readonly IPlatformFileSystem? _basePlatform;
    private int _tempCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryFileSystemStore"/> class over an optional real read-only base.
    /// When a base is supplied the overlay is copy-on-write over it — a read that misses the node map falls through to the
    /// base; when it is absent the overlay is a pure in-memory filesystem and a miss reads as absent. Writes always land
    /// in the overlay and never touch the base.
    /// </summary>
    /// <param name="baseReader">The real file reader read through on a base fall-through, or <see langword="null"/>.</param>
    /// <param name="baseEnumerator">The real directory enumerator read through on a base fall-through, or
    /// <see langword="null"/>.</param>
    /// <param name="basePlatform">The real platform file system read through for base link reads, or
    /// <see langword="null"/>.</param>
    /// <param name="comparer">The path comparer that keys the node map, or <see langword="null"/> for the default
    /// <see cref="StringComparer.Ordinal"/>.</param>
    /// <param name="tempRoot">The absolute root under which <see cref="CreateTempSubdirectory"/> allocates unique
    /// subdirectories.</param>
    /// <param name="caseSensitive">The value the platform fake's case-sensitivity probe returns.</param>
    internal InMemoryFileSystemStore(
        IFileReader? baseReader,
        IDirectoryEnumerator? baseEnumerator,
        IPlatformFileSystem? basePlatform,
        IEqualityComparer<string>? comparer,
        string tempRoot,
        bool caseSensitive)
    {
        _comparer = comparer ?? StringComparer.Ordinal;
        _nodes = new Dictionary<string, OverlayNode>(_comparer);
        _handlers = new Dictionary<string, PathHandler>(_comparer);
        _baseReader = baseReader;
        _baseEnumerator = baseEnumerator;
        _basePlatform = basePlatform;
        TempRoot = tempRoot;
        CaseSensitive = caseSensitive;
    }

    /// <summary>Gets the absolute root under which unique temp subdirectories are allocated.</summary>
    internal string TempRoot { get; }

    /// <summary>Gets a value indicating whether the platform fake reports the backing store as case-sensitive.</summary>
    internal bool CaseSensitive { get; }

    /// <summary>Installs a per-path handler — the first link in the resolution chain for that path.</summary>
    /// <param name="path">The path the handler takes control of.</param>
    /// <param name="handler">The handler invoked first for every operation on <paramref name="path"/>.</param>
    internal void Handle(string path, PathHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[Normalize(path)] = handler;
    }

    /// <summary>
    /// Marks a directory inaccessible: <see cref="EnumerateChildren"/> on it throws
    /// <see cref="UnauthorizedAccessException"/> while existence checks still answer.
    /// </summary>
    /// <param name="path">The directory path to mark inaccessible.</param>
    internal void MarkInaccessible(string path)
    {
        string key = Normalize(path);
        if (!_nodes.TryGetValue(key, out OverlayNode? node))
        {
            node = OverlayNode.ForDirectory(DefaultTimestamp);
            _nodes[key] = node;
        }

        node.Inaccessible = true;
    }

    /// <summary>Determines whether a file exists at the path (symlinks followed).</summary>
    /// <param name="path">The path to test.</param>
    /// <returns><see langword="true"/> when a file exists there.</returns>
    internal bool FileExists(string path) => Dispatch(path, FileSystemOperation.Exists, FileExistsCore);

    /// <summary>Reads the full contents of a file as bytes (symlinks followed).</summary>
    /// <param name="path">The path to read.</param>
    /// <returns>The file's bytes.</returns>
    internal byte[] ReadAllBytes(string path) => Dispatch(path, FileSystemOperation.ReadAllBytes, ReadAllBytesCore);

    /// <summary>Reads the full contents of a file as UTF-8 text (symlinks followed).</summary>
    /// <param name="path">The path to read.</param>
    /// <returns>The file's contents.</returns>
    internal string ReadAllText(string path) => Dispatch(path, FileSystemOperation.ReadAllText, ReadAllTextCore);

    /// <summary>Reads the attributes of the entry at the path.</summary>
    /// <param name="path">The path whose attributes are read.</param>
    /// <returns>The entry's attributes.</returns>
    internal FileAttributes GetFileAttributes(string path) =>
        Dispatch(path, FileSystemOperation.GetAttributes, GetAttributesCore);

    /// <summary>Reads a file's last-write time in UTC.</summary>
    /// <param name="path">The path whose last-write time is read.</param>
    /// <returns>The file's last-write time.</returns>
    internal DateTimeOffset GetFileLastWriteTimeUtc(string path) =>
        Dispatch(path, FileSystemOperation.GetFileLastWriteTimeUtc, GetLastWriteCore);

    /// <summary>Determines whether a directory exists at the path (symlinks followed).</summary>
    /// <param name="path">The directory path to test.</param>
    /// <returns><see langword="true"/> when a directory exists there.</returns>
    internal bool DirectoryExists(string path) =>
        Dispatch(path, FileSystemOperation.DirectoryExists, DirectoryExistsCore);

    /// <summary>Returns the immediate children of one directory, throwing when it is marked inaccessible.</summary>
    /// <param name="path">The directory path whose children are listed.</param>
    /// <returns>The directory's immediate children, in no guaranteed order.</returns>
    internal IReadOnlyList<DirectoryChild> EnumerateChildren(string path) =>
        Dispatch(path, FileSystemOperation.EnumerateChildren, EnumerateChildrenCore);

    /// <summary>Lists the files in a directory matching a pattern.</summary>
    /// <param name="path">The directory path to search.</param>
    /// <param name="pattern">The search pattern to match file names against.</param>
    /// <returns>The matching file paths, in no guaranteed order.</returns>
    internal IReadOnlyList<string> EnumerateFiles(string path, string pattern) =>
        Dispatch(path, FileSystemOperation.EnumerateFiles, resolved => EnumerateNamesCore(resolved, pattern, wantDirectories: false));

    /// <summary>Lists the subdirectories in a directory matching a pattern.</summary>
    /// <param name="path">The directory path to search.</param>
    /// <param name="pattern">The search pattern to match directory names against.</param>
    /// <returns>The matching subdirectory paths, in no guaranteed order.</returns>
    internal IReadOnlyList<string> EnumerateDirectories(string path, string pattern) =>
        Dispatch(path, FileSystemOperation.EnumerateDirectories, resolved => EnumerateNamesCore(resolved, pattern, wantDirectories: true));

    /// <summary>Reads a directory's last-write time in UTC.</summary>
    /// <param name="path">The directory path whose last-write time is read.</param>
    /// <returns>The directory's last-write time.</returns>
    internal DateTimeOffset GetDirectoryLastWriteTimeUtc(string path) =>
        Dispatch(path, FileSystemOperation.GetDirectoryLastWriteTimeUtc, GetLastWriteCore);

    /// <summary>Writes bytes to a file, replacing any existing contents.</summary>
    /// <param name="path">The path to write.</param>
    /// <param name="bytes">The bytes to write.</param>
    internal void WriteAllBytes(string path, ReadOnlyMemory<byte> bytes)
    {
        byte[] copy = bytes.ToArray();
        DispatchVoid(path, FileSystemOperation.WriteFile, resolved => WriteFileCore(resolved, copy));
    }

    /// <summary>Writes text to a file as UTF-8, replacing any existing contents.</summary>
    /// <param name="path">The path to write.</param>
    /// <param name="contents">The text to write.</param>
    internal void WriteAllText(string path, string contents)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(contents);
        DispatchVoid(path, FileSystemOperation.WriteFile, resolved => WriteFileCore(resolved, bytes));
    }

    /// <summary>Copies a file.</summary>
    /// <param name="source">The absolute source path.</param>
    /// <param name="destination">The absolute destination path.</param>
    /// <param name="overwrite"><see langword="true"/> to replace an existing destination.</param>
    internal void Copy(string source, string destination, bool overwrite)
    {
        byte[] bytes = ReadAllBytes(source);
        string resolvedDestination = Resolve(destination, followFinal: true);
        if (!overwrite && FileExistsCore(resolvedDestination))
        {
            throw new IOException($"The destination file '{destination}' already exists.");
        }

        WriteFileCore(resolvedDestination, bytes);
    }

    /// <summary>Moves a file.</summary>
    /// <param name="source">The absolute source path.</param>
    /// <param name="destination">The absolute destination path.</param>
    /// <param name="overwrite"><see langword="true"/> to replace an existing destination.</param>
    internal void Move(string source, string destination, bool overwrite)
    {
        Copy(source, destination, overwrite);
        DeleteFile(source);
    }

    /// <summary>Deletes the file at the path, masking any base entry with a tombstone.</summary>
    /// <param name="path">The path to delete.</param>
    internal void DeleteFile(string path) =>
        DispatchVoid(path, FileSystemOperation.DeleteFile, resolved => _nodes[Normalize(resolved)] = OverlayNode.ForTombstone());

    /// <summary>Creates a directory (and its ancestors) in the overlay.</summary>
    /// <param name="path">The absolute directory path to create.</param>
    internal void CreateDirectory(string path) =>
        DispatchVoid(path, FileSystemOperation.CreateDirectory, CreateDirectoryCore);

    /// <summary>Deletes a directory, masking base entries with tombstones.</summary>
    /// <param name="path">The absolute directory path to delete.</param>
    /// <param name="recursive"><see langword="true"/> to delete the directory and its contents.</param>
    internal void DeleteDirectory(string path, bool recursive) =>
        DispatchVoid(path, FileSystemOperation.DeleteDirectory, resolved => DeleteDirectoryCore(resolved, recursive));

    /// <summary>Sets a directory's last-write time in UTC.</summary>
    /// <param name="path">The absolute directory path whose last-write time is set.</param>
    /// <param name="time">The last-write time to set.</param>
    internal void SetDirectoryLastWriteTimeUtc(string path, DateTimeOffset time) =>
        DispatchVoid(path, FileSystemOperation.SetLastWriteTimeUtc, resolved => SetLastWriteCore(resolved, time));

    /// <summary>Creates a uniquely-named temporary subdirectory under <see cref="TempRoot"/> and returns its path.</summary>
    /// <param name="prefix">An optional name prefix on the created directory.</param>
    /// <returns>The absolute path of the created temporary subdirectory.</returns>
    internal string CreateTempSubdirectory(string prefix)
    {
        int ordinal = ++_tempCounter;
        string name = (prefix ?? string.Empty) + ordinal.ToString(CultureInfo.InvariantCulture);
        string path = Path.Combine(TempRoot, name);
        CreateDirectoryCore(path);
        return path;
    }

    /// <summary>Determines whether the entry at the path is a symlink; the final component is not followed.</summary>
    /// <param name="linkPath">The path to test.</param>
    /// <returns><see langword="true"/> when the entry is a symlink.</returns>
    internal bool IsLinkTarget(string linkPath)
    {
        string entry = Resolve(linkPath, followFinal: false);
        return OverlaySymlinkTarget(entry) is not null
            || (!_nodes.ContainsKey(Normalize(entry)) && (_basePlatform?.IsLinkTarget(entry) ?? false));
    }

    /// <summary>Reads a symlink's raw (unresolved) target string; the final component is not followed.</summary>
    /// <param name="linkPath">The path expected to be a symlink.</param>
    /// <returns>The raw target the link points at, or <see langword="null"/> when it is not a symlink.</returns>
    internal string? ReadLinkTarget(string linkPath)
    {
        string entry = Resolve(linkPath, followFinal: false);
        string? overlayTarget = OverlaySymlinkTarget(entry);
        if (overlayTarget is not null)
        {
            return overlayTarget;
        }

        return _nodes.ContainsKey(Normalize(entry)) ? null : _basePlatform?.ReadLinkTarget(entry);
    }

    /// <summary>Atomically points a symlink at a relative target, refusing when a real entry exists there.</summary>
    /// <param name="linkPath">The symlink path to create or replace.</param>
    /// <param name="relativeTarget">The relative target the link should resolve to.</param>
    internal void MakeLinkTarget(string linkPath, string relativeTarget)
    {
        string entry = Resolve(linkPath, followFinal: false);
        RefuseIfRealEntry(entry, "point");
        string? parent = Path.GetDirectoryName(entry);
        if (parent is not null)
        {
            CreateDirectoryCore(parent);
        }

        _nodes[Normalize(entry)] = OverlayNode.ForSymlink(relativeTarget, DefaultTimestamp);
    }

    /// <summary>Removes a symlink, and only the link, refusing when a real entry exists there.</summary>
    /// <param name="linkPath">The symlink path to remove.</param>
    internal void RemoveLinkTarget(string linkPath)
    {
        string entry = Resolve(linkPath, followFinal: false);
        RefuseIfRealEntry(entry, "remove");
        _nodes[Normalize(entry)] = OverlayNode.ForTombstone();
    }

    // A write to the final path targets the entry the last component names, so writes do not follow the final symlink;
    // reads and existence checks follow it to the entry it points at. Intermediate components are always followed.
    private static bool OperationFollowsFinal(FileSystemOperation operation) => operation switch
    {
        FileSystemOperation.WriteFile => false,
        FileSystemOperation.DeleteFile => false,
        FileSystemOperation.CreateDirectory => false,
        FileSystemOperation.DeleteDirectory => false,
        FileSystemOperation.SetLastWriteTimeUtc => false,
        _ => true,
    };

    // Trim trailing separators (except a lone root) so a path and the same path with a trailing slash key one node.
    private static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        string trimmed = path.TrimEnd('/', '\\');
        return trimmed.Length == 0 ? path : trimmed;
    }

    // A minimal glob matcher over the two patterns the enumeration surface uses in tests — "*" (everything) and a plain
    // "*.ext" suffix — sufficient for the fail-closed scanner and grant-token reads; an exact name matches literally.
    private static bool MatchesPattern(string name, string pattern)
    {
        if (pattern.Length == 0 || string.Equals(pattern, "*", StringComparison.Ordinal))
        {
            return true;
        }

        return pattern.StartsWith('*')
            ? name.EndsWith(pattern[1..], StringComparison.Ordinal)
            : string.Equals(name, pattern, StringComparison.Ordinal);
    }

    private T Dispatch<T>(string path, FileSystemOperation operation, Func<string, T> core)
    {
        string resolved = Resolve(path, followFinal: OperationFollowsFinal(operation));
        if (_handlers.TryGetValue(Normalize(path), out PathHandler? handler))
        {
            return (T)handler(operation, () => core(resolved))!;
        }

        return core(resolved);
    }

    private void DispatchVoid(string path, FileSystemOperation operation, Action<string> core)
    {
        string resolved = Resolve(path, followFinal: OperationFollowsFinal(operation));
        if (_handlers.TryGetValue(Normalize(path), out PathHandler? handler))
        {
            handler(
                operation,
                () =>
                {
                    core(resolved);
                    return null;
                });
            return;
        }

        core(resolved);
    }

    // The one segment-by-segment multi-hop symlink resolver both fakes call. An intermediate component is always
    // resolved through a symlink, while the final component is resolved only when followFinal asks for it. The absolute
    // path is split with the pure directory-name and file-name path helpers rather than the owned raw separator field,
    // so the store carries no owned separator read.
    private string Resolve(string path, bool followFinal)
    {
        var segments = new List<string>();
        string root = path;
        while (true)
        {
            string? parent = Path.GetDirectoryName(root);
            if (parent is null || parent.Length == 0)
            {
                break;
            }

            segments.Add(Path.GetFileName(root));
            root = parent;
        }

        segments.Reverse();

        string current = root;
        for (int i = 0; i < segments.Count; i++)
        {
            string candidate = Path.Combine(current, segments[i]);
            bool isFinal = i == segments.Count - 1;
            current = isFinal && !followFinal ? candidate : FollowLinks(candidate);
        }

        return current;
    }

    // Follow an overlay (or base) symlink chain at one path, splicing each relative target against the link's own parent
    // directory (the pure two-argument Path.GetFullPath), bounded by MaxSymlinkHops.
    private string FollowLinks(string path)
    {
        string current = path;
        for (int hops = 0; hops <= MaxSymlinkHops; hops++)
        {
            string? target = OverlaySymlinkTarget(current);
            if (target is null && !_nodes.ContainsKey(Normalize(current)))
            {
                target = _basePlatform is not null && _basePlatform.IsLinkTarget(current)
                    ? _basePlatform.ReadLinkTarget(current)
                    : null;
            }

            if (target is null)
            {
                return current;
            }

            string parent = Path.GetDirectoryName(current) ?? current;
            current = Path.GetFullPath(target, parent);
        }

        throw new IOException($"Too many levels of symbolic links resolving '{path}'.");
    }

    private string? OverlaySymlinkTarget(string path) =>
        _nodes.TryGetValue(Normalize(path), out OverlayNode? node) && node.Kind == NodeKind.Symlink
            ? node.LinkTarget
            : null;

    private bool FileExistsCore(string resolved)
    {
        if (_nodes.TryGetValue(Normalize(resolved), out OverlayNode? node))
        {
            return node.Kind == NodeKind.File;
        }

        return _baseReader?.Exists(resolved) ?? false;
    }

    private byte[] ReadAllBytesCore(string resolved)
    {
        if (_nodes.TryGetValue(Normalize(resolved), out OverlayNode? node))
        {
            return node.Kind == NodeKind.File
                ? (byte[])node.Content.Clone()
                : throw new FileNotFoundException($"Could not find file '{resolved}'.", resolved);
        }

        return _baseReader is not null
            ? _baseReader.ReadAllBytes(resolved)
            : throw new FileNotFoundException($"Could not find file '{resolved}'.", resolved);
    }

    private string ReadAllTextCore(string resolved)
    {
        if (_nodes.TryGetValue(Normalize(resolved), out OverlayNode? node))
        {
            return node.Kind == NodeKind.File
                ? Encoding.UTF8.GetString(node.Content)
                : throw new FileNotFoundException($"Could not find file '{resolved}'.", resolved);
        }

        return _baseReader is not null
            ? _baseReader.ReadAllText(resolved)
            : throw new FileNotFoundException($"Could not find file '{resolved}'.", resolved);
    }

    private FileAttributes GetAttributesCore(string resolved)
    {
        if (_nodes.TryGetValue(Normalize(resolved), out OverlayNode? node))
        {
            return node.Kind switch
            {
                NodeKind.Directory => FileAttributes.Directory,
                NodeKind.Symlink => FileAttributes.ReparsePoint,
                NodeKind.File => FileAttributes.Normal,
                _ => throw new FileNotFoundException($"Could not find '{resolved}'.", resolved),
            };
        }

        return _baseReader is not null
            ? _baseReader.GetAttributes(resolved)
            : throw new FileNotFoundException($"Could not find '{resolved}'.", resolved);
    }

    private DateTimeOffset GetLastWriteCore(string resolved)
    {
        if (_nodes.TryGetValue(Normalize(resolved), out OverlayNode? node))
        {
            return node.LastWriteUtc;
        }

        if (_baseReader is not null && _baseReader.Exists(resolved))
        {
            return _baseReader.GetLastWriteTimeUtc(resolved);
        }

        return _baseEnumerator is not null && _baseEnumerator.DirectoryExists(resolved)
            ? _baseEnumerator.GetLastWriteTimeUtc(resolved)
            : DefaultTimestamp;
    }

    private bool DirectoryExistsCore(string resolved)
    {
        if (_nodes.TryGetValue(Normalize(resolved), out OverlayNode? node))
        {
            return node.Kind == NodeKind.Directory;
        }

        return _baseEnumerator?.DirectoryExists(resolved) ?? false;
    }

    private List<DirectoryChild> EnumerateChildrenCore(string resolved)
    {
        string key = Normalize(resolved);
        if (_nodes.TryGetValue(key, out OverlayNode? node) && node.Inaccessible)
        {
            throw new UnauthorizedAccessException($"Access to the path '{resolved}' is denied.");
        }

        var children = new Dictionary<string, DirectoryChild>(_comparer);
        if (_baseEnumerator is not null && _baseEnumerator.DirectoryExists(resolved) && !_nodes.ContainsKey(key))
        {
            foreach (DirectoryChild child in _baseEnumerator.EnumerateChildren(resolved))
            {
                children[Normalize(child.FullPath)] = child;
            }
        }

        foreach (KeyValuePair<string, OverlayNode> entry in OverlayChildrenOf(key))
        {
            if (entry.Value.Kind == NodeKind.Tombstone)
            {
                children.Remove(entry.Key);
                continue;
            }

            children[entry.Key] = new DirectoryChild(
                entry.Key,
                entry.Value.Kind == NodeKind.Directory,
                entry.Value.Kind == NodeKind.Symlink);
        }

        return children.Values.ToList();
    }

    private List<string> EnumerateNamesCore(string resolved, string pattern, bool wantDirectories)
    {
        var results = new List<string>();
        foreach (DirectoryChild child in EnumerateChildrenCore(resolved))
        {
            if (child.IsDirectory == wantDirectories && MatchesPattern(Path.GetFileName(child.FullPath), pattern))
            {
                results.Add(child.FullPath);
            }
        }

        return results;
    }

    private IEnumerable<KeyValuePair<string, OverlayNode>> OverlayChildrenOf(string directoryKey)
    {
        foreach (KeyValuePair<string, OverlayNode> entry in _nodes)
        {
            string? parent = Path.GetDirectoryName(entry.Key);
            if (parent is not null && _comparer.Equals(Normalize(parent), directoryKey))
            {
                yield return entry;
            }
        }
    }

    private void WriteFileCore(string resolved, byte[] bytes) =>
        _nodes[Normalize(resolved)] = OverlayNode.ForFile(bytes, DefaultTimestamp);

    private void CreateDirectoryCore(string resolved)
    {
        string key = Normalize(resolved);
        if (_nodes.TryGetValue(key, out OverlayNode? existing) && existing.Kind == NodeKind.Directory)
        {
            return;
        }

        string? parent = Path.GetDirectoryName(key);
        if (parent is not null && parent.Length > 0 && !_comparer.Equals(Normalize(parent), key))
        {
            CreateDirectoryCore(parent);
        }

        _nodes[key] = OverlayNode.ForDirectory(DefaultTimestamp);
    }

    private void DeleteDirectoryCore(string resolved, bool recursive)
    {
        if (recursive)
        {
            foreach (DirectoryChild child in EnumerateChildrenCore(resolved))
            {
                if (child.IsDirectory)
                {
                    DeleteDirectoryCore(child.FullPath, recursive: true);
                }
                else
                {
                    _nodes[Normalize(child.FullPath)] = OverlayNode.ForTombstone();
                }
            }
        }

        _nodes[Normalize(resolved)] = OverlayNode.ForTombstone();
    }

    private void SetLastWriteCore(string resolved, DateTimeOffset time)
    {
        string key = Normalize(resolved);
        if (!_nodes.TryGetValue(key, out OverlayNode? node))
        {
            node = OverlayNode.ForDirectory(time);
            _nodes[key] = node;
        }

        node.LastWriteUtc = time;
    }

    private void RefuseIfRealEntry(string entry, string action)
    {
        string key = Normalize(entry);
        if (_nodes.TryGetValue(key, out OverlayNode? node))
        {
            if (node.Kind is NodeKind.File or NodeKind.Directory)
            {
                throw new IOException($"Cannot {action} '{entry}': a real file or directory exists there.");
            }

            return;
        }

        if (_basePlatform is not null && !_basePlatform.IsLinkTarget(entry)
            && ((_baseReader?.Exists(entry) ?? false) || (_baseEnumerator?.DirectoryExists(entry) ?? false)))
        {
            throw new IOException($"Cannot {action} '{entry}': a real file or directory exists there.");
        }
    }
}
