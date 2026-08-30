// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the public WinRT <c>AsyncStatus</c> values (<c>Windows.Foundation</c>) the Windows presence tests
/// use — spelled exactly once here (DRY) so the async-status mapper test (<c>WindowsMapAsyncStatusTests</c>) and the
/// orchestrator-flow test (<c>WindowsUserPresenceFlowTests</c>) share one set of ordinals. These are the OS's stable enum
/// values, not values this project invents.
/// </summary>
internal static class AsyncStatusCode
{
    /// <summary>The async operation completed; the orchestrator reads the real verification result.</summary>
    internal const int Completed = 1;

    /// <summary>The async operation was canceled.</summary>
    internal const int Canceled = 2;

    /// <summary>The async operation errored.</summary>
    internal const int Error = 3;
}
