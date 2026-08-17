// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// Capture could not snapshot a protected file the call could touch, so the change could not be reverted if it
/// landed. The Engine must deny the call: no protection means no permission.
/// </summary>
/// <param name="Reason">Why the pre-image could not be captured.</param>
public sealed record CaptureFailed(string Reason) : CaptureResult;
