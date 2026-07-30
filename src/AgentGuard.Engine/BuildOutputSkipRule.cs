// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Linq;

namespace AgentGuard.Engine;

/// <summary>
/// Skips a C# build-output directory: a folder named <c>bin</c> or <c>obj</c> that sits directly inside a
/// project directory (one containing a <c>.csproj</c>). It is anchored to project directories, so a genuine
/// source folder that merely shares the name <c>bin</c> at some other depth is not dropped.
/// </summary>
internal sealed class BuildOutputSkipRule : IDirectorySkipRule
{
    private static readonly string[] OutputNames = { "bin", "obj" };

    private BuildOutputSkipRule()
    {
    }

    /// <inheritdoc />
    public string Identity => "build-output:bin,obj@csproj";

    /// <inheritdoc />
    public bool ShouldSkip(string directoryFullPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryFullPath);
        string name = Path.GetFileName(directoryFullPath);
        if (!OutputNames.Contains(name, StringComparer.Ordinal))
        {
            return false;
        }

        string? parent = Path.GetDirectoryName(directoryFullPath);
        if (parent is null)
        {
            return false;
        }

        try
        {
            return Directory.EnumerateFiles(parent, "*.csproj", SearchOption.TopDirectoryOnly).Any();
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates the C# build-output skip rule.
    /// </summary>
    /// <returns>The skip rule, as its interface.</returns>
    internal static IDirectorySkipRule Create() => new BuildOutputSkipRule();
}
