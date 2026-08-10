// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// A path in the one canonical form all matching is done against — absolute, with <c>.</c>/<c>..</c> and
/// symlinks resolved. It is a distinct type so a raw, unresolved path cannot reach a matcher by mistake: the
/// compiler rejects a bare string, forcing the value to come from <see cref="Contracts.IPathCanonicalizer"/>.
/// </summary>
/// <param name="Value">The canonical absolute path.</param>
public readonly record struct CanonicalPath(string Value);
