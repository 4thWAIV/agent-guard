// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.CrossPlatform.Linux;

/// <summary>
/// The inspectable, pre-wire assembly of polkit's
/// <c>org.freedesktop.PolicyKit1.Authority.CheckAuthorization</c> arguments, produced by the pure
/// <see cref="TmdsPolkitAuthority.BuildCheckAuthorizationRequest"/> so a unit test can assert the built call carries
/// <c>AllowUserInteraction=1</c> (<see cref="Flags"/>) and pins <c>start-time</c> as a 64-bit D-Bus variant
/// (<see cref="SubjectVariantTypes"/> — <c>"t"</c>, not the 32-bit <c>"u"</c>) WITHOUT opening a bus. It mirrors the
/// wire call <c>(sa{sv}) s a{ss} u s</c>: the subject as <see cref="SubjectKind"/> plus the triple values
/// (<see cref="Subject"/>) and their pinned per-key variant type codes (<see cref="SubjectVariantTypes"/>); then the
/// <see cref="ActionId"/>; the <c>a{ss}</c> details (<see cref="Details"/>); the flags <c>u</c> (<see cref="Flags"/>);
/// and the empty <see cref="CancellationId"/>. It carries no <c>Tmds.DBus.Protocol</c> type, so a test inspects it
/// without referencing the pinned package.
/// </summary>
/// <param name="SubjectKind">The polkit subject kind — <c>unix-process</c>.</param>
/// <param name="Subject">The unix-process triple values (pid, start time, uid).</param>
/// <param name="SubjectVariantTypes">Each subject <c>a{sv}</c> key mapped to its pinned D-Bus variant type code — notably <c>start-time</c> to <c>t</c> (64-bit), never <c>u</c> (32-bit).</param>
/// <param name="ActionId">The polkit action id being checked.</param>
/// <param name="Details">The <c>a{ss}</c> details — <c>polkit.message</c> mapped to the reviewed prompt text.</param>
/// <param name="Flags">The polkit <c>CheckAuthorizationFlags</c> value — <c>AllowUserInteraction=1</c>.</param>
/// <param name="CancellationId">The cancellation id — empty; the port passes none.</param>
internal sealed record PolkitCheckAuthorizationRequest(
    string SubjectKind,
    PolkitSubject Subject,
    IReadOnlyDictionary<string, string> SubjectVariantTypes,
    string ActionId,
    IReadOnlyDictionary<string, string> Details,
    uint Flags,
    string CancellationId);
