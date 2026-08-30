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
    /// per-call message is the reviewed prompt text. The caller-generated <paramref name="cancellationId"/> is written
    /// into the request so an in-flight <c>CheckAuthorization</c> can be cancelled: when the flow port calls
    /// <see cref="CancelCheckAuthorization"/> with the same id, polkit ends the pending check and this call surfaces the
    /// cancellation as an <see cref="System.OperationCanceledException"/>. It mints no timeout of its own; the flow port
    /// owns the token registration and the gate owns the one 60-second bound.
    /// </summary>
    /// <param name="subject">The unix-process subject (pid, start time, uid) naming the calling process to polkit.</param>
    /// <param name="actionId">The polkit action id being checked.</param>
    /// <param name="message">The per-call <c>polkit.message</c> — the reviewed prompt text.</param>
    /// <param name="cancellationId">The non-empty, per-call unique id written into the request so a matching
    /// <see cref="CancelCheckAuthorization"/> can cancel this in-flight check; polkit rejects a reused id.</param>
    /// <param name="ct">The gate's cancellation token; observed before the call starts and used to surface an in-flight
    /// cancellation as an <see cref="System.OperationCanceledException"/>. The port mints no timeout of its own.</param>
    /// <returns>The plain polkit reply — authorized, challenge, and the reply details.</returns>
    Task<PolkitResult> CheckAuthorizationAsync(
        PolkitSubject subject, string actionId, string message, string cancellationId, CancellationToken ct);

    /// <summary>
    /// Sends polkit's <c>CancelCheckAuthorization</c> for the in-flight <see cref="CheckAuthorizationAsync"/> call
    /// identified by <paramref name="cancellationId"/>, so a still-pending check is actually ended (its out-of-band
    /// prompt dismissed) rather than left running. It is the synchronous, straight-line cancel primitive the flow port
    /// wires onto the cancellation token (<c>ct.Register</c>); it fires the D-Bus method and does not await polkit's
    /// acknowledgement (the cancellation surfaces on the pending <see cref="CheckAuthorizationAsync"/> instead). It mints
    /// no timeout of its own.
    /// </summary>
    /// <param name="cancellationId">The id that was written into the in-flight <see cref="CheckAuthorizationAsync"/>
    /// request this cancels.</param>
    void CancelCheckAuthorization(string cancellationId);
}
