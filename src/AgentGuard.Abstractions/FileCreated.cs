// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// A protected file that did not exist before the call and was created by it. There are no pre-call bytes.
/// </summary>
/// <param name="Path">The absolute path of the created file.</param>
/// <param name="After">The bytes the call wrote.</param>
public sealed record FileCreated(string Path, ReadOnlyMemory<byte> After) : FileChange(Path);
