// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a second named type in the same compilation that implements one of the owned boundary interfaces. Each of
/// those interfaces has exactly one owner class; a second implementer — even one that structurally satisfies
/// <c>OwnerClass.Implements</c> — is a build error (ag0025-one-owner-per-interface). This closes the trick of adding
/// <c>: IFileReader</c> with stub members to an inconvenient type to launder a raw call past the single-owner
/// exemption of the boundary rules: the second implementer is flagged here regardless of whether it would have been
/// exempted. Every named-type kind counts — class, struct, record, and record struct — because the exemption predicate
/// AG0025 polices (<c>OwnerClass.Implements</c>) has no <see cref="TypeKind"/> gate, so a struct declaring
/// <c>: IFileReader</c> would otherwise launder a raw call yet be invisible to this scan. The owner-interface set is
/// the one derived from <c>ISystemServices</c> by <see cref="BoundaryServices.Resolve"/>, captured once per
/// compilation. Only source types in the compilation are considered — <see cref="SymbolKind.NamedType"/> visits every
/// declared named type (nested included) and excludes referenced-assembly types, so a referenced assembly's own single
/// implementer never counts against a second one here. The accumulate-and-report-second-implementer algorithm is the
/// one shared with the container rule (AG0022) through <see cref="SecondImplementerGuard"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneOwnerPerInterfaceAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0025";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "An owner interface may be implemented by at most one type per compilation",
        messageFormat: "Type '{0}' is a second implementer of owner interface '{1}'; each owned boundary interface has exactly one owner class, so route through that one owner instead of adding a second implementer",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "At most one type per compilation may implement a given owner interface (the owned boundary services derived from ISystemServices). A second implementer of any named-type kind — class, struct, record, or record struct — is a build error even if it structurally satisfies the single-owner exemption, closing the trick of adding ': IFileReader' with stub members to an inconvenient type to launder a raw call. There is exactly one owner; route through it.",
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
        // One-owner-per-interface is a SHIPPING-code invariant. The test system (test-system decision) deliberately
        // ships BOTH a fake (e.g. InMemoryFileSystem : IFileReader) AND a Wrap proxy base (e.g. RecordingFileReader :
        // IFileReader) per service in AgentGuard.TestHelpers — two legitimate implementers of the same owner interface
        // in one compilation — and the test projects reference them, so the test/TestHelpers assemblies are not gated.
        // The "TestHelpers OR .Tests" predicate is the one owned by TestAssembly, shared with NoCoverageOptOutAnalyzer.
        // The container rule (AG0022) shares the SAME accumulation but deliberately does NOT take this early-return.
        if (TestAssembly.IsTestSystemAssembly(context.Compilation))
        {
            return;
        }

        // Derive the owner-interface set from ISystemServices once per compilation, then delegate to the shared
        // second-implementer accumulation — no hand-maintained list (derive-service-set-from-isystemservices).
        DerivedServices services = BoundaryServices.Resolve(context.Compilation);
        SecondImplementerGuard.Register(context, services.ServiceInterfaces, Rule);
    }
}
