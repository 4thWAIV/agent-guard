// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine;

/// <summary>
/// A grant token as stored on disk: a payload and the base64 Ed25519 signature over the payload's canonical
/// bytes.
/// </summary>
/// <param name="Payload">The signed payload.</param>
/// <param name="Signature">The base64 Ed25519 signature over the payload's canonical bytes.</param>
public sealed record GrantToken(GrantTokenPayload Payload, string Signature);
