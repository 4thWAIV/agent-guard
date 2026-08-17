// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace AgentGuard.Boundaries;

/// <summary>
/// The owned <see cref="ISignatureService"/> — the single class that implements the Ed25519 grant-signature seam, so
/// it is the ONE place the raw BouncyCastle Ed25519 types are allowed (AG0021 exempts exactly this owner). The
/// <c>Ed25519Signer</c> sequence (<c>Init</c>/<c>BlockUpdate</c>/<c>VerifySignature</c>/<c>GenerateSignature</c>),
/// key generation, the <c>Ed25519*Parameters</c>, and the choice of Ed25519 all hide inside here, and no BouncyCastle
/// type crosses <see cref="ISignatureService"/> (signature-verify-behind-isignatureverifier). BouncyCastle is managed
/// and OS-uniform and its consumers sit above both layers, so the owner lives in <c>AgentGuard.Boundaries</c>. The
/// fail-closed guarantee lives here: <see cref="Verify"/> returns <see langword="false"/> on any malformed or
/// wrong-length input and never throws, so a bad grant is denied rather than crashing. It is <c>internal</c> with a
/// <c>private</c> constructor (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class Ed25519SignatureService : ISignatureService
{
    // The raw byte length of an Ed25519 public/private key and of a signature. A public key of any other length is
    // malformed, so Verify denies rather than constructing key parameters that would throw.
    private const int Ed25519KeyLength = 32;

    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private Ed25519SignatureService()
    {
    }

    /// <inheritdoc />
    public bool Verify(ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> message, ReadOnlyMemory<byte> signature)
    {
        // A public key of the wrong length is malformed: deny before touching BouncyCastle, whose parameter
        // constructor would throw on it. Everything else is wrapped so any malformed message or signature denies
        // rather than crashes — the fail-closed guarantee.
        if (publicKey.Length != Ed25519KeyLength)
        {
            return false;
        }

        try
        {
            var parameters = new Ed25519PublicKeyParameters(publicKey.ToArray(), 0);
            var verifier = new Ed25519Signer();
            verifier.Init(forSigning: false, parameters);

            byte[] messageBytes = message.ToArray();
            verifier.BlockUpdate(messageBytes, 0, messageBytes.Length);
            return verifier.VerifySignature(signature.ToArray());
        }
        catch (ArgumentException)
        {
            // A malformed key or buffer length surfaces as an argument exception here; deny rather than crash so a
            // bad grant is fail-closed.
            return false;
        }
        catch (CryptoException)
        {
            // A crypto-level failure denies rather than crashes — the same fail-closed guarantee.
            return false;
        }
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Sign(ReadOnlyMemory<byte> privateKey, ReadOnlyMemory<byte> message)
    {
        var parameters = new Ed25519PrivateKeyParameters(privateKey.ToArray(), 0);
        var signer = new Ed25519Signer();
        signer.Init(forSigning: true, parameters);

        byte[] messageBytes = message.ToArray();
        signer.BlockUpdate(messageBytes, 0, messageBytes.Length);
        return signer.GenerateSignature();
    }

    /// <inheritdoc />
    public SigningKeyPair GenerateKeyPair()
    {
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        AsymmetricCipherKeyPair pair = generator.GenerateKeyPair();

        byte[] publicKey = ((Ed25519PublicKeyParameters)pair.Public).GetEncoded();
        byte[] privateKey = ((Ed25519PrivateKeyParameters)pair.Private).GetEncoded();
        return new SigningKeyPair(publicKey, privateKey);
    }

    /// <summary>
    /// Creates the Ed25519 signature service.
    /// </summary>
    /// <returns>The signature service, as its interface.</returns>
    internal static ISignatureService Create() => new Ed25519SignatureService();
}
