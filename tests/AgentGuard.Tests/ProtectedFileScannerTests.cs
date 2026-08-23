// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Engine;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// The protected-file scanner's fail-closed contract, observed through the public pipeline: an inaccessible
/// directory encountered while walking the protected tree must surface as a denial (a capture failure), never be
/// swallowed into a silent partial walk that could hide a protected file. Driven through
/// <see cref="GuardEngine.CreatePipeline(GuardEngineOptions)"/> over the copy-on-write simulator, with a nested
/// directory marked inaccessible so the recursive scan throws when it descends into it.
/// </summary>
public sealed class ProtectedFileScannerTests
{
    [Fact]
    public async Task Capture_WhenANestedDirectoryIsInaccessible_DeniesRatherThanSwallowing()
    {
        using var fixture = new FixtureProject();
        SystemServicesBuilder builder = TestSupport.FakeServices();
        ISystemServices services = builder.Build();
        IDirectoryWriter directoryWriter = services.FileSystem.GetDirectoryWriter();
        directoryWriter.CreateDirectory(fixture.Root);
        string nested = System.IO.Path.Combine(fixture.Root, "nested");
        directoryWriter.CreateDirectory(nested);
        builder.OnFileSystem().MarkInaccessible(nested);
        IPipeline pipeline = GuardEngine.CreatePipeline(new GuardEngineOptions(fixture.Root, services));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Bash("call-scan", "echo hi"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }
}
