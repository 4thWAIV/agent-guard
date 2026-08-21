// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Engine;
using AgentGuard.TestHelpers;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Unit spec for <see cref="ProtectedFileScanner"/>'s fail-closed contract: an inaccessible directory surfaced by
/// the enumerator must propagate out of the scan, never be swallowed, so an incomplete walk denies the call rather
/// than hiding a protected file. It threads a <see cref="ThrowingDirectoryEnumerator"/> through the scanner and
/// asserts the exception propagates from <see cref="ProtectedFileScanner.ScanAsync"/>.
/// </summary>
public sealed class ProtectedFileScannerTests
{
    [Fact]
    public async Task ScanAsync_WhenADirectoryIsInaccessible_PropagatesRatherThanSwallowing()
    {
        IProtectedFileScanner scanner = ProtectedFileScanner.Create(
            PathCanonicalizer.Create(),
            ProtectedSet.Create(Array.Empty<IRuleSource>()),
            new UnadjudicableRegionRegistry("never-matches"),
            Array.Empty<IDirectorySkipRule>(),
            new ThrowingDirectoryEnumerator());

        using var fixture = new FixtureProject();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            scanner.ScanAsync(TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None));
    }
}
