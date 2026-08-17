// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The Windows native bindings for the atomic in-place symlink re-point. A version pointer is a directory symlink,
/// and neither <c>MoveFileEx</c> nor the newer <c>FileRenameInfoEx</c> can rename over a directory, so re-pointing
/// an existing link must NOT use a rename (windows-atomic-repoint). Instead the link's reparse data is rewritten in
/// place: open a handle to the existing link with <c>FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS</c>
/// (<see cref="CreateFile"/>) and set the new target with <c>DeviceIoControl(FSCTL_SET_REPARSE_POINT)</c> — atomic,
/// and the link is never momentarily absent. Bindings use the Unicode (<c>*W</c>) entry point with UTF-16
/// marshalling, <c>SetLastError</c>, a System32-only search path, and explicit bool-return marshalling
/// (scan-resolutions #2, Tim approved 2026-08-09). Native interop lives ONLY in per-OS impl assemblies.
/// </summary>
internal static partial class WindowsNativeMethods
{
    /// <summary>Right to write data/attributes, required to set a reparse point.</summary>
    internal const uint GenericWrite = 0x40000000;

    /// <summary>FILE_READ_ATTRIBUTES: the minimal right needed to query a directory's case-sensitivity flag.</summary>
    internal const uint FileReadAttributes = 0x00000080;

    /// <summary>
    /// FileCaseSensitiveInfo: the FILE_INFO_BY_HANDLE_CLASS value (23) that reads the per-directory case-sensitivity
    /// flag through <see cref="GetFileInformationByHandleEx"/>.
    /// </summary>
    internal const int FileCaseSensitiveInfo = 23;

    /// <summary>
    /// FILE_CS_FLAG_CASE_SENSITIVE_DIR: the flag in FILE_CASE_SENSITIVE_INFO set when the directory is case-sensitive.
    /// </summary>
    internal const uint FileCsFlagCaseSensitiveDir = 0x00000001;

    /// <summary>FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE.</summary>
    internal const uint FileShareReadWriteDelete = 0x00000001 | 0x00000002 | 0x00000004;

    /// <summary>OPEN_EXISTING: open the link only if it already exists.</summary>
    internal const uint OpenExisting = 3;

    /// <summary>FILE_FLAG_BACKUP_SEMANTICS: required to obtain a handle to a directory (a directory symlink).</summary>
    internal const uint FileFlagBackupSemantics = 0x02000000;

    /// <summary>FILE_FLAG_OPEN_REPARSE_POINT: open the link itself rather than following it to its target.</summary>
    internal const uint FileFlagOpenReparsePoint = 0x00200000;

    /// <summary>FSCTL_SET_REPARSE_POINT: rewrites a reparse point's data in place.</summary>
    internal const uint FsctlSetReparsePoint = 0x000900A4;

    /// <summary>
    /// Opens a handle to an existing filesystem entry.
    /// </summary>
    /// <param name="fileName">The path to open.</param>
    /// <param name="desiredAccess">The requested access rights.</param>
    /// <param name="shareMode">The sharing mode.</param>
    /// <param name="securityAttributes">Security attributes, or <see cref="IntPtr.Zero"/> for none.</param>
    /// <param name="creationDisposition">The creation disposition (for example <see cref="OpenExisting"/>).</param>
    /// <param name="flagsAndAttributes">The flags and attributes.</param>
    /// <param name="templateFile">A template handle, or <see cref="IntPtr.Zero"/> for none.</param>
    /// <returns>A handle to the entry; <see cref="SafeHandle.IsInvalid"/> is <see langword="true"/> on failure with
    /// the last Win32 error set.</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    /// <summary>
    /// Sends a control code to a device or filesystem — here, <see cref="FsctlSetReparsePoint"/> to rewrite a
    /// link's reparse data in place.
    /// </summary>
    /// <param name="device">The open handle to the link.</param>
    /// <param name="ioControlCode">The control code.</param>
    /// <param name="inBuffer">The input buffer (the reparse data to set), or <see langword="null"/> for none.</param>
    /// <param name="inBufferSize">The input buffer's byte length.</param>
    /// <param name="outBuffer">The output buffer, or <see langword="null"/> for none.</param>
    /// <param name="outBufferSize">The output buffer's byte length.</param>
    /// <param name="bytesReturned">The number of bytes written to the output buffer.</param>
    /// <param name="overlapped">An overlapped structure, or <see cref="IntPtr.Zero"/> for a synchronous call.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/> with the last Win32 error set.</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeviceIoControl(
        SafeFileHandle device,
        uint ioControlCode,
        byte[]? inBuffer,
        uint inBufferSize,
        byte[]? outBuffer,
        uint outBufferSize,
        out uint bytesReturned,
        IntPtr overlapped);

    /// <summary>
    /// Reads a file/directory information class off an open handle — here <see cref="FileCaseSensitiveInfo"/>, whose
    /// FILE_CASE_SENSITIVE_INFO payload is a single ULONG of flags marshalled as <paramref name="fileInformation"/>.
    /// </summary>
    /// <param name="handle">The open handle to the directory.</param>
    /// <param name="fileInformationClass">The FILE_INFO_BY_HANDLE_CLASS value (for example
    /// <see cref="FileCaseSensitiveInfo"/>).</param>
    /// <param name="fileInformation">Receives the returned flags (the FILE_CASE_SENSITIVE_INFO ULONG).</param>
    /// <param name="bufferSize">The byte length of <paramref name="fileInformation"/> (four bytes).</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/> with the last Win32 error set.</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "GetFileInformationByHandleEx", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetFileInformationByHandleEx(
        SafeFileHandle handle,
        int fileInformationClass,
        out uint fileInformation,
        uint bufferSize);
}
