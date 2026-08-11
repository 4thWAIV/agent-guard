// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Engine;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class SnapshotSerializerTests
{
    [Fact]
    public void Serialize_RoundTrips_FingerprintAndEntries()
    {
        var data = new SnapshotData(
            "fingerprint-x",
            new[]
            {
                new SnapshotEntry("/repo/a", Present: true, new byte[] { 1, 2, 3 }),
                new SnapshotEntry("/repo/b", Present: false, ReadOnlyMemory<byte>.Empty),
            });

        byte[] bytes = SnapshotSerializer.Serialize(data);

        SnapshotSerializer.TryDeserialize(bytes, out SnapshotData? round).Should().BeTrue();
        round!.RulesetFingerprint.Should().Be("fingerprint-x");
        round.Entries.Should().HaveCount(2);
        round.Entries[0].Present.Should().BeTrue();
        round.Entries[0].Content.ToArray().Should().Equal(1, 2, 3);
        round.Entries[1].Present.Should().BeFalse();
    }

    [Fact]
    public void TryDeserialize_TruncatedBlob_IsTreatedAsMissing()
    {
        var data = new SnapshotData("fp", new[] { new SnapshotEntry("/repo/a", Present: true, new byte[] { 9, 9, 9, 9, 9 }) });
        byte[] bytes = SnapshotSerializer.Serialize(data);

        SnapshotSerializer.TryDeserialize(bytes.AsMemory(0, bytes.Length / 2), out SnapshotData? round).Should().BeFalse();
        round.Should().BeNull();
    }

    [Fact]
    public void TryDeserialize_GarbageBlob_IsTreatedAsMissing()
    {
        SnapshotSerializer.TryDeserialize(new byte[] { 7, 7, 7, 7 }, out SnapshotData? round).Should().BeFalse();
        round.Should().BeNull();
    }
}
