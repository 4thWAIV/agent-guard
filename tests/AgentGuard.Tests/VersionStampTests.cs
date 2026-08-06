// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Reflection;
using AgentGuard.Engine.Abstractions;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Verifies the build-version stamp (decisions 15-18, 25): the scheme's shape and field ranges, and the
/// compute-once guarantee that every project in a build carries the one shared version.
/// </summary>
public sealed class VersionStampTests
{
    /// <summary>
    /// Within one build every project must carry the same stamped version (AssemblyVersion and SemVer); a
    /// per-project override would diverge here. The full compute-once guarantee <em>across separate CI jobs</em>
    /// — decision 25's hard fail — is enforced by a CI step, not this in-process test.
    /// </summary>
    [Fact]
    public void EveryProjectInThisBuildCarriesTheSameVersion()
    {
        Assembly engine = typeof(CanonicalPath).Assembly;
        Assembly tests = typeof(VersionStampTests).Assembly;
        Assert.NotNull(engine.GetName().Version);
        Assert.Equal(engine.GetName().Version, tests.GetName().Version);
        Assert.Equal(
            engine.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            tests.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
    }

    /// <summary>
    /// AssemblyVersion is <c>MAJOR.MINOR.Day.Time</c>; the computed Day/Time fields must sit inside the UInt16
    /// range, and Time is <c>floor(seconds-since-midnight / 2)</c> so it never exceeds 43199 (decision 15).
    /// MAJOR/MINOR are not asserted here — their single source is eng/version.props (decision 15).
    /// </summary>
    [Fact]
    public void AssemblyVersionFieldsAreWithinRange()
    {
        Version version = typeof(VersionStampTests).Assembly.GetName().Version!;
        Assert.InRange(version.Build, 0, 65535);
        Assert.InRange(version.Revision, 0, 43199);
    }

    /// <summary>
    /// The SemVer (informational) version is <c>MAJOR.MINOR.&lt;Day*43200+Time&gt;[-pre-release]+&lt;hash&gt;</c>
    /// with a single <c>+hash</c> segment (decisions 15-17); the hash is a short git hash or the local fallback.
    /// MAJOR.MINOR is matched generically (<c>\d+\.\d+</c>) so bumping the single source doesn't break this.
    /// </summary>
    [Fact]
    public void SemVerMatchesTheScheme()
    {
        string semVer = typeof(VersionStampTests).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Matches(@"^\d+\.\d+\.\d+(-pre-release)?\+([0-9a-f]{7}|local)$", semVer);
    }
}
