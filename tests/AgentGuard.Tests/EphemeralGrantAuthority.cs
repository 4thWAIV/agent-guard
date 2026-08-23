// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Engine;
using AgentGuard.TestHelpers;

namespace AgentGuard.Tests;

/// <summary>
/// A throwaway Ed25519 authority used only to sign grant fixtures. It mirrors what a human key-holder does: sign a
/// token's canonical payload bytes with the private key the guard verifies against the matching public key. The crypto
/// is reached only through the owned <see cref="ISignatureService"/> off a real container — the interface whose
/// <c>Sign</c> and <c>GenerateKeyPair</c> members exist for exactly this test grant authority — never a raw
/// BouncyCastle type.
/// </summary>
internal sealed class EphemeralGrantAuthority
{
    private readonly ISignatureService _signatures;
    private readonly ReadOnlyMemory<byte> _privateKey;

    internal EphemeralGrantAuthority()
    {
        _signatures = SystemServicesBuilder.Real().Build().Signatures;
        SigningKeyPair pair = _signatures.GenerateKeyPair();
        _privateKey = pair.PrivateKey;
        PublicKey = pair.PublicKey;
    }

    /// <summary>
    /// Gets the raw 32-byte Ed25519 public key.
    /// </summary>
    internal ReadOnlyMemory<byte> PublicKey { get; }

    /// <summary>
    /// Signs a grant payload, producing a token whose signature the guard verifies.
    /// </summary>
    /// <param name="payload">The payload to sign.</param>
    /// <returns>The signed token.</returns>
    internal GrantToken Sign(GrantTokenPayload payload)
    {
        byte[] message = GrantTokenCodec.CanonicalBytes(payload);
        ReadOnlyMemory<byte> signature = _signatures.Sign(_privateKey, message);
        return new GrantToken(payload, Convert.ToBase64String(signature.Span));
    }
}
