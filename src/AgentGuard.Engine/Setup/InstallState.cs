// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The machine install record persisted at <c>~/.agentguard/state.json</c>: the installed version and the
/// SHA-256 of the installed binary. The hook integrity self-check compares the running binary against this hash.
/// </summary>
/// <param name="Version">The installed, normalized version.</param>
/// <param name="Sha256">The lowercase hex SHA-256 of the installed binary.</param>
internal sealed record InstallState(string Version, string Sha256);
