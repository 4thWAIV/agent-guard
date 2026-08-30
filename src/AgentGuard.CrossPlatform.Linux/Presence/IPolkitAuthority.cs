// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace AgentGuard.CrossPlatform.Linux;

/// <summary>
/// The internal Linux presence port — the one seam through which the Linux presence check reaches polkit. Its sole
/// implementer (<see cref="TmdsPolkitAuthority"/>) is the only class that touches the pinned
/// <c>Tmds.DBus.Protocol</c> package and makes the <c>org.freedesktop.PolicyKit1.Authority.CheckAuthorization</c> call
/// (per-os-native-behind-a-port; enforced by AG0110/AG0114). It is native-call-free (managed D-Bus), so it is NOT a
/// native-interop owner. It never creates, spawns, or registers an authentication agent (no-self-answerable-agent).
///
/// It is marked <see cref="RequiresNativeSpecTestAttribute"/> — the production-side, single source of the guardrail-1
/// requirement that this owner have its own <c>[Trait("Category","NativeSpec")]</c> spec test (the Linux off-bus
/// serialization spec test the IMPLEMENT step adds).
/// </summary>
[RequiresNativeSpecTest]
internal interface IPolkitAuthority
{
    /// <summary>
    /// Calls <c>CheckAuthorization</c> once for the calling process, with <c>AllowUserInteraction</c> set, and returns
    /// the plain reply. The subject is the classic unix-process triple; the action is <see cref="PolkitAction.Id"/>; the
    /// per-call message is the reviewed prompt text. It passes no cancellation id, so cancellation is observed only
    /// before the call starts (start-only): an in-flight <c>CheckAuthorization</c> cannot be cancelled (residual risk
    /// tracked in #47). It mints no timeout of its own.
    /// </summary>
    /// <param name="subject">The unix-process subject (pid, start time, uid) naming the calling process to polkit.</param>
    /// <param name="actionId">The polkit action id being checked.</param>
    /// <param name="message">The per-call <c>polkit.message</c> — the reviewed prompt text.</param>
    /// <param name="ct">The gate's cancellation token; the port observes it only before the call starts (start-only, no
    /// cancellation id passed) and mints no timeout of its own.</param>
    /// <returns>The plain polkit reply — authorized, challenge, and the reply details.</returns>
    Task<PolkitResult> CheckAuthorizationAsync(PolkitSubject subject, string actionId, string message, CancellationToken ct);
}
