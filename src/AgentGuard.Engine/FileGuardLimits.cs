// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine;

/// <summary>
/// The size ceilings that bound a Capture: a breach of either returns a capture failure, which denies the call.
/// </summary>
/// <param name="PerFileByteCeiling">The maximum size, in bytes, any single protected file may reach.</param>
/// <param name="TotalByteCeiling">The maximum total size, in bytes, of the whole snapshot.</param>
internal sealed record FileGuardLimits(long PerFileByteCeiling, long TotalByteCeiling);
