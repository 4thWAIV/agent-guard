// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Exercises the real <c>Ed25519SignatureService</c> in <c>AgentGuard.Boundaries</c> through the owned
/// <see cref="ISignatureService"/> off a real container (<see cref="SystemServicesBuilder.Real"/>) — no fake verifier
/// exists by design, so these run the real BouncyCastle Ed25519 sign, verify, and key-generation paths and assert the
/// real cryptographic outcomes, including the fail-closed denials.
/// </summary>
public sealed class BoundariesEd25519SignatureServiceTests
{
    private readonly ISignatureService signatures = SystemServicesBuilder.Real().Build().Signatures;

    [Fact]
    public void GenerateKeyPair_ProducesA32ByteEd25519KeyPair()
    {
        SigningKeyPair pair = this.signatures.GenerateKeyPair();

        Assert.Equal(32, pair.PublicKey.Length);
        Assert.Equal(32, pair.PrivateKey.Length);
    }

    [Fact]
    public void SignThenVerify_WithTheMatchingKey_Succeeds()
    {
        SigningKeyPair pair = this.signatures.GenerateKeyPair();
        byte[] message = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        ReadOnlyMemory<byte> signature = this.signatures.Sign(pair.PrivateKey, message);

        Assert.Equal(64, signature.Length);
        Assert.True(this.signatures.Verify(pair.PublicKey, message, signature));
    }

    [Fact]
    public void Verify_WithATamperedMessage_FailsClosed()
    {
        SigningKeyPair pair = this.signatures.GenerateKeyPair();
        byte[] message = [10, 20, 30];
        byte[] tampered = [10, 20, 31];
        ReadOnlyMemory<byte> signature = this.signatures.Sign(pair.PrivateKey, message);

        Assert.False(this.signatures.Verify(pair.PublicKey, tampered, signature));
    }

    [Fact]
    public void Verify_WithADifferentPublicKey_FailsClosed()
    {
        SigningKeyPair signer = this.signatures.GenerateKeyPair();
        SigningKeyPair other = this.signatures.GenerateKeyPair();
        byte[] message = [42, 43, 44];
        ReadOnlyMemory<byte> signature = this.signatures.Sign(signer.PrivateKey, message);

        Assert.False(this.signatures.Verify(other.PublicKey, message, signature));
    }

    [Fact]
    public void Verify_WithAWrongLengthPublicKey_FailsClosedWithoutThrowing()
    {
        SigningKeyPair pair = this.signatures.GenerateKeyPair();
        byte[] message = [7, 7, 7];
        ReadOnlyMemory<byte> signature = this.signatures.Sign(pair.PrivateKey, message);
        byte[] shortKey = new byte[16];

        Assert.False(this.signatures.Verify(shortKey, message, signature));
    }
}
