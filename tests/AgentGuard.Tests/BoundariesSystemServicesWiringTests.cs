// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Proves the <c>SystemServices.Create()</c> composition in <c>AgentGuard.Boundaries</c> wires every owned service to
/// its real adapter: building over <see cref="SystemServicesBuilder.Real"/> and driving each accessor through real
/// behavior (distinct GUIDs, a system-clock read, the real directory separator, and an end-to-end Ed25519 verify)
/// exercises the container's construction and its property accessors and asserts the wiring reached the real owners.
/// </summary>
public sealed class BoundariesSystemServicesWiringTests
{
    [Fact]
    public void Real_RandomOwnerProducesDistinctGuids()
    {
        IRandomGenerator random = SystemServicesBuilder.Real().Build().Random;

        Assert.NotEqual(random.NewGuid(), random.NewGuid());
    }

    [Fact]
    public void Real_ClockOwnerReadsTheSystemTime()
    {
        TimeProvider clock = SystemServicesBuilder.Real().Build().Clock;

        Assert.True(clock.GetUtcNow().Year >= 2020);
    }

    [Fact]
    public void Real_PlatformOwnerExposesTheRealDirectorySeparator()
    {
        IPlatformFileSystem platform = SystemServicesBuilder.Real().Build().Platform.FileSystem;

        char separator = platform.DirectorySeparator;
        Assert.True(separator is '/' or '\\', $"Unexpected directory separator '{separator}'.");
    }

    [Fact]
    public void Real_SignatureOwnerVerifiesARealSignatureEndToEnd()
    {
        ISystemServices services = SystemServicesBuilder.Real().Build();
        SigningKeyPair pair = services.Signatures.GenerateKeyPair();
        byte[] message = [9, 8, 7, 6];

        ReadOnlyMemory<byte> signature = services.Signatures.Sign(pair.PrivateKey, message);

        Assert.True(services.Signatures.Verify(pair.PublicKey, message, signature));
    }
}
