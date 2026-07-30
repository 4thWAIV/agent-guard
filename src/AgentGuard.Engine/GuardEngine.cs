// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The composition root: it wires the File Guard pipeline and the Claude Code host adapter from the frozen
/// interfaces, handing every dependency out through its interface. Pre and Post are separate processes that both
/// build this same graph, so the ruleset fingerprint and store layout agree across the two.
/// </summary>
public static class GuardEngine
{
    /// <summary>
    /// The stable name of the File Guard, used to key its Context records.
    /// </summary>
    public const string FileGuardName = "FileGuard";

    /// <summary>
    /// Builds the File Guard pipeline for the given options.
    /// </summary>
    /// <param name="options">The pipeline configuration.</param>
    /// <returns>The pipeline, as its interface.</returns>
    public static IPipeline CreatePipeline(GuardEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        string root = options.ProjectRoot;

        IPathCanonicalizer canonicalizer = PathCanonicalizer.Create();
        IFileReader fileReader = FileReader.Create();
        IProvider csharpProvider = CSharpProvider.Create();
        var providers = new List<IProvider> { csharpProvider };

        var sources = new List<IRuleSource>
        {
            SealedRuleSource.Create(canonicalizer, root),
            SystemRuleSource.Create(canonicalizer, root),
            ProviderRuleSource.Create(csharpProvider),
            ProjectRuleSource.Create(canonicalizer, root),
        };
        IProtectedSet protectedSet = ProtectedSet.Create(sources);

        var skipRules = new List<IDirectorySkipRule>
        {
            BuildOutputSkipRule.Create(),
            NamedDirectorySkipRule.Create(
                canonicalizer.Canonicalize(CoreSystemPaths.Absolute(root, CoreSystemPaths.SnapshotStoreRelative)).Value),
            NamedDirectorySkipRule.Create(
                canonicalizer.Canonicalize(CoreSystemPaths.Absolute(root, CoreSystemPaths.GrantStoreRelative)).Value),
        };
        IProtectedFileScanner scanner = ProtectedFileScanner.Create(canonicalizer, protectedSet, skipRules);

        string fingerprint = RulesetFingerprint.Compute(sources, providers, skipRules);
        ReadOnlyMemory<byte> grantPublicKey = options.GrantPublicKey.IsEmpty
            ? LoadGrantPublicKey(root)
            : options.GrantPublicKey;
        IGrantStore grantStore = GrantStore.Create(
            CoreSystemPaths.Absolute(root, CoreSystemPaths.GrantStoreRelative),
            root,
            grantPublicKey,
            canonicalizer,
            options.TimeProvider);

        var services = new FileGuardServices(protectedSet, scanner, fileReader, canonicalizer, grantStore);
        var configuration = new FileGuardConfiguration(
            FileGuardName,
            services,
            fingerprint,
            CoreSystemPaths.BashReferenceTokens,
            new FileGuardLimits(options.PerFileSnapshotByteCeiling, options.TotalSnapshotByteCeiling));
        IGuard fileGuard = FileGuard.Create(configuration);

        IGuardRegistry registry = GuardRegistry.Create(new[] { fileGuard });
        IContextStore store = ContextStore.Create(root, options.TimeProvider);
        IPrivilegedWriter privilegedWriter = PrivilegedWriter.Create();
        return Pipeline.Create(registry, store, privilegedWriter);
    }

    /// <summary>
    /// Creates the Claude Code host adapter.
    /// </summary>
    /// <returns>The host adapter, as its interface.</returns>
    public static IHostAdapter CreateClaudeCodeAdapter() => ClaudeCodeHostAdapter.Create();

    /// <summary>
    /// Returns the absolute snapshot-store base directory for a project, for diagnostics and tests.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <returns>The absolute snapshot-store base directory.</returns>
    public static string SnapshotStoreDirectory(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        return ContextStorePaths.BaseDirectory(projectRoot);
    }

    private static ReadOnlyMemory<byte> LoadGrantPublicKey(string projectRoot)
    {
        string keyPath = CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.GrantPublicKeyRelative);
        if (!File.Exists(keyPath))
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        try
        {
            byte[] key = Convert.FromBase64String(File.ReadAllText(keyPath).Trim());
            return key.Length == GrantStore.Ed25519PublicKeyLength ? key : ReadOnlyMemory<byte>.Empty;
        }
        catch (FormatException)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        catch (IOException)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
    }
}
