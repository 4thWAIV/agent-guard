// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Rule 4 of the cross-OS ruleset (a test, not a Roslyn analyzer): a source file whose name carries an OS suffix —
/// <c>*.MacOS.cs</c>, <c>*.Linux.cs</c>, <c>*.Windows.cs</c> (os-specific-file-naming-convention) — must be compiled
/// only into its own OS's per-OS project, never physically placed in, nor <c>&lt;Compile Include=… Link=…&gt;</c>-shared
/// into, another OS's project. It matches on the suffix, never a specific filename, so every OS-specific file added
/// later is covered without touching the rule. This stops the macOS-only native case query (once it moves to
/// <c>PosixFileSystem.MacOS.cs</c>) being re-linked into the Linux build, where it would be permanently-dead,
/// uncoverable code that drops the leg below the coverage floor. No OS-suffixed file exists yet, so the repository
/// check is green (preventive); the planted-regression check proves it has teeth.
/// </summary>
public class OsSpecificFileNotCrossCompiledTests
{
    private static readonly IReadOnlyList<string> PerOsNames = new[] { "MacOS", "Linux", "Windows" };

    [Fact]
    public void NoPerOsProject_CompilesAnotherOsSpecificFile()
    {
        string repositoryRoot = RepositoryFiles.FindRepositoryRoot();

        foreach (string os in PerOsNames)
        {
            string projectDirectory = Path.Combine(repositoryRoot, "src", $"AgentGuard.CrossPlatform.{os}");
            Assert.True(Directory.Exists(projectDirectory), $"The per-OS project directory must exist: {projectDirectory}");

            List<string> offenders = CrossCompiledOsFiles(os, CompiledFileNames(projectDirectory));

            string message =
                $"AgentGuard.CrossPlatform.{os} compiles OS-specific file(s) belonging to another OS: "
                + $"{string.Join(", ", offenders)}. An OS-suffixed file (*.MacOS.cs/*.Linux.cs/*.Windows.cs) may be "
                + "compiled only into its own OS's project.";
            Assert.True(offenders.Count == 0, message);
        }
    }

    [Fact]
    public void CrossCompiledOsFiles_FlagsAPlantedForeignOsFile()
    {
        // Planted regression: a macOS-only file link-shared into (or dropped in) the Linux project is caught.
        var compiled = new[] { "PosixFileSystem.cs", "PosixFileSystem.MacOS.cs" };

        List<string> offenders = CrossCompiledOsFiles("Linux", compiled);

        var expected = new[] { "PosixFileSystem.MacOS.cs" };
        Assert.Equal(expected, offenders);
    }

    [Fact]
    public void CrossCompiledOsFiles_AllowsAFileMatchingItsOwnOs()
    {
        // A file suffixed for the project's own OS, and an unsuffixed shared file, are both fine.
        var compiled = new[] { "PosixFileSystem.cs", "PosixFileSystem.MacOS.cs" };

        Assert.Empty(CrossCompiledOsFiles("MacOS", compiled));
    }

    // The compiled file names of a per-OS project: the SDK default glob (every .cs physically under the project
    // directory, minus bin/obj) plus every explicit <Compile Include=…> (which is how a cross-directory link-share is
    // written). Reduced to file names because the rule keys on the OS suffix, not the path.
    private static HashSet<string> CompiledFileNames(string projectDirectory)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
        {
            if (!RepositoryFiles.IsUnderIntermediateOutput(file, projectDirectory))
            {
                names.Add(Path.GetFileName(file));
            }
        }

        foreach (string projectFile in Directory.EnumerateFiles(projectDirectory, "*.csproj", SearchOption.TopDirectoryOnly))
        {
            XDocument project = XDocument.Load(projectFile);
            IEnumerable<string> includes = project
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.Ordinal)
                    && element.Attribute("Include") is not null)
                .Select(element => element.Attribute("Include")!.Value);

            foreach (string include in includes)
            {
                names.Add(Path.GetFileName(RepositoryFiles.ResolvePath(projectDirectory, include)));
            }
        }

        return names;
    }

    // The names among compiled that carry an OS suffix naming an OS OTHER than projectOs.
    private static List<string> CrossCompiledOsFiles(string projectOs, IEnumerable<string> compiled)
    {
        var offenders = new List<string>();
        foreach (string name in compiled)
        {
            string? suffixOs = OsSuffixOf(name);
            if (suffixOs is not null && !string.Equals(suffixOs, projectOs, StringComparison.Ordinal))
            {
                offenders.Add(name);
            }
        }

        return offenders;
    }

    // The OS a file name is suffixed for (*.MacOS.cs/*.Linux.cs/*.Windows.cs), or null when it carries no OS suffix.
    private static string? OsSuffixOf(string fileName)
    {
        foreach (string os in PerOsNames)
        {
            if (fileName.EndsWith($".{os}.cs", StringComparison.Ordinal))
            {
                return os;
            }
        }

        return null;
    }
}
