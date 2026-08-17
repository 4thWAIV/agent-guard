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
/// and the call site of a native P/Invoke method (including the native case-sensitivity query). The interface owner is
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
    // full name against the enclosing type's implemented interfaces, never by a class-name literal.
    private static readonly ImmutableArray<(string Namespace, string Name)> OwningInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IPlatformFileSystem"));

    // The assembly gate: the THREE per-OS implementation libraries (AgentGuard.CrossPlatform.MacOS/.Linux/.Windows),
    // where PosixFileSystem/WindowsFileSystem live — NOT the core AgentGuard.CrossPlatform contract assembly, which
    // holds PlatformFileSystemShared (ag0101-one-owner-per-os). Half of the conjunction OwnerClass.IsOwner applies —
    // implementing IPlatformFileSystem in any OTHER assembly, including the core assembly, does not exempt. This closes
    // the round-2 hole where an interface implementer self-granted in any assembly, and the latent hole where a class
    // implementing IPlatformFileSystem in the core assembly (for example PlatformFileSystemShared) self-granted. AG0008
    // is deliberately broader — its P/Invoke scope stays assembly-wide across all four platform libraries — while this
    // owner is narrow. Cached once so no delegate is allocated per analyzed operation.
    private static readonly Func<Compilation, bool> InPlatformLibrary =
        compilation => CrossPlatformBoundary.IsPerOsImplementationAssembly(compilation.AssemblyName);

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
        // The only place an OS-divergent call is allowed (one-owner-class-per-primitive, ag0101-one-owner-per-os): the
        // one per-OS class implementing IPlatformFileSystem (resolved structurally). There is no shared-helper
        // carve-out — PlatformFileSystemShared is NOT exempt — so a raw divergent call anywhere else, including a
        // sibling class in the same platform assembly, is RED.
        if (IsOwnerClass(context))
        {
            return;
        }

        if (IsOsDivergent(member, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsOwnerClass(OperationAnalysisContext context)
    {
        // The conjunction (owners-live-at-lowest-consumer): the operation is exempt only when it compiles into one of
        // the THREE per-OS implementation libraries (AgentGuard.CrossPlatform.MacOS/.Linux/.Windows) AND the enclosing
        // type implements IPlatformFileSystem (the per-OS PosixFileSystem/WindowsFileSystem). Requiring both halves
        // blocks the self-grant: a class implementing IPlatformFileSystem in any other assembly — including the core
        // AgentGuard.CrossPlatform contract assembly that holds PlatformFileSystemShared — is not exempt. There is no
        // shared-helper additional owner (ag0101-one-owner-per-os).
        return OwnerClass.IsOwner(context, OwningInterfaces, InPlatformLibrary);
    }

    private static bool IsOsDivergent(ISymbol member, INamedTypeSymbol type)
    {
        // A call to a native P/Invoke method — the raw syscall site (AG0008 catches only the declaration). This is
        // what confines the native case-sensitivity query (pathconf / GetFileInformationByHandleEx) to the per-OS
        // owner too.
        if (member is IMethodSymbol method && PInvoke.IsPInvoke(method))
        {
            return true;
        }

        // Marshal — any use.
        if (WellKnownType.Is(type, KnownNamespaces.SystemRuntimeInteropServices, "Marshal"))
        {
            return true;
        }

        // The OS-divergent STATIC symlink and Unix-mode members on File/Directory (CreateSymbolicLink,
        // ResolveLinkTarget, Get/SetUnixFileMode). The *Info construction and the *Info instance members (LinkTarget,
        // UnixFileMode) are no longer this rule's: they are owned wholesale by the wrapper interfaces under AG0011
        // (fileinfo-abstraction-stays-in-ag0011).
        return WellKnownType.IsAnyOf(type, FilesystemMembers.FileAndDirectory)
            && FilesystemMembers.IsOsDivergentMember(member);
    }
}
