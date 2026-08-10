// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.Engine;

/// <summary>
/// The inputs that configure a File Guard pipeline for one project: where it runs, its time source, an optional
/// grant public-key override, and the snapshot size ceilings. When the override is empty the Engine loads the
/// committed public key from its one canonical location, so a caller (the CLI) never restates the trust-anchor
/// path. Tests supply the override and the ceilings to exercise the fail-closed paths.
/// </summary>
/// <param name="ProjectRoot">The absolute project root the guard protects.</param>
/// <param name="TimeProvider">The time source used for grant expiry and orphan-snapshot sweeps.</param>
/// <param name="GrantPublicKey">An optional raw 32-byte Ed25519 public key that overrides the committed key; empty means load the committed key.</param>
/// <param name="PerFileSnapshotByteCeiling">The maximum size any single protected file may reach at capture.</param>
/// <param name="TotalSnapshotByteCeiling">The maximum total snapshot size at capture.</param>
public sealed record GuardEngineOptions(
    string ProjectRoot,
    TimeProvider TimeProvider,
    ReadOnlyMemory<byte> GrantPublicKey = default,
    long PerFileSnapshotByteCeiling = 5_242_880,
    long TotalSnapshotByteCeiling = 104_857_600);
