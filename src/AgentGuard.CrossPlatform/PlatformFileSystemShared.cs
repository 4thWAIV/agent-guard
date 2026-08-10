// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The OS-uniform managed logic shared by every <see cref="IPlatformFileSystem"/> implementation (the POSIX impl,
/// the Windows impl) and the engine tests' managed test double. It is plain BCL code that behaves identically on
/// every OS, so it is authored ONCE here in the contract assembly — which every per-OS implementation library and
/// the test project already reference — and each implementation CALLS it (composition), never copying it and never
/// inheriting it through a shared base class. The per-OS-specific native primitives (the atomic re-point, the
/// executable bit) stay in each implementation.
/// </summary>
public static class PlatformFileSystemShared
{
    /// <summary>
    /// Creates the platform-capability container wrapping <paramref name="fileSystem"/>. The container itself holds
    /// no OS-specific logic — it is identical on every OS — so it is owned once here in the contract assembly and
    /// each per-OS <c>Platform.Create()</c> factory calls this rather than authoring its own copy (composition, not a
    /// shared base class). A new capability becomes an additional constructor-injected property on the container with
    /// no change to any factory signature or caller.
    /// </summary>
    /// <param name="fileSystem">The platform's file-system capability.</param>
    /// <returns>The platform services container.</returns>
    public static IPlatformServices CreateServices(IPlatformFileSystem fileSystem) => new PlatformServices(fileSystem);

    /// <summary>
    /// Builds a unique temporary sibling path beside <paramref name="path"/> — a <c>.tmp-</c> name in the same
    /// directory, so an atomic create-and-swap stays on one filesystem. This single recipe is shared by the POSIX
    /// atomic symlink swap and the engine's atomic file writes, so it is spelled once.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <returns>The temporary sibling path in the destination's directory.</returns>
    public static string TemporarySiblingPath(string path) => Path.Combine(
        Path.GetDirectoryName(path)!,
        Path.GetFileName(path) + ".tmp-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Reads the raw, unresolved target of a symlink, or <see langword="null"/> when the path is not a symlink (a
    /// regular file, a directory, or an absent path).
    /// </summary>
    /// <param name="linkPath">The path to inspect.</param>
    /// <returns>The raw link target, or <see langword="null"/> when the path is not a link.</returns>
    public static string? ReadLinkTarget(string linkPath)
    {
        try
        {
            // FileSystemInfo.LinkTarget is the raw, unresolved target and is null for a non-link (a regular file, a
            // directory, or an absent path) — exactly the contract's null case.
            return new FileInfo(linkPath).LinkTarget ?? new DirectoryInfo(linkPath).LinkTarget;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Determines whether <paramref name="linkPath"/> is a symlink.
    /// </summary>
    /// <param name="linkPath">The path to inspect.</param>
    /// <returns><see langword="true"/> when the path is a symlink.</returns>
    public static bool IsLinkTarget(string linkPath) => ReadLinkTarget(linkPath) is not null;

    /// <summary>
    /// Throws when <paramref name="linkPath"/> holds a real file or directory rather than a symlink, so a re-point
    /// never replaces, and a remove never deletes, a real file or directory (scan-resolutions #4).
    /// </summary>
    /// <param name="linkPath">The path to inspect.</param>
    /// <param name="verb">The attempted action, named in the message (for example <c>point</c> or <c>remove</c>).</param>
    public static void RefuseIfRealEntry(string linkPath, string verb)
    {
        // A symlink reads back a non-null raw target; a real file or directory does not.
        if (ReadLinkTarget(linkPath) is null && (File.Exists(linkPath) || Directory.Exists(linkPath)))
        {
            throw new IOException($"Refusing to {verb} '{linkPath}': it is a real file or directory, not a symlink.");
        }
    }

    /// <summary>
    /// Removes the symlink at <paramref name="linkPath"/> — the link only, never its target — choosing the
    /// directory-symlink or file-symlink delete by the entry's kind. A no-op when the path holds no symlink.
    /// Removing the link entry never follows it, so the target and its contents are untouched. This is the managed
    /// removal the Windows implementation and the test double use; the POSIX implementation removes a link of either
    /// kind with a single <c>unlink</c> and so does not need this kind branch.
    /// </summary>
    /// <param name="linkPath">The symlink to remove.</param>
    public static void DeleteLinkEntry(string linkPath)
    {
        if (!IsLinkTarget(linkPath))
        {
            return;
        }

        if ((File.GetAttributes(linkPath) & FileAttributes.Directory) == FileAttributes.Directory)
        {
            Directory.Delete(linkPath);
        }
        else
        {
            File.Delete(linkPath);
        }
    }

    /// <summary>
    /// Creates a fresh symlink at <paramref name="linkPath"/> pointing at <paramref name="relativeTarget"/>, choosing
    /// the directory-symlink or file-symlink call by the kind of the resolved target. Windows records the two kinds
    /// distinctly and needs the right one for the link to resolve; the choice is harmless on POSIX. The raw relative
    /// target is stored verbatim. The precondition is that the target already exists so its kind can be read (the
    /// guard's version pointers always target an already-created versions directory). This is the managed create the
    /// Windows implementation and the engine test double share; the POSIX implementation creates a link of either kind
    /// with a single native call and so does not need this kind branch.
    /// </summary>
    /// <param name="linkPath">The symlink to create.</param>
    /// <param name="relativeTarget">The raw relative target, stored verbatim.</param>
    public static void CreateLinkEntry(string linkPath, string relativeTarget)
    {
        string linkDirectory = Path.GetDirectoryName(linkPath)!;
        if (Directory.Exists(Path.GetFullPath(Path.Combine(linkDirectory, relativeTarget))))
        {
            Directory.CreateSymbolicLink(linkPath, relativeTarget);
        }
        else
        {
            File.CreateSymbolicLink(linkPath, relativeTarget);
        }
    }

    /// <summary>
    /// The single concrete <see cref="IPlatformServices"/> container, owned once here because it holds zero
    /// OS-specific logic. Every per-OS factory obtains it through <see cref="CreateServices"/>; new capabilities are
    /// added as constructor-injected properties here without changing the factory signature or any caller.
    /// </summary>
    private sealed class PlatformServices : IPlatformServices
    {
        internal PlatformServices(IPlatformFileSystem fileSystem) => FileSystem = fileSystem;

        public IPlatformFileSystem FileSystem { get; }
    }
}
