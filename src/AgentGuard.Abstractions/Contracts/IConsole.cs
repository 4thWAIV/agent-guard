// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned seam over <c>System.Console</c>, following the shape of the type it wraps so console output and input
/// are captured in tests instead of hitting the real streams.
/// </summary>
public interface IConsole
{
    /// <summary>
    /// Writes text to standard output without a trailing newline.
    /// </summary>
    /// <param name="text">The text to write.</param>
    void Write(string text);

    /// <summary>
    /// Writes text to standard output followed by a newline.
    /// </summary>
    /// <param name="text">The text to write.</param>
    void WriteLine(string text);

    /// <summary>
    /// Writes text to standard error without a trailing newline.
    /// </summary>
    /// <param name="text">The text to write.</param>
    void ErrorWrite(string text);

    /// <summary>
    /// Writes text to standard error followed by a newline.
    /// </summary>
    /// <param name="text">The text to write.</param>
    void ErrorWriteLine(string text);

    /// <summary>
    /// Reads standard input to its end.
    /// </summary>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the full text read from standard input.</returns>
    Task<string> ReadToEndAsync(CancellationToken ct);
}
