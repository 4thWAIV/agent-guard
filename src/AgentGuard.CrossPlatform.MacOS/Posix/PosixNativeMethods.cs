// Copyright (c) 4thWAIV. All rights reserved.

using System.Runtime.InteropServices;

namespace AgentGuard.CrossPlatform.Posix;

/// <summary>
/// The POSIX <c>rename(2)</c> binding. Atomically replacing an existing symlink with another — which is how a
/// version pointer is flipped without a window where it is absent — is a single <c>rename</c>; the managed
/// <c>File.Move</c>/<c>Directory.Move</c> APIs do not replace a directory symlink atomically. The same libc
/// <c>rename</c> serves macOS and Linux, so this source is authored once here and linked into the Linux impl
/// unchanged. Native interop lives ONLY in per-OS impl assemblies — never in the contract assembly.
/// </summary>
internal static partial class PosixNativeMethods
{
    /// <summary>
    /// Atomically renames <paramref name="oldPath"/> to <paramref name="newPath"/>, replacing any existing entry
    /// at the destination on the same filesystem.
    /// </summary>
    /// <param name="oldPath">The source path.</param>
    /// <param name="newPath">The destination path.</param>
    /// <returns>Zero on success; a non-zero value with <c>errno</c> set on failure.</returns>
    [LibraryImport("libc", EntryPoint = "rename", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int Rename(string oldPath, string newPath);

    /// <summary>
    /// Queries a filesystem configuration value for <paramref name="path"/> — with name
    /// <c>_PC_CASE_SENSITIVE</c> (11 on macOS) it answers whether the file system backing the path is case-sensitive.
    /// The libc signature is <c>long pathconf(const char *path, int name)</c>; <c>long</c> is 64-bit on the LP64
    /// macOS ABI, marshalled as <see cref="long"/>. It returns 1 (case-sensitive) or 0 (not), or -1 with <c>errno</c>
    /// set when the query is unavailable or the name is unsupported (as on Linux, where this name has no meaning).
    /// </summary>
    /// <param name="path">The path whose backing file system is queried.</param>
    /// <param name="name">The configuration name (for example <c>_PC_CASE_SENSITIVE</c>).</param>
    /// <returns>The configuration value, or -1 on error with <c>errno</c> set.</returns>
    [LibraryImport("libc", EntryPoint = "pathconf", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial long PathConf(string path, int name);
}
