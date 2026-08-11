// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// C# source snippets shared verbatim by more than one analyzer test class, held in one place so a byte-identical
/// fixture is not spelled twice. <see cref="LinkTargetSource"/> is read by both the AG0011 filesystem test (which
/// proves the rule does NOT claim the OS-divergent LinkTarget read) and the AG0101 OS-divergent test (which proves it
/// DOES) — the same source, exercised by the two rules that partition the filesystem surface between them.
/// </summary>
internal static class SharedAnalyzerSources
{
    /// <summary>
    /// A <c>FileInfo.LinkTarget</c> read — an OS-divergent symlink member on an inert <c>FileInfo</c> construction.
    /// AG0011 must not report it (it belongs to AG0101); AG0101 must report it outside the platform libraries.
    /// </summary>
    internal const string LinkTargetSource = """
        using System.IO;

        public class Sample
        {
            public string? Read(string path) => new FileInfo(path).LinkTarget;
        }
        """;
}
