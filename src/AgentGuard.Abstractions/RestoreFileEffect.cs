// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// An <see cref="Effect"/> that restores a file to its pre-call bytes, undoing an unauthorized change.
/// </summary>
/// <param name="Path">The absolute path of the file to restore.</param>
/// <param name="Content">The pre-call bytes to write back.</param>
public sealed record RestoreFileEffect(string Path, ReadOnlyMemory<byte> Content) : Effect(Path);
