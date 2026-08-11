// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw OS-divergent filesystem call — one whose behavior differs by operating system — made anywhere but
/// the classes that own OS-divergent behavior: a class implementing <c>AgentGuard.Abstractions.IPlatformFileSystem</c>
/// (the per-OS <c>PosixFileSystem</c>/<c>WindowsFileSystem</c> implementations), plus the one shared helper class
/// <c>AgentGuard.CrossPlatform.PlatformFileSystemShared</c> those implementations delegate to. Both owners are exempt
/// only when compiled into one of the four <c>AgentGuard.CrossPlatform.*</c> platform libraries: a class implementing
/// <c>IPlatformFileSystem</c> in any other assembly, and a same-named helper in any other assembly, both stay RED
/// (the self-grant is blocked). That covers the
/// Unix-mode members (<c>File.SetUnixFileMode</c>/<c>GetUnixFileMode</c>,
/// <c>FileInfo</c>/<c>DirectoryInfo.UnixFileMode</c>), the symlink members (<c>LinkTarget</c>,
/// <c>CreateSymbolicLink</c>, <c>ResolveLinkTarget</c>), <c>Marshal</c>, and the call site of a native P/Invoke
/// method. The interface owner is resolved structurally (the enclosing type's implemented interfaces) and the helper
/// by exact full name, each paired with the platform-library assembly gate; the raw call is a build error even in
/// another class of the same platform library. Every other type reaches
/// OS-divergent behavior through <c>IPlatformFileSystem</c>; the OS-uniform filesystem members are AG0011's, not this
/// rule's. Constructing a <c>FileInfo</c>/<c>DirectoryInfo</c> is inert and belongs to neither rule — the OS-divergent
/// member that is read is what this rule catches. This is the member-level, OS-divergent counterpart of AG0011, and
/// the <c>0101</c> series is where further OS-divergent rules are added.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OsDivergentFilesystemOnlyInCrossPlatformAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0101";

    private const string Category = "AgentGuard.Architecture";
    private const string SharedHelperTypeName = "PlatformFileSystemShared";

    // The owning interface whose implementing classes are the only place a raw OS-divergent filesystem call is
    // allowed (one-owner-class-per-primitive). Matched structurally by full name against the enclosing type's
    // implemented interfaces, never by a class-name literal.
    private static readonly ImmutableArray<(string Namespace, string Name)> OwningInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractions, "IPlatformFileSystem"));

    // The assembly gate: the four AgentGuard.CrossPlatform.* platform libraries (the same set AG0008 uses). Half of
    // the conjunction OwnerClass.IsOwner applies — implementing IPlatformFileSystem, or being the shared helper, in
    // any OTHER assembly does not exempt. This closes the round-2 hole where an interface implementer self-granted in
    // any assembly. Cached once so no delegate is allocated per analyzed operation.
    private static readonly Func<Compilation, bool> InPlatformLibrary = CrossPlatformBoundary.IsCrossPlatformLibrary;

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "OS-divergent filesystem calls must live only in the classes implementing IPlatformFileSystem",
        messageFormat: "OS-divergent filesystem call '{0}' is outside a class implementing IPlatformFileSystem (or the PlatformFileSystemShared helper it delegates to); reach OS-divergent behavior through IPlatformFileSystem",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An OS-divergent filesystem call — a Unix-mode or symlink member, Marshal, or a native P/Invoke call site — is allowed only in a class that implements AgentGuard.Abstractions.IPlatformFileSystem, plus the one shared helper AgentGuard.CrossPlatform.PlatformFileSystemShared it delegates to — not merely somewhere in a platform assembly. Every other type reaches OS-divergent behavior through IPlatformFileSystem; the OS-uniform filesystem members are governed by AG0011.");

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
        // The only places an OS-divergent call is allowed (one-owner-class-per-primitive): a class implementing
        // IPlatformFileSystem (resolved structurally), plus the one shared helper PlatformFileSystemShared those
        // implementations delegate to. Everywhere else, including a sibling class in the same assembly, is RED.
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
        // the AgentGuard.CrossPlatform.* platform libraries AND the enclosing type is an owner — it implements
        // IPlatformFileSystem (the per-OS PosixFileSystem/WindowsFileSystem), or it is the shared helper
        // PlatformFileSystemShared those implementations delegate to. Requiring the assembly half for BOTH branches
        // blocks the self-grant: a class implementing IPlatformFileSystem, or a same-named helper, in any other
        // assembly is not exempt.
        return OwnerClass.IsOwner(context, OwningInterfaces, InPlatformLibrary, IsSharedHelper);
    }

    // The one non-interface owner: the shared helper PlatformFileSystemShared the per-OS implementations delegate to,
    // matched by exact full name. OwnerClass.IsOwner pairs it with the assembly gate above, so a same-named helper in
    // another assembly cannot self-grant. A static method (not a field), because RS1008 forbids storing a symbol-typed
    // delegate in an analyzer field; the compiler caches the method-group conversion so it still allocates nothing.
    private static bool IsSharedHelper(INamedTypeSymbol? enclosingType)
    {
        return WellKnownType.Is(enclosingType, CrossPlatformBoundary.RootName, SharedHelperTypeName);
    }

    private static bool IsOsDivergent(ISymbol member, INamedTypeSymbol type)
    {
        // A call to a native P/Invoke method — the raw syscall site (AG0008 catches only the declaration).
        if (member is IMethodSymbol method && PInvoke.IsPInvoke(method))
        {
            return true;
        }

        // Marshal — any use.
        if (WellKnownType.Is(type, KnownNamespaces.SystemRuntimeInteropServices, "Marshal"))
        {
            return true;
        }

        // The Unix-mode and symlink members on the File/Directory/*Info family. Constructing an *Info is inert and
        // belongs to neither rule; the OS-divergent access is the member that is read, caught here.
        return WellKnownType.IsAnyOf(type, FilesystemMembers.Family) && FilesystemMembers.IsOsDivergentMember(member);
    }
}
