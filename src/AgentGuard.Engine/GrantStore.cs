// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Loads the active Grants for the current repository: it reads every token in the grant directory,
/// Ed25519-verifies each against the committed public key through the owned <see cref="ISignatureService"/>, drops
/// any that are malformed, unverifiable, or expired, and returns the rest. It never mints. A missing key or an
/// unreadable directory yields no active Grants, which fails closed — System writes stay denied and drift stays
/// reverted. No cryptographic library type crosses into this class; verification is the owned service's concern.
/// </summary>
internal sealed class GrantStore : IGrantStore
{
    /// <summary>
    /// The byte length of a raw Ed25519 public key. A key of any other length is malformed and disables grants.
    /// </summary>
    internal const int Ed25519PublicKeyLength = 32;

    private readonly string _grantDirectory;
    private readonly string _projectRoot;
    private readonly IPathCanonicalizer _canonicalizer;
    private readonly TimeProvider _timeProvider;
    private readonly ISignatureService _signatures;
    private readonly IFileReader _fileReader;
    private readonly IDirectoryEnumerator _directories;
    private readonly ReadOnlyMemory<byte> _publicKey;

    private GrantStore(
        string grantDirectory,
        string projectRoot,
        IPathCanonicalizer canonicalizer,
        TimeProvider timeProvider,
        ISignatureService signatures,
        IFileReader fileReader,
        IDirectoryEnumerator directories,
        ReadOnlyMemory<byte> publicKey)
    {
        _grantDirectory = grantDirectory;
        _projectRoot = projectRoot;
        _canonicalizer = canonicalizer;
        _timeProvider = timeProvider;
        _signatures = signatures;
        _fileReader = fileReader;
        _directories = directories;
        _publicKey = publicKey;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Grant>> GetActiveGrantsAsync(CancellationToken cancellationToken)
    {
        var active = new List<Grant>();
        if (_publicKey.IsEmpty || !_directories.DirectoryExists(_grantDirectory))
        {
            return active;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        var options = new EnumerationOptions { IgnoreInaccessible = false };
        foreach (string file in _directories.EnumerateFiles(_grantDirectory, "*.token", options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Grant? grant = await TryLoadAsync(file, now, cancellationToken).ConfigureAwait(false);
            if (grant is not null)
            {
                active.Add(grant);
            }
        }

        return active;
    }

    /// <summary>
    /// Creates the grant store, drawing its signature verifier, filesystem owners, and clock from the container.
    /// </summary>
    /// <param name="services">The OS/CLR service container the store verifies, reads, and times against.</param>
    /// <param name="grantDirectory">The absolute directory holding grant tokens.</param>
    /// <param name="projectRoot">The absolute project root a relative covered path resolves against.</param>
    /// <param name="publicKey">The raw 32-byte Ed25519 public key; empty or malformed disables grants.</param>
    /// <param name="canonicalizer">The canonicalizer used to resolve covered paths.</param>
    /// <returns>The grant store, as its interface.</returns>
    internal static IGrantStore Create(
        ISystemServices services,
        string grantDirectory,
        string projectRoot,
        ReadOnlyMemory<byte> publicKey,
        IPathCanonicalizer canonicalizer)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(grantDirectory);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ReadOnlyMemory<byte> validatedKey = publicKey.Length == Ed25519PublicKeyLength
            ? publicKey
            : ReadOnlyMemory<byte>.Empty;
        return new GrantStore(
            grantDirectory,
            projectRoot,
            canonicalizer,
            services.Clock,
            services.Signatures,
            services.FileSystem.GetFileReader(),
            services.FileSystem.GetDirectoryReader(),
            validatedKey);
    }

    private async Task<Grant?> TryLoadAsync(string file, DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            string text = await _fileReader.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            GrantToken token = GrantTokenCodec.Deserialize(text);
            if (!IsSignatureValid(token))
            {
                return null;
            }

            if (token.Payload.ExpiresAt <= now)
            {
                return null;
            }

            return BuildGrant(token.Payload);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private bool IsSignatureValid(GrantToken token)
    {
        byte[] message = GrantTokenCodec.CanonicalBytes(token.Payload);
        byte[] signature = Convert.FromBase64String(token.Signature);
        return _signatures.Verify(_publicKey, message, signature);
    }

    private Grant BuildGrant(GrantTokenPayload payload)
    {
        var matchers = new List<IPathMatcher>();
        foreach (string coveredPath in payload.CoveredPaths)
        {
            matchers.Add(PatternMatcherFactory.Create(_canonicalizer, _projectRoot, coveredPath));
        }

        return new Grant(payload.Id, payload.Scope, matchers, payload.ExpiresAt);
    }
}
