// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Engine;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace AgentGuard.Tests;

/// <summary>
/// A throwaway Ed25519 authority used only to sign grant fixtures. It mirrors what a human key-holder does:
/// sign a token's canonical payload bytes with the private key the guard verifies against the matching public
/// key.
/// </summary>
internal sealed class EphemeralGrantAuthority
{
    private readonly Ed25519PrivateKeyParameters _privateKey;

    internal EphemeralGrantAuthority()
    {
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var pair = generator.GenerateKeyPair();
        _privateKey = (Ed25519PrivateKeyParameters)pair.Private;
        PublicKey = ((Ed25519PublicKeyParameters)pair.Public).GetEncoded();
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
        var signer = new Ed25519Signer();
        signer.Init(forSigning: true, _privateKey);
        signer.BlockUpdate(message, 0, message.Length);
        byte[] signature = signer.GenerateSignature();
        return new GrantToken(payload, Convert.ToBase64String(signature));
    }
}
