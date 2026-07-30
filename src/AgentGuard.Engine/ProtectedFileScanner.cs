// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Lists the configurable (Provider and Project) protected files currently present on disk under the project
/// root. It walks the tree without descending into the Sealed skip-list locations and matches each remaining
/// candidate against the Protected Set. It uses <see cref="EnumerationOptions.IgnoreInaccessible"/> set to
/// <see langword="false"/> so an inaccessible directory throws rather than being silently skipped — an
/// incomplete walk therefore denies the call instead of hiding a file.
/// </summary>
internal sealed class ProtectedFileScanner : IProtectedFileScanner
{
    private static readonly EnumerationOptions WalkOptions = new()
    {
        IgnoreInaccessible = false,
        AttributesToSkip = FileAttributes.None,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
    };

    private readonly IPathCanonicalizer _canonicalizer;
    private readonly IProtectedSet _protectedSet;
    private readonly IReadOnlyList<IDirectorySkipRule> _skipRules;

    private ProtectedFileScanner(
        IPathCanonicalizer canonicalizer,
        IProtectedSet protectedSet,
        IReadOnlyList<IDirectorySkipRule> skipRules)
    {
        _canonicalizer = canonicalizer;
        _protectedSet = protectedSet;
        _skipRules = skipRules;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CanonicalPath>> ScanAsync(CallEnvironment environment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(environment);
        string root = _canonicalizer.Canonicalize(environment.ProjectRoot).Value;
        var results = new List<CanonicalPath>();
        if (!Directory.Exists(root))
        {
            return Task.FromResult<IReadOnlyList<CanonicalPath>>(results);
        }

        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string directory = pending.Pop();
            foreach (FileSystemInfo entry in new DirectoryInfo(directory).EnumerateFileSystemInfos("*", WalkOptions))
            {
                bool isReparsePoint = entry.Attributes.HasFlag(FileAttributes.ReparsePoint);
                if (entry is DirectoryInfo)
                {
                    if (!isReparsePoint && !ShouldSkip(entry.FullName))
                    {
                        pending.Push(entry.FullName);
                    }

                    continue;
                }

                CanonicalPath candidate = _canonicalizer.Canonicalize(entry.FullName);
                if (IsConfigurableProtected(candidate))
                {
                    results.Add(candidate);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<CanonicalPath>>(results);
    }

    /// <summary>
    /// Creates the scanner.
    /// </summary>
    /// <param name="canonicalizer">The canonicalizer used to resolve each candidate.</param>
    /// <param name="protectedSet">The Protected Set each candidate is matched against.</param>
    /// <param name="skipRules">The directory skip rules that prune the walk.</param>
    /// <returns>The scanner, as its interface.</returns>
    internal static IProtectedFileScanner Create(
        IPathCanonicalizer canonicalizer,
        IProtectedSet protectedSet,
        IReadOnlyList<IDirectorySkipRule> skipRules)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentNullException.ThrowIfNull(protectedSet);
        ArgumentNullException.ThrowIfNull(skipRules);
        return new ProtectedFileScanner(canonicalizer, protectedSet, skipRules);
    }

    private bool ShouldSkip(string directoryFullPath) =>
        _skipRules.Any(rule => rule.ShouldSkip(directoryFullPath));

    private bool IsConfigurableProtected(CanonicalPath candidate) =>
        _protectedSet.Match(candidate).Any(rule => rule.Origin is RuleOrigin.Provider or RuleOrigin.Project);
}
