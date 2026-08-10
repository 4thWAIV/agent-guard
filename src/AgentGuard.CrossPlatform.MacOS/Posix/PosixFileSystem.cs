// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AgentGuard.CrossPlatform.Posix;

/// <summary>
/// The POSIX implementation of <see cref="IPlatformFileSystem"/>: managed link reads/removes plus a libc
/// <c>rename</c> for the atomic re-point, and the POSIX executable bit. This is the shared POSIX source — authored
/// here for macOS and linked into the Linux impl unchanged (ONE source, not duplicated, not collapsed to a shared
/// base class). Only the Windows impl, which needs <c>MoveFileEx</c> for its atomic swap, is written separately.
/// The executable-flag methods read and write the Unix file mode; the OS guard on each states the impl runs only
/// on POSIX (the guarantee that lets the managed Unix-mode calls be reached), which also satisfies the platform
/// compatibility analyzer without a suppression.
/// </summary>
internal sealed class PosixFileSystem : IPlatformFileSystem
{
    private const UnixFileMode ExecuteBits =
        UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

    private const string WindowsNotSupported =
        "The POSIX file system implementation does not run on Windows; the executable bit is a POSIX concept.";

    /// <inheritdoc/>
    public bool IsLinkTarget(string linkPath) => PlatformFileSystemShared.IsLinkTarget(linkPath);

    /// <inheritdoc/>
    public string? ReadLinkTarget(string linkPath) => PlatformFileSystemShared.ReadLinkTarget(linkPath);

    /// <inheritdoc/>
    public void MakeLinkTarget(string linkPath, string relativeTarget)
    {
        PlatformFileSystemShared.RefuseIfRealEntry(linkPath, "point");
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);

        // Write the new link to a unique temporary sibling in the same directory (so the rename stays on one
        // filesystem), then rename it over the destination. rename(2) replaces an existing symlink atomically, so
        // the link at linkPath is never momentarily absent — and creating over an absent destination just creates
        // it. The temporary is consumed by the rename, so no stray sibling is ever left behind.
        string temporaryPath = PlatformFileSystemShared.TemporarySiblingPath(linkPath);
        TryRemoveLink(temporaryPath);
        Directory.CreateSymbolicLink(temporaryPath, relativeTarget);

        if (PosixNativeMethods.Rename(temporaryPath, linkPath) != 0)
        {
            int error = Marshal.GetLastPInvokeError();
            TryRemoveLink(temporaryPath);
            throw new IOException(
                $"Failed to atomically point '{linkPath}' at '{relativeTarget}' (errno {error}).");
        }
    }

    /// <inheritdoc/>
    public void RemoveLinkTarget(string linkPath)
    {
        PlatformFileSystemShared.RefuseIfRealEntry(linkPath, "remove");

        // On POSIX, deleting a symlink unlinks the link itself and never follows it, so the target and its contents
        // are untouched. File.Delete maps to unlink(2) and removes a link to a directory as well as a link to a
        // file. A no-op when the link is already absent.
        File.Delete(linkPath);
    }

    /// <inheritdoc/>
    public bool NeedsExecutableFlag() => true;

    /// <inheritdoc/>
    public bool IsExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(WindowsNotSupported);
        }

        return (File.GetUnixFileMode(path) & UnixFileMode.UserExecute) != UnixFileMode.None;
    }

    /// <inheritdoc/>
    public void MakeExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(WindowsNotSupported);
        }

        File.SetUnixFileMode(path, File.GetUnixFileMode(path) | ExecuteBits);
    }

    /// <inheritdoc/>
    public void MakeNonExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(WindowsNotSupported);
        }

        File.SetUnixFileMode(path, File.GetUnixFileMode(path) & ~ExecuteBits);
    }

    private static void TryRemoveLink(string linkPath)
    {
        try
        {
            File.Delete(linkPath);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a stray temporary symlink.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup of a stray temporary symlink.
        }
    }
}
