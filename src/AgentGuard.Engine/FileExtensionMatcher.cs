// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Matches any canonical path whose final segment ends with a given suffix, wherever it appears in the tree.
/// Used for the Provider rule that protects every <c>*.globalconfig</c>.
/// </summary>
internal sealed class FileExtensionMatcher : IPathMatcher
{
    private readonly string _suffix;

    private FileExtensionMatcher(string suffix) => _suffix = suffix;

    /// <inheritdoc />
    public bool Matches(CanonicalPath canonicalPath)
    {
        string fileName = Path.GetFileName(canonicalPath.Value);
        return fileName.Length > _suffix.Length
            && fileName.EndsWith(_suffix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a matcher for one file-name suffix (for example <c>.globalconfig</c>).
    /// </summary>
    /// <param name="suffix">The suffix to match.</param>
    /// <returns>The matcher, as its interface.</returns>
    internal static IPathMatcher Create(string suffix)
    {
        ArgumentException.ThrowIfNullOrEmpty(suffix);
        return new FileExtensionMatcher(suffix);
    }
}
