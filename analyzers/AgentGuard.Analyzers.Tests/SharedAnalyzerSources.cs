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

    /// <summary>
    /// The <c>AgentGuard.TestHelpers</c> overlay stand-in — the copy-on-write store <c>InMemoryFileSystemStore</c> and
    /// the overlay factory <c>SystemServicesBuilder.NewOverlay</c> — shared byte-identical by the two seed-site rule
    /// test classes (<c>NoLiteralFakeRootAnalyzerTests</c> for AG0035 and <c>NoLiteralCaseModeSeedAnalyzerTests</c> for
    /// AG0036), so this fixture is spelled exactly once instead of hand-copied into each preamble. <c>NewOverlay</c>
    /// forwards its own <c>tempRoot</c>/<c>caseSensitive</c> parameters to the store constructor, exactly as the real
    /// builder does, so the fixture's own construction seeds from parameters (never a literal) and adds no diagnostic;
    /// the store carries the identity the analyzers pin on when the consuming compilation is named
    /// <c>AgentGuard.TestHelpers</c> (<c>WellKnownType.IsInAssembly</c>). The deliberate post-construction switches
    /// <c>SetCaseSensitive</c> (on the store) and <c>SimulateCaseSensitivity</c> (on the builder) are the entry points
    /// AG0036 must leave alone; the AG0035 preamble prepends this fixture with its own <c>FakeEnvironment</c> stand-in,
    /// the extra seed site only AG0035 reads.
    /// </summary>
    internal const string OverlaySeedHelpers = """
        namespace AgentGuard.TestHelpers
        {
            public sealed class InMemoryFileSystemStore
            {
                public InMemoryFileSystemStore(string tempRoot, bool caseSensitive) { }
                public void SetCaseSensitive(bool caseSensitive) { }
            }

            public sealed class SystemServicesBuilder
            {
                public static InMemoryFileSystemStore NewOverlay(string tempRoot, bool caseSensitive) =>
                    new InMemoryFileSystemStore(tempRoot, caseSensitive);
                public void SimulateCaseSensitivity(bool caseSensitive) { }
            }
        }
        """;

    /// <summary>
    /// The one canonical stub for the presence Abstractions surface — <c>IPresenceCheck</c> (carrying its single
    /// <c>Check</c> operation) and the <c>PresenceRequest</c> record — shared byte-identical by the three presence-rule
    /// test classes that each prepended their own hand-rolled copy (<c>NoTimeoutInPresenceImplAnalyzerTests</c> for
    /// AG0107, <c>PresenceCheckOnlyFromApprovalGateAnalyzerTests</c> for AG0108, and
    /// <c>NoInlinePromptLiteralAtPresenceCallAnalyzerTests</c> for AG0109), so this fixture is spelled exactly once
    /// (LESSON 1, DRY). It is the fullest shape: AG0107's fixtures implement <c>IPresenceCheck</c> without supplying
    /// <c>Check</c> (the analyzer runner returns only the analyzer's own diagnostics, so the incomplete-implementation
    /// compile error is irrelevant), AG0108 invokes <c>Check</c>, and AG0109 constructs <c>PresenceRequest</c>.
    /// </summary>
    internal const string PresenceContract = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IPresenceCheck
            {
                int Check();
            }

            public sealed record PresenceRequest(string PromptText);
        }
        """;

    /// <summary>
    /// The one canonical stub for the macOS native presence port — <c>ILocalAuthentication</c> in the
    /// <c>AgentGuard.CrossPlatform.MacOS</c> namespace — shared byte-identical by the three native-port rule test classes
    /// that each spelled their own copy (<c>NoCachedNativeAuthContextAnalyzerTests</c> for AG0112,
    /// <c>NativePresencePortSingleImplementerAnalyzerTests</c> for AG0114, and
    /// <c>NoTimeoutInPresenceImplAnalyzerTests</c> for AG0107's port-owner case), so this stub is spelled exactly once
    /// (LESSON 1, DRY). It is its own namespace block, so a consuming fixture prepends it and declares the implementer in
    /// a second <c>AgentGuard.CrossPlatform.MacOS</c> block that sees the interface across the two declarations.
    /// </summary>
    internal const string LocalAuthenticationPort = """
        namespace AgentGuard.CrossPlatform.MacOS
        {
            public interface ILocalAuthentication { }
        }
        """;

    /// <summary>
    /// The one canonical stub for the macOS presence port's native binding — the <c>LocalAuthNative</c> static class
    /// carrying the <c>objc_msgSend</c> P/Invoke (<c>[DllImport("libobjc")] internal static extern IntPtr
    /// ObjcMsgSend(...)</c>) in its own <c>AgentGuard.CrossPlatform.MacOS</c> namespace block — shared byte-identical by
    /// the two native-interop rule test classes that each spelled their own copy
    /// (<c>OsDivergentFilesystemOnlyInCrossPlatformAnalyzerTests</c> for AG0101 and
    /// <c>PresenceNativeInteropOwnerAnalyzerTests</c> for AG0113), so this binding is spelled exactly once (LESSON 1,
    /// DRY). It pairs with <see cref="LocalAuthenticationPort"/>: a consuming fixture prepends the port interface and
    /// this binding, then declares the presence-port implementer that calls <c>ObjcMsgSend</c>.
    /// </summary>
    internal const string LocalAuthNativeBinding = """
        namespace AgentGuard.CrossPlatform.MacOS
        {
            using System;
            using System.Runtime.InteropServices;
            internal static class LocalAuthNative
            {
                [DllImport("libobjc")]
                internal static extern IntPtr ObjcMsgSend(IntPtr self, IntPtr selector);
            }
        }
        """;

    /// <summary>
    /// The one canonical stub for the Linux native presence port — <c>IPolkitAuthority</c> in the
    /// <c>AgentGuard.CrossPlatform.Linux</c> namespace — the Linux sibling of <see cref="LocalAuthenticationPort"/>,
    /// shared byte-identical by the three Linux-port rule test classes that each spelled their own copy
    /// (<c>DbusOnlyInPolkitAuthorityAnalyzerTests</c> for AG0110, <c>NativePresencePortSingleImplementerAnalyzerTests</c>
    /// for AG0114's Polkit case, and <c>NoTimeoutInPresenceImplAnalyzerTests</c> for AG0107's Polkit port-owner case), so
    /// this stub is spelled exactly once (LESSON 1, DRY). Like the macOS sibling it is its own namespace block, so a
    /// consuming fixture prepends it and declares the implementer in a second <c>AgentGuard.CrossPlatform.Linux</c> block
    /// that sees the interface across the two declarations.
    /// </summary>
    internal const string PolkitAuthorityPort = """
        namespace AgentGuard.CrossPlatform.Linux
        {
            public interface IPolkitAuthority { }
        }
        """;

    /// <summary>
    /// The one canonical stub for the macOS native-OPS owner — <c>IObjCRuntime</c> in the
    /// <c>AgentGuard.CrossPlatform.MacOS</c> namespace — the thin raw-call layer the coverage refactor introduces one
    /// level below the <c>ILocalAuthentication</c> flow port. Shared byte-identical by the rule test classes that scope
    /// to the native-ops owner: the relocated raw-interop exemption (<c>PresenceNativeInteropOwnerAnalyzerTests</c> for
    /// AG0113, <c>OsDivergentFilesystemOnlyInCrossPlatformAnalyzerTests</c> for AG0101's native branch), the three
    /// native-ops guardrails (<c>NoControlFlowInNativeOpsAnalyzerTests</c> for AG0106,
    /// <c>NoAsyncOrchestrationInNativeOpsAnalyzerTests</c> for AG0115, <c>NoStateInNativeOpsAnalyzerTests</c> for AG0116),
    /// and the AG0107/AG0114 extensions, so this stub is spelled exactly once (LESSON 1, DRY). Its own namespace block,
    /// so a consuming fixture prepends it and declares the native-ops implementer in a second
    /// <c>AgentGuard.CrossPlatform.MacOS</c> block that sees the interface across the two declarations.
    /// </summary>
    internal const string ObjCRuntimeNativeOps = """
        namespace AgentGuard.CrossPlatform.MacOS
        {
            public interface IObjCRuntime { }
        }
        """;
}
