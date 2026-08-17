// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw OS/CLR primitive used anywhere but its single owner class. This is the one table-driven owner rule
/// (AG0011) the bridge's rule-phase consolidates the per-primitive boundary analyzers into: filesystem
/// (<c>File</c>/<c>Directory</c> split per member across <c>IFileReader</c>/<c>IDirectoryEnumerator</c>/
/// <c>IFileWriter</c>/<c>IDirectoryWriter</c>, and the <c>FileInfo</c>/<c>DirectoryInfo</c>/<c>FileSystemInfo</c>
/// wrappers owned wholesale by <c>IFileInfo</c>/<c>IDirectoryInfo</c>/<c>IFileSystemInfo</c>), environment
/// (<c>IEnvironment</c>), randomness (<c>IGuidFactory</c>), console (<c>IConsole</c>), crypto
/// (<c>ISignatureService</c>), version reflection (<c>IBuildInfo</c>), and the ownerless primitives banned everywhere
/// (<c>Process</c>, the stream/drive/watcher types). Every mapping lives once in <see cref="OwnedPrimitives"/>; a raw
/// call is legal only in the class that implements the owning interface AND compiles into the owner assembly — the
/// conjunction <see cref="OwnerClass.IsOwner"/> applies — so a class cannot self-grant by declaring the interface in
/// the wrong assembly, and a primitive with no owner stays a build error everywhere. The OS-divergent static members
/// are AG0101's, the clock members are AG0015's, <c>Path</c> is AG0020's, and OS branching is AG0009's; those are
/// carved out in <see cref="OwnedPrimitives.Resolve"/> so exactly one rule owns each site.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RawPrimitiveOnlyInOwnerAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0011";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A raw OS/CLR primitive must live only in its single owner class",
        messageFormat: "Raw primitive '{0}' is outside its single owner ({1}); reach it through the owned interface pulled off ISystemServices",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A raw OS/CLR primitive — a System.IO filesystem type, System.Environment, a version-reflection read, System.Random/Guid.NewGuid/RandomNumberGenerator, System.Console, or a BouncyCastle Ed25519 type — is allowed only in the single class that implements its owning interface (IFileReader/IDirectoryEnumerator/IFileWriter/IDirectoryWriter, IFileInfo/IDirectoryInfo/IFileSystemInfo, IEnvironment, IGuidFactory, IConsole, ISignatureService, IBuildInfo), compiled into the assembly where that owner lives — not merely somewhere in that assembly. Every other type reaches the primitive through the owned interface on ISystemServices so the engine stays mockable. A primitive with no owner (Process, the stream/drive/watcher types) is a build error everywhere; the OS-divergent static members are governed by AG0101.");

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
        // Resolve the one owner of this primitive (or null when it is not this rule's — a non-governed member or one a
        // different standalone rule owns). Then apply the single-owner conjunction: exempt only inside the class that
        // implements the owning interface AND compiles into the owner assembly. An empty owner set (banned everywhere)
        // makes OwnerClass.IsOwner false, so it is always reported.
        OwnedPrimitive? owned = OwnedPrimitives.Resolve(member, type);
        if (owned is not { } primitive)
        {
            return;
        }

        if (OwnerClass.IsOwner(context, primitive.Owners, primitive.Gate))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            context.Operation.Syntax.GetLocation(),
            MemberUseScanner.Describe(member, type),
            OwnedPrimitives.OwnerLabel(primitive)));
    }
}
