// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Exercises the real <c>BuildInfoReader</c> in <c>AgentGuard.Boundaries</c> through the owned <see cref="IBuildInfo"/>
/// off a real container (<see cref="SystemServicesBuilder.Real"/>) — no fake exists by design, so these read the real
/// version attributes the build stamped onto the guard assembly and assert they are present and well-formed.
/// </summary>
public sealed class BoundariesBuildInfoTests
{
    private readonly IBuildInfo buildInfo = SystemServicesBuilder.Real().Build().BuildInfo;

    [Fact]
    public void SemVer_IsStampedAndNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(this.buildInfo.SemVer));
    }

    [Fact]
    public void AssemblyVersion_IsAParseableVersion()
    {
        Assert.True(
            Version.TryParse(this.buildInfo.AssemblyVersion, out _),
            $"AssemblyVersion '{this.buildInfo.AssemblyVersion}' is not a parseable version.");
    }

    [Fact]
    public void FileVersion_IsAParseableVersion()
    {
        Assert.True(
            Version.TryParse(this.buildInfo.FileVersion, out _),
            $"FileVersion '{this.buildInfo.FileVersion}' is not a parseable version.");
    }
}
