// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Security.Cryptography;

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the binary SHA-256 used across install, the hook integrity self-check, and the
/// binary-hash condition, so the three never compute the digest differently. It hashes bytes only — the caller
/// reads the file's bytes through the owned <see cref="AgentGuard.Abstractions.Contracts.IFileReader"/> and passes
/// them in — so no raw filesystem call lives here; <c>SHA256.HashData</c> is a pure function of its bytes.
/// </summary>
internal static class Hashing
{
    /// <summary>
    /// Returns the lowercase hex SHA-256 of the given bytes.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The lowercase hex digest.</returns>
    internal static string Sha256Hex(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
