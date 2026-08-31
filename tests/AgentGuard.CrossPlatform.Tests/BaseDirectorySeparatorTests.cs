// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// Acceptance #8 / Decision 5: the base-directory accessor always ends in a directory separator. The three inputs the
/// contract names are each pinned to their own fact: the REAL value read through
/// <see cref="SystemServicesBuilder.Real"/> (which mirrors <c>AppContext.BaseDirectory</c>, itself separator-terminated);
/// the fake seeded EXPLICITLY through <c>FakeEnvironment.Create</c>
/// with a value that lacks a trailing separator; and the fake on its DEFAULT path (no <c>baseDirectory</c> supplied, so
/// it falls back to the home directory — the shared <c>Fake()</c> harness case at <c>SystemServicesBuilder.cs:247</c>).
/// A fourth fact pins the SECOND half of Decision 5 — a seed that ALREADY ends in the separator is returned unchanged,
/// with exactly one separator and never doubled — so an unconditional-append implementation cannot pass.
/// The separator asserted against is the owned <see cref="IPlatformFileSystem.DirectorySeparator"/>, never a hardcoded
/// <c>/</c>, so the assertion is correct on macOS, Linux, and Windows. Seed values are runtime reads (never a
/// compile-time literal), so AG0035 stays clean.
/// </summary>
public sealed class BaseDirectorySeparatorTests
{
    private readonly ISystemServices real = SystemServicesBuilder.Real().Build();
    private readonly char separator;
    private readonly string seed;

    public BaseDirectorySeparatorTests()
    {
        // Field initializers run before this constructor body, so `real` is already built here. The owned separator and
        // the seed are each read once and shared by every fact (never a hardcoded '/'), instead of recomputing the
        // expression per method. The seed is a runtime value (a real home read) with any trailing separator stripped —
        // never a compile-time literal, so AG0035 does not fire where it seeds the fake.
        separator = real.Platform.FileSystem.DirectorySeparator;
        seed = real.Environment.GetHomeDirectory().TrimEnd(separator);
    }

    [Fact]
    public void RealBaseDirectory_EndsWithADirectorySeparator()
    {
        string baseDirectory = real.Environment.GetBaseDirectory();

        baseDirectory.Should().EndWith(
            separator.ToString(),
            "the real adapter mirrors AppContext.BaseDirectory, which always ends with a directory separator");
    }

    [Fact]
    public void FakeBaseDirectory_WhenSeededWithoutATrailingSeparator_AppendsExactlyOne()
    {
        IEnvironment fake = FakeEnvironment.Create(seed, baseDirectory: seed);

        fake.GetBaseDirectory().Should().Be(
            seed + separator,
            "the fake appends the owned directory separator when the seeded base directory lacks one");
    }

    [Fact]
    public void FakeBaseDirectory_WhenSeededAlreadyTerminated_ReturnsItUnchangedWithExactlyOneSeparator()
    {
        // The SECOND half of Decision 5: when the seed ALREADY ends in the owned separator, the fake returns it
        // unchanged — exactly one separator, never doubled ("//"). An unconditional-append implementation fails this.
        // This fact is GREEN before implementation and is a regression guard, not a RED forcing function: the fake
        // skeleton returns the seeded value verbatim, which for an already-terminated seed is already the required
        // answer, and it cannot be made RED because a throwing stub leaves _baseDirectory unread and fails the build
        // (S4487). Tim waived RED-first for this one fact — see contract Decision 6.
        IEnvironment fake = FakeEnvironment.Create(seed, baseDirectory: seed + separator);

        fake.GetBaseDirectory().Should().Be(
            seed + separator,
            "an already-terminated base directory is returned unchanged, with a single separator and no doubling");
    }

    [Fact]
    public void FakeBaseDirectory_OnTheDefaultHomeFallbackPath_EndsWithADirectorySeparator()
    {
        // The shared Fake() harness case: no baseDirectory supplied, so it falls back to the home directory
        // (SystemServicesBuilder.cs:247). That default value must be separator-terminated too.
        IEnvironment fakeDefault = SystemServicesBuilder.Fake().Build().Environment;

        fakeDefault.GetBaseDirectory().Should().EndWith(
            separator.ToString(),
            "the Fake() default base directory falls back to the home directory and is still separator-terminated");
    }
}
