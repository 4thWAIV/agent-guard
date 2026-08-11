// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using AgentGuard.Engine;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class PathCanonicalizerTests
{
    [Fact]
    public void Canonicalize_ResolvesSymlinkToItsTarget()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile("target.txt", "content");
        string linkPath = fixture.PathOf("link.txt");
        File.CreateSymbolicLink(linkPath, fixture.PathOf("target.txt"));
        IPathCanonicalizer canonicalizer = PathCanonicalizer.Create();

        CanonicalPath viaLink = canonicalizer.Canonicalize(linkPath);
        CanonicalPath viaTarget = canonicalizer.Canonicalize(fixture.PathOf("target.txt"));

        viaLink.Value.Should().Be(viaTarget.Value);
    }

    [Fact]
    public void Canonicalize_NormalizesDotDotSegments()
    {
        using var fixture = new FixtureProject();
        IPathCanonicalizer canonicalizer = PathCanonicalizer.Create();

        CanonicalPath dotted = canonicalizer.Canonicalize(Path.Combine(fixture.Root, "sub", "..", "Directory.Build.props"));
        CanonicalPath direct = canonicalizer.Canonicalize(fixture.PathOf("Directory.Build.props"));

        dotted.Value.Should().Be(direct.Value);
    }
}
