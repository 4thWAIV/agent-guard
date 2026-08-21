// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform.Posix;

/// <summary>
/// The POSIX implementation of <see cref="IPlatformFileSystem"/>: the one per-OS owner of the OS-divergent
/// primitives (AG0101, ag0101-one-owner-per-os) — the symlink reads, the libc <c>rename</c> atomic re-point, the POSIX
/// executable bit, the raw directory separator, and the native case-sensitivity query. This is the shared POSIX source —
/// authored here for macOS and linked into the Linux impl unchanged (ONE source, not duplicated, not collapsed to a
/// shared base class). Only the Windows impl, which needs an in-place reparse re-point, is written separately. Its
/// OS-UNIFORM operations (create a directory, delete a file, existence checks, the case-sensitivity fallback probe) are
/// NOT authored here — they go through the injected <see cref="PlatformFileSystemShared"/>, which routes each through an
/// owned adapter, so this class makes no raw OS-uniform filesystem call. It obtains <c>*Info</c> views through the
/// injected <see cref="IFileInfoFactory"/> seam. The executable-flag methods read and write the Unix file mode; the OS
/// guard on each states the impl runs only on POSIX, which lets the managed Unix-mode calls be reached and satisfies the
/// platform compatibility analyzer without a suppression.
/// </summary>
internal sealed class PosixFileSystem : PlatformFileSystemBase, IPlatformFileSystem
{
    // The macOS pathconf name _PC_CASE_SENSITIVE, verified against the SDK header
    // (/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk/usr/include/sys/unistd.h: "#define _PC_CASE_SENSITIVE 11").
    // It is a macOS-specific constant; Linux has no such query, so it is used only under the OperatingSystem.IsMacOS()
    // guard below.
    private const int PosixCaseSensitiveName = 11;

    private const UnixFileMode ExecuteBits =
        UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

    private const string WindowsNotSupported =
        "The POSIX file system implementation does not run on Windows; the executable bit is a POSIX concept.";

    private readonly PlatformFileSystemShared _shared;
    private readonly IFileInfoFactory _factory;

    // Private constructor (AG0003): the per-OS PlatformServices.Create() is the only thing that builds it, through
    // Create().
    private PosixFileSystem(PlatformFileSystemShared shared, IFileInfoFactory factory)
    {
        _shared = shared;
        _factory = factory;
    }

    /// <inheritdoc/>
    // The BCL already gives the answer; this is now an owned read of the raw Path.DirectorySeparatorChar field
    // (AG0020 owner: the IPlatformFileSystem implementers), not a hardcode.
    public char DirectorySeparator => Path.DirectorySeparatorChar;

    /// <inheritdoc />
    // The wrapper factory this per-OS class injects directly (fileinfo-factory-breaks-the-cycle); the shared base reads
    // the OS-uniform symlink target through it, so IsLinkTarget/ReadLinkTarget live once on the base, not here.
    protected override IFileInfoFactory Factory => _factory;

    /// <inheritdoc/>
    public void MakeLinkTarget(string linkPath, string relativeTarget)
    {
        _shared.RefuseIfRealEntry(linkPath, "point", IsLinkTarget(linkPath));
        _shared.CreateDirectory(Path.GetDirectoryName(linkPath)!);

        // Write the new link to a unique temporary sibling in the same directory (so the rename stays on one
        // filesystem), then rename it over the destination. rename(2) replaces an existing symlink atomically, so
        // the link at linkPath is never momentarily absent — and creating over an absent destination just creates
        // it. The temporary is consumed by the rename, so no stray sibling is ever left behind.
        string temporaryPath = _shared.TemporarySiblingPath(linkPath);
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
        _shared.RefuseIfRealEntry(linkPath, "remove", IsLinkTarget(linkPath));

        // On POSIX, deleting a symlink unlinks the link itself and never follows it, so the target and its contents
        // are untouched. The owned file-delete maps to unlink(2) and removes a link to a directory as well as a link to
        // a file. A no-op when the link is already absent.
        _shared.DeleteFile(linkPath);
    }

    /// <inheritdoc/>
    public bool NeedsExecutableFlag() => true;

    /// <inheritdoc/>
    public bool IsExecutable(string path)
    {
        if (!IsPosix())
        {
            throw new PlatformNotSupportedException(WindowsNotSupported);
        }

        return (File.GetUnixFileMode(path) & UnixFileMode.UserExecute) != UnixFileMode.None;
    }

    /// <inheritdoc/>
    public void MakeExecutable(string path)
    {
        if (!IsPosix())
        {
            throw new PlatformNotSupportedException(WindowsNotSupported);
        }

        File.SetUnixFileMode(path, File.GetUnixFileMode(path) | ExecuteBits);
    }

    /// <inheritdoc/>
    public void MakeNonExecutable(string path)
    {
        if (!IsPosix())
        {
            throw new PlatformNotSupportedException(WindowsNotSupported);
        }

        File.SetUnixFileMode(path, File.GetUnixFileMode(path) & ~ExecuteBits);
    }

    /// <inheritdoc/>
    public bool IsCaseSensitive(string path)
    {
        // Native-first (case-sensitivity-detected-per-filesystem): macOS answers directly with
        // pathconf(path, _PC_CASE_SENSITIVE). The constant is macOS-specific and Linux has no such query, so the native
        // query runs only on macOS; on Linux, and whenever pathconf cannot answer (returns -1), the shared read-only
        // probe is the fallback, and the probe's own documented case-sensitive default is the last resort.
        if (OperatingSystem.IsMacOS())
        {
            long native = PosixNativeMethods.PathConf(path, PosixCaseSensitiveName);
            if (native >= 0)
            {
                // pathconf returns 1 when the file system is case-sensitive, 0 when it is not.
                return native == 1;
            }
        }

        return _shared.ProbeCaseSensitive(path);
    }

    /// <summary>
    /// Creates the POSIX file system, wired to the shared OS-uniform helper it routes its uniform calls through and the
    /// wrapper factory the shared base reads the OS-uniform symlink target through.
    /// </summary>
    /// <param name="shared">The shared OS-uniform file operations, built once by the platform factory.</param>
    /// <param name="factory">The wrapper factory the symlink-target read obtains <c>*Info</c> views through.</param>
    /// <returns>The file system, as its interface.</returns>
    internal static IPlatformFileSystem Create(PlatformFileSystemShared shared, IFileInfoFactory factory) =>
        new PosixFileSystem(shared, factory);

    // The single Windows guard for the three POSIX-only executable-bit methods. [UnsupportedOSPlatformGuard("windows")]
    // states that a true result guarantees the code is not on Windows, so the File.Get/SetUnixFileMode calls each
    // method reaches after this guard passes are proven safe to CA1416 without a suppression.
    [UnsupportedOSPlatformGuard("windows")]
    private static bool IsPosix() => !OperatingSystem.IsWindows();

    private void TryRemoveLink(string linkPath)
    {
        try
        {
            _shared.DeleteFile(linkPath);
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
