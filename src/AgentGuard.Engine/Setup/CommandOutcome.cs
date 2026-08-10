// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.Setup;

/// <summary>
/// The result of a mutating setup command (<c>install</c>, <c>init</c>, <c>remove</c>): whether it succeeded and
/// the lines to print.
/// </summary>
public sealed record CommandOutcome
{
    private CommandOutcome(bool success, IReadOnlyList<string> messages)
    {
        Success = success;
        Messages = messages;
    }

    /// <summary>
    /// Gets a value indicating whether the command succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the human-readable lines describing what happened.
    /// </summary>
    public IReadOnlyList<string> Messages { get; }

    /// <summary>
    /// Gets the process exit code: zero on success, otherwise one.
    /// </summary>
    public int ExitCode => Success ? 0 : 1;

    /// <summary>
    /// Creates a success outcome.
    /// </summary>
    /// <param name="messages">The lines to print.</param>
    /// <returns>A success outcome.</returns>
    internal static CommandOutcome Ok(IReadOnlyList<string> messages) => new(true, messages);

    /// <summary>
    /// Creates a failure outcome.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A failure outcome.</returns>
    internal static CommandOutcome Failed(string message) => new(false, new[] { message });
}
