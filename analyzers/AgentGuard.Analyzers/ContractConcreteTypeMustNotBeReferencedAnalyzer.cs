// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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

        // The four declaration positions — field, property, method (return and parameters), and local — are the shared
        // DeclaredTypeScanner lens, which was extracted from this rule and is now also driven by the access rules
        // (AG0041 and the Engine gates of AG0023/AG0029). This rule keeps exactly the positions it always had: the
        // invoked-return position the access rules need is registered only by the gated entry point.
        context.RegisterCompilationStartAction(startContext => DeclaredTypeScanner.RegisterDeclarations(
            startContext,
            (declaredType, _, location) => ContractPattern.ReferencesContractImplementation(declaredType)
                ? Diagnostic.Create(Rule, location)
                : null));
    }
}
