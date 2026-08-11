// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine;

/// <summary>
/// One immediate child of a directory, reduced to exactly what <see cref="ProtectedFileScanner"/>'s policy needs —
/// its full path, whether it is a directory, and whether it is a reparse point (symlink/junction) — so the scanner
/// never touches <c>FileSystemInfo</c> or <c>FileAttributes</c>. It is the value <see cref="IDirectoryEnumerator"/>
/// yields; it lives in its own file to satisfy the one-type-per-file analyzer, beside the seam it belongs to.
/// </summary>
/// <param name="FullPath">The child's absolute path.</param>
/// <param name="IsDirectory">Whether the child is a directory.</param>
/// <param name="IsReparsePoint">Whether the child is a reparse point (symlink/junction).</param>
internal readonly record struct DirectoryChild(string FullPath, bool IsDirectory, bool IsReparsePoint);
