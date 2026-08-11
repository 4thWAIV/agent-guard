// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Reflection;
using AgentGuard.Engine.Abstractions;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Verifies the build-version stamp (decisions 15-18): the scheme's shape and field ranges, and that every
/// project in a build carries the one shared id. The cross-CI-job hard fail (decision 25) is a CI step, not here.
/// </summary>
public sealed class VersionStampTests
{
    /// <summary>
    /// The Engine and the test project are separate projects; within one build they must carry the same stamped
    /// version (AssemblyVersion and SemVer). A per-project override would diverge here.
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
    /// MAJOR/MINOR are not asserted here — their single source is eng/version.props.
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
    /// with a single <c>+hash</c> segment (decisions 15-17); the hash is a short git hash (7+ hex, since
    /// <c>--short=7</c> is a minimum) or the local fallback.
    /// </summary>
    [Fact]
    public void SemVerMatchesTheScheme()
    {
        string semVer = typeof(VersionStampTests).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Matches(@"^\d+\.\d+\.\d+(-pre-release)?\+([0-9a-f]{7,}|local)$", semVer);
    }

    /// <summary>
    /// The two version forms must describe the same build: the SemVer third field equals
    /// <c>Day*43200 + Time</c> from the numeric version (decision 15). This catches a mismatch where the two
    /// forms were stamped from different inputs (e.g. partial CI globals).
    /// </summary>
    [Fact]
    public void SemVerCombinedEqualsDayTimesBaseplusTime()
    {
        Assembly assembly = typeof(VersionStampTests).Assembly;
        Version numeric = assembly.GetName().Version!;
        string semVer = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        // SemVer third field is the number after "MAJOR.MINOR." and before any "-pre-release" or "+hash".
        string third = semVer.Split('+')[0].Split('-')[0];
        long combined = long.Parse(
            third.AsSpan(third.LastIndexOf('.') + 1),
            provider: System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(((long)numeric.Build * 43200) + numeric.Revision, combined);
    }
}
