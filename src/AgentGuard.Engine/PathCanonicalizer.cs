// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Resolves a path to the single canonical form every match is done against: absolute, with <c>.</c> and
/// <c>..</c> segments and symlinks resolved. It resolves symlinks on every existing component, including
/// intermediate directories, so a symlinked temp root or a symlinked target both reduce to the same real path.
/// It fails closed: a link it cannot read, or a resolution that exceeds the link-depth ceiling (a probable
/// cycle), throws rather than returning an under-resolved path that could slip a protected target past a matcher.
/// It routes the current-directory resolution through the owned <see cref="IEnvironment"/> and the raw link read
/// through the owned <see cref="IPlatformFileSystem"/>, so no raw path or link primitive lives here.
/// </summary>
internal sealed class PathCanonicalizer : IPathCanonicalizer
{
    private const int MaxLinkDepth = 60;

    private readonly IEnvironment _environment;
    private readonly IPlatformFileSystem _fileSystem;

    private PathCanonicalizer(IEnvironment environment, IPlatformFileSystem fileSystem)
    {
        _environment = environment;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public CanonicalPath Canonicalize(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        string full = Path.GetFullPath(path, _environment.GetCurrentDirectory());
        string resolved = Resolve(full, 0);
        return new CanonicalPath(resolved);
    }

    /// <summary>
    /// Creates a canonicalizer, drawing the environment and platform file system from the container.
    /// </summary>
    /// <param name="services">The OS/CLR service container the environment and platform file system are drawn from.</param>
    /// <returns>The canonicalizer, as its interface.</returns>
    internal static IPathCanonicalizer Create(ISystemServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return new PathCanonicalizer(services.Environment, services.Platform.FileSystem);
    }

    private string Resolve(string path, int depth)
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

        string? linkTarget = _fileSystem.ReadLinkTarget(candidate);
        if (linkTarget is null)
        {
            return candidate;
        }

        string linkAbsolute = Path.IsPathRooted(linkTarget)
            ? linkTarget
            : Path.Combine(resolvedParent, linkTarget);
        return Resolve(Path.GetFullPath(linkAbsolute, _environment.GetCurrentDirectory()), depth + 1);
    }
}
