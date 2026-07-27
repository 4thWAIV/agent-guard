// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a cast, an <c>as</c> expression, or a type pattern whose target is a contract implementation
/// class. The concrete type must be reached only through its interface, never recovered by a cast.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContractConcreteTypeMustNotBeCastToAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0007";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Contract implementation must not be cast to",
        messageFormat: "Do not cast to the concrete contract type '{0}'; use its interface",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class that implements a contract interface must not be the target of a cast, an 'as' expression, or a type pattern; it must be reached only through its interface.");

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
        context.RegisterSyntaxNodeAction(AnalyzeCast, SyntaxKind.CastExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAs, SyntaxKind.AsExpression);
        context.RegisterSyntaxNodeAction(AnalyzeDeclarationPattern, SyntaxKind.DeclarationPattern);
        context.RegisterSyntaxNodeAction(AnalyzeTypePattern, SyntaxKind.TypePattern);
    }

    private static void AnalyzeCast(SyntaxNodeAnalysisContext context)
    {
        var cast = (CastExpressionSyntax)context.Node;
        Check(context, cast.Type);
    }

    private static void AnalyzeAs(SyntaxNodeAnalysisContext context)
    {
        var binary = (BinaryExpressionSyntax)context.Node;
        if (binary.Right is TypeSyntax type)
        {
            Check(context, type);
        }
    }

    private static void AnalyzeDeclarationPattern(SyntaxNodeAnalysisContext context)
    {
        var pattern = (DeclarationPatternSyntax)context.Node;
        Check(context, pattern.Type);
    }

    private static void AnalyzeTypePattern(SyntaxNodeAnalysisContext context)
    {
        var pattern = (TypePatternSyntax)context.Node;
        Check(context, pattern.Type);
    }

    private static void Check(SyntaxNodeAnalysisContext context, TypeSyntax typeSyntax)
    {
        if (context.SemanticModel.GetSymbolInfo(typeSyntax, context.CancellationToken).Symbol is INamedTypeSymbol type
            && ContractPattern.IsContractImplementation(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, typeSyntax.GetLocation(), type.Name));
        }
    }
}
