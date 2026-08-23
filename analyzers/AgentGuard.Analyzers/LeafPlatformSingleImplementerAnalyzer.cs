// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a source type in a <c>.Tests</c> compilation that implements one of the filesystem/platform LEAF interfaces
/// reached through a nested container — the four filesystem leaves (<c>IFileReader</c>/<c>IDirectoryEnumerator</c>/
/// <c>IFileWriter</c>/<c>IDirectoryWriter</c>) and <c>IPlatformFileSystem</c> (leaf-platform-single-implementer-rule).
/// ZERO source implementers are permitted: the only sanctioned implementers are the fakes and <c>Wrap</c> proxies in
/// <c>AgentGuard.TestHelpers</c>, reached through <c>SystemServicesBuilder</c> and referenced — never re-declared — by a
/// test project. A <c>.Tests</c> type hand-rolling <c>: IDirectoryEnumerator</c> (a throwing enumerator) or
/// <c>: IPlatformFileSystem</c> (a platform double) is a build error here, so a failure case goes through the shared
/// overlay's <c>Handle</c>/<c>MarkInaccessible</c> seam instead of a bespoke leaf implementer.
/// <para>
/// This is the corrected, <c>.Tests</c>-scoped form of the rejected "remove AG0025's blanket exemption": it gates on
/// <see cref="TestAssembly.IsTestAssembly"/> — which EXCLUDES <c>AgentGuard.TestHelpers</c> — so TestHelpers keeps
/// AG0025's deliberate fake-plus-<c>Wrap</c>-proxy dual-implementer exemption, and a shipping assembly (where
/// <c>PosixFileSystem</c> legitimately implements <c>IPlatformFileSystem</c>) is never gated because it is not a test
/// project. The candidate set is the nested-leaf set derived from the ONE container walk
/// (<see cref="BoundaryServices.NestedLeafInterfaces"/>), the same shared walk AG0022/AG0025 read, so a future
/// filesystem leaf added under a nested container is guarded automatically with no edit here. The
/// accumulate-and-report algorithm is the one shared with AG0022/AG0025 through <see cref="SecondImplementerGuard"/>,
/// permitting ZERO implementers so it reports the FIRST source implementer (and every later one). Only source types
/// in the compilation are considered (<see cref="SymbolKind.NamedType"/> excludes referenced-assembly types), so the
/// sanctioned TestHelpers implementers a test references never count here.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LeafPlatformSingleImplementerAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0030";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A filesystem/platform leaf interface may not be implemented by a test-project source type",
        messageFormat: "Type '{0}' implements leaf/platform interface '{1}' in a test project; the only sanctioned implementers are the fakes and Wrap proxies in AgentGuard.TestHelpers, reached through SystemServicesBuilder — build the fake through the builder (use the overlay's Handle/MarkInaccessible seam for a failure case) instead of hand-rolling an implementer",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In a .Tests compilation no source type may implement any of the four filesystem leaf interfaces (IFileReader, IDirectoryEnumerator, IFileWriter, IDirectoryWriter) or IPlatformFileSystem. The only sanctioned implementers are the fakes and Wrap proxies in AgentGuard.TestHelpers, reached through SystemServicesBuilder and referenced by the test project. A test hand-rolling a leaf/platform implementer — a throwing enumerator or a platform double — is a build error; route the fake through the builder and use the shared overlay's Handle/MarkInaccessible seam for a failure case. AgentGuard.TestHelpers keeps AG0025's fake-plus-Wrap-proxy exemption (it is not a .Tests assembly), and a shipping assembly's own single implementer is never gated here.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

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
        // Scoped to .Tests compilations only. IsTestAssembly EXCLUDES AgentGuard.TestHelpers, so the sanctioned
        // fake-plus-Wrap-proxy implementers there keep AG0025's exemption; a shipping assembly (where the per-OS
        // PosixFileSystem/WindowsFileSystem legitimately implement IPlatformFileSystem) is likewise never gated. A
        // non-test compilation registers nothing.
        if (!TestAssembly.IsTestAssembly(context.Compilation))
        {
            return;
        }

        // The candidate set is the nested-leaf set derived from the ONE container walk (the four filesystem leaves
        // reached through IFileSystem plus IPlatformFileSystem reached through IPlatformServices), so a future
        // filesystem leaf under a nested container is covered automatically. When the container is not in scope the
        // set is empty and the shared guard is inert.
        ContainerNode? root = BoundaryServices.ResolveTree(context.Compilation);
        ImmutableArray<(string Namespace, string Name)> leaves = root is null
            ? ImmutableArray<(string Namespace, string Name)>.Empty
            : BoundaryServices.NestedLeafInterfaces(root);

        // Permit ZERO implementers: the sanctioned implementers live in TestHelpers (referenced, not source), so the
        // FIRST source implementer in a .Tests compilation is reported (and every later one) — the shared guard's
        // report-first accumulation, a one-argument variation on AG0025's report-second.
        SecondImplementerGuard.Register(context, leaves, Rule, permittedImplementers: 0);
    }
}
