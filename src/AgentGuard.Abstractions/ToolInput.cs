// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The typed input of a tool call, restricted to the kinds the guard acts on. This is a closed set — only the
/// cases declared in this assembly exist — so a Guard that pattern-matches it can never be handed an unmodeled
/// shape. A tool the guard does not act on carries no input at all (see <see cref="ToolCall.Input"/>).
/// </summary>
public abstract record ToolInput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolInput"/> class. Declared <c>private protected</c> so the
    /// set of cases stays closed to this assembly.
    /// </summary>
    private protected ToolInput()
    {
    }
}
