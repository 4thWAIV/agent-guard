// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a public plain class declared in an <c>.Abstractions</c> namespace, or in any namespace
/// nested under one. Abstractions may declare interfaces, enums, records, delegates, and structs,
/// which are contracts, but not a plain class, which is an implementation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoClassInAbstractionsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0001";

    private const string Category = "AgentGuard.Architecture";
    private const string AbstractionsSegment = "Abstractions";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Abstractions must not declare a class",
        messageFormat: "Type '{0}' is a class declared in an '.Abstractions' namespace; abstractions declare contracts, not implementations",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A public class declared in an '.Abstractions' namespace, or a namespace nested under one, is not allowed. Abstractions may declare interfaces, enums, records, delegates, and structs.");

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

        if (symbol.ContainingType is not null)
        {
            return;
        }

        if (symbol.TypeKind != TypeKind.Class || symbol.IsRecord)
        {
            return;
        }

        if (!IsUnderAbstractionsNamespace(symbol.ContainingNamespace))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name));
    }

    private static bool IsUnderAbstractionsNamespace(INamespaceSymbol? containingNamespace)
    {
        for (INamespaceSymbol? ns = containingNamespace; ns is not null && !ns.IsGlobalNamespace; ns = ns.ContainingNamespace)
        {
            if (string.Equals(ns.Name, AbstractionsSegment, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
