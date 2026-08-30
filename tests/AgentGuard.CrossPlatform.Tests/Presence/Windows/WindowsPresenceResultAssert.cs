// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Windows;
using FluentAssertions;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the "cast the underlying int back to the nullable <see cref="WindowsPresenceResult"/> and assert"
/// step the Windows mapper table tests share (DRY). <see cref="WindowsPresenceResult"/> is internal, so a public xUnit
/// theory-method signature cannot carry it — each mapper test passes the expected outcome as its underlying
/// <see cref="int"/> (a <see langword="null"/> int meaning the mapper returns <see langword="null"/>). Both
/// <c>WindowsMapHelloResultTests</c> and <c>WindowsMapCredentialStatusTests</c> had the identical cast-and-assert block;
/// it lives here once so neither copies the other.
/// </summary>
internal static class WindowsPresenceResultAssert
{
    /// <summary>
    /// Asserts <paramref name="actual"/> equals the outcome <paramref name="expected"/> names — the
    /// <see cref="WindowsPresenceResult"/> whose underlying value is <paramref name="expected"/>, or <see langword="null"/>
    /// when <paramref name="expected"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="actual">The nullable outcome the mapper returned.</param>
    /// <param name="expected">The expected outcome as its underlying int, or <see langword="null"/> for a null result.</param>
    internal static void ShouldMatch(this WindowsPresenceResult? actual, int? expected)
    {
        WindowsPresenceResult? expectedResult = expected is null ? null : (WindowsPresenceResult)expected.Value;
        actual.Should().Be(expectedResult);
    }
}
