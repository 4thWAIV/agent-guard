// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single home for the identities the OS-presence guardrails (AG0105–AG0116) match against — the presence
/// contract on <c>ISystemServices.Platform</c>, the three internal per-OS FLOW ports (the orchestrators), the per-OS
/// native-OPS owners one layer below them (the thin raw-call classes the coverage refactor introduces), the gate that
/// owns the one presence call, and the existing filesystem-native binding types the presence native-interop owner rule
/// must NOT re-govern. Each <c>(namespace, name)</c> pair is spelled exactly once here (LESSON 1, DRY) so a rename
/// touches one place and every presence rule resolves the same owner. The per-OS port namespaces are the RootNamespace
/// of each per-OS implementation assembly (<c>AgentGuard.CrossPlatform.MacOS</c>/<c>.Windows</c>/<c>.Linux</c>): the
/// presence ports are authored per OS and NOT link-shared (per-os-presence-impls-not-shared), unlike the POSIX
/// filesystem source that sits under <c>AgentGuard.CrossPlatform.Posix</c>. IMPLEMENT/ARCHITECTURE must declare each
/// port and native-ops owner under the namespace named here; a mismatch leaves the owner unresolved and the native call
/// RED (the forcing function). The <see cref="AllNativeOpsOwners"/> layer is where the coverage refactor relocates the
/// raw-native-interop exemption (AG0113 + AG0101's presence branch): raw presence P/Invoke and <c>Marshal</c> are legal
/// ONLY inside a native-ops owner, so a raw call left in (or added back into) a flow-port orchestrator is a build error.
/// </summary>
internal static class PresenceContracts
{
    /// <summary>The one production caller of <c>IPresenceCheck.Check</c> — the async approval gate
    /// (<c>AgentGuard.Setup.ApprovalGate</c>, internal to <c>AgentGuard.Engine</c>). AG0108 exempts only this type.</summary>
    internal const string ApprovalGateNamespace = "AgentGuard.Setup";

    /// <summary>The simple name of the approval gate type that owns the single production presence call.</summary>
    internal const string ApprovalGateName = "ApprovalGate";

    /// <summary>The one operation on <c>IPresenceCheck</c> — <c>Check</c>. Spelled once here so AG0108 (only the gate may
    /// invoke it) and AG0109 (no inline prompt literal at the call) resolve the same operation name.</summary>
    internal const string CheckMethodName = "Check";

    /// <summary>
    /// The presence contract interface (<c>IPresenceCheck</c>) on <c>ISystemServices.Platform.Presence</c> — the
    /// owner of the one <c>Check</c> operation. Matched by AG0107 (a presence impl is a class implementing it),
    /// AG0108 (only <c>ApprovalGate</c> may invoke <c>Check</c>), and AG0109 (no inline prompt literal at the call).
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> PresenceCheck =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IPresenceCheck"));

    /// <summary>The <c>PresenceRequest</c> record (Abstractions) whose prompt text a production call site must resolve
    /// from the owned per-verb resource, never an inline literal (AG0109).</summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> PresenceRequest =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "PresenceRequest"));

    /// <summary>The owned dialog-text resource (<c>PresenceDialogText</c>, in the gate's <c>AgentGuard.Setup</c>
    /// namespace) — the single owner of the reviewed, action-specific prompt strings, keyed by <c>SetupVerb</c>
    /// (os-dialog-text-owned-resource). AG0109 recognizes a read through its accessor as the ONE sanctioned prompt
    /// source; a bare literal or a const/static-readonly field at the call site is a dodge. Spelled once here so a
    /// rename of the resource touches one place. IMPLEMENT must declare <c>PresenceDialogText</c> under this namespace.</summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> PresenceDialogText =
        ImmutableArray.Create((ApprovalGateNamespace, "PresenceDialogText"));

    /// <summary>The macOS presence native port (<c>ILocalAuthentication</c>, <c>objc_msgSend</c> → LocalAuthentication),
    /// declared in the macOS per-OS implementation assembly.</summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> LocalAuthentication =
        ImmutableArray.Create((CrossPlatformBoundary.MacOsName, "ILocalAuthentication"));

    /// <summary>The Windows presence native port (<c>IWindowsUserPresence</c>, Hello via the WinRT
    /// <c>UserConsentVerifier</c> interop or the secure-desktop credential prompt), declared in the Windows per-OS
    /// implementation assembly.</summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> WindowsUserPresence =
        ImmutableArray.Create((CrossPlatformBoundary.WindowsName, "IWindowsUserPresence"));

    /// <summary>The Linux presence port (<c>IPolkitAuthority</c>, the sole <c>Tmds.DBus.Protocol</c> user), declared in
    /// the Linux per-OS implementation assembly. Native-call-free (managed D-Bus), so it is NOT a native-interop owner
    /// (AG0113), only a Tmds owner (AG0110) and a single-implementer port (AG0114).</summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> PolkitAuthority =
        ImmutableArray.Create((CrossPlatformBoundary.LinuxName, "IPolkitAuthority"));

    /// <summary>
    /// The two per-OS native-interop presence ports (macOS + Windows) — the only owners raw native P/Invoke and
    /// <c>Marshal</c> for presence are allowed behind (AG0113), and the only implementers that may not cache a native
    /// auth-context handle in a field (AG0112). Linux is excluded: its polkit path is managed D-Bus, native-call-free.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> NativePresencePorts =
        LocalAuthentication.AddRange(WindowsUserPresence);

    /// <summary>
    /// All three internal per-OS native ports (macOS <c>ILocalAuthentication</c>, Windows <c>IWindowsUserPresence</c>,
    /// Linux <c>IPolkitAuthority</c>) — the single-implementer candidate set for AG0114. These ports live outside the
    /// <c>ISystemServices</c> tree, so AG0025/AG0030 do not cover them; without AG0114 "exactly one class owns each
    /// native library" would be a convention, not a build invariant.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> AllNativePorts =
        NativePresencePorts.AddRange(PolkitAuthority);

    /// <summary>
    /// The macOS native-ops owner (<c>IObjCRuntime</c>) — the thin, stateless class the coverage refactor splits out of
    /// the <c>ILocalAuthentication</c> orchestrator to hold the raw Objective-C runtime calls (<c>objc_msgSend</c>,
    /// <c>objc_getClass</c>, <c>sel_registerName</c>, <c>dlsym</c>, the block build, and their <c>Marshal</c> uses). It
    /// sits one layer below the flow port in the macOS assembly. A DESIGN placeholder name ARCHITECTURE may rename here.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> ObjCRuntime =
        ImmutableArray.Create((CrossPlatformBoundary.MacOsName, "IObjCRuntime"));

    /// <summary>
    /// The Windows Hello native-ops owner (<c>IWindowsHelloNativeOps</c>) — the thin, stateless class holding the raw
    /// WinRT <c>UserConsentVerifier</c> interop (activation, <c>RequestVerificationForWindowAsync</c>, the completed
    /// handler, and its <c>Marshal</c> uses), split out of the <c>IWindowsUserPresence</c> orchestrator. A DESIGN
    /// placeholder name ARCHITECTURE may rename here (macos-one-windows-two-native-ops: Hello is its own subsystem).
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> WindowsHelloNativeOps =
        ImmutableArray.Create((CrossPlatformBoundary.WindowsName, "IWindowsHelloNativeOps"));

    /// <summary>
    /// The Windows credential-prompt native-ops owner (<c>ICredentialPromptNativeOps</c>) — the thin, stateless class
    /// holding the raw secure-desktop credential prompt (<c>CredUIPromptForWindowsCredentials</c>,
    /// <c>CredUnPackAuthenticationBuffer</c>, <c>LogonUser</c>, and its <c>Marshal</c> uses), split out of the
    /// <c>IWindowsUserPresence</c> orchestrator. Windows gets TWO native-ops owners because Hello and the credential
    /// prompt are independent native subsystems sharing no handle (macos-one-windows-two-native-ops). A DESIGN
    /// placeholder name ARCHITECTURE may rename here.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> CredentialPromptNativeOps =
        ImmutableArray.Create((CrossPlatformBoundary.WindowsName, "ICredentialPromptNativeOps"));

    /// <summary>
    /// The two Windows native-ops owners (Hello + credential prompt) — the native-ops layer inside the Windows assembly.
    /// A class implementing EITHER is a Windows native-ops owner; the relocated raw-interop exemption
    /// (<see cref="IsInNativeOpsOwner"/>) matches this set in the Windows assembly.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> WindowsNativeOps =
        WindowsHelloNativeOps.AddRange(CredentialPromptNativeOps);

    /// <summary>
    /// All the per-OS native-ops owners — macOS <c>IObjCRuntime</c> plus the two Windows owners (<see cref="ObjCRuntime"/>
    /// and <see cref="WindowsNativeOps"/>). This is the candidate set the coverage refactor's guardrails scope to: the
    /// no-control-flow (AG0106), no-async-orchestration (AG0115), and no-state (AG0116) rules constrain only the contents
    /// of these owners, and the single-implementer guard (AG0114) requires each of these interfaces to have exactly one
    /// implementer (the one thin native-ops class). Linux has no native-ops owner: its polkit path is managed D-Bus. The
    /// interfaces are internal to their per-OS assemblies, so in any one compilation only that assembly's owners resolve.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> AllNativeOpsOwners =
        ObjCRuntime.AddRange(WindowsNativeOps);

    /// <summary>
    /// The existing filesystem-native binding types whose P/Invoke methods belong to AG0101's FILESYSTEM family — the
    /// shared POSIX bindings (<c>PosixNativeMethods</c>, <c>libc rename</c>/<c>pathconf</c>) and the Windows bindings
    /// (<c>WindowsNativeMethods</c>, <c>CreateFileW</c>/<c>DeviceIoControl</c>/<c>GetFileInformationByHandleEx</c>). The
    /// presence native-interop owner rule (AG0113) SKIPS a P/Invoke call to a method declared in one of these so it
    /// never re-reports or false-positives the existing filesystem-native code AG0101 already confines to
    /// <c>IPlatformFileSystem</c>. This is the family split's filesystem side; issue #39 folds AG0101 + AG0113 into one
    /// owner→primitive-family map and removes this by-type coupling.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> FilesystemNativeMethodTypes =
        ImmutableArray.Create(
            (CrossPlatformBoundary.RootName + ".Posix", "PosixNativeMethods"),
            (CrossPlatformBoundary.WindowsName, "WindowsNativeMethods"));

    /// <summary>The OS-divergent filesystem owner (<c>IPlatformFileSystem</c>) — AG0101's family. AG0113 recognizes a
    /// <c>Marshal</c> use inside this owner as filesystem-family (the existing <c>Marshal.GetLastPInvokeError</c> after
    /// a filesystem syscall) and does not govern it.</summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> PlatformFileSystem =
        ContractInterfaces.PlatformFileSystem;

    /// <summary>The assembly gate for the macOS per-OS implementation library, where <c>ILocalAuthentication</c>'s
    /// owner lives. Cached so no delegate is allocated per analyzed operation.</summary>
    internal static readonly Func<Compilation, bool> InMacOs =
        OwnerClass.InAssembly(CrossPlatformBoundary.MacOsName);

    /// <summary>The assembly gate for the Windows per-OS implementation library, where <c>IWindowsUserPresence</c>'s
    /// owner lives.</summary>
    internal static readonly Func<Compilation, bool> InWindows =
        OwnerClass.InAssembly(CrossPlatformBoundary.WindowsName);

    /// <summary>The assembly gate for the Linux per-OS implementation library, where <c>IPolkitAuthority</c>'s owner
    /// (<c>TmdsPolkitAuthority</c>) lives.</summary>
    internal static readonly Func<Compilation, bool> InLinux =
        OwnerClass.InAssembly(CrossPlatformBoundary.LinuxName);

    /// <summary>
    /// The assembly gate spanning the two per-OS implementation libraries that can host a native-OPS owner — the macOS
    /// and Windows assemblies (macos-one-windows-two-native-ops; Linux is native-call-free managed D-Bus). The compound
    /// of <see cref="InMacOs"/> and <see cref="InWindows"/>, spelled once here (LESSON 1, DRY) so the native-ops rules
    /// (AG0106, AG0113, AG0115) each call this one gate instead of hand-copying
    /// <c>!InMacOs(compilation) &amp;&amp; !InWindows(compilation)</c>. Cached in a static field like its sibling gates,
    /// so no delegate is allocated per compilation start.
    /// </summary>
    internal static readonly Func<Compilation, bool> InMacOsOrWindows =
        compilation => InMacOs(compilation) || InWindows(compilation);

    /// <summary>
    /// Runs <paramref name="register"/> exactly once when the compilation being analyzed is one of the two per-OS
    /// implementation libraries that can host a native-OPS owner — the macOS or Windows assembly
    /// (<see cref="InMacOsOrWindows"/>); any other compilation registers nothing. The "gate to macOS-or-Windows, then
    /// register" block is owned once here (LESSON 1, DRY) rather than re-spelled in each native-ops rule's
    /// <c>OnCompilationStart</c> (AG0106 no-control-flow, AG0113 native-interop-owner, AG0115 no-async-orchestration),
    /// mirroring <see cref="MemberUseScanner.RegisterForAssembly"/> — the same gate-then-register extraction the two
    /// one-door rules share. Each caller passes only its own registration lambda; the OS gate lives here.
    /// </summary>
    /// <param name="context">The compilation-start context to gate and register on.</param>
    /// <param name="register">The rule's registration, invoked once when the compilation is the macOS or Windows library.</param>
    internal static void RegisterInMacOsOrWindows(
        CompilationStartAnalysisContext context,
        Action<CompilationStartAnalysisContext> register)
    {
        // The native-ops owners live only in the macOS and Windows per-OS assemblies (macos-one-windows-two-native-ops).
        // Registering elsewhere would never match, so it is skipped for every other compilation.
        if (!InMacOsOrWindows(context.Compilation))
        {
            return;
        }

        register(context);
    }

    /// <summary>
    /// Gets a value indicating whether the analyzed operation is inside a per-OS native-OPS OWNER — a class implementing
    /// <c>IObjCRuntime</c> compiled into the macOS assembly, or one of the two Windows native-ops interfaces
    /// (<c>IWindowsHelloNativeOps</c>/<c>ICredentialPromptNativeOps</c>) compiled into the Windows assembly. This is the
    /// relocated presence-native-interop exemption: the coverage refactor moves the raw-interop carve-out OFF the flow
    /// port and ONTO this native-ops layer, so this is the ONE owner check both AG0113 (which confines the presence
    /// native family here) and AG0101 (whose narrow family-scoped edit exempts these same owners on its P/Invoke and
    /// <c>Marshal</c> branches) call — the two rules resolve the identical owner and neither over-grants. A raw presence
    /// P/Invoke or <c>Marshal</c> left in a flow-port orchestrator (<c>LocalAuthentication</c>/<c>WindowsUserPresence</c>)
    /// is therefore RED until it moves into a native-ops owner (the forcing function; RED-first against the pre-refactor
    /// code). The macOS and Windows <see cref="OwnerClass.IsOwner"/> calls keep their own per-OS assembly gate rather than
    /// a combined set + combined gate, so a class implementing one OS's native-ops interface in the OTHER OS's assembly is
    /// not exempt.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <returns><see langword="true"/> when the enclosing class is a native-ops owner in its per-OS assembly.</returns>
    internal static bool IsInNativeOpsOwner(OperationAnalysisContext context) =>
        OwnerClass.IsOwner(context, ObjCRuntime, InMacOs)
        || OwnerClass.IsOwner(context, WindowsNativeOps, InWindows);

    /// <summary>
    /// Gets a value indicating whether the analyzed operation is inside one of the two NATIVE FLOW ports — a class
    /// implementing <c>ILocalAuthentication</c> compiled into the macOS assembly, or <c>IWindowsUserPresence</c>
    /// compiled into the Windows assembly. These are the orchestrators one level above the native-ops layer; the raw
    /// native interop no longer lives here (that exemption moved to <see cref="IsInNativeOpsOwner"/>), but the timeout
    /// ban (AG0107, through <see cref="IsInAnyPresencePortOwner"/>) still covers them so a flow port cannot wrap its own
    /// timeout around the native call. The two per-OS <see cref="OwnerClass.IsOwner"/> calls are kept separate (each with
    /// its own per-OS assembly gate) so a class implementing one port's interface in the OTHER OS's assembly is not
    /// matched.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <returns><see langword="true"/> when the enclosing class is a native flow-port owner in its per-OS assembly.</returns>
    internal static bool IsInNativePresencePortOwner(OperationAnalysisContext context) =>
        OwnerClass.IsOwner(context, LocalAuthentication, InMacOs)
        || OwnerClass.IsOwner(context, WindowsUserPresence, InWindows);

    /// <summary>
    /// Gets a value indicating whether the analyzed operation is inside ANY of the three per-OS FLOW-port owners —
    /// the two native flow ports (<see cref="IsInNativePresencePortOwner"/>: <c>ILocalAuthentication</c> in the macOS
    /// assembly, <c>IWindowsUserPresence</c> in the Windows assembly) PLUS the Linux <c>IPolkitAuthority</c> owner
    /// (<c>TmdsPolkitAuthority</c> in the Linux assembly). This spans <see cref="AllNativePorts"/> — the flow-port layer
    /// one level below <c>IPresenceCheck</c> — for the timeout ban (AG0107), which must catch a port implementer that
    /// wraps its own timeout around the native or D-Bus call, not only the <c>IPresenceCheck</c> impl above it. AG0107
    /// additionally covers the native-ops layer below these ports through <see cref="IsInNativeOpsOwner"/>. Like
    /// <see cref="IsInNativePresencePortOwner"/>, the three <see cref="OwnerClass.IsOwner"/> calls keep their own per-OS
    /// assembly gate rather than a combined set + combined gate, so a class implementing one port's interface in another
    /// OS's assembly is not matched.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <returns><see langword="true"/> when the enclosing class is a per-OS flow-port owner in its per-OS assembly.</returns>
    internal static bool IsInAnyPresencePortOwner(OperationAnalysisContext context) =>
        IsInNativePresencePortOwner(context)
        || OwnerClass.IsOwner(context, PolkitAuthority, InLinux);

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/> is the <c>Check</c> operation on <c>IPresenceCheck</c>
    /// — matched by the method name and the interface identity, whether the call is reached through the interface itself
    /// OR through a CONCRETE-typed reference to an implementer (<c>LinuxPresenceCheck</c>/<c>MacOsPresenceCheck</c>/
    /// <c>WindowsPresenceCheck</c>/<c>FakePresenceCheck</c>), so a same-named <c>Check</c> on an unrelated type is not
    /// caught but a call through a concrete reference is not a bypass. This is the single "is this the presence check
    /// call" recognition both AG0108 (which then requires the caller to be the approval gate) and AG0109 (which then bans
    /// an inline prompt literal at the call) key off, so the two rules resolve the identical operation. It routes through
    /// the centralized <see cref="OwnerClass.IsInterfaceMemberInvocation"/> — which resolves interface membership through
    /// <see cref="OwnerClass.Implements"/>/<see cref="WellKnownType"/> — rather than re-deriving an interface-membership
    /// check here. Whether the call is legal is the caller's policy, applied on top.
    /// </summary>
    /// <param name="member">The invoked member.</param>
    /// <param name="type">The type that declares the invoked member (the interface, or the concrete implementer).</param>
    /// <returns><see langword="true"/> when the invocation targets <c>IPresenceCheck.Check</c> through the interface or an implementer.</returns>
    internal static bool IsPresenceCheckInvocation(ISymbol member, INamedTypeSymbol type) =>
        OwnerClass.IsInterfaceMemberInvocation(member, type, CheckMethodName, PresenceCheck);
}
