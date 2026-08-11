// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Setup;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class ShellProfileTests
{
    [Fact]
    public void Acceptance_3g_Resolve_TargetsTheRightProfilePerShell()
    {
        const string home = "/home/tester";

        ShellProfile.Resolve("/bin/zsh", home).Should().Be(Path.Combine(home, ".zshrc"));
        ShellProfile.Resolve("/usr/bin/bash", home).Should().Be(Path.Combine(home, ".bashrc"));

        // Shells whose PATH syntax differs (fish) are not special-cased: they fall back to .profile rather than
        // having a bash-syntax `export` line written into a file the shell would source and choke on.
        ShellProfile.Resolve("/opt/homebrew/bin/fish", home).Should().Be(Path.Combine(home, ".profile"));
        ShellProfile.Resolve(string.Empty, home).Should().Be(Path.Combine(home, ".profile"));
    }

    [Fact]
    public void Acceptance_3g_PathLine_IsIdempotentWithNoDuplicate()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0").Success.Should().BeTrue();

        // A second install re-runs the PATH ensure; it must not duplicate the line.
        harness.Install("0.2.0").Success.Should().BeTrue();

        string profile = File.ReadAllText(harness.ShellProfilePath);
        CountOccurrences(profile, ShellProfile.Marker).Should().Be(1);
    }

    private static int CountOccurrences(string text, string token)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
