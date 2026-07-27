// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a class that implements a contract interface but whose constructor is not private.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContractConstructorMustBePrivateAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0003";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Contract implementation constructor must be private",
        messageFormat: "'{0}' implements a contract interface, so its constructor must be private",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class that implements a contract interface must have a private constructor; instances are handed out only through a static factory that returns the interface.");

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

        var declaredConstructors = type.InstanceConstructors
            .Where(constructor => !constructor.IsImplicitlyDeclared)
            .ToImmutableArray();

        if (declaredConstructors.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.Name));
            return;
        }

        foreach (var constructor in declaredConstructors)
        {
            if (constructor.DeclaredAccessibility != Accessibility.Private)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, constructor.Locations[0], type.Name));
            }
        }
    }
}
