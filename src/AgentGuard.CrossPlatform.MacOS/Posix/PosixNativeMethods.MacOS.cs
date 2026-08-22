// Copyright (c) 4thWAIV. All rights reserved.

using System.Runtime.InteropServices;

namespace AgentGuard.CrossPlatform.Posix;

/// <summary>
/// The macOS-only fragment of the POSIX native bindings (os-specific-file-naming-convention): the
/// <c>pathconf(2)</c> query, compiled only into the macOS project and never link-shared into the Linux build, where
/// the <c>_PC_CASE_SENSITIVE</c> name has no meaning and the P/Invoke's generated marshalling would be
/// permanently-dead, uncoverable code. The shared <c>PosixNativeMethods</c> keeps the cross-kernel <c>rename(2)</c>
/// binding. It lives in its own file (not alongside the <c>PosixFileSystem.MacOS</c> fragment) because the
/// repository's SA1402/MA0048 house rules require one type per file.
/// </summary>
internal static partial class PosixNativeMethods
{
    /// <summary>
    /// Queries a filesystem configuration value for <paramref name="path"/> — with name
    /// <c>_PC_CASE_SENSITIVE</c> (11 on macOS) it answers whether the file system backing the path is case-sensitive.
    /// The libc signature is <c>long pathconf(const char *path, int name)</c>; <c>long</c> is 64-bit on the LP64
    /// macOS ABI, marshalled as <see cref="long"/>. It returns 1 (case-sensitive) or 0 (not), or -1 with <c>errno</c>
    /// set when the query is unavailable or the name is unsupported.
    /// </summary>
    /// <param name="path">The path whose backing file system is queried.</param>
    /// <param name="name">The configuration name (for example <c>_PC_CASE_SENSITIVE</c>).</param>
    /// <returns>The configuration value, or -1 on error with <c>errno</c> set.</returns>
    [LibraryImport("libc", EntryPoint = "pathconf", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial long PathConf(string path, int name);
}
