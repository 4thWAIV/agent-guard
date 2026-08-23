// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a <c>true</c>/<c>false</c> compile-time literal passed as the <c>caseSensitive</c> argument of the single
/// overlay factory <c>AgentGuard.TestHelpers.SystemServicesBuilder.NewOverlay</c> — the one place a caller's
/// <c>caseSensitive</c> value is visible, because <c>NewOverlay</c> forwards its own parameter to the copy-on-write
/// overlay <c>InMemoryFileSystemStore</c>'s constructor. The check targets the <c>NewOverlay</c> call, NOT the
/// <c>InMemoryFileSystemStore</c> constructor: the constructor is only ever reached through <c>NewOverlay</c>, which
/// forwards its own parameter, so the constructor call never carries a literal and a check there could never fire; a
/// re-hardcode would be a literal at the <c>NewOverlay</c> call, which is what this rule catches. The simulator's case
/// mode must be seeded from the real host (through the
/// case-sensitivity detection the platform file system already carries) so a case-insensitive-host regression is
/// exercised where the developer's machine cannot see it; a hardcoded <c>caseSensitive: true</c> at the seed site
/// pins the fake to one host's behavior.
/// <para>
/// The rule fires ONLY on the <c>caseSensitive</c> argument at that site, matched by the target member's identity
/// (namespace + name AND the declaring assembly through <see cref="WellKnownType.IsInAssembly"/>) AND the parameter
/// name, and only on a value the caller EXPLICITLY wrote that is a compile-time constant. The deliberate test entry
/// points that switch the case mode after construction — <c>SimulateCaseSensitivity</c> on the builder and
/// <c>SetCaseSensitive</c> on the store — are ordinary methods, neither the constructor nor <c>NewOverlay</c>, so they
/// are never inspected and stay free to take a literal, exactly as the contract carves out.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoLiteralCaseModeSeedAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0036";

    private const string Category = "AgentGuard.Architecture";
    private const string CaseSensitiveParameterName = "caseSensitive";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "The simulator's case mode must not be seeded with a compile-time literal",
        messageFormat: "The 'caseSensitive' argument seeds the simulator's case mode with a compile-time literal; source it from the real host through the platform file system's case-sensitivity detection so a case-insensitive host is exercised, instead of pinning the fake to one host's behavior",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The overlay's caseSensitive seed must be sourced from the real host (through IPlatformFileSystem.IsCaseSensitive), not a true/false literal, so the copy-on-write simulator reflects the running filesystem's case mode. The seed is passed at the overlay factory SystemServicesBuilder.NewOverlay — the one place a caller's caseSensitive is visible, because NewOverlay forwards its own parameter to the InMemoryFileSystemStore constructor. The check targets the NewOverlay call, NOT the InMemoryFileSystemStore constructor: the constructor is only ever reached through NewOverlay, which forwards its own parameter, so a re-hardcode would be a literal at the NewOverlay call. A compile-time literal at that site is a build error. The rule matches only the caseSensitive argument at that site and only a value the caller explicitly wrote; the deliberate SetCaseSensitive/SimulateCaseSensitivity entry points are methods, neither the constructor nor NewOverlay, and are never flagged.");

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
        // The case mode reaches the overlay through SystemServicesBuilder.NewOverlay, which forwards its own
        // caseSensitive parameter to the store constructor — so a caller's re-hardcoded value is visible only at the
        // NewOverlay CALL (in Fake()/SimulateFileSystem), never at the constructor. The check targets the NewOverlay
        // call, NOT the InMemoryFileSystemStore constructor: the constructor is only ever reached through NewOverlay,
        // which forwards its own parameter, so the constructor call never carries a literal and a check there could
        // never fire. Inspect that call's caseSensitive argument. The overlay-factory predicate lives once on
        // TestHelperTypes (shared with AG0035), so a same-named method elsewhere never fires here either. The deliberate
        // post-construction switches SetCaseSensitive/SimulateCaseSensitivity are ordinary methods — neither the
        // constructor nor NewOverlay — so they are never inspected and stay free to take a literal, exactly as the
        // contract carves out.
        if (TestHelperTypes.IsOverlayFactory(context.Operation, member, type))
        {
            SeedArgument.ReportIfLiteral(context, Rule, CaseSensitiveParameterName);
        }
    }
}
