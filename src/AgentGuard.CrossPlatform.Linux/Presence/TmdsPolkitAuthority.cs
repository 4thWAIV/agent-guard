// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace AgentGuard.CrossPlatform.Linux;

/// <summary>
/// The sole implementer of <see cref="IPolkitAuthority"/> and the ONE class allowed to touch the pinned
/// <c>Tmds.DBus.Protocol</c> package (AG0110/AG0114). It opens the system-bus connection and calls
/// <c>org.freedesktop.PolicyKit1.Authority.CheckAuthorization</c> at <c>/org/freedesktop/PolicyKit1/Authority</c>,
/// writing the subject triple into the <c>a{sv}</c> dict with polkit's expected variant types (start-time 64-bit),
/// action <see cref="PolkitAction.Id"/>, details <c>a{ss}</c> with <c>polkit.message</c>, flags
/// <c>AllowUserInteraction=1</c>, and the caller-generated cancellation id — returning the plain
/// <see cref="PolkitResult"/>. It honors cancellation IN FLIGHT: the flow port (<see cref="LinuxPresenceCheck"/>) wires
/// the token to <see cref="CancelCheckAuthorization"/>, which sends polkit's <c>CancelCheckAuthorization</c> for the
/// pending call; polkit then ends the check (its reply becomes an error), and this class surfaces that as an
/// <see cref="OperationCanceledException"/> the flow port maps to <see cref="AgentGuard.Abstractions.ApprovalReason.Cancelled"/>.
/// It never creates, spawns, or registers an authentication agent, and mints no timeout of its own.
/// </summary>
internal sealed class TmdsPolkitAuthority : IPolkitAuthority
{
    // The polkit D-Bus target: the well-known bus name, the Authority object path and interface, and the method members.
    private const string PolkitBusName = "org.freedesktop.PolicyKit1";
    private const string PolkitAuthorityObjectPath = "/org/freedesktop/PolicyKit1/Authority";
    private const string PolkitAuthorityInterface = "org.freedesktop.PolicyKit1.Authority";
    private const string CheckAuthorizationMember = "CheckAuthorization";
    private const string CancelCheckAuthorizationMember = "CancelCheckAuthorization";

    // The CheckAuthorization input signature: subject (sa{sv}), action_id s, details a{ss}, flags u, cancellation_id s.
    private const string CheckAuthorizationSignature = "(sa{sv})sa{ss}us";

    // The CancelCheckAuthorization input signature: cancellation_id s.
    private const string CancelCheckAuthorizationSignature = "s";

    // The unix-process subject kind and its a{sv} keys (presence-subject-triple).
    private const string UnixProcessSubjectKind = "unix-process";
    private const string ProcessIdKey = "pid";
    private const string StartTimeKey = "start-time";
    private const string UserIdKey = "uid";

    // polkit's expected D-Bus variant type codes for the subject values: pid uint32 ("u"), start-time uint64 ("t" — the
    // 64-bit pin, never 32-bit "u", so it does not truncate past ~497 days of uptime), uid int32 ("i").
    private const string DBusUInt32TypeCode = "u";
    private const string DBusUInt64TypeCode = "t";
    private const string DBusInt32TypeCode = "i";

    // The per-call details key polkit shows the human, and the AllowUserInteraction flag value.
    private const string PolkitMessageKey = "polkit.message";
    private const uint AllowUserInteractionFlag = 1u;

    /// <inheritdoc />
    public async Task<PolkitResult> CheckAuthorizationAsync(
        PolkitSubject subject, string actionId, string message, string cancellationId, CancellationToken ct)
    {
        // Don't start a check that is already cancelled. It mints no timeout of its own (AG0107); the flow port owns the
        // token registration that cancels an in-flight check and the gate owns the one 60-second bound.
        ct.ThrowIfCancellationRequested();

        MessageBuffer callMessage =
            CreateCheckAuthorizationCall(subject, actionId, message, cancellationId, out DBusConnection connection);

        try
        {
            return await connection
                .CallMethodAsync(callMessage, ReadAuthorizationResult, readerState: PolkitBusName)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ct.IsCancellationRequested)
        {
            // The flow port's registered CancelCheckAuthorization ended this pending call: polkit answers the cancelled
            // check with an error (org.freedesktop.PolicyKit1.Error.Cancelled), which surfaces here as a thrown fault.
            // Because the token is cancelled, that fault IS the cancellation — surface it as OperationCanceledException
            // so the flow port maps it to Cancelled, not Error. The filter keys off the token (not a Tmds error type),
            // so any way polkit signals the ended call is treated as the cancellation it is.
            throw new OperationCanceledException("polkit CheckAuthorization was cancelled in flight", ex, ct);
        }
    }

    /// <inheritdoc />
    public void CancelCheckAuthorization(string cancellationId)
    {
        // The synchronous, straight-line cancel primitive the flow port wires onto the cancellation token: build and
        // fire polkit's CancelCheckAuthorization for the in-flight check with this id. It is sent with NoReplyExpected
        // and via the synchronous TrySendMessage — we do not await polkit's acknowledgement here; the cancellation is
        // observed on the pending CheckAuthorizationAsync call instead. Uses the same system-bus connection the check
        // runs on, so no per-call handle is cached in a field.
        MessageBuffer cancelMessage =
            CreateCancelCheckAuthorizationCall(cancellationId, out DBusConnection connection);

        connection.TrySendMessage(cancelMessage);
    }

    /// <summary>
    /// Runs the whole request-assembly and D-Bus serialization path — <see cref="BuildCheckAuthorizationRequest"/> then
    /// <see cref="CreateCheckAuthorizationMessage"/> and its writers — WITHOUT opening the system bus, so the Linux
    /// native-ops owner's serialization is exercised on any host by the guardrail-1 <c>NativeSpec</c> spec test
    /// (the message is built and never sent). It takes and returns no <c>Tmds.DBus.Protocol</c> type. The produced
    /// <c>MessageBuffer</c> is not <see cref="IDisposable"/> in the pinned Tmds.DBus.Protocol 0.94.0 — the connection
    /// returns it to its own pool only when the message is actually sent — so there is nothing to dispose here.
    /// </summary>
    /// <param name="subject">The unix-process triple naming the calling process to polkit.</param>
    /// <param name="actionId">The polkit action id being checked.</param>
    /// <param name="message">The reviewed per-call <c>polkit.message</c> prompt text.</param>
    /// <param name="cancellationId">The per-call cancellation id written into the request.</param>
    internal static void SerializeCheckAuthorizationOffBus(
        PolkitSubject subject, string actionId, string message, string cancellationId)
    {
        _ = CreateCheckAuthorizationCall(subject, actionId, message, cancellationId, out _);
    }

    /// <summary>
    /// Assembles the <c>CheckAuthorization</c> arguments — the unix-process <paramref name="subject"/>, the
    /// <paramref name="actionId"/>, and the reviewed <paramref name="message"/> — into an inspectable
    /// <see cref="PolkitCheckAuthorizationRequest"/> WITHOUT sending on a bus, so a unit test can assert the built call
    /// carries <c>AllowUserInteraction=1</c> and encodes <c>start-time</c> as a 64-bit D-Bus variant (<c>t</c>, not the
    /// 32-bit <c>u</c>). Pure and side-effect-free; <see cref="CheckAuthorizationAsync"/> composes and sends the real
    /// message from the same assembly.
    /// </summary>
    /// <param name="subject">The unix-process triple (pid, start time, uid) naming the calling process to polkit.</param>
    /// <param name="actionId">The polkit action id being checked.</param>
    /// <param name="message">The reviewed per-call <c>polkit.message</c> prompt text.</param>
    /// <param name="cancellationId">The per-call cancellation id polkit correlates a <c>CancelCheckAuthorization</c> against.</param>
    /// <returns>The inspectable assembled request.</returns>
    internal static PolkitCheckAuthorizationRequest BuildCheckAuthorizationRequest(
        PolkitSubject subject, string actionId, string message, string cancellationId) =>
        new(
            SubjectKind: UnixProcessSubjectKind,
            Subject: subject,
            SubjectVariantTypes: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ProcessIdKey] = DBusUInt32TypeCode,
                [StartTimeKey] = DBusUInt64TypeCode,
                [UserIdKey] = DBusInt32TypeCode,
            },
            ActionId: actionId,
            Details: new Dictionary<string, string>(StringComparer.Ordinal) { [PolkitMessageKey] = message },
            Flags: AllowUserInteractionFlag,
            CancellationId: cancellationId);

    // The shared CheckAuthorization preamble both entry points run: assemble the request, take the system-bus
    // connection (handed back through `connection` for the real send), and serialize the method-call message. Kept
    // private because it takes/returns Tmds.DBus.Protocol types (AG0110 confines those to this class).
    private static MessageBuffer CreateCheckAuthorizationCall(
        PolkitSubject subject, string actionId, string message, string cancellationId, out DBusConnection connection)
    {
        PolkitCheckAuthorizationRequest request =
            BuildCheckAuthorizationRequest(subject, actionId, message, cancellationId);
        connection = DBusConnection.System;
        return CreateCheckAuthorizationMessage(connection, request);
    }

    // The CancelCheckAuthorization preamble, parallel to CreateCheckAuthorizationCall: take the system-bus connection
    // (handed back through `connection` for the synchronous send) and serialize the CancelCheckAuthorization(cancellation_id)
    // method-call message on its writer. Kept private because it takes/returns Tmds.DBus.Protocol types (AG0110). The
    // message carries NoReplyExpected: the caller does not await polkit's acknowledgement — the cancellation is observed
    // on the pending CheckAuthorization call.
    private static MessageBuffer CreateCancelCheckAuthorizationCall(
        string cancellationId, out DBusConnection connection)
    {
        connection = DBusConnection.System;
        MessageWriter writer = connection.GetMessageWriter();
        try
        {
            writer.WriteMethodCallHeader(
                destination: PolkitBusName,
                path: PolkitAuthorityObjectPath,
                @interface: PolkitAuthorityInterface,
                member: CancelCheckAuthorizationMember,
                signature: CancelCheckAuthorizationSignature,
                flags: MessageFlags.NoReplyExpected);

            writer.WriteString(cancellationId);

            return writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
    }

    // Serializes the assembled request into the CheckAuthorization method-call message on the connection's writer.
    private static MessageBuffer CreateCheckAuthorizationMessage(
        DBusConnection connection, PolkitCheckAuthorizationRequest request)
    {
        MessageWriter writer = connection.GetMessageWriter();
        try
        {
            writer.WriteMethodCallHeader(
                destination: PolkitBusName,
                path: PolkitAuthorityObjectPath,
                @interface: PolkitAuthorityInterface,
                member: CheckAuthorizationMember,
                signature: CheckAuthorizationSignature,
                flags: MessageFlags.None);

            WriteSubject(ref writer, request);
            writer.WriteString(request.ActionId);
            WriteDetails(ref writer, request.Details);
            writer.WriteUInt32(request.Flags);
            writer.WriteString(request.CancellationId);

            return writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
    }

    // Writes the (sa{sv}) subject: the kind string then the a{sv} triple, each value with polkit's pinned variant type.
    private static void WriteSubject(ref MessageWriter writer, PolkitCheckAuthorizationRequest request)
    {
        writer.WriteStructureStart();
        writer.WriteString(request.SubjectKind);

        ArrayStart dict = writer.WriteDictionaryStart();
        WriteSubjectEntry(ref writer, ProcessIdKey, request);
        WriteSubjectEntry(ref writer, StartTimeKey, request);
        WriteSubjectEntry(ref writer, UserIdKey, request);
        writer.WriteDictionaryEnd(dict);
    }

    // One a{sv} entry: the key string then the value written with the variant kind that mirrors the wire type pinned for
    // that key in SubjectVariantTypes. start-time is a 64-bit "t" (WriteVariantUInt64); a 32-bit "u" would truncate and
    // mismatch the process. pid is "u", uid is "i".
    private static void WriteSubjectEntry(ref MessageWriter writer, string key, PolkitCheckAuthorizationRequest request)
    {
        writer.WriteDictionaryEntryStart();
        writer.WriteString(key);

        switch (key)
        {
            case ProcessIdKey:
                writer.WriteVariantUInt32((uint)request.Subject.ProcessId);
                break;
            case StartTimeKey:
                writer.WriteVariantUInt64(request.Subject.StartTime);
                break;
            case UserIdKey:
                writer.WriteVariantInt32(unchecked((int)request.Subject.UserId));
                break;
            default:
                throw new InvalidOperationException($"unexpected polkit subject entry '{key}'");
        }
    }

    // Writes the a{ss} details (polkit.message -> the reviewed prompt).
    private static void WriteDetails(ref MessageWriter writer, IReadOnlyDictionary<string, string> details)
    {
        ArrayStart dict = writer.WriteDictionaryStart();
        foreach (KeyValuePair<string, string> entry in details)
        {
            writer.WriteDictionaryEntryStart();
            writer.WriteString(entry.Key);
            writer.WriteString(entry.Value);
        }

        writer.WriteDictionaryEnd(dict);
    }

    // Reads the (bba{ss}) reply into the plain PolkitResult the decision maps: is_authorized, is_challenge, details.
    private static PolkitResult ReadAuthorizationResult(Message message, object? state)
    {
        _ = state;
        Reader reader = message.GetBodyReader();
        reader.AlignStruct();
        bool isAuthorized = reader.ReadBool();
        bool isChallenge = reader.ReadBool();

        var details = new Dictionary<string, string>(StringComparer.Ordinal);
        ArrayEnd entries = reader.ReadDictionaryStart();
        while (reader.HasNext(entries))
        {
            string key = reader.ReadString();
            details[key] = reader.ReadString();
        }

        return new PolkitResult(isAuthorized, isChallenge, details);
    }
}
