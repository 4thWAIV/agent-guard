// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Confines the PRESENCE family of raw native interop — a native P/Invoke call site and a <c>Marshal</c> use — to the
/// per-OS native-OPS owners: a class implementing <c>IObjCRuntime</c> in the macOS assembly, or one of the two Windows
/// native-ops interfaces (<c>IWindowsHelloNativeOps</c>/<c>ICredentialPromptNativeOps</c>) in the Windows assembly
/// (presence-native-owner-rule, Tim's personal yes, family-scoped; fence-relocation, decision-gated). The coverage
/// refactor RELOCATES this exemption OFF the flow port (<c>ILocalAuthentication</c>/<c>IWindowsUserPresence</c>) and ONTO
/// the thin native-ops layer below it, so raw presence interop left in — or added back into — a flow-port orchestrator is
/// a build error (RED-first: it flags the pre-refactor <c>LocalAuthentication</c>/<c>WindowsUserPresence</c> raw calls
/// before any code moves). This is its OWN rule, paired with a narrow family-scoped edit to AG0101: AG0101's P/Invoke and
/// <c>Marshal</c> branches ALSO exempt these same native-ops owners (sharing one native-use detector —
/// <see cref="NativeInteropUse"/> — and one owner check — <see cref="PresenceContracts.IsInNativeOpsOwner"/> — with this
/// rule), while AG0101's <c>File</c>/<c>Directory</c>-divergent-member ban stays filesystem-owner-only. The two are
/// FAMILY-SCOPED so neither over-grants: a native-ops owner is never exempt for AG0101's <c>File</c>/<c>Directory</c>-
/// divergent members (AG0101 still confines those to the filesystem owner), and the filesystem owner gets no PRESENCE
/// exemption — a presence-family native call made from the filesystem owner is reported here.
/// <para>
/// The category split (which this rule owns vs AG0101): the FILESYSTEM native family — a P/Invoke to a method declared
/// in the existing native-binding types (<c>PosixNativeMethods</c>/<c>WindowsNativeMethods</c>: <c>rename</c>,
/// <c>pathconf</c>, <c>CreateFileW</c>, <c>DeviceIoControl</c>, <c>GetFileInformationByHandleEx</c>) and the
/// <c>Marshal.GetLastPInvokeError</c> that the filesystem owner reads after such a call — belongs to AG0101 and is
/// SKIPPED here (so this rule never re-reports or false-positives the existing filesystem code). Everything else in the
/// macOS/Windows assemblies — the presence P/Invokes (<c>objc_msgSend</c>, <c>CredUIPromptForWindowsCredentials</c>,
/// <c>LogonUser</c>) and their <c>Marshal</c> uses — is the presence family, allowed only in the presence port. Issue
/// #39 folds AG0101 + this rule into one owner→primitive-family map and removes the by-type filesystem coupling.
/// Preventive: the only native calls today are the filesystem family (skipped), so this compiles clean.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PresenceNativeInteropOwnerAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0113";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Presence native interop must live only in the per-OS native-ops owner",
        messageFormat: "Presence native interop '{0}' is outside the per-OS native-ops owner; raw presence P/Invoke and Marshal are allowed only in the class implementing IObjCRuntime (macOS) or IWindowsHelloNativeOps/ICredentialPromptNativeOps (Windows), not in the flow-port orchestrator",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A presence-family native call — a P/Invoke that is not one of the filesystem-native bindings, or a Marshal use that is not inside the filesystem owner — is allowed only in the per-OS native-ops owner: a class implementing IObjCRuntime (macOS assembly) or IWindowsHelloNativeOps/ICredentialPromptNativeOps (Windows assembly). The coverage refactor relocated this exemption off the flow-port orchestrator (ILocalAuthentication/IWindowsUserPresence) and onto the thin native-ops layer below it, so a raw presence call in the orchestrator is a build error. This is a separate rule paired with a narrow family-scoped edit to AG0101, whose P/Invoke and Marshal branches also exempt these same native-ops owners (sharing one native-use detector and one owner check with this rule) while its File/Directory-divergent-member ban stays filesystem-owner-only; the two are family-scoped so neither over-grants. The filesystem-native family (PosixNativeMethods/WindowsNativeMethods calls and the filesystem owner's Marshal) is AG0101's and is skipped here.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedRules;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // Scoped to the two per-OS assemblies that host a native presence port (macOS, Windows) through the shared
        // gate-then-register helper. Linux is native-call-free (managed D-Bus, governed by AG0110), and native interop
        // lives only in the per-OS libraries (AG0008), so registering elsewhere is unnecessary and would risk
        // double-reporting AG0101's Marshal scope.
        PresenceContracts.RegisterInMacOsOrWindows(context, Register);
    }

    private static void Register(CompilationStartAnalysisContext context)
    {
        MemberUseScanner.Register(context, Inspect);
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (!IsPresenceFamilyNativeUse(context, member, type))
        {
            return;
        }

        // Legal only inside the per-OS native-ops owner: IObjCRuntime in the macOS assembly, or a Windows native-ops
        // interface in the Windows assembly. The one native-ops owner check, shared with AG0101's narrow family-scoped
        // edit; the coverage refactor relocated it here off the flow-port orchestrator (fence-relocation).
        if (PresenceContracts.IsInNativeOpsOwner(context))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
    }

    private static bool IsPresenceFamilyNativeUse(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        // A native P/Invoke call site (the shared recognizer) — unless the target is one of the filesystem-native
        // bindings (AG0101's family).
        if (NativeInteropUse.IsPInvokeCall(member))
        {
            return !WellKnownType.IsAnyOf(((IMethodSymbol)member).ContainingType, PresenceContracts.FilesystemNativeMethodTypes);
        }

        // A Marshal use (the shared recognizer) — unless it is inside the filesystem owner (the existing
        // Marshal.GetLastPInvokeError read after a filesystem syscall, AG0101's family).
        if (NativeInteropUse.IsMarshalUse(type))
        {
            return !OwnerClass.IsOwner(context, PresenceContracts.PlatformFileSystem, CrossPlatformBoundary.IsAnyPerOsImplementationLibrary);
        }

        return false;
    }
}
