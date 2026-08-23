// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Engine;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AgentGuard.Tests;

public sealed class ContextStoreSweepTests
{
    [Fact]
    public async Task SweepExpired_RemovesOrphanOlderThanTtl_KeepsFresh()
    {
        // The orphan-snapshot sweep is the best-effort housekeeping a Pre run performs before the guards; it removes
        // per-call snapshot directories older than the 24h orphan TTL and keeps fresher ones. This drives it through
        // the public pipeline over the copy-on-write simulator: two orphan directories are seeded under the store base
        // with backdated modified times, a Pre run sweeps, and the owned directory reader proves the outcome.
        using var fixture = new FixtureProject();
        var time = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ISystemServices services = SystemServicesBuilder.Real().With((TimeProvider)time).SimulateFileSystem().Build();

        string storeBase = GuardEngine.SnapshotStoreDirectory(services, fixture.Root);
        IDirectoryWriter directoryWriter = services.FileSystem.GetDirectoryWriter();
        string oldDir = Path.Combine(storeBase, "orphan-old");
        string freshDir = Path.Combine(storeBase, "orphan-fresh");
        directoryWriter.CreateDirectory(oldDir);
        directoryWriter.CreateDirectory(freshDir);
        directoryWriter.SetLastWriteTimeUtc(oldDir, time.GetUtcNow().AddDays(-2));
        directoryWriter.SetLastWriteTimeUtc(freshDir, time.GetUtcNow().AddHours(-1));

        IPipeline pipeline = GuardEngine.CreatePipeline(new GuardEngineOptions(fixture.Root, services));
        await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Bash("call-sweep", "echo hi"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        IDirectoryEnumerator directories = services.FileSystem.GetDirectoryReader();
        directories.DirectoryExists(oldDir).Should().BeFalse();
        directories.DirectoryExists(freshDir).Should().BeTrue();
    }
}
