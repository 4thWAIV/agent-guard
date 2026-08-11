// Copyright (c) 4thWAIV. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using NuGet.Versioning;

namespace AgentGuard.Setup;

/// <summary>
/// SemVer precedence for the install downgrade check, including prerelease ordering. Delegates to NuGet's
/// <see cref="NuGetVersion"/> so <c>0.1.0-alpha</c> parses and <c>0.10.0</c> orders above <c>0.9.0</c> — never a
/// string comparison.
/// </summary>
internal static class SemVer
{
    /// <summary>
    /// Parses a version string with SemVer precedence.
    /// </summary>
    /// <param name="value">The version string, which may include prerelease and build-metadata components.</param>
    /// <param name="version">The parsed version when parsing succeeds.</param>
    /// <returns><see langword="true"/> when the value parses.</returns>
    internal static bool TryParse(string value, [NotNullWhen(true)] out NuGetVersion? version) =>
        NuGetVersion.TryParse(value, out version);

    /// <summary>
    /// Normalizes a version string, dropping build metadata; returns the input unchanged when it does not parse.
    /// </summary>
    /// <param name="value">The version string.</param>
    /// <returns>The normalized version, or the input when it does not parse.</returns>
    internal static string Normalize(string value) =>
        TryParse(value, out NuGetVersion? version) ? version.ToNormalizedString() : value;

    /// <summary>
    /// Compares two parsed versions by SemVer precedence.
    /// </summary>
    /// <param name="left">The left version.</param>
    /// <param name="right">The right version.</param>
    /// <returns>A negative, zero, or positive value per <see cref="System.IComparable{T}"/>.</returns>
    internal static int Compare(NuGetVersion left, NuGetVersion right) => left.CompareTo(right);
}
