// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// A protected file that existed before and after the call, but whose bytes changed.
/// </summary>
/// <param name="Path">The absolute path of the modified file.</param>
/// <param name="Before">The pre-call bytes.</param>
/// <param name="After">The post-call bytes.</param>
public sealed record FileModified(string Path, ReadOnlyMemory<byte> Before, ReadOnlyMemory<byte> After) : FileChange(Path);
