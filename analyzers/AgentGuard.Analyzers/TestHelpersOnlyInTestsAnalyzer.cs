// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a reference to a type from the <c>AgentGuard.TestHelpers</c> assembly — <c>SystemServicesBuilder</c>,
/// the in-memory fakes, the proxies — made from an assembly that is not a test assembly. The test helpers build the
/// container without calling <c>SystemServices.Create()</c>, so neither construction wall covers them; this rule is
/// the wall. It keeps the shipped guard from ever being built against fake state: only a test project (an assembly
/// whose name ends in <c>.Tests</c>) may reach the test helpers. Built on the same assembly-name gate as AG0008 and
/// AG0011.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestHelpersOnlyInTestsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0018";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "AgentGuard.TestHelpers may be referenced only from test assemblies",
        messageFormat: "Type '{0}' from AgentGuard.TestHelpers is referenced from a non-test assembly; only a test project (name ending in .Tests) may use the test helpers, so the shipped guard is never built against fake state",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type from the AgentGuard.TestHelpers assembly (SystemServicesBuilder, the fakes, the proxies) may be referenced only from a test assembly whose name ends in .Tests. Shipping code never references TestHelpers, so the shipped guard cannot be run against the fake container the helpers build.");

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
        // Test assemblies are the one place the helpers are allowed; the helpers assembly itself references its own
        // types freely. Everything else is shipping code and must not touch the helpers. The "TestHelpers OR .Tests"
        // disjunction is the shared TestAssembly.IsTestSystemAssembly predicate, owned once so it cannot drift.
        if (TestAssembly.IsTestSystemAssembly(context.Compilation))
        {
            return;
        }

        MemberUseScanner.Register(context, Inspect);
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (TestAssembly.IsFromTestHelpers(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), type.Name));
        }
    }
}
