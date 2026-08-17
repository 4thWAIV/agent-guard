// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Lists the configurable (Provider and Project) protected files currently present on disk under the project root.
/// It walks the tree through <see cref="IDirectoryEnumerator"/> without descending into the Sealed skip-list
/// locations and matches each remaining candidate against the Protected Set. The enumerator surfaces an
/// inaccessible directory as an exception (never silently skipped), so an incomplete walk propagates and denies the
/// call instead of hiding a file. The scanner keeps all its policy over <see cref="DirectoryChild"/> and touches no
/// <c>System.IO</c> filesystem type itself.
/// </summary>
internal sealed class ProtectedFileScanner : IProtectedFileScanner
{
    private readonly IPathCanonicalizer _canonicalizer;
    private readonly IProtectedSet _protectedSet;
    private readonly IRegionMapRegistry _regionRegistry;
    private readonly IReadOnlyList<IDirectorySkipRule> _skipRules;
    private readonly IDirectoryEnumerator _enumerator;

    private ProtectedFileScanner(
        IPathCanonicalizer canonicalizer,
        IProtectedSet protectedSet,
        IRegionMapRegistry regionRegistry,
        IReadOnlyList<IDirectorySkipRule> skipRules,
        IDirectoryEnumerator enumerator)
    {
        _canonicalizer = canonicalizer;
        _protectedSet = protectedSet;
        _regionRegistry = regionRegistry;
        _skipRules = skipRules;
        _enumerator = enumerator;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CanonicalPath>> ScanAsync(CallEnvironment environment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(environment);
        string root = _canonicalizer.Canonicalize(environment.ProjectRoot).Value;
        var results = new List<CanonicalPath>();
        if (!_enumerator.DirectoryExists(root))
        {
            return Task.FromResult<IReadOnlyList<CanonicalPath>>(results);
        }

        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string directory = pending.Pop();
            foreach (DirectoryChild entry in _enumerator.EnumerateChildren(directory))
            {
                if (entry.IsDirectory)
                {
                    if (!entry.IsReparsePoint && !ShouldSkip(entry.FullPath))
                    {
                        pending.Push(entry.FullPath);
                    }

                    continue;
                }

                CanonicalPath candidate = _canonicalizer.Canonicalize(entry.FullPath);
                if (IsConfigurableProtected(candidate) || _regionRegistry.IsRegistered(candidate))
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
    /// <param name="regionRegistry">The region-map registry whose watched files join the after-check set.</param>
    /// <param name="skipRules">The directory skip rules that prune the walk.</param>
    /// <param name="services">The OS/CLR service container the directory enumerator is drawn from; the walk fails
    /// closed on access errors.</param>
    /// <returns>The scanner, as its interface.</returns>
    internal static IProtectedFileScanner Create(
        IPathCanonicalizer canonicalizer,
        IProtectedSet protectedSet,
        IRegionMapRegistry regionRegistry,
        IReadOnlyList<IDirectorySkipRule> skipRules,
        ISystemServices services)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentNullException.ThrowIfNull(protectedSet);
        ArgumentNullException.ThrowIfNull(regionRegistry);
        ArgumentNullException.ThrowIfNull(skipRules);
        ArgumentNullException.ThrowIfNull(services);
        return new ProtectedFileScanner(canonicalizer, protectedSet, regionRegistry, skipRules, services.FileSystem.GetDirectoryReader());
    }

    private bool ShouldSkip(string directoryFullPath) =>
        _skipRules.Any(rule => rule.ShouldSkip(directoryFullPath));

    private bool IsConfigurableProtected(CanonicalPath candidate) =>
        _protectedSet.Match(candidate).Any(rule => rule.Origin is RuleOrigin.Provider or RuleOrigin.Project);
}
