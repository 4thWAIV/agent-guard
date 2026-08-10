// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a member of a class that implements a contract interface when that member hands back the
/// concrete class itself instead of the interface.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContractMustNotExposeConcreteTypeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0004";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Contract implementation must not expose its concrete type",
        messageFormat: "'{0}' exposes the concrete contract type; hand back the interface instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No member of a class that implements a contract interface may return or expose the concrete class; expose the interface instead. A static factory may return the interface, overloaded as needed.");

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
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (!ContractPattern.IsContractImplementation(type))
        {
            return;
        }

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared || member.DeclaredAccessibility == Accessibility.Private)
            {
                continue;
            }

            ITypeSymbol? exposed = GetExposedType(member);
            if (exposed is not null && SymbolEqualityComparer.Default.Equals(exposed, type))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, member.Locations[0], member.Name));
            }
        }
    }

    private static ITypeSymbol? GetExposedType(ISymbol member)
    {
        return member switch
        {
            IMethodSymbol { MethodKind: MethodKind.Ordinary } method => method.ReturnType,
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null,
        };
    }
}
