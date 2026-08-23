// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IRandomGenerator"/> adapter. It is the single class that implements the randomness seam, so it is
/// the ONE place the raw <c>Guid.NewGuid()</c> and <c>Path.GetRandomFileName()</c> are allowed (AG0011 and AG0020 exempt
/// exactly this owner class in this assembly, which sits in <c>AgentGuard.CrossPlatform</c> because the platform code
/// consumes it — the owner is the randomness concern, so a random file name has a home beside <c>NewGuid</c>). Every
/// other type obtains randomness through <see cref="IRandomGenerator"/> so tests stay deterministic. It is
/// <c>internal</c> with a <c>private</c> constructor (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class RandomGeneratorAdapter : IRandomGenerator
{
    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private RandomGeneratorAdapter()
    {
    }

    /// <inheritdoc />
    public Guid NewGuid() => Guid.NewGuid();

    /// <inheritdoc />
    public string GetRandomFileName() => Path.GetRandomFileName();

    /// <summary>
    /// Creates the random generator.
    /// </summary>
    /// <returns>The random generator, as its interface.</returns>
    internal static IRandomGenerator Create() => new RandomGeneratorAdapter();
}
