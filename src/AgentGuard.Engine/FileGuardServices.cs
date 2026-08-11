// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The collaborators the File Guard runs against, bundled so the guard's factory stays within the parameter
/// budget and every dependency arrives through its interface.
/// </summary>
/// <param name="ProtectedSet">The assembled Protected Set.</param>
/// <param name="Scanner">The whole-tree scanner Capture and Post use.</param>
/// <param name="FileReader">The read-only working-tree reader.</param>
/// <param name="Canonicalizer">The path canonicalizer.</param>
/// <param name="GrantStore">The active-grant store.</param>
/// <param name="RegionRegistry">The region-map registry that routes a watched file to the region-aware verifier.</param>
internal sealed record FileGuardServices(
    IProtectedSet ProtectedSet,
    IProtectedFileScanner Scanner,
    IFileReader FileReader,
    IPathCanonicalizer Canonicalizer,
    IGrantStore GrantStore,
    IRegionMapRegistry RegionRegistry);
