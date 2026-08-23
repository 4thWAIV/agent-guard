// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// A protected file that existed before the call and was removed by it. There are no post-call bytes.
/// </summary>
/// <param name="Path">The absolute path of the removed file.</param>
/// <param name="Before">The pre-call bytes.</param>
public sealed record FileRemoved(string Path, ReadOnlyMemory<byte> Before) : FileChange(Path);
