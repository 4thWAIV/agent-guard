// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// A throwaway on-disk C# fixture project the File Guard operates against — the REAL read-only base a copy-on-write
/// simulator can be layered over, or the working tree an integration test drives directly. It materializes a realistic
/// project tree (a project file, a protected <c>Directory.Build.props</c> and <c>global.json</c>, and populated
/// <c>bin</c>/<c>obj</c> folders) in a real temp directory and deletes it on disposal. Every read and write goes through
/// the REAL services from <see cref="SystemServicesBuilder.Real"/> — the owned <see cref="IFileWriter"/>,
/// <see cref="IDirectoryWriter"/>, and <see cref="IFileReader"/> — never raw <c>System.IO</c>.
/// </summary>
public sealed class FixtureProject : IDisposable
{
    private readonly ISystemServices _services;
    private readonly string _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureProject"/> class, materializing the fixture in a fresh real
    /// temp directory through the real services.
    /// </summary>
    public FixtureProject()
    {
        _services = SystemServicesBuilder.Real().Build();
        _root = _services.FileSystem.GetDirectoryWriter().CreateTempSubdirectory("agentguard-fixture-");
        WriteFile("Sample.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>\n");
        WriteFile("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n</Project>\n");
        WriteFile("global.json", "{ \"sdk\": { \"version\": \"9.0.101\" } }\n");
        WriteFile(Path.Combine("bin", "Debug", "Sample.dll"), "build output that must be skipped");
        WriteFile(Path.Combine("obj", "Sample.csproj.nuget.g.props"), "build output that must be skipped");
    }

    /// <summary>Gets the absolute root of the fixture project.</summary>
    public string Root => _root;

    /// <summary>Gets the real home directory the container reports, for deriving the machine launcher path a test
    /// cross-checks against the pipeline's own launcher.</summary>
    public string HomeDirectory => _services.Environment.GetHomeDirectory();

    /// <summary>Resolves a project-relative path to its absolute form.</summary>
    /// <param name="relative">The project-relative path.</param>
    /// <returns>The absolute path.</returns>
    public string PathOf(string relative) => Path.Combine(_root, relative);

    /// <summary>Writes a text file at a project-relative path, creating parent directories.</summary>
    /// <param name="relative">The project-relative path.</param>
    /// <param name="content">The text content.</param>
    public void WriteFile(string relative, string content)
    {
        string absolute = PathOf(relative);
        string? directory = Path.GetDirectoryName(absolute);
        if (directory is not null)
        {
            _services.FileSystem.GetDirectoryWriter().CreateDirectory(directory);
        }

        _services.FileSystem.GetFileWriter().WriteAllText(absolute, content);
    }

    /// <summary>Reads a project-relative text file.</summary>
    /// <param name="relative">The project-relative path.</param>
    /// <returns>The file's text.</returns>
    public string ReadText(string relative) => _services.FileSystem.GetFileReader().ReadAllText(PathOf(relative));

    /// <summary>Determines whether a project-relative path exists.</summary>
    /// <param name="relative">The project-relative path.</param>
    /// <returns><see langword="true"/> when the file exists.</returns>
    public bool Exists(string relative) => _services.FileSystem.GetFileReader().Exists(PathOf(relative));

    /// <summary>Deletes a project-relative file through the owned file writer.</summary>
    /// <param name="relative">The project-relative path.</param>
    public void Delete(string relative) => _services.FileSystem.GetFileWriter().DeleteFile(PathOf(relative));

    /// <summary>Creates a real symlink at an absolute path pointing at an absolute target, through the owned platform
    /// file system (the real per-OS implementation), so an integration test drives a genuine on-disk symlink without a
    /// raw <c>System.IO</c> call.</summary>
    /// <param name="linkAbsolute">The absolute symlink path to create.</param>
    /// <param name="targetAbsolute">The absolute target the link points at.</param>
    public void MakeSymlink(string linkAbsolute, string targetAbsolute) =>
        _services.Platform.FileSystem.MakeLinkTarget(linkAbsolute, targetAbsolute);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            _services.FileSystem.GetDirectoryWriter().DeleteDirectory(_root, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a throwaway temporary directory.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup of a throwaway temporary directory.
        }
    }
}
