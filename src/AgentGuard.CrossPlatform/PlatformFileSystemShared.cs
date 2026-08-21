// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The OS-uniform file operations shared by every per-OS <see cref="IPlatformFileSystem"/> implementation
/// (<c>PosixFileSystem</c>, <c>WindowsFileSystem</c>). It behaves identically on every OS, so it is authored ONCE here
/// in the contract assembly and each per-OS implementation CALLS it (composition), never copying it and never
/// inheriting it through a shared base class. It makes ZERO raw boundary calls: every filesystem operation goes through
/// the owned adapters it receives by constructor injection (<see cref="IFileReader"/>, <see cref="IDirectoryEnumerator"/>,
/// <see cref="IFileWriter"/>, <see cref="IDirectoryWriter"/>), and every GUID through the injected
/// <see cref="IRandomGenerator"/>. The OS-DIVERGENT primitives (the symlink reads/writes, the executable bit, the atomic
/// re-point, and constructing a <c>FileInfo</c>/<c>DirectoryInfo</c>) do NOT live here — they live only in the one
/// per-OS class (AG0101, ag0101-one-owner-per-os).
/// </summary>
internal sealed class PlatformFileSystemShared
{
    private readonly IFileReader _fileReader;
    private readonly IDirectoryEnumerator _directories;
    private readonly IFileWriter _fileWriter;
    private readonly IDirectoryWriter _directoryWriter;
    private readonly IRandomGenerator _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformFileSystemShared"/> class, wired to the owned adapters
    /// every OS-uniform operation routes through so this helper makes no raw boundary call.
    /// </summary>
    /// <param name="fileReader">The owned read side of the filesystem primitive.</param>
    /// <param name="directories">The owned read-only directory enumerator.</param>
    /// <param name="fileWriter">The owned file-write side of the filesystem primitive.</param>
    /// <param name="directoryWriter">The owned directory-write side of the filesystem primitive.</param>
    /// <param name="random">The owned random generator.</param>
    internal PlatformFileSystemShared(
        IFileReader fileReader,
        IDirectoryEnumerator directories,
        IFileWriter fileWriter,
        IDirectoryWriter directoryWriter,
        IRandomGenerator random)
    {
        _fileReader = fileReader;
        _directories = directories;
        _fileWriter = fileWriter;
        _directoryWriter = directoryWriter;
        _random = random;
    }

    /// <summary>
    /// Builds a unique temporary sibling path beside <paramref name="path"/> — a <c>.tmp-</c> name in the same
    /// directory, so an atomic create-and-swap stays on one filesystem. The unique suffix comes from the injected
    /// random generator, not a raw <c>Guid.NewGuid()</c>.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <returns>The temporary sibling path in the destination's directory.</returns>
    internal string TemporarySiblingPath(string path) => Path.Combine(
        Path.GetDirectoryName(path)!,
        Path.GetFileName(path) + ".tmp-" + _random.NewGuid().ToString("N"));

    /// <summary>
    /// Throws when <paramref name="linkPath"/> holds a real file or directory rather than a symlink, so a re-point
    /// never replaces, and a remove never deletes, a real file or directory. The link/non-link decision is the
    /// caller's, since it is OS-divergent; the existence checks go through the injected owned adapters.
    /// </summary>
    /// <param name="linkPath">The path to inspect.</param>
    /// <param name="verb">The attempted action, named in the message (for example <c>point</c> or <c>remove</c>).</param>
    /// <param name="isLink">Whether the per-OS owner has determined the path is a symlink.</param>
    internal void RefuseIfRealEntry(string linkPath, string verb, bool isLink)
    {
        if (!isLink && (_fileReader.Exists(linkPath) || _directories.DirectoryExists(linkPath)))
        {
            throw new IOException($"Refusing to {verb} '{linkPath}': it is a real file or directory, not a symlink.");
        }
    }

    /// <summary>
    /// Creates the directory at <paramref name="path"/>, including any missing parents, through the owned directory
    /// writer.
    /// </summary>
    /// <param name="path">The directory path to create.</param>
    internal void CreateDirectory(string path) => _directoryWriter.CreateDirectory(path);

    /// <summary>
    /// Deletes the file at <paramref name="path"/> through the owned file writer.
    /// </summary>
    /// <param name="path">The file path to delete.</param>
    internal void DeleteFile(string path) => _fileWriter.DeleteFile(path);

    /// <summary>
    /// Determines whether a directory exists at <paramref name="path"/> through the owned directory enumerator.
    /// </summary>
    /// <param name="path">The directory path to test.</param>
    /// <returns><see langword="true"/> when the directory exists.</returns>
    internal bool DirectoryExists(string path) => _directories.DirectoryExists(path);

    /// <summary>
    /// Removes the symlink at <paramref name="linkPath"/> — the link only, never its target — choosing the
    /// directory-symlink or file-symlink delete by the entry's kind, both through the owned adapters. A no-op when the
    /// path holds no symlink. This is the managed removal the Windows implementation uses; the POSIX implementation
    /// removes a link of either kind with a single delete and does not need this kind branch.
    /// </summary>
    /// <param name="linkPath">The symlink to remove.</param>
    /// <param name="isLink">Whether the per-OS owner has determined the path is a symlink.</param>
    internal void DeleteLinkEntry(string linkPath, bool isLink)
    {
        if (!isLink)
        {
            return;
        }

        if (_fileReader.GetAttributes(linkPath).HasFlag(FileAttributes.Directory))
        {
            _directoryWriter.DeleteDirectory(linkPath, recursive: false);
        }
        else
        {
            _fileWriter.DeleteFile(linkPath);
        }
    }

    /// <summary>
    /// Detects whether name matching against <paramref name="path"/> is case-sensitive, using only the injected owned
    /// adapters. This is the shared read-only fallback probe every per-OS implementation reuses when its native query
    /// is unavailable or unclear (case-sensitivity-detected-per-filesystem). It picks an existing entry in the
    /// directory whose case-flipped name is NOT itself another entry (no case-variant sibling, so no collision), then
    /// tests whether that flipped-case name resolves to an existing entry: because the flipped name is not a distinct
    /// sibling, an existing hit can only be the SAME entry, which means matching is case-insensitive. When no entry can
    /// be probed (the directory is empty, holds no letter-bearing name, or every candidate has a case-variant sibling),
    /// it returns the documented case-sensitive default.
    /// </summary>
    /// <param name="path">The directory path whose backing file system is probed.</param>
    /// <returns><see langword="true"/> when matching against <paramref name="path"/> is case-sensitive.</returns>
    internal bool ProbeCaseSensitive(string path)
    {
        IReadOnlyList<DirectoryChild> children = _directories.EnumerateChildren(path);

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (DirectoryChild child in children)
        {
            names.Add(Path.GetFileName(child.FullPath));
        }

        foreach (DirectoryChild child in children)
        {
            string name = Path.GetFileName(child.FullPath);
            string flipped = FlipCase(name);

            // Usable only when flipping actually changed the name (it bears a letter) AND the flipped spelling is not
            // itself a distinct sibling — then an existing hit on the flipped name is provably the same entry.
            if (string.Equals(flipped, name, StringComparison.Ordinal) || names.Contains(flipped))
            {
                continue;
            }

            string flippedPath = Path.Combine(path, flipped);
            bool flippedExists = child.IsDirectory
                ? _directories.DirectoryExists(flippedPath)
                : _fileReader.Exists(flippedPath);

            // The flipped-case name resolves to the same entry => case-insensitive; it does not resolve => the exact
            // case was required => case-sensitive.
            return !flippedExists;
        }

        // Even the probe cannot tell (no probeable entry): documented case-sensitive default.
        return true;
    }

    // Toggles the case of every letter in the name. A name with no letters comes back unchanged, which the caller
    // treats as "not probeable". Pure string work, no boundary call.
    private static string FlipCase(string name)
    {
        char[] characters = name.ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            char character = characters[i];
            if (char.IsUpper(character))
            {
                characters[i] = char.ToLowerInvariant(character);
            }
            else if (char.IsLower(character))
            {
                characters[i] = char.ToUpperInvariant(character);
            }
        }

        return new string(characters);
    }
}
