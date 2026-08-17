// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IGuidFactory"/> adapter. It is the single class that implements the GUID seam, so it is the ONE
/// place the raw <c>Guid.NewGuid()</c> is allowed (AG0014 exempts exactly this owner class in this assembly, which sits
/// in <c>AgentGuard.CrossPlatform</c> because the platform code consumes it — guid-seam-lives-in-crossplatform). Every
/// other type obtains a GUID through <see cref="IGuidFactory"/> so tests stay deterministic. It is <c>internal</c> with
/// a <c>private</c> constructor (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class GuidFactoryAdapter : IGuidFactory
{
    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private GuidFactoryAdapter()
    {
    }

    /// <inheritdoc />
    public Guid NewGuid() => Guid.NewGuid();

    /// <summary>
    /// Creates the GUID factory.
    /// </summary>
    /// <returns>The GUID factory, as its interface.</returns>
    internal static IGuidFactory Create() => new GuidFactoryAdapter();
}
