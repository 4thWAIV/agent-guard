// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Matches any canonical path whose final segment is a given file name, wherever it appears in the tree. Used
/// for the Provider rules that protect every instance of a build-config file (for example
/// <c>Directory.Build.props</c>).
/// </summary>
internal sealed class FileNameMatcher : IPathMatcher
{
    private readonly string _fileName;

    private FileNameMatcher(string fileName) => _fileName = fileName;

    /// <inheritdoc />
    public bool Matches(CanonicalPath canonicalPath) =>
        string.Equals(Path.GetFileName(canonicalPath.Value), _fileName, StringComparison.Ordinal);

    /// <summary>
    /// Creates a matcher for one exact file name.
    /// </summary>
    /// <param name="fileName">The file name to match.</param>
    /// <returns>The matcher, as its interface.</returns>
    internal static IPathMatcher Create(string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        return new FileNameMatcher(fileName);
    }
}
