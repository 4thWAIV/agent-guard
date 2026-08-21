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

    /// <summary>
    /// A synthetic <c>ISystemServices</c> mirroring the REAL shipped container, shared byte-identical by the derivation
    /// tests (<c>DerivedBoundaryServicesTests</c>, which drives the internal walk) and the AG0034 whole-tree preventive
    /// test (<c>SystemServicesMemberMustBeServiceAccessorAnalyzerTests.WellFormedRealShape_IsNotReported</c>), so this
    /// ~36-line fixture is spelled exactly once. The <c>IFileSystem</c> sub-container's no-arg accessors expose the four
    /// filesystem services and its parameterized factories return <c>IFileInfo</c>/<c>IDirectoryInfo</c>; the
    /// <c>IPlatformServices</c> sub-container nests <c>IPlatformFileSystem</c>; the root adds environment, guids,
    /// console, signatures, build-info, the platform sub-container, and the <c>TimeProvider</c> clock. The four
    /// filesystem services are reached THROUGH <c>IFileSystem</c>'s accessors, not as direct container properties;
    /// <c>IFileInfo</c>/<c>IDirectoryInfo</c> exist only as the parameterized-factory returns, so they are reachable but
    /// NOT accessors. <c>IPlatformFileSystem</c> is a leaf whose non-service members (<c>char DirectorySeparator</c>, the
    /// separator pass-through, and <c>NeedsExecutableFlag()</c>) are not guarded, so folding the separator member in
    /// changes neither the derived leaf set nor the container set: it is still the ten leaves and the three containers.
    /// </summary>
    internal const string RealShapeContainer = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface IDirectoryEnumerator { }
            public interface IFileWriter { }
            public interface IDirectoryWriter { }
            public interface IEnvironment { }
            public interface IGuidFactory { }
            public interface IConsole { }
            public interface ISignatureService { }
            public interface IBuildInfo { }
            public interface IFileInfo { }
            public interface IDirectoryInfo { }
            public interface IFileSystem
            {
                IFileInfo GetFileInfo(string path);
                IDirectoryInfo GetDirectoryInfo(string path);
                IFileReader GetFileReader();
                IDirectoryEnumerator GetDirectoryReader();
                IFileWriter GetFileWriter();
                IDirectoryWriter GetDirectoryWriter();
            }
            public interface IPlatformFileSystem
            {
                char DirectorySeparator { get; }
                bool NeedsExecutableFlag();
            }
            public interface IPlatformServices
            {
                IPlatformFileSystem FileSystem { get; }
            }
            public interface ISystemServices
            {
                IFileSystem FileSystem { get; }
                IEnvironment Environment { get; }
                IGuidFactory Guids { get; }
                IConsole Console { get; }
                IPlatformServices Platform { get; }
                ISignatureService Signatures { get; }
                IBuildInfo BuildInfo { get; }
                System.TimeProvider Clock { get; }
            }
        }
        """;
}
