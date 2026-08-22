// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a <c>true</c>/<c>false</c> compile-time literal passed as the <c>caseSensitive</c> argument where the
/// copy-on-write overlay <c>AgentGuard.TestHelpers.InMemoryFileSystemStore</c> is constructed. The simulator's case
/// mode must be seeded from the real host (through the case-sensitivity detection the platform file system already
/// carries) so a case-insensitive-host regression is exercised where the developer's machine cannot see it; a
/// hardcoded <c>caseSensitive: true</c> at the construction site pins the fake to one host's behavior.
/// <para>
/// The rule fires ONLY on the store constructor's <c>caseSensitive</c> parameter, matched by the created type's
/// identity (namespace + name through <see cref="WellKnownType"/>) AND the parameter name, and only on a value the
/// caller EXPLICITLY wrote that is a compile-time constant. The deliberate test entry points that switch the case
/// mode after construction — <c>SimulateCaseSensitivity</c> on the builder and <c>SetCaseSensitive</c> on the store —
/// are ordinary methods, not the constructor, so they are never inspected and stay free to take a literal, exactly as
/// the contract carves out.
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
        description: "The InMemoryFileSystemStore overlay's caseSensitive construction argument must be seeded from the real host (through IPlatformFileSystem.IsCaseSensitive), not a true/false literal, so the copy-on-write simulator reflects the running filesystem's case mode. A compile-time literal there is a build error. The rule matches only the store constructor's caseSensitive parameter and only a value the caller explicitly wrote; the deliberate SetCaseSensitive/SimulateCaseSensitivity entry points are methods, not the constructor, and are never flagged.");

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
        // The case mode is seeded only where the store overlay is constructed; the constructor's caseSensitive
        // argument is the one place a literal pins it. The store-construction predicate lives once on TestHelperTypes
        // (shared with AG0027/AG0035), so a same-named type elsewhere never fires here either.
        if (!TestHelperTypes.IsStoreConstruction(context.Operation, type))
        {
            return;
        }

        IArgumentOperation? argument =
            SeedArgument.ExplicitLiteral(SeedArgument.Of(context.Operation), CaseSensitiveParameterName);
        if (argument is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Syntax.GetLocation()));
        }
    }
}
