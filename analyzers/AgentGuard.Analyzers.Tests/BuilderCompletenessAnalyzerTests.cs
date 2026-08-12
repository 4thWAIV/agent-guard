// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class BuilderCompletenessAnalyzerTests
{
    // The container in its real namespace AgentGuard.Abstractions.Contracts, exposing two services. Held in a separate reference
    // assembly, exactly as the real ISystemServices lives in AgentGuard.Abstractions.Contracts and the builder in
    // AgentGuard.TestHelpers.
    private const string AbstractionsSource = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface IConsole { }

            public interface ISystemServices
            {
                IFileReader FileReader { get; }
                IConsole Console { get; }
            }
        }
        """;

    // The same reference assembly WITHOUT the ISystemServices container — the preventive state before the container
    // exists: there is nothing the builder can fall behind, so the rule does not fire.
    private const string AbstractionsNoContainerSource = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface IConsole { }
        }
        """;

    // A builder complete for both services: a With(T) and a Wrap(Func<T,T>) for each.
    private const string CompleteBuilderSource = """
        using System;
        using AgentGuard.Abstractions.Contracts;

        namespace AgentGuard.TestHelpers
        {
            public sealed class SystemServicesBuilder
            {
                public SystemServicesBuilder With(IFileReader reader) => this;
                public SystemServicesBuilder With(IConsole console) => this;
                public SystemServicesBuilder Wrap(Func<IFileReader, IFileReader> proxy) => this;
                public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy) => this;
            }
        }
        """;

    // A builder missing the With(IConsole) overload — the Console service can be wrapped but not substituted.
    private const string MissingWithBuilderSource = """
        using System;
        using AgentGuard.Abstractions.Contracts;

        namespace AgentGuard.TestHelpers
        {
            public sealed class SystemServicesBuilder
            {
                public SystemServicesBuilder With(IFileReader reader) => this;
                public SystemServicesBuilder Wrap(Func<IFileReader, IFileReader> proxy) => this;
                public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy) => this;
            }
        }
        """;

    // A builder missing the Wrap(Func<IConsole,IConsole>) overload — the Console service can be substituted but not
    // wrapped in a proxy.
    private const string MissingWrapBuilderSource = """
        using System;
        using AgentGuard.Abstractions.Contracts;

        namespace AgentGuard.TestHelpers
        {
            public sealed class SystemServicesBuilder
            {
                public SystemServicesBuilder With(IFileReader reader) => this;
                public SystemServicesBuilder With(IConsole console) => this;
                public SystemServicesBuilder Wrap(Func<IFileReader, IFileReader> proxy) => this;
            }
        }
        """;

    [Fact]
    public async Task CompleteBuilder_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<BuilderCompletenessAnalyzer>(
            CompleteBuilderSource, "AgentGuard.TestHelpers", AbstractionsSource, "AgentGuard.Abstractions"));
    }

    [Fact]
    public async Task BuilderMissingWith_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunWithReferenceAsync<BuilderCompletenessAnalyzer>(
                MissingWithBuilderSource, "AgentGuard.TestHelpers", AbstractionsSource, "AgentGuard.Abstractions"));

        Assert.Equal("AG0019", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Console", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("With", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuilderMissingWrap_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunWithReferenceAsync<BuilderCompletenessAnalyzer>(
                MissingWrapBuilderSource, "AgentGuard.TestHelpers", AbstractionsSource, "AgentGuard.Abstractions"));

        Assert.Equal("AG0019", diagnostic.Id);
        Assert.Contains("Console", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Wrap", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuilderDefinedButNoContainer_IsNotReported()
    {
        // Preventive: without an ISystemServices container to fall behind, the rule does not fire.
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<BuilderCompletenessAnalyzer>(
            CompleteBuilderSource, "AgentGuard.TestHelpers", AbstractionsNoContainerSource, "AgentGuard.Abstractions"));
    }

    [Fact]
    public async Task IncompleteBuilder_OnlyReferenced_NotDefinedHere_IsNotReported()
    {
        // The rule checks only the assembly that DEFINES the builder. An incomplete builder living in a REFERENCED
        // AgentGuard.TestHelpers is not re-checked from a .Tests project that merely consumes it — otherwise every
        // test project would re-report it. The builder is incomplete on purpose: were it checked here it would fire, so
        // an empty result proves the defining-assembly guard, not mere completeness.
        const string referenceWithContainerAndIncompleteBuilder = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFileReader { }
                public interface ISystemServices { IFileReader FileReader { get; } }
            }

            namespace AgentGuard.TestHelpers
            {
                public sealed class SystemServicesBuilder { }
            }
            """;
        const string consumerSource = """
            public class Sample { }
            """;

        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<BuilderCompletenessAnalyzer>(
            consumerSource, "AgentGuard.Cli.Tests", referenceWithContainerAndIncompleteBuilder, "AgentGuard.TestHelpers"));
    }

    [Fact]
    public async Task IncompleteBuilder_DefinedHere_IsReported()
    {
        // The same incompleteness, but with the builder DEFINED in this compilation, fires — proving the previous
        // empty result was the defining-assembly guard, not the rule failing to notice a missing overload.
        const string containerAndIncompleteBuilder = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFileReader { }
                public interface ISystemServices { IFileReader FileReader { get; } }
            }

            namespace AgentGuard.TestHelpers
            {
                public sealed class SystemServicesBuilder { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<BuilderCompletenessAnalyzer>(
                containerAndIncompleteBuilder, "AgentGuard.TestHelpers");

        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0019", diagnostic.Id));
        Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("FileReader", StringComparison.Ordinal));
    }
}
