// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// An Ed25519 key pair produced by <see cref="ISignatureService.GenerateKeyPair"/> — a named type rather than a
/// tuple, so the pair crosses the interface with named members.
/// </summary>
/// <param name="PublicKey">The Ed25519 public key.</param>
/// <param name="PrivateKey">The Ed25519 private key.</param>
public readonly record struct SigningKeyPair(ReadOnlyMemory<byte> PublicKey, ReadOnlyMemory<byte> PrivateKey);
