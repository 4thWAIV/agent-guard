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
/// The File Guard: it blocks writes to the Sealed and System core system at Pre, captures a per-call pre-image
/// of the whole configurable protected set, and at Post reverts any unauthorized drift to a configurable
/// protected file — including drift from a shell command whose target Pre never parsed. Every failure path
/// fails closed.
/// </summary>
internal sealed class FileGuard : IGuard
{
    private const string SnapshotKind = "snapshot";

    private readonly string _name;
    private readonly IProtectedSet _protectedSet;
    private readonly IProtectedFileScanner _scanner;
    private readonly IFileReader _fileReader;
    private readonly IPathCanonicalizer _canonicalizer;
    private readonly IGrantStore _grantStore;
    private readonly IVerifier _defaultVerifier;
    private readonly string _rulesetFingerprint;
    private readonly IReadOnlyList<string> _coreSystemPathTokens;
    private readonly long _perFileByteCeiling;
    private readonly long _totalByteCeiling;

    private FileGuard(FileGuardConfiguration configuration)
    {
        _name = configuration.Name;
        _protectedSet = configuration.Services.ProtectedSet;
        _scanner = configuration.Services.Scanner;
        _fileReader = configuration.Services.FileReader;
        _canonicalizer = configuration.Services.Canonicalizer;
        _grantStore = configuration.Services.GrantStore;
        _defaultVerifier = NoChangeVerifier.Create();
        _rulesetFingerprint = configuration.RulesetFingerprint;
        _coreSystemPathTokens = configuration.CoreSystemPathTokens;
        _perFileByteCeiling = configuration.Limits.PerFileByteCeiling;
        _totalByteCeiling = configuration.Limits.TotalByteCeiling;
    }

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public async Task<Verdict> PrecheckAsync(
        ToolCall toolCall,
        CallEnvironment environment,
        IContextStoreInspector store,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(store);
        StoreInspection inspection = await store.InspectAsync(cancellationToken).ConfigureAwait(false);
        if (inspection.IsAnomalous)
        {
            return Verdict.Deny($"Snapshot store anomaly, failing closed: {inspection.Reason}");
        }

        return toolCall.Input switch
        {
            FileWriteInput write => await PrecheckWriteAsync(write, cancellationToken).ConfigureAwait(false),
            ShellCommandInput shell => PrecheckShell(shell),
            _ => Verdict.Allow(),
        };
    }

    /// <inheritdoc />
    public async Task<CaptureResult> CaptureAsync(
        ToolCall toolCall,
        CallEnvironment environment,
        IContextWriter context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(context);
        IReadOnlyList<CanonicalPath> paths;
        try
        {
            paths = await _scanner.ScanAsync(environment, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException exception)
        {
            return new CaptureFailed($"The protected tree could not be scanned: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return new CaptureFailed($"The protected tree could not be scanned: {exception.Message}");
        }

        return await WriteSnapshotAsync(paths, context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PostcheckResult> PostcheckAsync(
        ToolCall toolCall,
        CallEnvironment environment,
        IContextReader context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(context);
        ContextRead read = await context.ReadAsync(SnapshotKind, cancellationToken).ConfigureAwait(false);
        if (read is not ContextFound found)
        {
            return Deny("No pre-image snapshot exists for this call; failing closed.");
        }

        if (!SnapshotSerializer.TryDeserialize(found.Data, out SnapshotData? snapshot) || snapshot is null)
        {
            return Deny("The pre-image snapshot is corrupt; failing closed.");
        }

        if (!string.Equals(snapshot.RulesetFingerprint, _rulesetFingerprint, StringComparison.Ordinal))
        {
            return Deny("The ruleset changed between capture and post; refusing to diff across a changed membership.");
        }

        Dictionary<string, ReadOnlyMemory<byte>>? current = await TryReadCurrentAsync(environment, cancellationToken)
            .ConfigureAwait(false);
        if (current is null)
        {
            return Deny("Post could not scan or read the tree after a retry; failing closed with no effects.");
        }

        var preImage = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (SnapshotEntry entry in snapshot.Entries.Where(entry => entry.Present))
        {
            preImage[entry.Path] = entry.Content;
        }

        IReadOnlyList<FileChange> changes = SnapshotDiffer.Diff(preImage, current);
        return changes.Count == 0
            ? PostcheckResult.Allow()
            : await ReconcileAsync(changes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the File Guard from its configuration.
    /// </summary>
    /// <param name="configuration">The guard's collaborators, fingerprint, tokens, and limits.</param>
    /// <returns>The guard, as its interface.</returns>
    internal static IGuard Create(FileGuardConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new FileGuard(configuration);
    }

    private static PostcheckResult Deny(string reason) =>
        new(Verdict.Deny(reason), Array.Empty<Effect>());

    private static Effect BuildEffect(FileChange change) => change switch
    {
        FileCreated created => new DeleteFileEffect(created.Path),
        FileModified modified => new RestoreFileEffect(modified.Path, modified.Before),
        FileRemoved removed => new RestoreFileEffect(removed.Path, removed.Before),
        _ => throw new InvalidOperationException($"Unhandled change kind: {change.GetType().Name}"),
    };

    private async Task<CaptureResult> WriteSnapshotAsync(
        IReadOnlyList<CanonicalPath> paths,
        IContextWriter context,
        CancellationToken cancellationToken)
    {
        var entries = new List<SnapshotEntry>(paths.Count);
        var captured = new List<string>(paths.Count);
        long total = 0;
        foreach (string pathValue in paths.Select(path => path.Value))
        {
            ReadOnlyMemory<byte> bytes;
            try
            {
                bytes = await _fileReader.ReadAsync(pathValue, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException exception)
            {
                return new CaptureFailed($"A protected file could not be read: {pathValue}: {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                return new CaptureFailed($"A protected file could not be read: {pathValue}: {exception.Message}");
            }

            if (bytes.Length > _perFileByteCeiling)
            {
                return new CaptureFailed($"A protected file exceeds the per-file snapshot ceiling: {pathValue}");
            }

            total += bytes.Length;
            if (total > _totalByteCeiling)
            {
                return new CaptureFailed("The protected set exceeds the total snapshot ceiling.");
            }

            entries.Add(new SnapshotEntry(pathValue, Present: true, bytes));
            captured.Add(pathValue);
        }

        byte[] serialized = SnapshotSerializer.Serialize(new SnapshotData(_rulesetFingerprint, entries));
        try
        {
            await context.WriteAsync(SnapshotKind, serialized, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException exception)
        {
            return new CaptureFailed($"The snapshot could not be written: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return new CaptureFailed($"The snapshot could not be written: {exception.Message}");
        }

        return new CaptureSucceeded(captured);
    }

    private async Task<PostcheckResult> ReconcileAsync(
        IReadOnlyList<FileChange> changes,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Grant> grants = await _grantStore.GetActiveGrantsAsync(cancellationToken).ConfigureAwait(false);
        var effects = new List<Effect>();
        var reasons = new List<string>();
        foreach (FileChange change in changes)
        {
            var canonicalPath = new CanonicalPath(change.Path);
            if (CoverageChecker.FindAuthorizing(grants, canonicalPath) is not null)
            {
                continue;
            }

            Grant? covering = CoverageChecker.FindCovering(grants, canonicalPath);
            IVerifier verifier = SelectVerifier(canonicalPath);
            Verdict verdict = await verifier.VerifyAsync(change, covering, cancellationToken).ConfigureAwait(false);
            if (verdict.Kind == VerdictKind.Deny)
            {
                effects.Add(BuildEffect(change));
                reasons.Add(verdict.Message ?? change.Path);
            }
        }

        if (effects.Count == 0)
        {
            return PostcheckResult.Allow();
        }

        string reason = "Reverted unauthorized drift to the protected set:\n" + string.Join("\n", reasons);
        return new PostcheckResult(Verdict.Deny(reason), effects);
    }

    private async Task<Verdict> PrecheckWriteAsync(FileWriteInput write, CancellationToken cancellationToken)
    {
        IReadOnlyList<Grant>? grants = null;
        foreach (string rawPath in write.Paths)
        {
            CanonicalPath canonicalPath = _canonicalizer.Canonicalize(rawPath);
            IReadOnlyList<Rule> matched = _protectedSet.Match(canonicalPath);
            if (matched.Any(rule => rule.Origin == RuleOrigin.Sealed))
            {
                return Verdict.Deny($"A write to a Sealed core-system path is never allowed: {rawPath}");
            }

            if (matched.Any(rule => rule.Origin == RuleOrigin.System))
            {
                grants ??= await _grantStore.GetActiveGrantsAsync(cancellationToken).ConfigureAwait(false);
                if (CoverageChecker.FindAuthorizing(grants, canonicalPath) is null)
                {
                    return Verdict.Deny($"A write to a System path requires an authorizing grant: {rawPath}");
                }
            }
        }

        return Verdict.Allow();
    }

    private Verdict PrecheckShell(ShellCommandInput shell)
    {
        string? token = _coreSystemPathTokens
            .FirstOrDefault(token => shell.Command.Contains(token, StringComparison.Ordinal));
        return token is null
            ? Verdict.Allow()
            : Verdict.Deny($"The command references the core-system path token '{token}' and is denied at Pre.");
    }

    private async Task<Dictionary<string, ReadOnlyMemory<byte>>?> TryReadCurrentAsync(
        CallEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ReadCurrentOnceAsync(environment, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // First attempt failed transiently; retry once below before failing closed.
        }
        catch (UnauthorizedAccessException)
        {
            // First attempt failed transiently; retry once below before failing closed.
        }

        try
        {
            return await ReadCurrentOnceAsync(environment, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async Task<Dictionary<string, ReadOnlyMemory<byte>>> ReadCurrentOnceAsync(
        CallEnvironment environment,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CanonicalPath> paths = await _scanner.ScanAsync(environment, cancellationToken)
            .ConfigureAwait(false);
        var map = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (string pathValue in paths.Select(path => path.Value))
        {
            map[pathValue] = await _fileReader.ReadAsync(pathValue, cancellationToken).ConfigureAwait(false);
        }

        return map;
    }

    private IVerifier SelectVerifier(CanonicalPath canonicalPath)
    {
        Rule? rule = _protectedSet.Match(canonicalPath)
            .FirstOrDefault(rule => rule.Origin is RuleOrigin.Provider or RuleOrigin.Project);
        return rule?.Verifier ?? _defaultVerifier;
    }
}
