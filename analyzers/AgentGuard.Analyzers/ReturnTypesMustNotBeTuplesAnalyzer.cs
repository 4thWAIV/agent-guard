// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports any method, property, indexer, delegate, or local function whose return type is, or
/// contains, a tuple type. A named type or record must be returned instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReturnTypesMustNotBeTuplesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0002";

    private const string Category = "AgentGuard.Design";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Return types must not be tuples",
        messageFormat: "'{0}' returns a tuple type; return a named type or record instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Methods, properties, indexers, delegates, and local functions must not return a tuple type; declare a named type or record instead.");

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
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeDelegate, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var symbol = (IMethodSymbol)context.Symbol;

        if (symbol.IsImplicitlyDeclared)
        {
            return;
        }

        if (symbol.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor
            or MethodKind.Destructor or MethodKind.PropertyGet or MethodKind.PropertySet
            or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise
            or MethodKind.LambdaMethod or MethodKind.LocalFunction)
        {
            return;
        }

        if (symbol.ReturnsVoid || !ReturnsTuple(symbol.ReturnType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name));
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var symbol = (IPropertySymbol)context.Symbol;

        if (symbol.IsImplicitlyDeclared || !ReturnsTuple(symbol.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name));
    }

    private static void AnalyzeDelegate(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;

        if (symbol.TypeKind != TypeKind.Delegate || symbol.IsImplicitlyDeclared)
        {
            return;
        }

        var invoke = symbol.DelegateInvokeMethod;
        if (invoke is null || invoke.ReturnsVoid || !ReturnsTuple(invoke.ReturnType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name));
    }

    private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
    {
        var node = (LocalFunctionStatementSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(node) is not IMethodSymbol symbol)
        {
            return;
        }

        if (symbol.ReturnsVoid || !ReturnsTuple(symbol.ReturnType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, node.Identifier.GetLocation(), symbol.Name));
    }

    private static bool ReturnsTuple(ITypeSymbol? type)
    {
        return ContainsTuple(type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
    }

    private static bool ContainsTuple(ITypeSymbol? type, HashSet<ITypeSymbol> visited)
    {
        if (type is null || !visited.Add(type))
        {
            return false;
        }

        return type switch
        {
            INamedTypeSymbol named => named.IsTupleType
                || named.TypeArguments.Any(argument => ContainsTuple(argument, visited)),
            IArrayTypeSymbol array => ContainsTuple(array.ElementType, visited),
            IPointerTypeSymbol pointer => ContainsTuple(pointer.PointedAtType, visited),
            _ => false,
        };
    }
}
