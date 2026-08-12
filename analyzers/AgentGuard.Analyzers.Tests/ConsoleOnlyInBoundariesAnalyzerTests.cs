// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ConsoleOnlyInBoundariesAnalyzerTests
{
    private const string WriteLineSource = """
        using System;

        public class Sample
        {
            public void Say() => Console.WriteLine("hi");
        }
        """;

    private const string ConsoleOutSource = """
        using System;

        public class Sample
        {
            public void Say() => Console.Out.Write("hi");
        }
        """;

    // The single-owner proof: a stub owning interface in AgentGuard.Abstractions, and TWO classes in the SAME
    // assembly — the owner implementing IConsole (its Console.WriteLine is exempt) and a sibling that does not
    // implement it (its Console.ReadLine is RED). This proves the tightening from an assembly-wide exemption to the
    // one owner class.
    private const string OwnerAndSiblingSource = """
        using System;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IConsole { }
        }

        namespace Boundaries
        {
            public sealed class ConsoleAdapter : AgentGuard.Abstractions.Contracts.IConsole
            {
                public void Say() => Console.WriteLine("hi");
            }

            public sealed class Sibling
            {
                public string? Ask() => Console.ReadLine();
            }
        }
        """;

    // Wrong-assembly self-grant probe: the ConsoleAdapter owner implementing IConsole and calling Console.WriteLine.
    // Exempt only in its owner assembly AgentGuard.Boundaries; RED in any other assembly.
    private const string ConsoleAdapterOwnerSource = """
        using System;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IConsole { }
        }

        namespace Boundaries
        {
            public sealed class ConsoleAdapter : AgentGuard.Abstractions.Contracts.IConsole
            {
                public void Say() => Console.WriteLine("hi");
            }
        }
        """;

    [Fact]
    public async Task ConsoleWriteLine_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ConsoleOnlyInBoundariesAnalyzer>(WriteLineSource, "AgentGuard.Cli"));
        Assert.Equal("AG0016", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task ConsoleOut_OutsideBoundaries_IsReported()
    {
        // A read of the Console.Out stream is caught the same as a direct Console call.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ConsoleOnlyInBoundariesAnalyzer>(ConsoleOutSource, "AgentGuard.Cli"));
        Assert.Equal("AG0016", diagnostic.Id);
    }

    [Fact]
    public async Task OwnerImplementingInterface_IsExempt_SiblingInSameAssembly_IsStillReported()
    {
        // one-owner-class-per-primitive: only the class implementing IConsole is exempt, resolved structurally.
        // Compiled into AgentGuard.Boundaries to prove assembly membership no longer grants the exemption — the
        // owner's Console.WriteLine is clean, but the sibling's Console.ReadLine in the SAME assembly is still RED.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ConsoleOnlyInBoundariesAnalyzer>(OwnerAndSiblingSource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0016", diagnostic.Id);
        Assert.Contains(
            "ReadLine", AnalyzerRunner.SpanText(OwnerAndSiblingSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConsoleAdapterOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        // FIX 1: implementing IConsole is not enough — the class must also compile into AgentGuard.Boundaries, where
        // the ConsoleAdapter lives. In AgentGuard.Cli (the wrong assembly) the owner's Console.WriteLine is RED, so a
        // fake declaring ': IConsole' cannot launder it.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ConsoleOnlyInBoundariesAnalyzer>(
                ConsoleAdapterOwnerSource, "AgentGuard.Cli"));
        Assert.Equal("AG0016", diagnostic.Id);
        Assert.Contains(
            "WriteLine", AnalyzerRunner.SpanText(ConsoleAdapterOwnerSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConsoleAdapterOwner_InOwnerAssembly_IsExempt()
    {
        // The same class compiled into its owner assembly AgentGuard.Boundaries is exempt: both halves pass.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<ConsoleOnlyInBoundariesAnalyzer>(
                ConsoleAdapterOwnerSource, "AgentGuard.Boundaries"));
    }
}
