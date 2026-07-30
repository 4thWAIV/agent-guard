// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Security.Cryptography;

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the binary SHA-256 used across install, the hook integrity self-check, and the
/// binary-hash condition, so the three never compute the digest differently.
/// </summary>
internal static class Hashing
{
    /// <summary>
    /// Returns the lowercase hex SHA-256 of the given bytes.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The lowercase hex digest.</returns>
    internal static string Sha256Hex(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>
    /// Returns the lowercase hex SHA-256 of a file's bytes; symlinks are followed to the target's bytes.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The lowercase hex digest.</returns>
    internal static string Sha256HexOfFile(string path) => Sha256Hex(File.ReadAllBytes(path));
}
