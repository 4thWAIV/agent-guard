// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Setup;

namespace AgentGuard.Engine;

/// <summary>
/// The composition root: it wires the File Guard pipeline and the Claude Code host adapter from the frozen
/// interfaces, handing every dependency out through its interface. Pre and Post are separate processes that both
/// build this same graph, so the ruleset fingerprint and store layout agree across the two. Every OS/CLR service
/// is pulled off the single <see cref="ISystemServices"/> container on the options and handed to each class by
/// constructor.
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
    public static IPipeline CreatePipeline(GuardEngineOptions options) =>
        CreatePipeline(options, regionMapRegistry: null);

    /// <summary>
    /// Creates the Claude Code host adapter, drawing the owned environment from the container.
    /// </summary>
    /// <param name="services">The OS/CLR service container the environment is drawn from.</param>
    /// <returns>The host adapter, as its interface.</returns>
    public static IHostAdapter CreateClaudeCodeAdapter(ISystemServices services) => ClaudeCodeHostAdapter.Create(services);

    /// <summary>
    /// Returns the absolute snapshot-store base directory for a project, for diagnostics and tests.
    /// </summary>
    /// <param name="services">The OS/CLR service container the store-path owner draws its environment from.</param>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <returns>The absolute snapshot-store base directory.</returns>
    public static string SnapshotStoreDirectory(ISystemServices services, string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        return ContextStorePaths.Create(services).BaseDirectory(projectRoot);
    }

    /// <summary>
    /// Builds the File Guard pipeline, optionally with a supplied region-map registry so a test can drive the
    /// fail-closed path where a file is registered as protected yet produces no region/adapter to adjudicate it.
    /// The directory enumerator (and every other boundary service) is drawn from the container on the options, so a
    /// test drives the fail-closed enumeration path by substituting the directory enumerator
    /// (<see cref="IFileSystem.GetDirectoryReader"/>) through the builder.
    /// </summary>
    /// <param name="options">The pipeline configuration.</param>
    /// <param name="regionMapRegistry">The registry to use, or <see langword="null"/> to build the default.</param>
    /// <returns>The pipeline, as its interface.</returns>
    internal static IPipeline CreatePipeline(
        GuardEngineOptions options,
        IRegionMapRegistry? regionMapRegistry)
    {
        ArgumentNullException.ThrowIfNull(options);
        ISystemServices services = options.Services;
        string root = options.ProjectRoot;

        IPathCanonicalizer canonicalizer = PathCanonicalizer.Create(services);
        IProvider csharpProvider = CSharpProvider.Create();
        var providers = new List<IProvider> { csharpProvider };

        var sources = new List<IRuleSource>
        {
            SealedRuleSource.Create(canonicalizer, root, services.Platform.FileSystem.DirectorySeparator),
            SystemRuleSource.Create(canonicalizer, root),
            ProviderRuleSource.Create(csharpProvider),
            ProjectRuleSource.Create(services, canonicalizer, root),
        };
        IProtectedSet protectedSet = ProtectedSet.Create(sources);

        IRegionMapRegistry regionRegistry = regionMapRegistry ?? RegionMapRegistry.CreateDefault(
            canonicalizer, root, MachinePaths.BinGuardIn(services.Environment.GetHomeDirectory()));

        var skipRules = new List<IDirectorySkipRule>
        {
            BuildOutputSkipRule.Create(services),
            NamedDirectorySkipRule.Create(
                canonicalizer.Canonicalize(CoreSystemPaths.Absolute(root, CoreSystemPaths.SnapshotStoreRelative)).Value),
            NamedDirectorySkipRule.Create(
                canonicalizer.Canonicalize(CoreSystemPaths.Absolute(root, CoreSystemPaths.GrantStoreRelative)).Value),
        };
        IProtectedFileScanner scanner = ProtectedFileScanner.Create(
            canonicalizer, protectedSet, regionRegistry, skipRules, services);

        string fingerprint = RulesetFingerprint.Compute(sources, providers, skipRules);
        ReadOnlyMemory<byte> grantPublicKey = options.GrantPublicKey.IsEmpty
            ? LoadGrantPublicKey(services, root)
            : options.GrantPublicKey;
        IGrantStore grantStore = GrantStore.Create(
            services,
            CoreSystemPaths.Absolute(root, CoreSystemPaths.GrantStoreRelative),
            root,
            grantPublicKey,
            canonicalizer);

        var guardServices = new FileGuardServices(protectedSet, scanner, services.FileSystem.GetFileReader(), canonicalizer, grantStore, regionRegistry);
        var configuration = new FileGuardConfiguration(
            FileGuardName,
            guardServices,
            fingerprint,
            CoreSystemPaths.BashReferenceTokens,
            new FileGuardLimits(options.PerFileSnapshotByteCeiling, options.TotalSnapshotByteCeiling));
        IGuard fileGuard = FileGuard.Create(configuration);

        IGuardRegistry registry = GuardRegistry.Create(new[] { fileGuard });
        ContextStorePaths paths = ContextStorePaths.Create(services);
        IContextStore store = ContextStore.Create(services, paths, root);
        IPrivilegedWriter privilegedWriter = PrivilegedWriter.Create(services);
        return Pipeline.Create(registry, store, privilegedWriter, services, paths);
    }

    private static ReadOnlyMemory<byte> LoadGrantPublicKey(ISystemServices services, string projectRoot)
    {
        string keyPath = CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.GrantPublicKeyRelative);
        if (!services.FileSystem.GetFileReader().Exists(keyPath))
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        try
        {
            byte[] key = Convert.FromBase64String(services.FileSystem.GetFileReader().ReadAllText(keyPath).Trim());
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
