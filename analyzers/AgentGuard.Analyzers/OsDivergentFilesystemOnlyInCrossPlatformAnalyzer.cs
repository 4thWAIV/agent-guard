// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw OS-divergent filesystem call — one whose behavior differs by operating system — made anywhere but
/// the ONE per-OS class that owns OS-divergent behavior: a class implementing
/// <c>AgentGuard.Abstractions.Contracts.IPlatformFileSystem</c> (the per-OS <c>PosixFileSystem</c>/<c>WindowsFileSystem</c>
/// implementations). There is no shared-helper carve-out (ag0101-one-owner-per-os): the shared helper
/// <c>AgentGuard.CrossPlatform.PlatformFileSystemShared</c> is NOT exempt and must make zero raw divergent calls — its
/// divergent logic moves into the per-OS class. The owner is exempt only when compiled into one of the THREE per-OS
/// implementation libraries (<c>AgentGuard.CrossPlatform.MacOS</c>/<c>.Linux</c>/<c>.Windows</c>), where
/// <c>PosixFileSystem</c>/<c>WindowsFileSystem</c> live — NOT the core <c>AgentGuard.CrossPlatform</c> contract
/// assembly, which holds <c>PlatformFileSystemShared</c> (ag0101-one-owner-per-os): a class implementing
/// <c>IPlatformFileSystem</c> in any other assembly, including the core assembly, stays RED (the self-grant is
/// blocked). That covers the
/// STATIC Unix-mode members (<c>File.SetUnixFileMode</c>/<c>GetUnixFileMode</c>), the STATIC symlink members
/// (<c>File</c>/<c>Directory.CreateSymbolicLink</c>, <c>File</c>/<c>Directory.ResolveLinkTarget</c>), <c>Marshal</c>,
/// and the call site of a native P/Invoke method (including the native case-sensitivity query). The NATIVE-interop half
/// — <c>Marshal</c> and a P/Invoke call site — is recognized through the one <see cref="NativeInteropUse"/> detector this
/// rule SHARES with AG0113, and on those two branches only it ALSO exempts the per-OS native-ops owners
/// (<c>IObjCRuntime</c> in the macOS assembly / <c>IWindowsHelloNativeOps</c>/<c>ICredentialPromptNativeOps</c> in the
/// Windows assembly), reusing the one <see cref="PresenceContracts.IsInNativeOpsOwner"/> check the paired AG0113 uses
/// (presence-native-owner-rule, family-scoped, Tim's personal yes; fence-relocation moved this carve-out off the flow
/// port and onto the native-ops layer). The <c>File</c>/<c>Directory</c>-divergent-member ban
/// stays FILESYSTEM-owner-only — no presence exemption — so a native-ops owner's <c>File.SetUnixFileMode</c> /
/// <c>CreateSymbolicLink</c> is still RED (it is not the filesystem owner), and AG0101's shared File/Directory owner
/// identity (<c>ContractInterfaces.PlatformFileSystem</c>, used by AG0020) is untouched. The interface owner is
/// resolved structurally (the enclosing type's implemented interfaces), paired with the platform-library assembly
/// gate; the raw call is a build error even in another class of the same platform library (including
/// <c>PlatformFileSystemShared</c>). Every other type reaches OS-divergent behavior through <c>IPlatformFileSystem</c>;
/// the OS-uniform filesystem members are AG0011's, not this rule's. The <c>FileInfo</c>/<c>DirectoryInfo</c>/
/// <c>FileSystemInfo</c> construction and their instance members (<c>LinkTarget</c>, <c>UnixFileMode</c>) are NO
/// LONGER this rule's: the <c>*Info</c> types are owned wholesale by the wrapper interfaces under AG0011
/// (fileinfo-abstraction-stays-in-ag0011). This is the member-level, OS-divergent counterpart of AG0011, and the
/// <c>0101</c> series is where further OS-divergent rules are added.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OsDivergentFilesystemOnlyInCrossPlatformAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0101";

    private const string Category = "AgentGuard.Architecture";

    // The owning interface whose implementing class is the only place a raw OS-divergent filesystem call is allowed
    // (one-owner-class-per-primitive, ag0101-one-owner-per-os — no shared-helper carve-out). Matched structurally by
    // full name against the enclosing type's implemented interfaces, never by a class-name literal. The identity is
    // spelled once in ContractInterfaces because AG0020 shares it for the Path.DirectorySeparatorChar owner exemption.
    private static readonly ImmutableArray<(string Namespace, string Name)> OwningInterfaces =
        ContractInterfaces.PlatformFileSystem;

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "OS-divergent filesystem calls must live only in the per-OS class implementing IPlatformFileSystem",
        messageFormat: "OS-divergent filesystem call '{0}' is outside the one per-OS class implementing IPlatformFileSystem; reach OS-divergent behavior through IPlatformFileSystem",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An OS-divergent filesystem call — a Unix-mode or symlink member, Marshal, or a native P/Invoke call site — is allowed only in the one per-OS class that implements AgentGuard.Abstractions.Contracts.IPlatformFileSystem, compiled into one of the three per-OS implementation libraries (AgentGuard.CrossPlatform.MacOS/.Linux/.Windows) — not the core AgentGuard.CrossPlatform contract assembly, not merely somewhere in a platform assembly, and NOT in the shared helper PlatformFileSystemShared (ag0101-one-owner-per-os), which must make zero raw divergent calls. Every other type reaches OS-divergent behavior through IPlatformFileSystem; the OS-uniform filesystem members are governed by AG0011.");

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
        context.RegisterCompilationStartAction(context => MemberUseScanner.Register(context, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        // The one per-OS class implementing IPlatformFileSystem (resolved structurally) is exempt for EVERY
        // OS-divergent call it makes — the native-interop half AND the File/Directory-divergent members
        // (one-owner-class-per-primitive, ag0101-one-owner-per-os). There is no shared-helper carve-out —
        // PlatformFileSystemShared is NOT exempt — so a raw divergent call anywhere else, including a sibling class in
        // the same platform assembly, is RED.
        if (IsFilesystemOwnerClass(context))
        {
            return;
        }

        // The NATIVE-interop family — a P/Invoke call site or a Marshal use — recognized through the one
        // NativeInteropUse detector shared with AG0113. It is the raw syscall site (AG0008 catches only the
        // declaration; this confines the native case-sensitivity query too) plus any Marshal use. Banned outside the
        // filesystem owner, EXCEPT the per-OS native-ops owners, which own their presence native interop
        // (presence-native-owner-rule, family-scoped — the paired AG0113 confines it to those same owners; the coverage
        // refactor relocated this carve-out off the flow port and onto the native-ops layer). This presence exemption is
        // a separate check on the native branches only; it does not touch AG0101's File/Directory owner identity
        // (ContractInterfaces.PlatformFileSystem, shared with AG0020).
        if (NativeInteropUse.IsNativeUse(member, type))
        {
            if (PresenceContracts.IsInNativeOpsOwner(context))
            {
                return;
            }

            Report(context, member, type);
            return;
        }

        // The OS-divergent STATIC symlink and Unix-mode members on File/Directory (CreateSymbolicLink,
        // ResolveLinkTarget, Get/SetUnixFileMode). This ban stays FILESYSTEM-owner-only: a presence port is NOT exempt
        // here, so a presence port's File.SetUnixFileMode / CreateSymbolicLink is still RED (it is not the filesystem
        // owner). The *Info construction and the *Info instance members (LinkTarget, UnixFileMode) are no longer this
        // rule's: they are owned wholesale by the wrapper interfaces under AG0011 (fileinfo-abstraction-stays-in-ag0011).
        if (WellKnownType.IsAnyOf(type, FilesystemMembers.FileAndDirectory)
            && FilesystemMembers.IsOsDivergentMember(member))
        {
            Report(context, member, type);
        }
    }

    private static bool IsFilesystemOwnerClass(OperationAnalysisContext context)
    {
        // The conjunction (owners-live-at-lowest-consumer): the operation is exempt only when it compiles into one of
        // the THREE per-OS implementation libraries (AgentGuard.CrossPlatform.MacOS/.Linux/.Windows) AND the enclosing
        // type implements IPlatformFileSystem (the per-OS PosixFileSystem/WindowsFileSystem). Requiring both halves
        // blocks the self-grant: a class implementing IPlatformFileSystem in any other assembly — including the core
        // AgentGuard.CrossPlatform contract assembly that holds PlatformFileSystemShared — is not exempt. There is no
        // shared-helper additional owner (ag0101-one-owner-per-os).
        return OwnerClass.IsOwner(context, OwningInterfaces, CrossPlatformBoundary.IsAnyPerOsImplementationLibrary);
    }

    private static void Report(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
    }
}
