// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Rule 4 of the cross-platform interop ruleset (a test, not a Roslyn analyzer): the POSIX implementation source
/// is authored once in the MacOS project and <c>&lt;Compile Include=… Link=…&gt;</c>-shared into the Linux
/// project, never copied. This fences three-per-os-libs-shared-source. It is RED until the implementation phase
/// creates the Linux project and link-shares the single authored MacOS POSIX source across the two project
/// directories; then it passes.
/// </summary>
public class PosixSourceIsLinkSharedTests
{
    [Fact]
    public void LinuxProject_LinkSharesTheMacOsPosixSource()
    {
        string repositoryRoot = RepositoryFiles.FindRepositoryRoot();
        string linuxProjectPath = Path.Combine(
            repositoryRoot, "src", "AgentGuard.CrossPlatform.Linux", "AgentGuard.CrossPlatform.Linux.csproj");

        Assert.True(
            File.Exists(linuxProjectPath),
            $"The Linux platform project must exist and <Compile Include=… Link=…> the MacOS POSIX source, not copy it. Expected the project at: {linuxProjectPath}");

        string macOsProjectDirectory = Path.Combine(repositoryRoot, "src", "AgentGuard.CrossPlatform.MacOS");
        string linuxProjectDirectory = Path.GetDirectoryName(linuxProjectPath)!;

        XDocument project = XDocument.Load(linuxProjectPath);
        List<string> linkedMacOsSources = project
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.Ordinal)
                && element.Attribute("Link") is not null
                && element.Attribute("Include") is not null)
            .Select(element => RepositoryFiles.ResolvePath(linuxProjectDirectory, element.Attribute("Include")!.Value))
            .Where(includePath => RepositoryFiles.IsUnder(includePath, macOsProjectDirectory)
                && includePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            linkedMacOsSources.Count > 0,
            "The Linux project must <Compile Include=… Link=…> at least one POSIX .cs source authored in the MacOS project; no such link-shared source was found.");

        foreach (string linkedSource in linkedMacOsSources)
        {
            Assert.True(
                File.Exists(linkedSource),
                $"The link-shared POSIX source must be a single authored file that exists on disk: {linkedSource}");
        }
    }
}
