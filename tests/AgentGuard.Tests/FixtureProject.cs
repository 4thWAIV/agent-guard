// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using AgentGuard.TestSupport;

namespace AgentGuard.Tests;

/// <summary>
/// A throwaway on-disk C# fixture project the File Guard operates against in a test. It materializes a realistic
/// project tree in an isolated temporary directory — a project file so the build-output skip list can anchor,
/// a protected <c>Directory.Build.props</c> and <c>global.json</c>, and populated <c>bin</c>/<c>obj</c> folders —
/// and deletes it on disposal.
/// </summary>
public sealed class FixtureProject : IDisposable
{
    private readonly DirectoryInfo _root;

    public FixtureProject()
    {
        _root = Directory.CreateTempSubdirectory("agentguard-fixture-");
        WriteFile("Sample.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>\n");
        WriteFile("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n</Project>\n");
        WriteFile("global.json", "{ \"sdk\": { \"version\": \"9.0.101\" } }\n");
        WriteFile(Path.Combine("bin", "Debug", "Sample.dll"), "build output that must be skipped");
        WriteFile(Path.Combine("obj", "Sample.csproj.nuget.g.props"), "build output that must be skipped");
    }

    /// <summary>
    /// Gets the absolute root of the fixture project.
    /// </summary>
    public string Root => _root.FullName;

    /// <summary>
    /// Resolves a project-relative path to its absolute form.
    /// </summary>
    /// <param name="relative">The project-relative path.</param>
    /// <returns>The absolute path.</returns>
    public string PathOf(string relative) => Path.Combine(_root.FullName, relative);

    /// <summary>
    /// Writes a text file at a project-relative path, creating parent directories.
    /// </summary>
    /// <param name="relative">The project-relative path.</param>
    /// <param name="content">The text content.</param>
    public void WriteFile(string relative, string content)
    {
        string absolute = PathOf(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, content);
    }

    /// <summary>
    /// Reads a project-relative text file.
    /// </summary>
    /// <param name="relative">The project-relative path.</param>
    /// <returns>The file's text.</returns>
    public string ReadText(string relative) => File.ReadAllText(PathOf(relative));

    /// <summary>
    /// Determines whether a project-relative path exists.
    /// </summary>
    /// <param name="relative">The project-relative path.</param>
    /// <returns><see langword="true"/> when the file exists.</returns>
    public bool Exists(string relative) => File.Exists(PathOf(relative));

    /// <inheritdoc />
    public void Dispose() => TestTempDirectory.DeleteBestEffort(_root.FullName);
}
