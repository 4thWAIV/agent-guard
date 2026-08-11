// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw OS-divergent filesystem call — one whose behavior differs by operating system — made outside the
/// <c>AgentGuard.CrossPlatform.*</c> platform libraries. That covers the Unix-mode members
/// (<c>File.SetUnixFileMode</c>/<c>GetUnixFileMode</c>, <c>FileInfo</c>/<c>DirectoryInfo.UnixFileMode</c>), the
/// symlink members (<c>LinkTarget</c>, <c>CreateSymbolicLink</c>, <c>ResolveLinkTarget</c>), <c>Marshal</c>, and the
/// call site of a native P/Invoke method. Every other assembly reaches these through <c>IPlatformFileSystem</c>; the
/// OS-uniform filesystem members are AG0011's, not this rule's. Constructing a <c>FileInfo</c>/<c>DirectoryInfo</c> is
/// inert and belongs to neither rule — the OS-divergent member that is read is what this rule catches. This is the
/// member-level, OS-divergent counterpart of AG0011, and the <c>0101</c> series is where further OS-divergent rules
/// are added.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OsDivergentFilesystemOnlyInCrossPlatformAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0101";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "OS-divergent filesystem calls must live only in the AgentGuard.CrossPlatform libraries",
        messageFormat: "OS-divergent filesystem call '{0}' is outside the AgentGuard.CrossPlatform.* libraries; reach OS-divergent behavior through IPlatformFileSystem",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An OS-divergent filesystem call — a Unix-mode or symlink member, Marshal, or a native P/Invoke call site — is allowed only in the AgentGuard.CrossPlatform.* platform libraries. Every other assembly reaches OS-divergent behavior through IPlatformFileSystem; the OS-uniform filesystem members are governed by AG0011.");

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
        context.RegisterCompilationStartAction(
            context => MemberUseScanner.RegisterUnless(context, CrossPlatformBoundary.IsCrossPlatformLibrary, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (IsOsDivergent(member, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
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
