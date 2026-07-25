// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports every public type declared directly in a namespace whose name ends in
/// <c>.Abstractions</c> that is not an interface.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AbstractionsMustBeInterfacesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0001";

    private const string Category = "AgentGuard.Architecture";
    private const string AbstractionsSuffix = ".Abstractions";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Abstractions must be interfaces",
        messageFormat: "Type '{0}' is declared in an '.Abstractions' namespace and must be an interface",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Public types declared directly in a namespace ending in '.Abstractions' must be interfaces.");

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
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;

        if (symbol.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        if (symbol.TypeKind == TypeKind.Interface)
        {
            return;
        }

        if (symbol.ContainingType is not null)
        {
            return;
        }

        var containingNamespace = symbol.ContainingNamespace;
        if (containingNamespace is null || containingNamespace.IsGlobalNamespace)
        {
            return;
        }

        var namespaceName = containingNamespace.ToDisplayString();
        if (!namespaceName.EndsWith(AbstractionsSuffix, StringComparison.Ordinal))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
