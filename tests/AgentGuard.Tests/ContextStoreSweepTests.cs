// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AgentGuard.Tests;

public sealed class ContextStoreSweepTests
{
    [Fact]
    public async Task SweepExpired_RemovesOrphanOlderThanTtl_KeepsFresh()
    {
        using var fixture = new FixtureProject();
        var time = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        IContextStore store = ContextStore.Create(fixture.Root, time);
        var oldKey = new ContextKey(new ToolCallId("orphan-old"), GuardEngine.FileGuardName, "snapshot");
        var freshKey = new ContextKey(new ToolCallId("orphan-fresh"), GuardEngine.FileGuardName, "snapshot");
        await store.WriteAsync(oldKey, new byte[] { 1 }, CancellationToken.None);
        await store.WriteAsync(freshKey, new byte[] { 1 }, CancellationToken.None);

        string oldDir = ContextStorePaths.CallDirectory(fixture.Root, oldKey.ToolCall);
        string freshDir = ContextStorePaths.CallDirectory(fixture.Root, freshKey.ToolCall);
        Directory.SetLastWriteTimeUtc(oldDir, time.GetUtcNow().UtcDateTime.AddDays(-2));
        Directory.SetLastWriteTimeUtc(freshDir, time.GetUtcNow().UtcDateTime.AddHours(-1));

        await store.SweepExpiredAsync(TimeSpan.FromDays(1), CancellationToken.None);

        Directory.Exists(oldDir).Should().BeFalse();
        Directory.Exists(freshDir).Should().BeTrue();
    }
}
