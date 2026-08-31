// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the shared polkit reply-building helpers used by the Linux presence tests. The empty reply-detail
/// dictionary was previously spelled verbatim in both <c>PolkitDecisionTests</c> and <c>LinuxPresenceCheckTests</c>; it
/// lives here once (DRY) so a change to how a detail-less polkit reply is fabricated touches one place.
/// </summary>
internal static class PolkitTestReplies
{
    /// <summary>Creates the empty reply-detail dictionary a polkit reply with no details carries.</summary>
    /// <returns>A fresh, empty, ordinal-keyed detail dictionary.</returns>
    internal static Dictionary<string, string> NoDetails() => new(StringComparer.Ordinal);
}
