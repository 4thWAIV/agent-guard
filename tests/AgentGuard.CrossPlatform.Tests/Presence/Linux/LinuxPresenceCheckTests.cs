// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.CrossPlatform.Linux;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Linux <c>IPresenceCheck</c> implementation driven over its polkit port fake and a fabricated <c>/proc/self</c>
/// read through the owned <c>IFileReader</c> (acceptance #4 and "What to do" #7). It proves: <c>Check</c> calls
/// <c>CheckAuthorization</c> exactly once per call, with the owned action id and the request's prompt as the message;
/// the unix-process subject is built from <c>/proc/self</c> with the REAL uid (the first <c>Uid:</c> column) and a
/// 64-bit start-time that does not truncate; and a port fault maps to <see cref="ApprovalReason.Error"/>, never
/// <see cref="ApprovalReason.Approved"/>. Compiled only on the Linux CI leg.
/// </summary>
public sealed class LinuxPresenceCheckTests
{
    // /proc/self/stat with a comm field that contains a space AND an embedded ')' — the gotcha the parser must survive
    // by reading fields from the LAST ')'. pid (field 1) and start-time (field 22) come from the shared representative
    // subject, so a start-time above uint.MaxValue is exercised and the fixture can never drift from the assertions.
    private static readonly string ProcStat =
        $"{PolkitSubjectFixtures.Pid} (gu a)rd) S 1 1 1 0 -1 0 0 0 0 0 0 0 0 0 20 0 1 0 {PolkitSubjectFixtures.StartTime} 0 0 0";

    // /proc/self/status whose real uid (the first Uid: column) is the shared representative uid and differs from the
    // effective uid (0), so the test proves the subject uses the REAL uid, not the effective one. The Gid line is
    // unrelated scaffolding.
    private static readonly string ProcStatus =
        $"Name:\tguard\nState:\tS (sleeping)\nUid:\t{PolkitSubjectFixtures.Uid}\t0\t0\t0\nGid:\t1000\t1000\t1000\t1000\n";

    // The fabricated /proc/self on the fake container is identical scaffolding for every test, so it is built once in
    // the constructor (mirroring PlatformFileSystemSpecTests) and read from this field; only the polkit reply differs
    // per test.
    private readonly ISystemServices services;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinuxPresenceCheckTests"/> class, building the fake container and
    /// writing the fabricated <c>/proc/self</c> once for every test.
    /// </summary>
    public LinuxPresenceCheckTests()
    {
        services = SystemServicesBuilder.Fake().Build();
        WriteProc(services);
    }

    [Fact]
    public async Task Check_CallsPolkitOncePerCheck_WithTheOwnedActionIdAndPromptMessage()
    {
        var port = FakePolkitAuthority.Returning(
            new PolkitResult(IsAuthorized: false, IsChallenge: false, PolkitTestReplies.NoDetails()));
        IPresenceCheck check = LinuxPresenceCheck.Create(port);

        PresenceResult result = await check.Check(
            new PresenceRequest("please confirm the install"), services, CancellationToken.None);

        port.CallCount.Should().Be(1);
        port.LastActionId.Should().Be(PolkitAction.Id);
        port.LastMessage.Should().Be("please confirm the install");
        result.Reason.Should().Be(ApprovalReason.Denied);
    }

    [Fact]
    public async Task Check_BuildsTheUnixProcessSubject_FromProcSelf_WithRealUidAnd64BitStartTime()
    {
        var port = FakePolkitAuthority.Returning(
            new PolkitResult(IsAuthorized: true, IsChallenge: false, PolkitTestReplies.NoDetails()));
        IPresenceCheck check = LinuxPresenceCheck.Create(port);

        await check.Check(new PresenceRequest("please confirm"), services, CancellationToken.None);

        port.LastSubject.Should().NotBeNull();
        port.LastSubject!.ProcessId.Should().Be(PolkitSubjectFixtures.Pid);
        port.LastSubject.StartTime.Should().Be(PolkitSubjectFixtures.StartTime, "start-time is 64-bit and must not truncate past ~497 days of uptime");
        port.LastSubject.UserId.Should().Be(PolkitSubjectFixtures.Uid, "the subject uses the REAL uid — the first Uid: column — not the effective uid");
    }

    [Fact]
    public async Task Check_ReturnsError_WhenTheAuthorityFaults_AndNeverApproves()
    {
        FakePolkitAuthority port = FakePolkitAuthority.Faulting();
        IPresenceCheck check = LinuxPresenceCheck.Create(port);

        PresenceResult result = await check.Check(
            new PresenceRequest("please confirm"), services, CancellationToken.None);

        result.Reason.Should().Be(ApprovalReason.Error);
        result.Reason.Should().NotBe(ApprovalReason.Approved);
    }

    private static void WriteProc(ISystemServices services)
    {
        IDirectoryWriter directories = services.FileSystem.GetDirectoryWriter();
        directories.CreateDirectory("/proc");
        directories.CreateDirectory("/proc/self");

        IFileWriter files = services.FileSystem.GetFileWriter();
        files.WriteAllText("/proc/self/stat", ProcStat);
        files.WriteAllText("/proc/self/status", ProcStatus);
    }
}
