// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The Windows implementation of <see cref="IPlatformFileSystem"/>. Windows authors its own file-system source (it
/// does NOT link the POSIX source) because a version pointer is a directory symlink and neither <c>MoveFileEx</c>
/// nor <c>FileRenameInfoEx</c> can rename over a directory: creating a fresh link uses
/// <see cref="Directory.CreateSymbolicLink(string, string)"/>, while re-pointing an existing link rewrites its
/// reparse data in place with <c>DeviceIoControl(FSCTL_SET_REPARSE_POINT)</c> so the link is never momentarily
/// absent (windows-atomic-repoint). Windows carries a directory-vs-file distinction that POSIX does not, so a fresh
/// link infers its kind from the target (refute-round2 option B). The precondition is that the target exists when a
/// fresh link is created, so the kind can be inferred correctly — the guard's version pointers always target an
/// already-created versions directory, so the pointer resolves and traverses as a directory. This kind concern
/// stays entirely inside this implementation and never reaches the OS-uniform interface. The OS-uniform managed
/// logic (link reads, refuse-if-real-entry, the directory-vs-file link delete) is shared by composition through
/// <see cref="AgentGuard.CrossPlatform.PlatformFileSystemShared"/>, not copied. The behavior it presents is
/// identical to the POSIX impl and is proven so by the one OS-agnostic spec. Windows has no executable bit, so
/// <see cref="NeedsExecutableFlag"/> is <see langword="false"/> and the executable-flag methods throw.
/// </summary>
internal sealed class WindowsFileSystem : IPlatformFileSystem
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

    /// <inheritdoc/>
    public bool IsLinkTarget(string linkPath) => PlatformFileSystemShared.IsLinkTarget(linkPath);

    /// <inheritdoc/>
    public string? ReadLinkTarget(string linkPath) => PlatformFileSystemShared.ReadLinkTarget(linkPath);

    /// <inheritdoc/>
    public void MakeLinkTarget(string linkPath, string relativeTarget)
    {
        PlatformFileSystemShared.RefuseIfRealEntry(linkPath, "point");
        string linkDirectory = Path.GetDirectoryName(linkPath)!;
        Directory.CreateDirectory(linkDirectory);

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
            CreateSymbolicLink(linkPath, relativeTarget);
        }
    }

    /// <inheritdoc/>
    public void RemoveLinkTarget(string linkPath)
    {
        PlatformFileSystemShared.RefuseIfRealEntry(linkPath, "remove");

        // Removes the link itself (the directory-symlink call for a directory link, the file-symlink call for a file
        // link); each removes the link and never follows it, so the target is untouched. A no-op when absent.
        PlatformFileSystemShared.DeleteLinkEntry(linkPath);
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

    private static void CreateSymbolicLink(string linkPath, string relativeTarget)
    {
        // The kind inference and the fresh-create are the OS-uniform recipe shared with the test double; only the
        // privilege-not-held mapping to the Developer-Mode guidance is Windows-specific, so it stays here.
        try
        {
            PlatformFileSystemShared.CreateLinkEntry(linkPath, relativeTarget);
        }
        catch (IOException exception) when (exception.HResult == PrivilegeNotHeldHResult)
        {
            throw new UnauthorizedAccessException(DeveloperModeMessage, exception);
        }
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
}
