// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Globalization;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The deterministic <see cref="IRandomGenerator"/> fake: <see cref="NewGuid"/> returns a fixed GUID (chosen through
/// <see cref="Create(System.Guid)"/>, so a test that keys on the value can pick it) and <see cref="GetRandomFileName"/>
/// returns a stable, sequential name, so a test run is reproducible. Temp-directory uniqueness never relies on this fake
/// — the overlay's own counter guarantees it (copy-on-write-simulator-design) — so a constant GUID is safe. It reaches
/// for neither <c>Guid.NewGuid()</c> nor <c>Path.GetRandomFileName()</c> (both owned elsewhere); every value is
/// synthesized in memory. Its constructor is private (AG0003) and it is handed out only as its interface through the
/// static factories.
/// </summary>
public sealed class FixedGuidFactory : IRandomGenerator
{
    private static readonly Guid DefaultGuid = new("00000000-0000-0000-0000-0000000000AA");

    private readonly Guid _fixed;
    private int _fileNameCounter;

    private FixedGuidFactory(Guid fixedGuid) => _fixed = fixedGuid;

    /// <summary>Creates the fake with the default fixed GUID.</summary>
    /// <returns>The random-generator fake, as its interface.</returns>
    public static IRandomGenerator Create() => new FixedGuidFactory(DefaultGuid);

    /// <summary>Creates the fake with an explicit fixed GUID.</summary>
    /// <param name="fixedGuid">The GUID <see cref="NewGuid"/> returns on every call.</param>
    /// <returns>The random-generator fake, as its interface.</returns>
    public static IRandomGenerator Create(Guid fixedGuid) => new FixedGuidFactory(fixedGuid);

    /// <inheritdoc />
    public Guid NewGuid() => _fixed;

    /// <inheritdoc />
    public string GetRandomFileName()
    {
        int ordinal = ++_fileNameCounter;
        return "random-" + ordinal.ToString(CultureInfo.InvariantCulture) + ".tmp";
    }
}
