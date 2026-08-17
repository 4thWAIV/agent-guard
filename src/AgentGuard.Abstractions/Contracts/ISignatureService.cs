// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The one purpose-built service for Ed25519 grant signatures — verify (production), plus sign and key generation
/// (the test grant authority). It is not a 1:1 wrapper of the crypto library: the underlying signer sequence, key
/// generation, the key parameters, and the choice of Ed25519 all hide inside the owner, and no library type crosses
/// this interface. Expanding it hands production no new power — <see cref="Sign"/> takes a private key passed in,
/// and production only ever holds the committed public key.
/// </summary>
public interface ISignatureService
{
    /// <summary>
    /// Verifies a signature over a message against a public key. Returns <see langword="false"/> on any malformed or
    /// wrong-length input and never throws, so a bad grant is denied rather than crashing.
    /// </summary>
    /// <param name="publicKey">The Ed25519 public key to verify against.</param>
    /// <param name="message">The message the signature covers.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <returns><see langword="true"/> when the signature is valid; otherwise <see langword="false"/>.</returns>
    bool Verify(ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> message, ReadOnlyMemory<byte> signature);

    /// <summary>
    /// Signs a message with a private key.
    /// </summary>
    /// <param name="privateKey">The Ed25519 private key to sign with.</param>
    /// <param name="message">The message to sign.</param>
    /// <returns>The Ed25519 signature over the message.</returns>
    ReadOnlyMemory<byte> Sign(ReadOnlyMemory<byte> privateKey, ReadOnlyMemory<byte> message);

    /// <summary>
    /// Generates a fresh Ed25519 key pair for the test grant authority.
    /// </summary>
    /// <returns>A new Ed25519 <see cref="SigningKeyPair"/>.</returns>
    SigningKeyPair GenerateKeyPair();
}
