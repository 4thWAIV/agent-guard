// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions.Contracts;

/// <summary>
/// Turns a path into the single canonical form every match is done against: absolute, with <c>.</c> and
/// <c>..</c> segments and <b>symlinks</b> resolved. Without this, a symlink to a protected file slips past an
/// exact-path match, so every write target and every matcher pattern must pass through here first.
/// </summary>
public interface IPathCanonicalizer
{
    /// <summary>
    /// Resolves a path to its canonical absolute form, following symlinks.
    /// </summary>
    /// <param name="path">The raw path to canonicalize.</param>
    /// <returns>The absolute, symlink-resolved canonical path.</returns>
    CanonicalPath Canonicalize(string path);
}
