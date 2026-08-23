// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The outcome of Capture. It is a closed set of exactly two cases so a Guard cannot leave the result
/// ambiguous: either the pre-image was captured, or it could not be — and the latter must make the Engine deny
/// the call, because a change that cannot be snapshotted cannot be reverted.
/// </summary>
public abstract record CaptureResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureResult"/> class. Declared <c>private protected</c>
    /// so the set of cases stays closed to this assembly.
    /// </summary>
    private protected CaptureResult()
    {
    }
}
