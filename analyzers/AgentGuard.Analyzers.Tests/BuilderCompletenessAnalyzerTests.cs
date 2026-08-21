// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0019 proves the test SystemServicesBuilder mirrors the ISystemServices container at every nesting level
/// (ag0019-recursive-nested-completeness): a direct leaf service (a Contracts-interface accessor or the TimeProvider
/// clock) on a container node needs a With(T)/Wrap(Func&lt;T,T&gt;) on that node's builder scope, and a nested container
/// accessor needs a public zero-parameter On&lt;AccessorMemberName&gt;() navigator whose return type recursively
/// satisfies the same check. A missing navigator is reported once, not per leaf beneath it. The rule runs only in the
/// assembly that DEFINES the builder, and is preventive until the builder exists.
/// </summary>
public class BuilderCompletenessAnalyzerTests
{
    // A container with a nested sub-container: ISystemServices exposes the Console leaf, the TimeProvider clock, and the
    // FileSystem sub-container, which itself exposes the IFileReader and IFileWriter leaves. Held in one compilation
    // with the builder (as the real container is referenced and the builder defined in AgentGuard.TestHelpers).
    private const string ContainerSource = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface IFileWriter { }
            public interface IConsole { }
            public interface IFileSystem
            {
                IFileReader GetFileReader();
                IFileWriter GetFileWriter();
            }
            public interface ISystemServices
            {
                IFileSystem FileSystem { get; }
                IConsole Console { get; }
                System.TimeProvider Clock { get; }
            }
        }
        """;

    // A builder that mirrors the container at every level: the two root leaves (Console + the TimeProvider clock)
    // substituted and wrapped on the top level, and the FileSystem sub-container reached through an OnFileSystem()
    // navigator whose sub-builder substitutes and wraps the two nested leaves.
    private const string CompleteBuilderSource = """
        using System;
        using AgentGuard.Abstractions.Contracts;

        namespace AgentGuard.TestHelpers
        {
            public sealed class SystemServicesBuilder
            {
                public SystemServicesBuilder With(IConsole console) => this;
                public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy) => this;
                public SystemServicesBuilder With(TimeProvider clock) => this;
                public SystemServicesBuilder Wrap(Func<TimeProvider, TimeProvider> proxy) => this;
                public FileSystemBuilder OnFileSystem() => new FileSystemBuilder();

                public sealed class FileSystemBuilder
                {
                    public FileSystemBuilder With(IFileReader reader) => this;
                    public FileSystemBuilder Wrap(Func<IFileReader, IFileReader> proxy) => this;
                    public FileSystemBuilder With(IFileWriter writer) => this;
                    public FileSystemBuilder Wrap(Func<IFileWriter, IFileWriter> proxy) => this;
                }
            }
        }
        """;

    [Fact]
    public async Task CompleteRecursiveBuilder_IsNotReported()
    {
        // The builder mirrors every level, so nothing fires.
        Assert.Empty(await AnalyzerRunner.RunAsync<BuilderCompletenessAnalyzer>(
            CompleteBuilderSource + "\n" + ContainerSource, "AgentGuard.TestHelpers"));
    }

    [Fact]
    public async Task NestedLeafParkedOnTopLevelBuilder_IsReported()
    {
        // The IFileReader substitution/wrap is parked on the TOP-LEVEL builder instead of the FileSystem sub-builder,
        // which is the correct scope for a nested leaf. The sub-builder is therefore incomplete for IFileReader, so
        // AG0019 fires there — the top-level overloads do not satisfy the nested-scope requirement.
        const string parkedBuilder = """
            using System;
            using AgentGuard.Abstractions.Contracts;

            namespace AgentGuard.TestHelpers
            {
                public sealed class SystemServicesBuilder
                {
                    public SystemServicesBuilder With(IConsole console) => this;
                    public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy) => this;
                    public SystemServicesBuilder With(TimeProvider clock) => this;
                    public SystemServicesBuilder Wrap(Func<TimeProvider, TimeProvider> proxy) => this;
                    public SystemServicesBuilder With(IFileReader reader) => this;
                    public SystemServicesBuilder Wrap(Func<IFileReader, IFileReader> proxy) => this;
                    public FileSystemBuilder OnFileSystem() => new FileSystemBuilder();

                    public sealed class FileSystemBuilder
                    {
                        public FileSystemBuilder With(IFileWriter writer) => this;
                        public FileSystemBuilder Wrap(Func<IFileWriter, IFileWriter> proxy) => this;
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<BuilderCompletenessAnalyzer>(
            parkedBuilder + "\n" + ContainerSource, "AgentGuard.TestHelpers");

        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0019", diagnostic.Id));
        Assert.All(
            diagnostics,
            diagnostic => Assert.Contains(
                "IFileReader", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal));
    }

    [Fact]
    public async Task MissingNavigator_ReportsOnce_NotPerNestedLeaf()
    {
        // The builder has NO OnFileSystem() navigator. The FileSystem sub-container has two nested leaves; AG0019 must
        // report the missing navigator ONCE at that node, not once per leaf beneath it. The two root leaves are
        // complete, so exactly one diagnostic fires, naming the navigator.
        const string noNavigatorBuilder = """
            using System;
            using AgentGuard.Abstractions.Contracts;

            namespace AgentGuard.TestHelpers
            {
                public sealed class SystemServicesBuilder
                {
                    public SystemServicesBuilder With(IConsole console) => this;
                    public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy) => this;
                    public SystemServicesBuilder With(TimeProvider clock) => this;
                    public SystemServicesBuilder Wrap(Func<TimeProvider, TimeProvider> proxy) => this;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<BuilderCompletenessAnalyzer>(
            noNavigatorBuilder + "\n" + ContainerSource, "AgentGuard.TestHelpers"));

        Assert.Equal("AG0019", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("OnFileSystem", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingTopLevelWith_IsReported()
    {
        // A root leaf (Console) with no With overload on the top-level builder fires — the service can be wrapped but
        // not substituted.
        const string missingWith = """
            using System;
            using AgentGuard.Abstractions.Contracts;

            namespace AgentGuard.TestHelpers
            {
                public sealed class SystemServicesBuilder
                {
                    public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy) => this;
                    public SystemServicesBuilder With(TimeProvider clock) => this;
                    public SystemServicesBuilder Wrap(Func<TimeProvider, TimeProvider> proxy) => this;
                    public FileSystemBuilder OnFileSystem() => new FileSystemBuilder();

                    public sealed class FileSystemBuilder
                    {
                        public FileSystemBuilder With(IFileReader reader) => this;
                        public FileSystemBuilder Wrap(Func<IFileReader, IFileReader> proxy) => this;
                        public FileSystemBuilder With(IFileWriter writer) => this;
                        public FileSystemBuilder Wrap(Func<IFileWriter, IFileWriter> proxy) => this;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<BuilderCompletenessAnalyzer>(
            missingWith + "\n" + ContainerSource, "AgentGuard.TestHelpers"));

        Assert.Equal("AG0019", diagnostic.Id);
        Assert.Contains("Console", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("With", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingClockSubstitution_IsReported()
    {
        // The TimeProvider clock is a direct leaf on the root, so it needs a With/Wrap on the top-level builder like any
        // other leaf. Dropping the clock's With overload fires (proving the clock is enforced as a leaf).
        const string missingClock = """
            using System;
            using AgentGuard.Abstractions.Contracts;

            namespace AgentGuard.TestHelpers
            {
                public sealed class SystemServicesBuilder
                {
                    public SystemServicesBuilder With(IConsole console) => this;
                    public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy) => this;
                    public SystemServicesBuilder Wrap(Func<TimeProvider, TimeProvider> proxy) => this;
                    public FileSystemBuilder OnFileSystem() => new FileSystemBuilder();

                    public sealed class FileSystemBuilder
                    {
                        public FileSystemBuilder With(IFileReader reader) => this;
                        public FileSystemBuilder Wrap(Func<IFileReader, IFileReader> proxy) => this;
                        public FileSystemBuilder With(IFileWriter writer) => this;
                        public FileSystemBuilder Wrap(Func<IFileWriter, IFileWriter> proxy) => this;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<BuilderCompletenessAnalyzer>(
            missingClock + "\n" + ContainerSource, "AgentGuard.TestHelpers"));

        Assert.Equal("AG0019", diagnostic.Id);
        Assert.Contains("TimeProvider", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("With", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuilderDefinedButNoContainer_IsNotReported()
    {
        // Preventive: without an ISystemServices container to fall behind, the rule does not fire.
        const string noContainer = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IConsole { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<BuilderCompletenessAnalyzer>(
            CompleteBuilderSource + "\n" + noContainer, "AgentGuard.TestHelpers"));
    }

    [Fact]
    public async Task IncompleteBuilder_OnlyReferenced_NotDefinedHere_IsNotReported()
    {
        // The rule checks only the assembly that DEFINES the builder. An incomplete builder living in a REFERENCED
        // AgentGuard.TestHelpers is not re-checked from a .Tests project that merely consumes it — otherwise every test
        // project would re-report it. The builder is incomplete on purpose: were it checked here it would fire, so an
        // empty result proves the defining-assembly guard, not mere completeness.
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
        // The same incompleteness, but with the builder DEFINED in this compilation, fires — proving the previous empty
        // result was the defining-assembly guard, not the rule failing to notice a missing overload. A direct leaf with
        // no With/Wrap fires.
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

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<BuilderCompletenessAnalyzer>(
            containerAndIncompleteBuilder, "AgentGuard.TestHelpers");

        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0019", diagnostic.Id));
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("FileReader", StringComparison.Ordinal));
    }
}
