// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// Bridges a specific host runtime to the Engine in both directions: it normalizes the host's raw hook payload
/// into a host-agnostic <see cref="NormalizedCall"/> — the single place that classifies each tool's input into
/// the typed <see cref="ToolInput"/> form, so no Guard parses raw payloads — and it renders the aggregate
/// verdict back into that host's exit-code protocol. One implementation exists per supported host (Claude Code,
/// Codex). It fails closed: an unparsable payload yields an unparsable result the Engine denies on.
/// </summary>
public interface IHostAdapter
{
    /// <summary>
    /// Gets the identifier of the host runtime this adapter handles.
    /// </summary>
    string Host { get; }

    /// <summary>
    /// Parses a raw hook payload into a normalized call, or reports that it could not be parsed.
    /// </summary>
    /// <param name="hookEvent">The lifecycle event the payload belongs to.</param>
    /// <param name="rawPayload">The raw payload the host wrote to standard input.</param>
    /// <returns>The parsed call, or an unparsable result the Engine must deny on.</returns>
    HostReadResult Read(HookEvent hookEvent, string rawPayload);

    /// <summary>
    /// Renders an aggregate verdict into this host's decision output — its exit code and any message — in the
    /// host-specific protocol.
    /// </summary>
    /// <param name="verdict">The aggregate verdict for the call.</param>
    /// <param name="hookEvent">The lifecycle event being answered.</param>
    /// <returns>The host-specific decision to emit.</returns>
    HostDecision Render(Verdict verdict, HookEvent hookEvent);
}
