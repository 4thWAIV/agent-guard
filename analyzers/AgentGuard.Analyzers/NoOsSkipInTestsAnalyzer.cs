// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a custom skip attribute that re-opens the OS-skip dodge (os-skip-ban-rule): a type deriving from xUnit's
/// <c>FactAttribute</c> or <c>TheoryAttribute</c>, in a test compilation, is a build error. That subclass is exactly
/// how an OS-conditional skip is reintroduced under a new name — the deleted <c>PosixOnlyFactAttribute</c> derived from
/// <c>FactAttribute</c> and set <c>Skip</c> from an OS check in its constructor, so a cross-OS CLI test quietly did not
/// run on Windows. Making a custom Fact/Theory subclass a build error closes that, forcing a genuinely needed
/// OS-specific skip through the signed rule-exception system rather than a hand-rolled attribute. The xUnit base types
/// are matched by full name (namespace + name) through <see cref="WellKnownType"/> along the type's base chain, so a
/// subclass at any depth is caught. The rule is scoped to test compilations (where xUnit is referenced); only
/// source-declared types are visited (<see cref="SymbolKind.NamedType"/> excludes referenced-assembly types), so
/// xUnit's own <c>TheoryAttribute : FactAttribute</c> is never flagged.
/// <para>
/// The companion half of os-skip-ban — an OS-guarded RUNTIME skip in a test body (<c>Assert.Skip</c> / <c>Skip.If</c>
/// conditioned on an <c>OperatingSystem.Is*</c> / <c>RuntimeInformation.IsOSPlatform</c> check) — applies only where
/// that API exists in the live xUnit surface. The pinned xUnit is v2.7.0, which has no dynamic-skip API
/// (<c>Assert.Skip</c>/<c>Skip.If</c> arrived in xUnit v3), so in this repo an OS-conditional skip can only be built by
/// SETTING <c>Skip</c> — which requires the custom-subclass mechanism this rule bans. The runtime-skip half is
/// therefore a no-op here by design and grows a body only if the repo moves to a xUnit surface that has the API.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoOsSkipInTestsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0026";

    private const string Category = "AgentGuard.Architecture";
    private const string XunitNamespace = "Xunit";

    // The xUnit test-method attribute base types a custom skip attribute derives from. TheoryAttribute itself derives
    // from FactAttribute in xUnit, so a subclass of either is caught by walking the base chain against both.
    private static readonly ImmutableArray<(string Namespace, string Name)> XunitTestAttributeBases = ImmutableArray.Create(
        (XunitNamespace, "FactAttribute"),
        (XunitNamespace, "TheoryAttribute"));

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A test must not derive a custom skip attribute from xUnit's FactAttribute/TheoryAttribute",
        messageFormat: "Type '{0}' derives from xUnit's FactAttribute/TheoryAttribute; a custom Fact/Theory attribute is the OS-skip dodge (it can set Skip from an OS check), so use a plain [Fact]/[Theory] and route a genuinely OS-specific skip through the signed rule-exception system",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type deriving from xUnit's FactAttribute or TheoryAttribute is a build error in a test compilation. A custom Fact/Theory subclass is how an OS-conditional skip is reintroduced under a new name — it can set Skip from an OS check in its constructor — so a cross-OS test quietly does not run on some OS. Use a plain [Fact]/[Theory]; a genuinely needed OS-specific skip goes through the signed rule-exception system, not a hand-rolled attribute.",
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
        // Scoped to test compilations — the only place xUnit's Fact/Theory attributes are referenced. A shipping
        // assembly cannot declare a Fact subclass anyway (no xUnit reference), so gating keeps the scan off it.
        if (!TestAssembly.IsTestAssembly(context.Compilation))
        {
            return;
        }

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (DerivesFromXunitTestAttribute(type.BaseType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.Name));
        }
    }

    private static bool DerivesFromXunitTestAttribute(INamedTypeSymbol? baseType)
    {
        // Walk the base chain — a custom skip attribute may sit any number of levels above FactAttribute — matching each
        // base by full name against the xUnit bases.
        for (INamedTypeSymbol? current = baseType; current is not null; current = current.BaseType)
        {
            if (WellKnownType.IsAnyOf(current, XunitTestAttributeBases))
            {
                return true;
            }
        }

        return false;
    }
}
