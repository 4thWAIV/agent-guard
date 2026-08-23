// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Shared filesystem helpers for the structural tests that read the repository's own files (the per-OS <c>.csproj</c>
/// link-share tests, the OS-specific-file placement test, and the CI workflow test). The repository-root walk and the
/// <c>Compile Include</c> path resolution live here once so no structural test re-spells them.
/// </summary>
internal static class RepositoryFiles
{
    private static readonly char[] DirectorySeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    /// <summary>
    /// Walks up from the test output directory to the repository root — the directory that holds
    /// <c>AgentGuard.sln</c>.
    /// </summary>
    /// <returns>The absolute path of the repository root.</returns>
    internal static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AgentGuard.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            "Could not locate the repository root (no AgentGuard.sln found above the test output directory).");
    }

    /// <summary>
    /// Resolves a <c>Compile Include</c> path (which uses either separator and may be relative to the project
    /// directory) to an absolute, normalized path.
    /// </summary>
    /// <param name="baseDirectory">The directory the path is relative to (the project directory).</param>
    /// <param name="relativeOrAbsolute">The include path as written in the project file.</param>
    /// <returns>The resolved absolute path.</returns>
    internal static string ResolvePath(string baseDirectory, string relativeOrAbsolute)
    {
        string normalized = relativeOrAbsolute
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.IsPathRooted(normalized) ? normalized : Path.Combine(baseDirectory, normalized));
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="path"/> lies under <paramref name="directory"/>.
    /// </summary>
    /// <param name="path">The absolute path to test.</param>
    /// <param name="directory">The candidate ancestor directory.</param>
    /// <returns><see langword="true"/> when the path is under the directory.</returns>
    internal static bool IsUnder(string path, string directory)
    {
        string normalizedDirectory = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="path"/> lies under a build intermediate-output directory —
    /// a <c>bin</c> or <c>obj</c> segment anywhere on its path relative to <paramref name="root"/>. The one owner of
    /// the bin/obj-exclusion walk: both the repo-wide source scan and the per-project compiled-file scan skip build
    /// output through this single method, so the exclusion is spelled once.
    /// </summary>
    /// <param name="path">The absolute path to test.</param>
    /// <param name="root">The directory the path's segments are taken relative to (the repository root or a project
    /// directory).</param>
    /// <returns><see langword="true"/> when any segment of the relative path is <c>bin</c> or <c>obj</c>.</returns>
    internal static bool IsUnderIntermediateOutput(string path, string root)
    {
        return Path.GetRelativePath(root, path)
            .Split(DirectorySeparators)
            .Any(segment => string.Equals(segment, "bin", StringComparison.Ordinal)
                || string.Equals(segment, "obj", StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets the <c>&lt;Compile Include=…&gt;</c> elements of a project document — the compile items that name a source
    /// path. The one place the "is this a Compile item carrying an Include" shape is spelled, so both the link-share
    /// and cross-compile structural tests read it the same way; a caller that also needs <c>Link</c> filters further.
    /// </summary>
    /// <param name="project">The loaded project document.</param>
    /// <returns>The Compile elements that carry an Include attribute.</returns>
    internal static IEnumerable<XElement> CompileIncludeElements(XDocument project)
    {
        return project
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.Ordinal)
                && element.Attribute("Include") is not null);
    }
}
