// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// Capture snapshotted every protected file the call could touch; the call may proceed.
/// </summary>
/// <param name="CapturedPaths">The absolute paths whose pre-call bytes were snapshotted.</param>
public sealed record CaptureSucceeded(IReadOnlyList<string> CapturedPaths) : CaptureResult;
