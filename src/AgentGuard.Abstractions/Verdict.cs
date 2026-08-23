// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// A Guard's judgment about a tool call: allow it, deny it with a reason, or allow it with an advisory warning.
/// </summary>
public sealed record Verdict
{
    private Verdict(VerdictKind kind, string? message)
    {
        this.Kind = kind;
        this.Message = message;
    }

    /// <summary>
    /// Gets the kind of judgment this verdict represents.
    /// </summary>
    public VerdictKind Kind { get; }

    /// <summary>
    /// Gets the deny reason or advisory message, or <see langword="null"/> for a plain allow.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Creates a verdict that permits the call to proceed.
    /// </summary>
    /// <returns>An allow verdict.</returns>
    public static Verdict Allow() => new(VerdictKind.Allow, message: null);

    /// <summary>
    /// Creates a verdict that blocks the call.
    /// </summary>
    /// <param name="reason">The human-readable reason the call was blocked.</param>
    /// <returns>A deny verdict carrying the reason.</returns>
    public static Verdict Deny(string reason) => new(VerdictKind.Deny, reason);

    /// <summary>
    /// Creates a verdict that lets the call proceed while surfacing an advisory message.
    /// </summary>
    /// <param name="message">The advisory message to surface.</param>
    /// <returns>A warn verdict carrying the message.</returns>
    public static Verdict Warn(string message) => new(VerdictKind.Warn, message);
}
