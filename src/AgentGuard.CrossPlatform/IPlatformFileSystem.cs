// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The platform's file-system capability: the complete set of operations the engine needs that the managed BCL
/// cannot do OS-uniformly — the version-pointer symlinks the machine layout depends on, and the POSIX executable
/// bit. It is a capability CRUD surface (create, read, replace, remove a link; plus the executable-flag set) and
/// is allowed to grow, but every method is part of one coherent complete set. Implementations are per-OS behind
/// this locked interface; the engine consumes only the interface and stays OS-agnostic.
/// </summary>
public interface IPlatformFileSystem
{
    /// <summary>
    /// Gets a value indicating whether the path is a symlink.
    /// </summary>
    /// <param name="linkPath">The path to test.</param>
    /// <returns><see langword="true"/> when <paramref name="linkPath"/> is a symlink; otherwise
    /// <see langword="false"/> (a regular file, a directory, or absent).</returns>
    bool IsLinkTarget(string linkPath);

    /// <summary>
    /// Reads a symlink's raw (unresolved) target string.
    /// </summary>
    /// <param name="linkPath">The path expected to be a symlink.</param>
    /// <returns>The raw target string the link points at, or <see langword="null"/> when
    /// <paramref name="linkPath"/> is not a symlink (a regular file, a directory, or absent).</returns>
    string? ReadLinkTarget(string linkPath);

    /// <summary>
    /// Atomically points the symlink at <paramref name="linkPath"/> at <paramref name="relativeTarget"/>, creating
    /// the link when it is absent and replacing it when it already exists — the link is never momentarily absent.
    /// Any missing parent directory is created first. This operation is unconditional: it does not compare the
    /// current target, so deciding whether a re-point is needed is the caller's concern. It refuses (throws) when
    /// <paramref name="linkPath"/> holds a real file or directory rather than a symlink, so a real file is never
    /// replaced by a link.
    /// </summary>
    /// <param name="linkPath">The symlink path to create or replace.</param>
    /// <param name="relativeTarget">The relative target the link should resolve to.</param>
    void MakeLinkTarget(string linkPath, string relativeTarget);

    /// <summary>
    /// Removes the symlink at <paramref name="linkPath"/>, and only the link. The target the link points at —
    /// whether a file or a directory, and everything inside it — is never touched. It refuses (throws) when
    /// <paramref name="linkPath"/> holds a real file or directory rather than a symlink.
    /// </summary>
    /// <param name="linkPath">The symlink path to remove.</param>
    void RemoveLinkTarget(string linkPath);

    /// <summary>
    /// Gets a value indicating whether this OS uses (or supports) the executable bit. It is hard <see langword="true"/>
    /// on POSIX and hard <see langword="false"/> on Windows. Callers guard the other three executable-flag methods
    /// on this, so the bit is touched only where the OS has one.
    /// </summary>
    /// <returns><see langword="true"/> on POSIX; <see langword="false"/> on Windows.</returns>
    bool NeedsExecutableFlag();

    /// <summary>
    /// Gets a value indicating whether the file at <paramref name="path"/> has its executable bit set. Valid only
    /// where <see cref="NeedsExecutableFlag"/> is <see langword="true"/>; on Windows it throws.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when the executable bit is set (POSIX only).</returns>
    bool IsExecutable(string path);

    /// <summary>
    /// Adds the executable bit to the file at <paramref name="path"/> (POSIX <c>chmod +x</c>), preserving its
    /// existing read/write bits. Valid only where <see cref="NeedsExecutableFlag"/> is <see langword="true"/>; on
    /// Windows it throws.
    /// </summary>
    /// <param name="path">The file path.</param>
    void MakeExecutable(string path);

    /// <summary>
    /// Removes the executable bit from the file at <paramref name="path"/> (POSIX <c>chmod -x</c>), preserving its
    /// existing read/write bits. Valid only where <see cref="NeedsExecutableFlag"/> is <see langword="true"/>; on
    /// Windows it throws.
    /// </summary>
    /// <param name="path">The file path.</param>
    void MakeNonExecutable(string path);
}
