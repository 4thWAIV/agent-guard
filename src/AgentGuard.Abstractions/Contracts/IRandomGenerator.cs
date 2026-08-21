// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The one seam through which randomness enters the codebase, so the single non-deterministic call is owned and
/// tests are deterministic. The owner is the randomness concern, not GUIDs specifically, so a random file name has a
/// discoverable home beside <see cref="NewGuid"/>.
/// </summary>
public interface IRandomGenerator
{
    /// <summary>
    /// Creates a new GUID.
    /// </summary>
    /// <returns>A new GUID.</returns>
    Guid NewGuid();

    /// <summary>
    /// Creates a cryptographically-weak random file (or directory) name of the form produced by
    /// <c>System.IO.Path.GetRandomFileName()</c> — eleven characters with an embedded dot, valid as a relative name.
    /// </summary>
    /// <returns>A random file name.</returns>
    string GetRandomFileName();
}
