// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.Engine;

/// <summary>
/// The per-format mechanic that addresses a region by its opaque locator and does exactly two things: read the
/// region's normalized value, and rewrite that one region to a new value while preserving every other byte of the
/// file. Normalization means key-order and whitespace differences are not changes. The region-aware verifier,
/// diff, and restore are format-blind and reach a file only through this seam, so a new format (XML, TOML, YAML)
/// is one new adapter with the verify/diff/restore reused unchanged.
/// </summary>
internal interface IRegionAdapter
{
    /// <summary>
    /// Gets the format id this adapter serves; a region map naming this format is routed here.
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Reads the normalized value of the region the locator addresses.
    /// </summary>
    /// <param name="file">The whole file's bytes.</param>
    /// <param name="locator">The opaque address of the region.</param>
    /// <returns>The region's normalized value, or <see langword="null"/> when the file cannot be parsed.</returns>
    string? Read(ReadOnlyMemory<byte> file, RegionLocator locator);

    /// <summary>
    /// Rewrites the one region the locator addresses to its corrected value, preserving every other byte.
    /// </summary>
    /// <param name="file">The whole file's bytes to rewrite.</param>
    /// <param name="locator">The opaque address of the region.</param>
    /// <param name="value">The corrected normalized value to write into the region.</param>
    /// <returns>The rewritten file's bytes, or <see langword="null"/> when the file cannot be parsed.</returns>
    ReadOnlyMemory<byte>? Rewrite(ReadOnlyMemory<byte> file, RegionLocator locator, string value);
}
