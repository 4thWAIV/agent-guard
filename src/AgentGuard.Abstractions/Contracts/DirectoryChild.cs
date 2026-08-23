// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// One immediate child of a directory, reduced to exactly what the scanner's policy needs — its full path,
/// whether it is a directory, and whether it is a reparse point (symlink/junction) — so callers never touch
/// <c>FileSystemInfo</c> or <c>FileAttributes</c>. It is the value <see cref="IDirectoryEnumerator"/> yields.
/// </summary>
/// <param name="FullPath">The child's absolute path.</param>
/// <param name="IsDirectory">Whether the child is a directory.</param>
/// <param name="IsReparsePoint">Whether the child is a reparse point (symlink/junction).</param>
public readonly record struct DirectoryChild(string FullPath, bool IsDirectory, bool IsReparsePoint);
