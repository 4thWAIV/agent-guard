// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.Engine;

/// <summary>
/// A protected config file's declaration of its regions and the format whose adapter interprets their locators. A
/// file joins the after-check by being registered with one of these; any slice of the file not named by a region
/// is implicitly unprotected.
/// </summary>
/// <param name="Format">The format id whose adapter reads and rewrites this file's regions (this build: <c>json</c>).</param>
/// <param name="Regions">The declared regions, evaluated in order so a structural region can precede a content region.</param>
internal sealed record RegionMap(string Format, IReadOnlyList<Region> Regions);
