// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Engine;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class PathCanonicalizerTests
{
    [Fact]
    public void Canonicalize_ResolvesSymlinkToItsTarget()
    {
        // Pointed integration: a real symlink on real disk, created and resolved through the owned interfaces.
        ISystemServices services = SystemServicesBuilder.Real().Build();
        using var fixture = new FixtureProject();
        fixture.WriteFile("target.txt", "content");
        string linkPath = fixture.PathOf("link.txt");
        services.Platform.FileSystem.MakeLinkTarget(linkPath, "target.txt");

        CanonicalPath viaLink = GuardEngine.Canonicalize(services, linkPath);
        CanonicalPath viaTarget = GuardEngine.Canonicalize(services, fixture.PathOf("target.txt"));

        viaLink.Value.Should().Be(viaTarget.Value);
    }

    [Fact]
    public void Canonicalize_NormalizesDotDotSegments()
    {
        ISystemServices services = SystemServicesBuilder.Real().Build();
        using var fixture = new FixtureProject();

        CanonicalPath dotted = GuardEngine.Canonicalize(services, Path.Combine(fixture.Root, "sub", "..", "Directory.Build.props"));
        CanonicalPath direct = GuardEngine.Canonicalize(services, fixture.PathOf("Directory.Build.props"));

        dotted.Value.Should().Be(direct.Value);
    }
}
