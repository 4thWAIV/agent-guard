// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw OS-uniform filesystem call — a use of <c>File</c>, <c>Directory</c>, <c>FileInfo</c>,
/// <c>DirectoryInfo</c>, <c>FileSystemInfo</c>, <c>DriveInfo</c>, <c>FileStream</c>, <c>StreamReader</c>,
/// <c>StreamWriter</c>, <c>FileSystemWatcher</c>, or a <c>System.IO.Enumeration</c> type — made anywhere but the
/// single owner class that implements the filesystem interface that specific member belongs to. Each banned member
/// maps to exactly ONE of <c>AgentGuard.Abstractions.IFileReader</c>, <c>IDirectoryEnumerator</c>,
/// <c>IFileWriter</c>, or <c>IDirectoryWriter</c> (<see cref="FilesystemMembers.OwningInterfaceFor"/>), and only the
/// class implementing THAT one interface — AND compiled into <c>AgentGuard.CrossPlatform</c>, where the file-op
/// adapters live because the platform code consumes them (owners-live-at-lowest-consumer) — is exempt for it. A class
/// implementing a different one of the four is still RED (a class implementing only <c>IFileWriter</c> is RED on
/// <c>File.Exists</c>, which is <c>IFileReader</c>'s member), a class implementing the right interface in the wrong
/// assembly is still RED (the self-grant is blocked), and the raw call is a build error even in another class of the
/// owner assembly. Every other type reaches the filesystem through those owned interfaces pulled off <c>ISystemServices</c>, so the whole
/// engine stays mockable and nothing touches the disk unwatched. The OS-divergent members (symlink and Unix-mode)
/// route to AG0101 and the current-directory members route to AG0012; those are carved out here so exactly one rule
/// owns each. Constructing a <c>FileInfo</c>/<c>DirectoryInfo</c> is inert (no OS access until a member is touched),
/// so the bare <c>new</c> is not flagged — the member read is.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FilesystemOnlyInBoundariesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0011";

    private const string Category = "AgentGuard.Architecture";

    // The dual-nature File/Directory/*Info family (shared with AG0101) plus the stream, watcher, and drive types
    // that carry only OS-uniform behavior. Derived from the one Family list and the one StreamWatcherAndDriveTypes
    // list so the members are not spelled twice; the same drive/stream list drives the member→owner partition.
    private static readonly ImmutableArray<(string Namespace, string Name)> FilesystemTypes =
        FilesystemMembers.Family.AddRange(FilesystemMembers.StreamWatcherAndDriveTypes);

    // The owner assembly for the file-op adapters (FileReader/SystemDirectoryEnumerator/FileWriter/DirectoryWriter):
    // AgentGuard.CrossPlatform, because the platform code consumes them (owners-live-at-lowest-consumer). Half of the
    // conjunction the OwnerClass helper applies — implementing the owning interface in any OTHER assembly does not
    // exempt. Reuses the shared RootName constant so the assembly name is not re-spelled. Cached once as a field so
    // no delegate is allocated per analyzed operation.
    private static readonly Func<Compilation, bool> InOwnerAssembly =
        OwnerClass.InAssembly(CrossPlatformBoundary.RootName);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Raw filesystem calls must live only in the class implementing the owning filesystem interface",
        messageFormat: "Raw filesystem call '{0}' is outside the single owner class implementing IFileReader/IDirectoryEnumerator/IFileWriter/IDirectoryWriter; reach the filesystem through that interface pulled off ISystemServices",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A use of File, Directory, FileInfo, DirectoryInfo, FileSystemInfo, DriveInfo, FileStream, StreamReader, StreamWriter, FileSystemWatcher, or a System.IO.Enumeration type is allowed only in the single class that implements AgentGuard.Abstractions.IFileReader, IDirectoryEnumerator, IFileWriter, or IDirectoryWriter — not merely somewhere in its assembly. Every other type reaches the filesystem through the owned interfaces on ISystemServices so the engine stays mockable and no code touches the disk unwatched.");

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
        // The OS-divergent members are AG0101's and the current-directory members are AG0012's; carve them out so
        // exactly one rule owns each site. The bare *Info construction is inert and flagged by neither rule.
        if (FilesystemMembers.IsOsDivergentMember(member)
            || FilesystemMembers.IsCurrentDirectoryMember(member)
            || FilesystemMembers.IsInertInfoConstruction(member, type))
        {
            return;
        }

        if (!WellKnownType.IsAnyOf(type, FilesystemTypes)
            && !WellKnownType.IsInNamespace(type, KnownNamespaces.SystemIOEnumeration))
        {
            return;
        }

        // one-owner-class-per-primitive + owners-live-at-lowest-consumer: resolve the ONE interface that owns THIS
        // member and exempt only the class implementing that interface AND compiled into AgentGuard.CrossPlatform (the
        // conjunction OwnerClass.IsOwner applies) — not any class implementing one of the four, and not that class in
        // another assembly. A member with no single owner (e.g. the stream/drive types, zero usage in the tree)
        // resolves to an empty set, so it is never exempt and stays RED everywhere. Requiring the assembly half closes
        // the self-grant hole: declaring ': IFileReader' in another assembly no longer launders a raw call.
        if (OwnerClass.IsOwner(context, FilesystemMembers.OwningInterfaceFor(member, type), InOwnerAssembly))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
    }
}
