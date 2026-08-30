// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned presence capability, reached at <c>ISystemServices.Platform.Presence</c> (sibling to
/// <c>IPlatformServices.FileSystem</c>) and consumed only by the approval gate. It has one operation, <see cref="Check"/>,
/// which proves a physically-present human through an out-of-band channel the calling process cannot answer — macOS
/// LocalAuthentication, Windows Hello or the secure-desktop credential prompt, or a Linux polkit
/// <c>CheckAuthorization</c>. Each per-OS implementation forces a fresh human interaction per call and never returns a
/// cached authorization. Fingerprint enrolment and one-time setup are not operations on this interface.
/// </summary>
public interface IPresenceCheck
{
    /// <summary>
    /// Evaluates presence for a mutating action, showing the OS's own out-of-band prompt and returning the mapped
    /// outcome. The implementation reaches its per-OS native port through <paramref name="services"/> where it needs an
    /// owned boundary (for example the Linux subject read from <c>/proc/self</c> through <c>IFileReader</c>), and honors
    /// <paramref name="ct"/> — but mints no timeout of its own; the gate owns the single 60-second bound.
    /// </summary>
    /// <param name="request">The presence request carrying the resolved prompt text.</param>
    /// <param name="services">The owned service container the implementation draws any boundary it needs from.</param>
    /// <param name="ct">The cancellation token the gate cancels when its timeout wins.</param>
    /// <returns>The presence result — the mapped reason and its detail. Never <c>ApprovalReason.TimedOut</c>.</returns>
    Task<PresenceResult> Check(PresenceRequest request, ISystemServices services, CancellationToken ct);
}
