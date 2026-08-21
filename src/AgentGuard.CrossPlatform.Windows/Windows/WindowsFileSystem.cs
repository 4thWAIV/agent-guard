// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.CrossPlatform;
using Microsoft.Win32.SafeHandles;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The Windows implementation of <see cref="IPlatformFileSystem"/>: the one per-OS owner of the OS-divergent
/// primitives (AG0101, ag0101-one-owner-per-os) — the symlink reads, the in-place reparse re-point, and the
/// <c>FileInfo</c>/<c>DirectoryInfo</c> construction point. Windows authors its own file-system source (it does NOT
/// link the POSIX source) because a version pointer is a directory symlink and neither <c>MoveFileEx</c> nor
/// <c>FileRenameInfoEx</c> can rename over a directory: creating a fresh link uses
/// <see cref="Directory.CreateSymbolicLink(string, string)"/>, while re-pointing an existing link rewrites its reparse
/// data in place with <c>DeviceIoControl(FSCTL_SET_REPARSE_POINT)</c> so the link is never momentarily absent
/// (windows-atomic-repoint). Windows carries a directory-vs-file distinction that POSIX does not, so a fresh link
/// infers its kind from the target. Its OS-UNIFORM operations (create a directory, delete a file/link, existence
/// checks, the case-sensitivity probe) are NOT authored here — they go through the injected
/// <see cref="PlatformFileSystemShared"/>, which routes each through an owned adapter, so this class makes no raw
/// OS-uniform filesystem call. Windows has no executable bit, so <see cref="NeedsExecutableFlag"/> is
/// <see langword="false"/> and the executable-flag methods throw.
/// </summary>
internal sealed class WindowsFileSystem : PlatformFileSystemBase, IPlatformFileSystem
{
    // ERROR_PRIVILEGE_NOT_HELD (1314): the failure Windows raises for a symlink operation when the user is neither
    // an administrator nor has Developer Mode enabled — the only case this message advises on. It surfaces as a raw
    // Win32 error from GetLastPInvokeError on the native re-point path, and as its HRESULT (0x80070522) on the
    // managed IOException from Directory/File.CreateSymbolicLink.
    private const int PrivilegeNotHeldWin32Error = 1314;

    private const int PrivilegeNotHeldHResult = unchecked((int)0x80070522);

    // IO_REPARSE_TAG_SYMLINK: the reparse tag of an NTFS symbolic link (as opposed to a mount-point/junction).
    private const uint IoReparseTagSymlink = 0xA000000C;

    // SYMLINK_FLAG_RELATIVE: the stored substitute name is a relative path (no \??\ prefix).
    private const uint SymlinkFlagRelative = 0x00000001;

    private const string DeveloperModeMessage =
        "Creating the guard's version-pointer symlink requires privilege on Windows. Run as an administrator, or "
        + "enable Developer Mode (Settings > Privacy & security > For developers), then re-run the command.";

    private readonly PlatformFileSystemShared _shared;
    private readonly IFileInfoFactory _factory;

    // Private constructor (AG0003): the per-OS PlatformServices.Create() is the only thing that builds it, through
    // Create().
    private WindowsFileSystem(PlatformFileSystemShared shared, IFileInfoFactory factory)
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

        // A version pointer is a directory symlink, which MoveFileEx cannot rename over. So a fresh link is created
        // directly, and an existing link is re-pointed by rewriting its reparse data in place (never absent, never a
        // rename-over-a-directory). The raw relative target is stored verbatim (forward slashes preserved) and reads
        // back unchanged.
        if (IsLinkTarget(linkPath))
        {
            RepointExistingLink(linkPath, relativeTarget);
        }
        else
        {
            CreateFreshLink(linkPath, relativeTarget);
        }
    }

    /// <inheritdoc/>
    public void RemoveLinkTarget(string linkPath)
    {
        _shared.RefuseIfRealEntry(linkPath, "remove", IsLinkTarget(linkPath));

        // Removes the link itself (the directory-symlink delete for a directory link, the file-symlink delete for a
        // file link) through the owned adapters; each removes the link and never follows it, so the target is
        // untouched. A no-op when absent.
        _shared.DeleteLinkEntry(linkPath, IsLinkTarget(linkPath));
    }

    /// <inheritdoc/>
    public bool NeedsExecutableFlag() => false;

    /// <inheritdoc/>
    public bool IsExecutable(string path) =>
        throw new PlatformNotSupportedException("Windows has no executable bit; guard IsExecutable on NeedsExecutableFlag().");

    /// <inheritdoc/>
    public void MakeExecutable(string path) =>
        throw new PlatformNotSupportedException("Windows has no executable bit; guard MakeExecutable on NeedsExecutableFlag().");

    /// <inheritdoc/>
    public void MakeNonExecutable(string path) =>
        throw new PlatformNotSupportedException("Windows has no executable bit; guard MakeNonExecutable on NeedsExecutableFlag().");

    /// <inheritdoc/>
    public bool IsCaseSensitive(string path)
    {
        // Native-first (case-sensitivity-detected-per-filesystem): Windows answers per-directory with
        // GetFileInformationByHandleEx(FileCaseSensitiveInfo). When the query is unavailable (an older Windows or a
        // non-NTFS volume that does not support the per-directory flag) it falls back to the shared read-only probe,
        // whose own documented case-sensitive default is the last resort.
        if (TryQueryCaseSensitive(path, out bool caseSensitive))
        {
            return caseSensitive;
        }

        return _shared.ProbeCaseSensitive(path);
    }

    /// <summary>
    /// Creates the Windows file system, wired to the shared OS-uniform helper it routes its uniform calls through and
    /// the wrapper factory the shared base reads the OS-uniform symlink target through.
    /// </summary>
    /// <param name="shared">The shared OS-uniform file operations, built once by the platform factory.</param>
    /// <param name="factory">The wrapper factory the symlink-target read obtains <c>*Info</c> views through.</param>
    /// <returns>The file system, as its interface.</returns>
    internal static IPlatformFileSystem Create(PlatformFileSystemShared shared, IFileInfoFactory factory) =>
        new WindowsFileSystem(shared, factory);

    // Reads the NTFS per-directory case-sensitivity flag natively: open a handle to the directory (a directory needs
    // FILE_FLAG_BACKUP_SEMANTICS), query FileCaseSensitiveInfo, and test FILE_CS_FLAG_CASE_SENSITIVE_DIR. Returns false
    // (so the caller falls back to the probe) when the handle cannot be opened or the query is unsupported. The native
    // interop calls are OS-divergent and owned by this per-OS class (AG0101).
    private static bool TryQueryCaseSensitive(string path, out bool caseSensitive)
    {
        caseSensitive = false;

        using SafeFileHandle handle = WindowsNativeMethods.CreateFile(
            path,
            WindowsNativeMethods.FileReadAttributes,
            WindowsNativeMethods.FileShareReadWriteDelete,
            IntPtr.Zero,
            WindowsNativeMethods.OpenExisting,
            WindowsNativeMethods.FileFlagBackupSemantics,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            return false;
        }

        if (!WindowsNativeMethods.GetFileInformationByHandleEx(
                handle,
                WindowsNativeMethods.FileCaseSensitiveInfo,
                out uint flags,
                sizeof(uint)))
        {
            return false;
        }

        caseSensitive = (flags & WindowsNativeMethods.FileCsFlagCaseSensitiveDir) != 0;
        return true;
    }

    private static void ThrowIfPrivilegeNotHeld(int error)
    {
        if (error == PrivilegeNotHeldWin32Error)
        {
            throw new UnauthorizedAccessException(DeveloperModeMessage);
        }
    }

    private static void RepointExistingLink(string linkPath, string relativeTarget)
    {
        // Rewrite the existing symbolic link's reparse data with the new target. FSCTL_SET_REPARSE_POINT replaces
        // the link's target atomically in place — the link is never momentarily absent, and it sidesteps the
        // rename-over-a-directory restriction because nothing is renamed. The reparse buffer stores the raw relative
        // target verbatim (as Directory.CreateSymbolicLink does for a fresh link), so a re-point reads back
        // byte-identical to a create.
        byte[] reparseData = BuildSymbolicLinkReparseData(relativeTarget);
        using SafeFileHandle handle = OpenLinkForReparseWrite(linkPath);

        if (!WindowsNativeMethods.DeviceIoControl(
                handle,
                WindowsNativeMethods.FsctlSetReparsePoint,
                reparseData,
                (uint)reparseData.Length,
                outBuffer: null,
                outBufferSize: 0,
                out _,
                IntPtr.Zero))
        {
            int error = Marshal.GetLastPInvokeError();
            ThrowIfPrivilegeNotHeld(error);

            throw new IOException(
                $"Failed to atomically re-point '{linkPath}' at '{relativeTarget}' (Win32 error {error}).");
        }
    }

    private static SafeFileHandle OpenLinkForReparseWrite(string linkPath)
    {
        SafeFileHandle handle = WindowsNativeMethods.CreateFile(
            linkPath,
            WindowsNativeMethods.GenericWrite,
            WindowsNativeMethods.FileShareReadWriteDelete,
            IntPtr.Zero,
            WindowsNativeMethods.OpenExisting,
            WindowsNativeMethods.FileFlagBackupSemantics | WindowsNativeMethods.FileFlagOpenReparsePoint,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            ThrowIfPrivilegeNotHeld(error);

            throw new IOException($"Failed to open the link '{linkPath}' to re-point it (Win32 error {error}).");
        }

        return handle;
    }

    private static byte[] BuildSymbolicLinkReparseData(string relativeTarget)
    {
        // REPARSE_DATA_BUFFER for a symbolic link:
        //   ReparseTag (UInt32) | ReparseDataLength (UInt16) | Reserved (UInt16)
        //   then SymbolicLinkReparseBuffer:
        //   SubstituteNameOffset (UInt16) | SubstituteNameLength (UInt16) |
        //   PrintNameOffset (UInt16) | PrintNameLength (UInt16) | Flags (UInt32) | PathBuffer
        // The PathBuffer holds the substitute name followed by the print name; for a relative link both are the raw
        // target verbatim (no \??\ prefix) with SYMLINK_FLAG_RELATIVE set, matching what CreateSymbolicLink records.
        byte[] nameBytes = Encoding.Unicode.GetBytes(relativeTarget);
        ushort nameLength = (ushort)nameBytes.Length;

        const int reparseHeaderSize = 8;   // ReparseTag + ReparseDataLength + Reserved
        const int symlinkHeaderSize = 12;  // the four USHORT name fields + the ULONG Flags
        ushort reparseDataLength = (ushort)(symlinkHeaderSize + (nameBytes.Length * 2));

        byte[] buffer = new byte[reparseHeaderSize + reparseDataLength];
        Span<byte> span = buffer;

        BinaryPrimitives.WriteUInt32LittleEndian(span[0..], IoReparseTagSymlink);
        BinaryPrimitives.WriteUInt16LittleEndian(span[4..], reparseDataLength);
        BinaryPrimitives.WriteUInt16LittleEndian(span[6..], 0);              // Reserved
        BinaryPrimitives.WriteUInt16LittleEndian(span[8..], 0);              // SubstituteNameOffset
        BinaryPrimitives.WriteUInt16LittleEndian(span[10..], nameLength);    // SubstituteNameLength
        BinaryPrimitives.WriteUInt16LittleEndian(span[12..], nameLength);    // PrintNameOffset (after substitute name)
        BinaryPrimitives.WriteUInt16LittleEndian(span[14..], nameLength);    // PrintNameLength
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..], SymlinkFlagRelative);

        int pathBufferStart = reparseHeaderSize + symlinkHeaderSize; // 20
        nameBytes.CopyTo(span[pathBufferStart..]);                          // SubstituteName
        nameBytes.CopyTo(span[(pathBufferStart + nameBytes.Length)..]);     // PrintName
        return buffer;
    }

    private void CreateFreshLink(string linkPath, string relativeTarget)
    {
        // Windows records a directory symlink and a file symlink distinctly and needs the right one for the link to
        // resolve, so the kind is inferred from the resolved target's existence — through the owned directory
        // enumerator. The relative target is resolved against the link's own (absolute) directory with the pure
        // two-argument Path.GetFullPath, so no current-directory read is involved. The CreateSymbolicLink calls are
        // OS-divergent and owned by this per-OS class (AG0101). Only the privilege-not-held mapping to the
        // Developer-Mode guidance is Windows-specific.
        string linkDirectory = Path.GetDirectoryName(linkPath)!;

        try
        {
            if (_shared.DirectoryExists(Path.GetFullPath(relativeTarget, linkDirectory)))
            {
                Directory.CreateSymbolicLink(linkPath, relativeTarget);
            }
            else
            {
                File.CreateSymbolicLink(linkPath, relativeTarget);
            }
        }
        catch (IOException exception) when (exception.HResult == PrivilegeNotHeldHResult)
        {
            throw new UnauthorizedAccessException(DeveloperModeMessage, exception);
        }
    }
}
