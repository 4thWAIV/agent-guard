// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Resolves a path to the single canonical form every match is done against: absolute, with <c>.</c> and
/// <c>..</c> segments and symlinks resolved. It resolves symlinks on every existing component, including
/// intermediate directories, so a symlinked temp root or a symlinked target both reduce to the same real path.
/// It fails closed: a link it cannot read, or a resolution that exceeds the link-depth ceiling (a probable
/// cycle), throws rather than returning an under-resolved path that could slip a protected target past a matcher.
/// </summary>
internal sealed class PathCanonicalizer : IPathCanonicalizer
{
    private const int MaxLinkDepth = 60;

    private PathCanonicalizer()
    {
    }

    /// <inheritdoc />
    public CanonicalPath Canonicalize(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        string full = Path.GetFullPath(path);
        string resolved = Resolve(full, 0);
        return new CanonicalPath(resolved);
    }

    /// <summary>
    /// Creates a canonicalizer.
    /// </summary>
    /// <returns>The canonicalizer, as its interface.</returns>
    internal static IPathCanonicalizer Create() => new PathCanonicalizer();

    private static string Resolve(string path, int depth)
    {
        if (depth > MaxLinkDepth)
        {
            throw new IOException($"Path canonicalization exceeded the link-depth ceiling (possible symlink cycle): {path}");
        }

        string? parent = Path.GetDirectoryName(path);
        if (parent is null)
        {
            return path;
        }

        string resolvedParent = Resolve(parent, depth + 1);
        string candidate = Path.Combine(resolvedParent, Path.GetFileName(path));

        string? linkTarget = ReadLinkTarget(candidate);
        if (linkTarget is null)
        {
            return candidate;
        }

        string linkAbsolute = Path.IsPathRooted(linkTarget)
            ? linkTarget
            : Path.Combine(resolvedParent, linkTarget);
        return Resolve(Path.GetFullPath(linkAbsolute), depth + 1);
    }

    private static string? ReadLinkTarget(string candidate)
    {
        // Deliberately does not swallow: a link-read failure must surface so the caller fails closed. A path
        // that is not a reparse point (or does not exist) returns null without throwing.
        FileSystemInfo info = Directory.Exists(candidate)
            ? new DirectoryInfo(candidate)
            : new FileInfo(candidate);
        return info.LinkTarget;
    }
}
