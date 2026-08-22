// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Exercises the real <c>EnvironmentAdapter</c> in <c>AgentGuard.Boundaries</c> through the owned
/// <see cref="IEnvironment"/> off a real container (<see cref="SystemServicesBuilder.Real"/>) — the only sanctioned
/// door to the internal adapter — asserting each owned environment read returns the real host value.
/// </summary>
public sealed class BoundariesEnvironmentAdapterTests
{
    private readonly IEnvironment environment = SystemServicesBuilder.Real().Build().Environment;

    [Fact]
    public void GetCurrentDirectory_ReturnsTheRealNonEmptyWorkingDirectory()
    {
        Assert.False(string.IsNullOrWhiteSpace(this.environment.GetCurrentDirectory()));
    }

    [Fact]
    public void GetHomeDirectory_ReturnsTheRealNonEmptyHomeDirectory()
    {
        Assert.False(string.IsNullOrWhiteSpace(this.environment.GetHomeDirectory()));
    }

    [Fact]
    public void GetEnvironmentVariable_ReadsAVariableThatIsSet()
    {
        // PATH is present in every CI and local shell on macOS, Linux, and Windows (env-var names are
        // case-insensitive on Windows), so a real read must return a non-null value.
        Assert.NotNull(this.environment.GetEnvironmentVariable("PATH"));
    }

    [Fact]
    public void GetEnvironmentVariable_ReturnsNullForAnUnsetVariable()
    {
        Assert.Null(this.environment.GetEnvironmentVariable("AGENTGUARD_DEFINITELY_UNSET_VARIABLE_9F3A2B"));
    }

    [Fact]
    public void GetProcessPath_ReturnsTheRealNonEmptyProcessPath()
    {
        Assert.False(string.IsNullOrWhiteSpace(this.environment.GetProcessPath()));
    }

    // Note: IEnvironment.GetTempDirectory() has no test caller by design — AG-S5443 bans every call site of the
    // shared publicly-writable temp root, so EnvironmentAdapter.GetTempDirectory is deliberately left uncovered rather
    // than reached through a suppression.
    [Fact]
    public void GetProcessAndOsArchitecture_ReturnDefinedArchitectures()
    {
        Assert.True(Enum.IsDefined(this.environment.GetProcessArchitecture()));
        Assert.True(Enum.IsDefined(this.environment.GetOSArchitecture()));
    }
}
