// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class RandomnessOnlyInBoundariesAnalyzerTests
{
    private const string NewGuidSource = """
        using System;

        public class Sample
        {
            public string Name() => Guid.NewGuid().ToString("N");
        }
        """;

    // The single-owner proof: a stub owning interface in AgentGuard.Abstractions, and TWO classes in the SAME
    // assembly — the owner implementing IGuidFactory (its Guid.NewGuid() is exempt) and a sibling that does not
    // implement it (its new Random() is RED). This proves the tightening from an assembly-wide exemption to the one
    // owner class.
    private const string OwnerAndSiblingSource = """
        using System;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IGuidFactory { }
        }

        namespace CrossPlatform
        {
            public sealed class GuidFactory : AgentGuard.Abstractions.Contracts.IGuidFactory
            {
                public string Name() => Guid.NewGuid().ToString("N");
            }

            public sealed class Sibling
            {
                public Random Make() => new Random();
            }
        }
        """;

    // Wrong-assembly self-grant probe: the GuidFactory owner class implementing IGuidFactory and calling Guid.NewGuid().
    // Exempt only in its owner assembly AgentGuard.CrossPlatform; RED in any other assembly.
    private const string GuidFactoryOwnerSource = """
        using System;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IGuidFactory { }
        }

        namespace CrossPlatform
        {
            public sealed class GuidFactory : AgentGuard.Abstractions.Contracts.IGuidFactory
            {
                public string Name() => Guid.NewGuid().ToString("N");
            }
        }
        """;

    [Fact]
    public async Task GuidNewGuid_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(NewGuidSource, "AgentGuard.Engine"));
        Assert.Equal("AG0014", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task GuidNewGuid_InOwnerAssembly_ButNotOwnerClass_IsStillReported()
    {
        // AgentGuard.CrossPlatform IS the owner assembly for AG0014 (guid-seam-lives-in-crossplatform), but this Sample
        // does not implement IGuidFactory, so it is not the GuidFactory owner class — its Guid.NewGuid() is still RED.
        // The assembly half of the conjunction alone never exempts; the class must be the owner too. This is the RED
        // that forces PlatformFileSystemShared's raw Guid.NewGuid() behind IGuidFactory.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(NewGuidSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0014", diagnostic.Id);
    }

    [Fact]
    public async Task OwnerImplementingInterface_IsExempt_SiblingInSameAssembly_IsStillReported()
    {
        // one-owner-class-per-primitive: only the class implementing IGuidFactory is exempt, resolved structurally.
        // Compiled into AgentGuard.CrossPlatform — where the GuidFactory owner actually lives
        // (guid-seam-lives-in-crossplatform) — to prove assembly membership no longer grants the exemption: the
        // owner's Guid.NewGuid() is clean, but the sibling's new Random() in the SAME assembly is RED.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(OwnerAndSiblingSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0014", diagnostic.Id);
        Assert.Contains(
            "Random", AnalyzerRunner.SpanText(OwnerAndSiblingSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task GuidFactoryOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        // FIX 1: implementing IGuidFactory is not enough — the class must also compile into AgentGuard.CrossPlatform,
        // where the GuidFactory owner lives (guid-seam-lives-in-crossplatform). In AgentGuard.Boundaries (the wrong
        // assembly) the owner's Guid.NewGuid() is RED, so a fake declaring ': IGuidFactory' cannot launder it.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(
                GuidFactoryOwnerSource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0014", diagnostic.Id);
        Assert.Contains(
            "NewGuid", AnalyzerRunner.SpanText(GuidFactoryOwnerSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task GuidFactoryOwner_InOwnerAssembly_IsExempt()
    {
        // The same class compiled into its owner assembly AgentGuard.CrossPlatform is exempt: both halves pass.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(
                GuidFactoryOwnerSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task NewRandom_OutsideBoundaries_IsReported()
    {
        const string source = """
            using System;

            public class Sample
            {
                public Random Make() => new Random();
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0014", diagnostic.Id);
    }

    [Fact]
    public async Task GuidParse_IsNotReported()
    {
        // Parsing or building a GUID from data is deterministic and stays legal; only the NewGuid() factory is banned.
        const string source = """
            using System;

            public class Sample
            {
                public Guid Parse(string text) => Guid.Parse(text);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
    }
}
