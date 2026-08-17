// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// C# source snippets shared verbatim by more than one analyzer test class, held in one place so a byte-identical
/// fixture is not spelled twice. <see cref="LinkTargetSource"/> is read by both the AG0011 owner test (which proves
/// the rule DOES claim the <c>*Info</c> member read now that the wrappers own <c>*Info</c> wholesale) and the AG0101
/// OS-divergent test (which proves it does NOT — that member moved to AG0011) — the same source, exercised by the two
/// rules on the two sides of the partition it moved across.
/// </summary>
internal static class SharedAnalyzerSources
{
    /// <summary>
    /// A <c>FileInfo.LinkTarget</c> read — a <c>*Info</c> instance member. It reads the member off a passed-in
    /// <c>FileInfo</c> so this fixture isolates the member-read partition: AG0011 reports it (the <c>*Info</c> types are
    /// owned wholesale by the wrapper interfaces, fileinfo-abstraction-stays-in-ag0011) and AG0101 does not (it kept
    /// only the static OS-divergent members after the shrink).
    /// </summary>
    internal const string LinkTargetSource = """
        using System.IO;

        public class Sample
        {
            public string? Read(FileInfo info) => info.LinkTarget;
        }
        """;

    /// <summary>
    /// The one <c>ISystemServices</c> container fixture the derived owner set
    /// (derive-service-set-from-isystemservices) walks: <c>IFileReader</c> is the leaf service accessor and
    /// <c>ISystemServices</c> is the container that exposes it. Prepended to the per-test sources in the AG0024, AG0025,
    /// and AG0031 test classes so the derivation walk sees a non-empty service set — an empty container would derive an
    /// empty set — held here so this byte-identical fixture is not spelled once per class.
    /// </summary>
    internal const string AbstractionsPrefix = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface ISystemServices { IFileReader FileReader { get; } }
        }
        """;
}
