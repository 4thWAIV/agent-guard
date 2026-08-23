// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports an <c>[ExcludeFromCodeCoverage]</c> attribute on any type, method, or property in a covered product
/// assembly. Excluding code from coverage is the easiest way to reach the 75% gate without writing the missing tests
/// (ag0032-no-coverage-opt-out); today only prose forbids it. The covered product assemblies are every AgentGuard
/// assembly except the ones out of the coverage scope — <c>AgentGuard.Abstractions</c> (interfaces only),
/// <c>AgentGuard.TestHelpers</c> (test-only), <c>AgentGuard.Analyzers</c>, and the test projects (name ending in
/// <c>.Tests</c>). In those, an exclusion is allowed; everywhere else it is a build error, so the number is lifted by
/// writing the test, never by excluding the code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoCoverageOptOutAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0032";

    private const string Category = "AgentGuard.Architecture";
    private const string ExcludeFromCodeCoverageNamespace = "System.Diagnostics.CodeAnalysis";
    private const string ExcludeFromCodeCoverageAttributeName = "ExcludeFromCodeCoverageAttribute";
    private const string AnalyzersAssemblyName = "AgentGuard.Analyzers";
    private const string AbstractionsAssemblyName = "AgentGuard.Abstractions";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Covered product code must not opt out of code coverage",
        messageFormat: "'{0}' carries [ExcludeFromCodeCoverage] in a covered product assembly; write the missing test instead of excluding the code to lift the coverage number",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An [ExcludeFromCodeCoverage] attribute on a type, method, or property in a covered product assembly is a build error — the easiest way to reach 75% coverage without tests. The coverage number is lifted by writing the missing test, never by excluding the code. Only the out-of-scope assemblies (AgentGuard.Abstractions, AgentGuard.TestHelpers, AgentGuard.Analyzers, and the test projects) may exclude.");

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
        // Only a covered product assembly is gated; the coverage-scope-out assemblies may exclude freely.
        if (!IsCoveredProductAssembly(context.Compilation))
        {
            return;
        }

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property);
    }

    private static bool IsCoveredProductAssembly(Compilation compilation)
    {
        string? assemblyName = compilation.AssemblyName;
        if (assemblyName is null)
        {
            return false;
        }

        // Out of the coverage scope (covered-assemblies): the interfaces-only assembly, the test-only helpers, the
        // analyzer assembly, and the test projects. Everything else that carries the analyzer is covered product code.
        // The "TestHelpers OR .Tests" half is the shared TestAssembly.IsTestSystemAssembly predicate (its De Morgan
        // dual), owned once so AG0025 and this rule cannot drift apart.
        return !string.Equals(assemblyName, AbstractionsAssemblyName, StringComparison.Ordinal)
            && !string.Equals(assemblyName, AnalyzersAssemblyName, StringComparison.Ordinal)
            && !TestAssembly.IsTestSystemAssembly(compilation);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        bool optsOutOfCoverage = context.Symbol.GetAttributes().Any(
            attribute => WellKnownType.Is(
                attribute.AttributeClass, ExcludeFromCodeCoverageNamespace, ExcludeFromCodeCoverageAttributeName));
        if (optsOutOfCoverage)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations[0], context.Symbol.Name));
        }
    }
}
