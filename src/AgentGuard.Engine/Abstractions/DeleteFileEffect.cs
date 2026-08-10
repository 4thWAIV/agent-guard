// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// An <see cref="Effect"/> that deletes a file the call created, undoing an unauthorized creation.
/// </summary>
/// <param name="Path">The absolute path of the file to delete.</param>
public sealed record DeleteFileEffect(string Path) : Effect(Path);
