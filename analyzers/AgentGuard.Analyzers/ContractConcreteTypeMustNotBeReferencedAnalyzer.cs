// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a field, property, parameter, return type, or local whose declared type is, or contains, a
/// contract implementation class. Such a class must be referred to only through its interface.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContractConcreteTypeMustNotBeReferencedAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0006";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Contract implementation must be referenced through its interface",
        messageFormat: "The concrete contract type is referenced here; refer to it through its interface instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class that implements a contract interface must not appear as a declared type. The only place the concrete type may be named is as the receiver of a static call such as its factory.");

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
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterOperationAction(AnalyzeLocal, OperationKind.VariableDeclarator);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (!field.IsImplicitlyDeclared && ContractPattern.ReferencesContractImplementation(field.Type))
        {
            ReportFirst(context, field.Locations);
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;
        if (!property.IsImplicitlyDeclared && ContractPattern.ReferencesContractImplementation(property.Type))
        {
            ReportFirst(context, property.Locations);
        }
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.IsImplicitlyDeclared
            || method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet
                or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise)
        {
            return;
        }

        if (ContractPattern.ReferencesContractImplementation(method.ReturnType))
        {
            ReportFirst(context, method.Locations);
        }

        foreach (var parameter in method.Parameters.Where(parameter => ContractPattern.ReferencesContractImplementation(parameter.Type)))
        {
            ReportFirst(context, parameter.Locations);
        }
    }

    private static void AnalyzeLocal(OperationAnalysisContext context)
    {
        var declarator = (IVariableDeclaratorOperation)context.Operation;
        if (ContractPattern.ReferencesContractImplementation(declarator.Symbol.Type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, declarator.Symbol.Locations[0]));
        }
    }

    private static void ReportFirst(SymbolAnalysisContext context, ImmutableArray<Location> locations)
    {
        if (locations.Length > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, locations[0]));
        }
    }
}
