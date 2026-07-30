// Copyright (c) 4thWAIV. All rights reserved.

using System.Runtime.InteropServices;

namespace AgentGuard.Setup;

/// <summary>
/// The POSIX <c>rename(2)</c> binding. Atomically replacing an existing symlink with another — which is how the
/// <c>current</c> version pointer is flipped without a window where it is absent — is a single <c>rename</c>; the
/// managed <c>File.Move</c>/<c>Directory.Move</c> APIs do not replace a directory symlink atomically. This build
/// targets macOS (and Linux); the platform's libc supplies <c>rename</c>.
/// </summary>
internal static partial class NativeInterop
{
    /// <summary>
    /// Atomically renames <paramref name="oldPath"/> to <paramref name="newPath"/>, replacing any existing entry
    /// at the destination on the same filesystem.
    /// </summary>
    /// <param name="oldPath">The source path.</param>
    /// <param name="newPath">The destination path.</param>
    /// <returns>Zero on success; a negative value with <c>errno</c> set on failure.</returns>
    [LibraryImport("libc", EntryPoint = "rename", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int Rename(string oldPath, string newPath);
}
