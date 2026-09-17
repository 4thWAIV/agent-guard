// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// C# source snippets held in one place so a byte-identical fixture is not spelled twice: most are shared verbatim by
/// more than one analyzer test class, and the three container-implementer member blocks
/// (<see cref="SystemServicesContainerMembers"/>, <see cref="FileSystemContainerMembers"/>,
/// <see cref="PlatformServicesContainerMembers"/>) hold the accessors an AG0022 container implementer must supply,
/// so each set is spelled once rather than once per implementer. <see cref="LinkTargetSource"/> is read by both the AG0011 owner
/// test (which proves the rule DOES claim the <c>*Info</c> member read now that the wrappers own <c>*Info</c>
/// wholesale) and the AG0101 OS-divergent test (which proves it does NOT — that member moved to AG0011) — the same
/// source, exercised by the two rules on the two sides of the partition it moved across.
/// <para>
/// Some members here are not fixture text but the shared readers and assertions the same test classes were each
/// spelling for themselves: <see cref="TypeReferencePositions"/>, the one table of every position an Engine class can
/// reach a guarded TYPE through; <see cref="Message"/>, the one reader of a diagnostic's message; and
/// <see cref="AssertReported"/> with <see cref="AssertAllReported"/>, the one "this is the expected diagnostic"
/// assertion, which takes the asking rule's own <c>DiagnosticId</c> constant so no test class spells a rule
/// identifier as a literal. Each had a byte-identical copy per consuming class before it was brought here. So do the
/// one-door message fragments (<see cref="CallSiteFailureFragment"/>, <see cref="ReferenceSiteFailureFragment"/>,
/// <see cref="CalledMemberFragment"/>, <see cref="ReferencedTypeFragment"/> and <see cref="NotTheDoorFragment"/>):
/// the three one-door rules share one message contract, so the text their tests assert on is spelled here once — in
/// the test layer, never read back off the analyzer.
/// </para>
/// <para>
/// <see cref="AssertNamesExactly"/> is the one owner of the PAIRED check that contract carries: the conditions a
/// one-door message must name, and the conditions it must not. It was hand-copied at each reporting test before it
/// was brought here, and the copies drifted — several asserted only that the failed condition was named, so they
/// passed whether or not the message also accused a condition that had not failed.
/// </para>
/// </summary>
internal static class SharedAnalyzerSources
{
    /// <summary>
    /// The fake <c>AgentGuard.Engine</c> assembly the access rules are exercised against, spelled once and referenced
    /// by every test class that needs the relocated container to come from a REAL second assembly: AG0041
    /// (<c>EngineInternalsOneDoorAnalyzerTests</c>), which reaches it from the <c>guard</c> and
    /// <c>AgentGuard.TestHelpers</c> compilations, and AG0017's post-relocation regression
    /// (<c>GuardedConstructionAnalyzerTests</c>), which reaches it from an <c>AgentGuard.Tests</c> compilation.
    /// <para>
    /// Every type and member the rules govern is <c>internal</c>, and the three <c>InternalsVisibleTo</c> grants
    /// mirror the ones the relocation gives Engine, so a fixture that reaches one of them COMPILES: the assertion then
    /// turns on the analyzer rather than on a CS0122 inaccessibility error that would read exactly like a clean accept.
    /// <c>Create()</c> hands back the PUBLIC <c>ISystemServices</c> contract, as the real factory does, so accepting the
    /// approved call does not drag an internal Engine type into the caller through the return value. A same-named
    /// decoy container sits in the wrong namespace so a test can prove the privileged caller identity is the
    /// namespace-plus-assembly-plus-name conjunction, not the bare name.
    /// </para>
    /// <para>
    /// The container here is UNSEALED with a <c>protected</c> constructor, which is the fake's one deliberate
    /// divergence from the production container (<c>internal sealed</c>, private constructor — and it stays that way).
    /// The rule has to be proved against the container named in a BASE LIST, and a fixture that cannot compile proves
    /// nothing: every fixture in this file compiles under the runner's default empty compiler-error expectation, so a
    /// reported diagnostic is the analyzer's judgement and not a by-product of broken code. Nothing else moves — the
    /// namespace, assembly, name, <c>internal</c> visibility and <c>ISystemServices</c> base list are the governed
    /// identity, and each is preserved exactly.
    /// </para>
    /// </summary>
    internal const string EngineAssemblySource = $$"""
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo("guard")]
        [assembly: InternalsVisibleTo("AgentGuard.TestHelpers")]
        [assembly: InternalsVisibleTo("AgentGuard.Tests")]

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface ISystemServices { }
        }

        namespace AgentGuard.Engine
        {
            using AgentGuard.Abstractions.Contracts;

            internal class SystemServices : ISystemServices
            {
                protected SystemServices() { }

                internal static ISystemServices Create() => new SystemServices();

                internal static object Other() => null!;

                internal int Value => 0;
            }

            internal class EngineInternal
            {
                internal static object Read() => null!;

                internal int Count => 0;
            }

            internal sealed class EngineCache<T>
            {
            }

            public sealed class EnginePublic
            {
                public static object Read() => null!;

                public int Count => 0;
            }
        }

        namespace {{WrongContainerNamespace}}
        {
            internal static class SystemServices
            {
                internal static object Create() => null!;
            }
        }
        """;

    /// <summary>
    /// The assembly name <see cref="EngineAssemblySource"/> is compiled into — the real
    /// <c>AgentGuard.Engine</c> identity the rules match on, so the fake is the governed assembly rather than a
    /// same-named impostor. One owner, read by <see cref="RunAgainstFakeEngineAsync{TAnalyzer}"/>.
    /// </summary>
    internal const string EngineAssemblyName = "AgentGuard.Engine";

    /// <summary>
    /// The using directive a consumer reaches the fake Engine assembly through — the mirror of
    /// <see cref="CrossPlatformUsing"/> for the Engine-facing fixtures, spelled here once rather than as a separately
    /// named constant per test class.
    /// </summary>
    internal const string EngineUsing = "using " + EngineAssemblyName + ";";

    /// <summary>
    /// The namespace a <c>SystemServices</c> container is declared in when a fixture needs the privileged CALLER's
    /// namespace to be the wrong one. It is a child of the real namespace, the nearest miss there is, so a caller test
    /// that matched on a prefix rather than on the whole name would wrongly accept it. <see cref="EngineAssemblySource"/>
    /// interpolates this same constant into its decoy container's <c>namespace</c> declaration, so the wrong-namespace
    /// container reads the same wherever a test declares one and changing it here moves every declaration and every
    /// expected message together.
    /// </summary>
    internal const string WrongContainerNamespace = EngineAssemblyName + ".Decoy";

    /// <summary>
    /// The compiled assembly name of the CLI consumer the Engine grant reaches — <c>guard</c>, not the
    /// <c>AgentGuard.Cli</c> root namespace. Held here once because more than one test class compiles a fixture as
    /// that consumer.
    /// </summary>
    internal const string GuardAssemblyName = "guard";

    /// <summary>
    /// The fully qualified name of the internal Engine type <see cref="EngineAssemblySource"/> declares, as a
    /// diagnostic message spells it. Held here once because more than one test class asserts on it.
    /// </summary>
    internal const string EngineInternalTypeName = EngineAssemblyName + ".EngineInternal";

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
    /// (LESSON 1, DRY). It is the fullest shape: AG0107's fixtures implement <c>IPresenceCheck</c> and supply
    /// <c>Check</c> (the analyzer runner requires each fixture to produce exactly the compiler errors its call site
    /// declares, and these declare none, so an unimplemented member would fail the run), AG0108 invokes <c>Check</c>,
    /// and AG0109 constructs <c>PresenceRequest</c>.
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

    /// <summary>
    /// The service accessors an <c>ISystemServices</c> implementer supplies for the AG0022 container tree declared by
    /// <c>ContainerInterfaceSingleOwnerAnalyzerTests.ContainersSource</c>, whose root container exposes the
    /// <c>IFileSystem</c> and <c>IPlatformServices</c> sub-containers. An AG0022 fixture that declares an
    /// <c>ISystemServices</c> implementer interpolates this block into the class body, so the pair is spelled exactly
    /// once instead of once per implementer (LESSON 1, DRY).
    /// </summary>
    internal const string SystemServicesContainerMembers = """
                public AgentGuard.Abstractions.Contracts.IFileSystem FileSystem => throw new System.NotImplementedException();

                public AgentGuard.Abstractions.Contracts.IPlatformServices Platform => throw new System.NotImplementedException();
        """;

    /// <summary>
    /// The service accessor an <c>IFileSystem</c> implementer supplies for the same AG0022 container tree, whose
    /// <c>IFileSystem</c> sub-container exposes the single <c>IFileReader</c> accessor. Interpolated into the class
    /// body of an AG0022 fixture that declares an <c>IFileSystem</c> implementer, so the accessor is spelled exactly
    /// once however many implementers a fixture declares (LESSON 1, DRY).
    /// </summary>
    internal const string FileSystemContainerMembers = """
                public AgentGuard.Abstractions.Contracts.IFileReader GetFileReader() => throw new System.NotImplementedException();
        """;

    /// <summary>
    /// The service accessor an <c>IPlatformServices</c> implementer supplies for the same AG0022 container tree, whose
    /// <c>IPlatformServices</c> sub-container exposes the single <c>IPlatformFileSystem</c> accessor. Interpolated
    /// into the class body of an AG0022 fixture that declares an <c>IPlatformServices</c> implementer, so the
    /// accessor is spelled exactly once however many implementers a fixture declares (LESSON 1, DRY).
    /// </summary>
    internal const string PlatformServicesContainerMembers = """
                public AgentGuard.Abstractions.Contracts.IPlatformFileSystem FileSystem => throw new System.NotImplementedException();
        """;

    /// <summary>
    /// The container factory that reaches NOTHING — the inert <c>Create()</c> a fixture supplies when the reach under
    /// test is written somewhere else and the one permitted site must stay empty. Spelled once here and used by every
    /// fixture that needs it: the "another method on the container" and "another Engine class" shells below, the
    /// <see cref="EngineHolder"/> shell, and each test class's own one-off Engine sources.
    /// </summary>
    internal const string InertContainerFactory = "        internal static object Create() => null!;";

    /// <summary>
    /// The identifier every aliased-reach fixture binds its using alias to. A using alias is one of the positions a
    /// guarded type can be reached through, and four fixtures declare one — AG0023 and AG0029 against their own
    /// guarded type, and AG0041 against an Engine internal and against the container — so the alias name is spelled
    /// here once and read by <see cref="AliasUsing"/> and <see cref="TypeofAliasDeclaration"/>.
    /// </summary>
    internal const string AliasName = "Aliased";

    /// <summary>
    /// A member that names the ALIASED type in a <c>typeof</c> — the reach three alias fixtures make once the alias is
    /// declared (AG0023, AG0029 and AG0041 against an Engine internal), spelled here rather than once per rule.
    /// </summary>
    internal const string TypeofAliasDeclaration =
        "        internal static object Typed() => typeof(" + AliasName + ");";

    /// <summary>
    /// The fragment a one-door rule's message carries when the reach is the door but the CALL site is not
    /// <c>AgentGuard.Engine.SystemServices.Create()</c>. The three one-door rules produce one message contract between
    /// them, and AG0040, AG0023 and AG0029 each asserted on this same text, so the expected fragments are spelled here
    /// once — in the test layer, never read back off the analyzer, so an assertion still proves more than that the
    /// rule equals itself.
    /// </summary>
    internal const string CallSiteFailureFragment = "the call site is not";

    /// <summary>
    /// The same fragment for a reach that NAMES or CARRIES the door type rather than calling it, asserted by the
    /// AG0023 and AG0029 type-reference tests.
    /// </summary>
    internal const string ReferenceSiteFailureFragment = "the reference site is not";

    /// <summary>
    /// The opening of the subject a one-door message names when the CALLED MEMBER is not the door, asserted where a
    /// test proves the site is right and only the member failed.
    /// </summary>
    internal const string CalledMemberFragment = "the called member";

    /// <summary>
    /// The opening of that same subject when the reach NAMES or CARRIES a guarded type rather than calling a member on
    /// it — the type-reference lens's half of the pair <see cref="CalledMemberFragment"/> opens for the call lens.
    /// </summary>
    internal const string ReferencedTypeFragment = "the referenced type";

    /// <summary>
    /// The close of that same subject. A test proving the message names the SITE alone asserts this fragment is
    /// absent, so a call to the door is never reported as not being the door.
    /// </summary>
    internal const string NotTheDoorFragment = "is not the permitted door";

    /// <summary>
    /// The namespace AG0023's and AG0029's guarded types are declared in. AG0023 guards the core assembly and AG0029
    /// guards the three per-OS implementations, and all four assemblies declare their types in this ONE namespace, so
    /// both test classes spell it — in a using directive and in the qualified names the rules' messages carry. The one
    /// owner of the name, so neither is a loose literal.
    /// </summary>
    internal const string CrossPlatformNamespace = "AgentGuard.CrossPlatform";

    /// <summary>
    /// The using directive for <see cref="CrossPlatformNamespace"/> — the directive both test classes reach their
    /// guarded types through, spelled here once rather than as a separately named constant per test class.
    /// </summary>
    internal const string CrossPlatformUsing = "using " + CrossPlatformNamespace + ";";

    /// <summary>
    /// Builds a CONSUMER compilation — the shell a gated consumer (<c>guard</c>, <c>AgentGuard.TestHelpers</c>) or the
    /// ungated <c>AgentGuard.Tests</c> reaches the fake Engine assembly from. It is the counterpart of
    /// <see cref="EngineCompilation"/> on the other side of the reference: AG0041's gate and the shared carried-type
    /// lens both place their reach in exactly this shell, so it is spelled once here instead of once per test class.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="body">The members of the consumer class, one of which makes the reach under test.</param>
    /// <param name="extraTypes">Further type declarations beside the consumer class.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string GatedConsumer(string usings, string body, string extraTypes)
    {
        const string template = """
            {0}

            internal static class Consumer
            {{
            {1}
            }}

            {2}
            """;

        return string.Format(CultureInfo.InvariantCulture, template, usings, body, extraTypes);
    }

    /// <summary>
    /// Builds a source compiled as the <c>AgentGuard.Engine</c> assembly: the <c>AgentGuard.Engine</c> namespace, the
    /// <c>SystemServices</c> container whose body is <paramref name="containerBody"/>, and any further Engine types in
    /// <paramref name="otherEngineTypes"/>. Every Engine-gated rule needs exactly this shell — AG0040, AG0023 and
    /// AG0029 all have to place the same reach inside <c>Create()</c>, inside another method on the container, and
    /// inside another Engine class — so the shell is spelled once here instead of once per test class.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="containerBody">The members of <c>AgentGuard.Engine.SystemServices</c>.</param>
    /// <param name="otherEngineTypes">Further type declarations inside the <c>AgentGuard.Engine</c> namespace.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string EngineCompilation(string usings, string containerBody, string otherEngineTypes)
    {
        return EngineCompilation(EngineAssemblyName, usings, containerBody, otherEngineTypes);
    }

    /// <summary>
    /// The same shell with the container's NAMESPACE supplied by the caller, so a fixture can vary the one leg of the
    /// privileged caller's identity that the assembly name and the type name do not cover. Only two namespaces are
    /// ever passed — the real <see cref="EngineAssemblyName"/> and <see cref="WrongContainerNamespace"/> — and both
    /// flow through this one template, so the correct-namespace and wrong-namespace fixtures differ in the namespace
    /// and in nothing else.
    /// </summary>
    /// <param name="containerNamespace">The namespace the <c>SystemServices</c> container is declared in.</param>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="containerBody">The members of the <c>SystemServices</c> container.</param>
    /// <param name="otherEngineTypes">Further type declarations inside that namespace.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string EngineCompilation(
        string containerNamespace, string usings, string containerBody, string otherEngineTypes)
    {
        const string template = """
            {0}

            namespace {1}
            {{
                internal sealed class SystemServices
                {{
            {2}
                }}

            {3}
            }}
            """;

        return string.Format(
            CultureInfo.InvariantCulture, template, usings, containerNamespace, containerBody, otherEngineTypes);
    }

    /// <summary>
    /// The fixture for a reach written at the ONE permitted site: directly inside
    /// <c>AgentGuard.Engine.SystemServices.Create()</c>. Every Engine-gated one-door rule needs this exact site, so it
    /// is built here once.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="expression">The expression whose reach is under test.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string InsideContainerFactory(string usings, string expression)
    {
        return ContainerFactoryIn(EngineAssemblyName, usings, expression);
    }

    /// <summary>
    /// The same fixture with the container declared in <see cref="WrongContainerNamespace"/> instead: a class still
    /// named <c>SystemServices</c>, whose <c>Create()</c> is still static, still compiled into the real
    /// <c>AgentGuard.Engine</c> assembly, reaching the SAME genuine door — with the namespace, and only the namespace,
    /// wrong. It is the caller-side counterpart of the wrong-namespace decoy each rule already tests on the callee
    /// side, and all three Engine-gated rules drive it, so it is built here once.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="expression">The expression whose reach is under test.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string InsideContainerFactoryInWrongNamespace(string usings, string expression)
    {
        return ContainerFactoryIn(WrongContainerNamespace, usings, expression);
    }

    /// <summary>
    /// The fixture for a reach written at that same permitted site when the site needs STATEMENTS rather than a single
    /// expression: <paramref name="statements"/> become the body of <c>AgentGuard.Engine.SystemServices.Create()</c>.
    /// The lambda and local-function shells below are built on it, and so is each one-door rule's own
    /// local-declaration fixture, so the block-bodied factory is spelled once.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="statements">The statements of <c>Create()</c>, indented as source.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string InsideContainerFactoryWithBody(string usings, string statements)
    {
        return EngineCompilation(usings, BlockBodiedMember("Create", statements), string.Empty);
    }

    /// <summary>
    /// The fixture for a reach written in a separate Engine HOLDER class beside an inert container factory — the shell
    /// the type-reference positions from <see cref="TypeReferencePositions"/> are dropped into, so the reach is a type
    /// a member holds or names rather than a call. AG0023 and AG0029 drive the same shell against their own guarded
    /// type, so it is built here once.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="holderBody">The members of the holder class, one of which reaches the guarded type.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string EngineHolder(string usings, string holderBody)
    {
        string holder = "    internal static class Holder\n    {\n" + holderBody + "\n    }";
        return EngineCompilation(usings, InertContainerFactory, holder);
    }

    /// <summary>
    /// A class declaration whose BASE LIST names <paramref name="baseType"/> — the one position that reaches a type by
    /// inheriting it rather than by using it. Four fixtures need exactly this declaration and differ only in the type
    /// they derive from — AG0023 against a CrossPlatform-core type, AG0029 against a per-OS type, and AG0041 against
    /// both an Engine internal and the container — so it is built here once. It carries no indentation of its own and
    /// is interpolated into whichever shell the consuming fixture uses.
    /// </summary>
    /// <param name="baseType">The name of the type the declaration derives from.</param>
    /// <returns>The class declaration.</returns>
    internal static string DerivedFrom(string baseType)
    {
        return "internal sealed class Derived : " + baseType + "\n{\n}";
    }

    /// <summary>
    /// The Engine fixture for a base-list reach: an inert container factory beside a class deriving from
    /// <paramref name="baseType"/>. AG0023 and AG0029 drive this same shell against their own guarded type, so the
    /// shell is spelled once here rather than once per rule.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="baseType">The guarded type the declaration derives from.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string EngineDerivedFrom(string usings, string baseType)
    {
        return EngineCompilation(usings, InertContainerFactory, DerivedFrom(baseType));
    }

    /// <summary>
    /// The Engine fixture that reaches <c>IPlatformServices</c> — a type declared in <c>AgentGuard.Abstractions</c>,
    /// named in a <c>typeof</c> and held as a property — beside an inert container factory. That type is in NEITHER
    /// one-door rule's set however its namespace reads, so AG0023 and AG0029 each prove it is never reported, from a
    /// fixture that was byte-identical for the two of them; it is built here once.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string EngineAbstractionsTypeReference(string usings)
    {
        const string abstractions = "}\n\nnamespace AgentGuard.Abstractions.Contracts\n{\n"
            + "    public interface IPlatformServices { }\n\n"
            + "    public static class Holder\n    {\n"
            + "        public static object Typed() => typeof(IPlatformServices);\n\n"
            + "        public static IPlatformServices Held => null!;\n    }";

        return EngineCompilation(usings, InertContainerFactory, abstractions);
    }

    /// <summary>
    /// The <c>AgentGuard.Boundaries</c> fixture that NAMES <paramref name="guardedType"/> without calling a member on
    /// it: the type held as a field and named in a <c>typeof</c>. Each one-door rule's Boundaries gate stays
    /// member-access only after the Engine type-reference extension, so AG0023 and AG0029 each prove such a reference
    /// is not reported there — from a fixture that was byte-identical for the two of them, comment included. It is
    /// built here once and varies only in the guarded type each rule names.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs.</param>
    /// <param name="guardedType">The simple name of the guarded type the holder reaches.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string BoundariesTypeReference(string usings, string guardedType)
    {
        return usings + "\n\npublic class Holder\n{\n"
            + "    private static readonly " + guardedType + " Field = null!;\n\n"
            + "    public object Typed() => typeof(" + guardedType + ");\n}";
    }

    /// <summary>
    /// A documented member: a summary whose two <c>cref</c>s name <paramref name="documentedType"/> and
    /// <paramref name="documentedMember"/>, above a member that reaches nothing. The documentation exclusion is proved
    /// by all three access rules and each spelled this same block, differing only in the names its crefs carry, so it
    /// is built here once.
    /// </summary>
    /// <param name="documentedType">The guarded type one cref names.</param>
    /// <param name="documentedMember">The guarded member the other cref names.</param>
    /// <returns>The documented member declaration, for a container, holder or consumer shell.</returns>
    internal static string DocumentedMember(string documentedType, string documentedMember)
    {
        return "        /// <summary>Documents <see cref=\"" + documentedType + "\"/> and <see cref=\""
            + documentedMember + "\"/>.</summary>\n"
            + "        internal static object Nothing() => null!;";
    }

    /// <summary>
    /// Adds a using ALIAS for <paramref name="aliasedType"/> to <paramref name="usings"/>, bound to
    /// <see cref="AliasName"/>. The alias declaration names the guarded type and so is a reach in its own right; four
    /// fixtures build exactly this directive, differing only in the type aliased, so it is built here once.
    /// </summary>
    /// <param name="usings">The using directives the fixture already needs.</param>
    /// <param name="aliasedType">The fully qualified type the alias binds to.</param>
    /// <returns>The using directives with the alias appended.</returns>
    internal static string AliasUsing(string usings, string aliasedType)
    {
        return usings + "\nusing " + AliasName + " = " + aliasedType + ";";
    }

    /// <summary>
    /// The fixture for a reach written in ANOTHER method on the container class. The permitted site is the
    /// <c>Create()</c> method itself, not the type, so this site is reported.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="expression">The expression whose reach is under test.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string InsideOtherContainerMethod(string usings, string expression)
    {
        string body = InertContainerFactory + "\n\n"
            + "        internal static object Other() => " + expression + ";";
        return EngineCompilation(usings, body, string.Empty);
    }

    /// <summary>
    /// The statements sibling of <see cref="InsideOtherContainerMethod"/>: the reach is written in another method on
    /// the container whose body needs STATEMENTS rather than a single expression — a local declaration, for instance —
    /// beside an inert container factory. The site is reported for the same reason the expression form is.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="statements">The statements of the other container method, indented as source.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string InsideOtherContainerMethodWithBody(string usings, string statements)
    {
        return EngineCompilation(
            usings, InertContainerFactory + "\n\n" + BlockBodiedMember("Other", statements), string.Empty);
    }

    /// <summary>
    /// The fixture for a reach written in a DIFFERENT Engine class. Nothing outside the container factory may reach a
    /// guarded assembly, so this site is reported.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="expression">The expression whose reach is under test.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string InsideOtherEngineClass(string usings, string expression)
    {
        string otherType = "    internal sealed class OtherEngineType\n"
            + "    {\n"
            + "        internal static object Build() => " + expression + ";\n"
            + "    }";
        return EngineCompilation(usings, InertContainerFactory, otherType);
    }

    /// <summary>
    /// The fixture for a reach written inside a LAMBDA nested in <c>Create()</c>'s body. The caller test takes the
    /// narrowest reading — the call site's own containing method must BE <c>Create()</c> — and a lambda is a distinct
    /// method symbol, so this site is reported.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="expression">The expression whose reach is under test.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string InsideLambdaInContainerFactory(string usings, string expression)
    {
        string statements = "            System.Func<object> build = () => " + expression + ";\n"
            + "            return build();";
        return InsideContainerFactoryWithBody(usings, statements);
    }

    /// <summary>
    /// The fixture for a reach written inside a LOCAL FUNCTION nested in <c>Create()</c>'s body. Like the lambda, a
    /// local function is a distinct method symbol, so this site is reported.
    /// </summary>
    /// <param name="usings">The using directives the fixture needs, or an empty string.</param>
    /// <param name="expression">The expression whose reach is under test.</param>
    /// <returns>The complete fixture source.</returns>
    internal static string InsideLocalFunctionInContainerFactory(string usings, string expression)
    {
        string statements = "            static object Build() => " + expression + ";\n"
            + "            return Build();";
        return InsideContainerFactoryWithBody(usings, statements);
    }

    /// <summary>
    /// Every position in which an Engine member can REACH the guarded type named <paramref name="guardedType"/>
    /// without calling a member on it — the one table the Engine type-reference expectation is proved against, built
    /// here once instead of being spelled out per rule. The first seven entries land on the written-name lens
    /// (<c>typeof</c>, <c>nameof</c>, a cast, an <c>is</c> pattern, a generic type argument, a generic argument inside
    /// a <c>typeof</c>, and a generic constraint); the last seven land on the carried-type lens (a field, a property,
    /// a method return, a parameter, a local, an inferred <c>var</c> local, and a type argument nested inside a
    /// generic of an array). The two consumers — AG0023 against a CrossPlatform-core type and AG0029 against a per-OS
    /// type — differ only in which type name they pass, so the table itself varies in nothing else.
    /// </summary>
    /// <param name="guardedType">The simple name of the guarded type each position reaches.</param>
    /// <returns>One member declaration per reference position, ready to interpolate into an Engine holder class.</returns>
    internal static TheoryData<string> TypeReferencePositions(string guardedType)
    {
        return new TheoryData<string>
        {
            "        internal static object Typed() => typeof(" + guardedType + ");",
            "        internal static string Named() => nameof(" + guardedType + ");",
            "        internal static object Cast(object value) => (" + guardedType + ")value;",
            "        internal static bool Pattern(object value) => value is " + guardedType + ";",
            "        internal static object Argument() => new System.Collections.Generic.List<" + guardedType + ">();",
            "        internal static object Attributed() => typeof(System.Collections.Generic.List<" + guardedType + ">);",
            "        internal static T Constrained<T>() where T : " + guardedType + " => default!;",
            "        private static readonly " + guardedType + " Field = null!;",
            "        internal static " + guardedType + " Property => null!;",
            "        internal static " + guardedType + " Returned() => null!;",
            "        internal static object Parameter(" + guardedType + " value) => value;",
            "        internal static object Local() { " + guardedType + " value = null!; return value; }",
            "        internal static object Inferred() { var value = Declared(); return value; }\n"
                + "        private static " + guardedType + " Declared() => null!;",
            "        internal static object Nested() { System.Collections.Generic.List<" + guardedType
                + "[]> all = null!; return all; }",
        };
    }

    /// <summary>
    /// Compiles <paramref name="source"/> and returns the semantic model of its single syntax tree. Three test
    /// classes drive a helper through a model rather than through an analyzer's diagnostics — the written-name lens,
    /// the shared resolver, and the carried-type lens — and each was spelling the same compile-then-model pair, so it
    /// is spelled here once. The returned model carries its own <see cref="SemanticModel.SyntaxTree"/> and
    /// <see cref="SemanticModel.Compilation"/>, so a caller that needs either still gets it from this one call.
    /// </summary>
    /// <param name="source">The C# source to compile.</param>
    /// <returns>The semantic model of the compiled source.</returns>
    internal static async Task<SemanticModel> SemanticModelAsync(string source)
    {
        Compilation compilation = await AnalyzerRunner.CompileAsync(source).ConfigureAwait(false);
        return compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
    }

    /// <summary>
    /// Reads the named type <paramref name="metadataName"/> out of <paramref name="compilation"/>, failing the test
    /// when the fixture does not declare it. A direct helper test that builds symbols by hand starts here, so the
    /// lookup and its "the fixture really declares this" check are spelled once rather than once per test class.
    /// </summary>
    /// <param name="compilation">The compilation to read the type from.</param>
    /// <param name="metadataName">The type's metadata name.</param>
    /// <returns>The named type symbol.</returns>
    internal static INamedTypeSymbol TypeIn(Compilation compilation, string metadataName)
    {
        return Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(metadataName));
    }

    /// <summary>
    /// Reads the one node of kind <typeparamref name="T"/> in <paramref name="tree"/>, optionally the one matching
    /// <paramref name="where"/>, failing the test when the fixture holds anything other than exactly one. A direct
    /// helper test hands a specific written node to the helper under test, and "the fixture's only node of this kind"
    /// is how each of them picks it, so the walk is spelled here once.
    /// </summary>
    /// <typeparam name="T">The syntax node kind to read.</typeparam>
    /// <param name="tree">The tree to read from.</param>
    /// <param name="where">An optional filter, when the kind alone does not single the node out.</param>
    /// <returns>The single matching node.</returns>
    internal static T OnlyNode<T>(SyntaxTree tree, Func<T, bool>? where = null)
        where T : SyntaxNode
    {
        IEnumerable<T> nodes = tree.GetRoot().DescendantNodes().OfType<T>();
        return where is null ? nodes.Single() : nodes.Single(where);
    }

    /// <summary>
    /// Runs <typeparamref name="TAnalyzer"/> over <paramref name="source"/> compiled as
    /// <paramref name="assemblyName"/>, against <see cref="EngineAssemblySource"/> compiled as a real second assembly
    /// named <see cref="EngineAssemblyName"/>. Three test classes need exactly this pairing — AG0041's gate, AG0017's
    /// post-relocation regression from <c>AgentGuard.Tests</c>, and the shared carried-type lens — so the reference
    /// wiring is spelled once instead of once per class.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer to run.</typeparam>
    /// <param name="source">The consumer source to analyze.</param>
    /// <param name="assemblyName">The assembly name the consumer source is compiled into.</param>
    /// <returns>The diagnostics the analyzer reported for the consumer source.</returns>
    internal static Task<ImmutableArray<Diagnostic>> RunAgainstFakeEngineAsync<TAnalyzer>(
        string source, string assemblyName)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        return AnalyzerRunner.RunWithReferenceAsync<TAnalyzer>(
            source, assemblyName, EngineAssemblySource, EngineAssemblyName);
    }

    /// <summary>
    /// The text of <paramref name="diagnostic"/>'s message, read culture-invariantly. The one reader, so a test class
    /// asserting on message content does not carry its own copy of the call.
    /// </summary>
    /// <param name="diagnostic">The diagnostic whose message to read.</param>
    /// <returns>The formatted message text.</returns>
    internal static string Message(this Diagnostic diagnostic)
    {
        return diagnostic.GetMessage(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Asserts that <paramref name="diagnostic"/> is the rule identified by <paramref name="diagnosticId"/>, reported
    /// at its declared warning severity. The one owner of that pair of assertions, so a test class proves its rule
    /// fired by handing over its analyzer's own <c>DiagnosticId</c> constant instead of re-spelling the sequence and
    /// the identifier.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to check.</param>
    /// <param name="diagnosticId">The asking rule's <c>DiagnosticId</c> constant.</param>
    internal static void AssertReported(this Diagnostic diagnostic, string diagnosticId)
    {
        Assert.Equal(diagnosticId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    /// <summary>
    /// Asserts that <paramref name="diagnostics"/> is not empty and that EVERY diagnostic in it is the rule identified
    /// by <paramref name="diagnosticId"/>. This is the shape a rule whose two lenses can both see one prohibited reach
    /// is proved with: the contract sets no diagnostic count, so the assertion pins what was reported rather than how
    /// many.
    /// </summary>
    /// <param name="diagnostics">The diagnostics the run produced.</param>
    /// <param name="diagnosticId">The asking rule's <c>DiagnosticId</c> constant.</param>
    internal static void AssertAllReported(this ImmutableArray<Diagnostic> diagnostics, string diagnosticId)
    {
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => diagnostic.AssertReported(diagnosticId));
    }

    /// <summary>
    /// Asserts that <paramref name="source"/> really contains an XML documentation reference — a PARSED
    /// <see cref="CrefSyntax"/> node, the very construct the exclusion discriminates on — and not merely the text
    /// <c>cref</c>. Every behavioral documentation test asserts this before asserting the rule reported nothing, so
    /// none of them can pass by the fixture's documentation never being parsed into a cref at all: with no cref node
    /// the exclusion under test is never reached and an empty result would prove nothing about it. The source is
    /// parsed exactly as <see cref="AnalyzerRunner"/> parses it, so what is asserted here is what the rule saw.
    /// </summary>
    /// <param name="source">The fixture source the documentation test is about to run.</param>
    internal static void AssertHasDocumentationReference(string source)
    {
        Assert.Contains(
            CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes(descendIntoTrivia: true),
            node => node is CrefSyntax);
    }

    /// <summary>
    /// The one-door message accusation that the CALLED MEMBER <paramref name="member"/> is not the permitted door,
    /// spelled from <see cref="CalledMemberFragment"/> and <see cref="NotTheDoorFragment"/>. Built here so a test
    /// hands <see cref="AssertNamesExactly"/> the complete accusation instead of a bare name: every one-door message
    /// also carries the rule's FIXED text, which names the permitted doors, so a bare name is contained in the message
    /// whether or not anything was accused of anything.
    /// </summary>
    /// <param name="member">The member the message must accuse, as <c>TypeName.MemberName</c>.</param>
    /// <returns>The complete accusation text the message must carry.</returns>
    internal static string CalledMemberAccusation(string member)
    {
        return Accusation(CalledMemberFragment, member);
    }

    /// <summary>
    /// The same accusation for a reach that NAMES or CARRIES a guarded type rather than calling a member on it, spelled
    /// from <see cref="ReferencedTypeFragment"/> and <see cref="NotTheDoorFragment"/>.
    /// </summary>
    /// <param name="type">The type the message must accuse, as the analyzer spells it.</param>
    /// <returns>The complete accusation text the message must carry.</returns>
    internal static string ReferencedTypeAccusation(string type)
    {
        return Accusation(ReferencedTypeFragment, type);
    }

    /// <summary>
    /// Asserts that <paramref name="diagnostics"/> points at EXACTLY the source text in <paramref name="expectedSpans"/>
    /// — one entry per diagnostic, so a position repeated because more than one lens saw the same reach is written out
    /// twice and a missing, extra or displaced report fails. The two sequences are compared by
    /// <see cref="AssertSameStrings"/>, so the order the scanners happen to run in is not pinned but the count is:
    /// this assertion is what pins how many diagnostics a fixture produces, alongside the identifier and message
    /// checks in <see cref="AssertNamesExactly"/>.
    /// <para>
    /// Every expected span is spelled by the asking test from its own fixture constants, never read back off the
    /// diagnostic, so the assertion proves the reported location is the required one rather than that the rule agrees
    /// with itself. The text is read through <see cref="AnalyzerRunner.SpanText"/>, the one owner of that lookup.
    /// </para>
    /// </summary>
    /// <param name="diagnostics">The diagnostics the run produced.</param>
    /// <param name="source">The fixture source that was analyzed.</param>
    /// <param name="expectedSpans">The source text each expected diagnostic must point at.</param>
    internal static void AssertSpans(
        this ImmutableArray<Diagnostic> diagnostics, string source, params string[] expectedSpans)
    {
        AssertSameStrings(
            expectedSpans,
            diagnostics.Select(diagnostic => AnalyzerRunner.SpanText(source, diagnostic)));
    }

    /// <summary>
    /// Asserts that <paramref name="actual"/> holds exactly the strings in <paramref name="expected"/> — the same
    /// strings and the same number of each. Both sequences are sorted with <see cref="StringComparer.Ordinal"/> and
    /// then compared in order, so the two are compared as sorted multisets: the order the producing code happens to
    /// emit them in is not pinned, while a value that appears twice on one side must appear twice on the other. Set
    /// equality would drop that count and weaken every caller.
    /// <para>
    /// The one owner of that comparison. Each caller projects its own expected and actual strings and hands both
    /// sequences here — <see cref="AssertSpans"/> projects the actual side through
    /// <see cref="AnalyzerRunner.SpanText"/>, and the AG0041 tests project the expected side through their own
    /// message builder and the actual side through <see cref="Message"/>.
    /// </para>
    /// </summary>
    /// <param name="expected">The strings the run is required to produce.</param>
    /// <param name="actual">The strings it did produce.</param>
    internal static void AssertSameStrings(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        Assert.Equal(
            expected.OrderBy(text => text, StringComparer.Ordinal),
            actual.OrderBy(text => text, StringComparer.Ordinal));
    }

    /// <summary>
    /// Asserts that <paramref name="diagnostics"/> names EVERY condition that failed and ONLY those — the paired check
    /// the one-door message contract requires, which a bare presence assertion cannot make: a message that also
    /// accuses a condition which did NOT fail passes a presence check and is still wrong.
    /// <para>
    /// The three cases are the three a one-door rule can report. Wrong site, permitted door: pass
    /// <paramref name="failedDoorAccusation"/> as <see langword="null"/> and the expected site fragment, and no
    /// diagnostic may accuse the reach of not being the door. Right site, wrong door: pass the accusation and
    /// <paramref name="failedSiteFragment"/> as <see langword="null"/>, and no diagnostic may fault the site in either
    /// of the two wordings it can carry. Both wrong: pass both, and both must be named.
    /// </para>
    /// <para>
    /// The required half is checked across the whole run rather than per diagnostic, because the two lenses see one
    /// prohibited reach from different sides and can legitimately fail on different conditions — a call to a guarded
    /// member from the wrong site fails the door AND the site through the call lens, while the same guarded type named
    /// in that call fails only the door through the written-name lens. The FORBIDDEN half is checked against every
    /// diagnostic, so no lens may smuggle in an accusation that did not fail.
    /// </para>
    /// </summary>
    /// <param name="diagnostics">The diagnostics the run produced.</param>
    /// <param name="diagnosticId">The asking rule's <c>DiagnosticId</c> constant.</param>
    /// <param name="failedDoorAccusation">The accusation from <see cref="CalledMemberAccusation"/> or
    /// <see cref="ReferencedTypeAccusation"/> when the door test failed; <see langword="null"/> when it did not.</param>
    /// <param name="failedSiteFragment"><see cref="CallSiteFailureFragment"/> or
    /// <see cref="ReferenceSiteFailureFragment"/> when the site test failed; <see langword="null"/> when it did not.</param>
    internal static void AssertNamesExactly(
        this ImmutableArray<Diagnostic> diagnostics,
        string diagnosticId,
        string? failedDoorAccusation,
        string? failedSiteFragment)
    {
        diagnostics.AssertAllReported(diagnosticId);

        AssertNames(diagnostics, failedDoorAccusation, NotTheDoorFragment);
        AssertNames(diagnostics, failedSiteFragment, CallSiteFailureFragment, ReferenceSiteFailureFragment);
    }

    // One condition of the message contract, checked in both directions from one place. A required text must be named
    // by at least one diagnostic; when nothing is required, NO diagnostic may name the condition at all, in any of the
    // wordings it can be spelled with.
    private static void AssertNames(
        ImmutableArray<Diagnostic> diagnostics, string? required, params string[] conditionFragments)
    {
        if (required is not null)
        {
            Assert.Contains(diagnostics, diagnostic => diagnostic.Message().Contains(required, StringComparison.Ordinal));
            return;
        }

        Assert.All(
            conditionFragments,
            fragment => Assert.DoesNotContain(
                diagnostics, diagnostic => diagnostic.Message().Contains(fragment, StringComparison.Ordinal)));
    }

    // The subject of a one-door message: the kind of reach, the symbol it named in quotes, and the close that makes it
    // an accusation. The one owner of that spelling, so the two public builders differ only in the opening fragment.
    private static string Accusation(string subjectKind, string subject)
    {
        return subjectKind + " '" + subject + "' " + NotTheDoorFragment;
    }

    // The expression-bodied container factory, in whichever namespace the caller names. The correct-namespace and
    // wrong-namespace entry points above both come through here, so the two fixtures cannot drift apart in anything
    // but the namespace.
    private static string ContainerFactoryIn(string containerNamespace, string usings, string expression)
    {
        string body = "        internal static object Create() => " + expression + ";";
        return EngineCompilation(containerNamespace, usings, body, string.Empty);
    }

    // The block-bodied shape of a container member: the declaration, its braces, and the statements between them.
    // Spelled once here so the factory shell, the other-method shell, and the lambda and local-function shells built
    // on the factory do not each carry a copy of it.
    private static string BlockBodiedMember(string methodName, string statements)
    {
        return "        internal static object " + methodName + "()\n"
            + "        {\n"
            + statements + "\n"
            + "        }";
    }
}
