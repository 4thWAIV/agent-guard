// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw OS-uniform filesystem call — a use of <c>File</c>, <c>Directory</c>, <c>FileInfo</c>,
/// <c>DirectoryInfo</c>, <c>FileSystemInfo</c>, <c>DriveInfo</c>, <c>FileStream</c>, <c>StreamReader</c>,
/// <c>StreamWriter</c>, <c>FileSystemWatcher</c>, or a <c>System.IO.Enumeration</c> type — made outside the
/// <c>AgentGuard.Boundaries</c> adapter assembly. Every other assembly reaches the filesystem through the owned
/// interfaces (<c>IFileReader</c>, <c>IDirectoryEnumerator</c>, <c>IFileWriter</c>, <c>IDirectoryWriter</c>) pulled
/// off <c>ISystemServices</c>, so the whole engine stays mockable and nothing touches the disk unwatched. The
/// OS-divergent members (symlink and Unix-mode) route to AG0101 and the current-directory members route to AG0012;
/// those are carved out here so exactly one rule owns each. Constructing a <c>FileInfo</c>/<c>DirectoryInfo</c> is
/// inert (no OS access until a member is touched), so the bare <c>new</c> is not flagged — the member read is.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FilesystemOnlyInBoundariesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0011";

    private const string Category = "AgentGuard.Architecture";

    // The dual-nature File/Directory/*Info family (shared with AG0101) plus the stream and drive types that carry
    // only OS-uniform behavior. Derived from the one Family list so the shared members are not spelled twice.
    private static readonly ImmutableArray<(string Namespace, string Name)> FilesystemTypes = FilesystemMembers.Family.AddRange(
        (KnownNamespaces.SystemIO, "DriveInfo"),
        (KnownNamespaces.SystemIO, "FileStream"),
        (KnownNamespaces.SystemIO, "StreamReader"),
        (KnownNamespaces.SystemIO, "StreamWriter"),
        (KnownNamespaces.SystemIO, "FileSystemWatcher"));

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Raw filesystem calls must live only in AgentGuard.Boundaries",
        messageFormat: "Raw filesystem call '{0}' is outside AgentGuard.Boundaries; reach the filesystem through IFileReader/IDirectoryEnumerator/IFileWriter/IDirectoryWriter pulled off ISystemServices",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A use of File, Directory, FileInfo, DirectoryInfo, FileSystemInfo, DriveInfo, FileStream, StreamReader, StreamWriter, FileSystemWatcher, or a System.IO.Enumeration type is allowed only in the AgentGuard.Boundaries adapter assembly. Every other assembly reaches the filesystem through the owned interfaces on ISystemServices so the engine stays mockable and no code touches the disk unwatched.");

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
            context => MemberUseScanner.RegisterUnless(context, BoundaryAssembly.IsBoundariesLibrary, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        // The OS-divergent members are AG0101's and the current-directory members are AG0012's; carve them out so
        // exactly one rule owns each site. The bare *Info construction is inert and flagged by neither rule.
        if (FilesystemMembers.IsOsDivergentMember(member)
            || FilesystemMembers.IsCurrentDirectoryMember(member)
            || FilesystemMembers.IsInertInfoConstruction(member, type))
        {
            return;
        }

        if (WellKnownType.IsAnyOf(type, FilesystemTypes) || WellKnownType.IsInNamespace(type, KnownNamespaces.SystemIOEnumeration))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }
}
