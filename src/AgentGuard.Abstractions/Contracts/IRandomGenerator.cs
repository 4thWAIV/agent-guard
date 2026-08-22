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

    /// <summary>
    /// Computes the path of a temporary file placed next to <paramref name="path"/> — a unique <c>.tmp-</c> name in the
    /// same directory — so a file or symlink can be replaced atomically: write or create the temporary sibling, then
    /// rename it over the target. Keeping the temporary in the target's own directory keeps the rename on one file
    /// system, and the unique suffix comes from the owned <see cref="NewGuid"/> so two writers never collide. The one
    /// formula lives here so every holder of an <see cref="IRandomGenerator"/> shares it rather than spelling it again.
    /// </summary>
    /// <param name="path">The target path the temporary sibling sits beside.</param>
    /// <returns>The temporary sibling path in the target's directory.</returns>
    string TemporarySiblingPath(string path) => Path.Combine(
        Path.GetDirectoryName(path)!,
        Path.GetFileName(path) + ".tmp-" + NewGuid().ToString("N"));
}
