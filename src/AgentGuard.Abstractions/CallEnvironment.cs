// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The surroundings of a tool call: where it runs and which event it belongs to. This is the design's
/// "Environment"; it is named <see cref="CallEnvironment"/> to avoid colliding with <see cref="System.Environment"/>.
/// </summary>
/// <param name="ProjectRoot">The absolute path to the root of the repository the call runs against.</param>
/// <param name="Event">The host lifecycle event this invocation corresponds to.</param>
/// <param name="SessionId">The host session identifier, when the host supplies one.</param>
public sealed record CallEnvironment(string ProjectRoot, HookEvent Event, string? SessionId);
