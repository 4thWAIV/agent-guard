// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Setup;
using FluentAssertions;
using NuGet.Versioning;
using Xunit;

namespace AgentGuard.Tests;

public sealed class SetupSemVerTests
{
    [Fact]
    public void Compare_OrdersByPrecedenceNotString()
    {
        SemVer.TryParse("0.10.0", out NuGetVersion? big).Should().BeTrue();
        SemVer.TryParse("0.9.0", out NuGetVersion? small).Should().BeTrue();

        SemVer.Compare(big!, small!).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Parse_HandlesPrereleaseAndOrdersBelowRelease()
    {
        SemVer.TryParse("0.1.0-alpha", out NuGetVersion? prerelease).Should().BeTrue();
        SemVer.TryParse("0.1.0", out NuGetVersion? release).Should().BeTrue();

        SemVer.Compare(prerelease!, release!).Should().BeLessThan(0);
        prerelease!.ToNormalizedString().Should().Be("0.1.0-alpha");
    }
}
