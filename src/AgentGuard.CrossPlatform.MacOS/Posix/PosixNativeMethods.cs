// Copyright (c) 4thWAIV. All rights reserved.

using System.Runtime.InteropServices;

namespace AgentGuard.CrossPlatform.Posix;

/// <summary>
/// The POSIX <c>rename(2)</c> binding. Atomically replacing an existing symlink with another — which is how a
/// version pointer is flipped without a window where it is absent — is a single <c>rename</c>; the managed
/// <c>File.Move</c>/<c>Directory.Move</c> APIs do not replace a directory symlink atomically. The same libc
/// <c>rename</c> serves macOS and Linux, so this source is authored once here and linked into the Linux impl
/// unchanged. Native interop lives ONLY in per-OS impl assemblies — never in the contract assembly. This class is
/// <c>partial</c>: the OS-divergent <c>pathconf</c> binding lives in the macOS-only fragment
/// (<c>PosixFileSystem.MacOS.cs</c>) that only the macOS project compiles, so the Linux build never carries it.
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
}
