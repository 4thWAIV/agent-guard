// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a public instance member of a class that implements a contract interface when that member
/// is not declared on any interface the class implements.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContractPublicSurfaceMustMatchInterfaceAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0005";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Contract implementation may only expose interface members",
        messageFormat: "'{0}' is a public member of a contract implementation that is not declared on any interface it implements",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every public instance member of a class that implements a contract interface must be declared on one of the interfaces it implements.");

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

        var interfaceImplementations = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var contractInterface in type.AllInterfaces)
        {
            foreach (var interfaceMember in contractInterface.GetMembers())
            {
                var implementation = type.FindImplementationForInterfaceMember(interfaceMember);
                if (implementation is not null)
                {
                    interfaceImplementations.Add(implementation);
                }
            }
        }

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared || member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (member is IMethodSymbol method && method.MethodKind != MethodKind.Ordinary)
            {
                continue;
            }

            if (!interfaceImplementations.Contains(member))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, member.Locations[0], member.Name));
            }
        }
    }
}
