// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.MacOS;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The macOS extracted-mapper table test (contract-coverage-refactor acceptance #5, "every extracted mapper"):
/// <c>LocalAuthentication.MapErrorCode</c> is the pure function the coverage refactor pulls out of the orchestrator so the
/// probe/reply error-code translation is unit-testable. It must translate EVERY documented raw <c>LAError</c> code to the
/// <see cref="LaResult"/> the frozen enum pairs it with, and fold any unrecognized code to <see cref="LaResult.Unknown"/>
/// — the two sides the enum's catch-all defines. Compiled only on the macOS CI leg (it reaches the internal macOS mapper).
/// </summary>
public sealed class LocalAuthenticationMapErrorCodeTests
{
    // LaResult is internal (reached here through the macOS IVT grant), so it cannot appear in a public theory-method
    // signature; the outcome is passed as its underlying int constant and cast back inside. The error-code correspondence
    // is owned once by MacOsErrorCodeOutcomes (DRY).
    [Theory]
    [MemberData(nameof(MacOsErrorCodeOutcomes.Cases), MemberType = typeof(MacOsErrorCodeOutcomes))]
    public void MapErrorCode_TranslatesEveryDocumentedErrorCode_ToItsLaResult(long code, int expectedLaResult)
    {
        LocalAuthentication.MapErrorCode(code).Should().Be((LaResult)expectedLaResult);
    }
}
