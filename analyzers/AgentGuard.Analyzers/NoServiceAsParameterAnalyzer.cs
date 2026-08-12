// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a boundary service interface used as a method parameter — one of the ten owned abstractions passed as a
/// lone argument to an ordinary method. Constructor injection is the one way a class receives a service, so a
/// constructor parameter is fine; passing a lone service into a method is the service-locator smell
/// constructor-injection-no-container forbids, which no other rule checked (ag0031-no-service-as-parameter). The two
/// exceptions are the single <c>Program</c> composition method and the test <c>SystemServicesBuilder</c>, whose
/// <c>With(...)</c> overloads legitimately take a service to substitute. Every other class injects the service
/// through its constructor, not a method argument.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoServiceAsParameterAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0031";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A boundary service interface must not be a method parameter",
        messageFormat: "Method '{0}' takes a boundary service interface as a parameter; inject the service through the constructor, not as a lone method argument",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A boundary service interface (one of the ten owned abstractions) may not be a method parameter, except on the single Program composition method and the test SystemServicesBuilder. Passing a lone service into a method is the service-locator shortcut constructor injection forbids; every class receives the service through its constructor. A constructor parameter is the injection mechanism and is exempt.");

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
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        // Constructor injection is the sanctioned mechanism, so a constructor parameter is never flagged; only an
        // ordinary method taking a lone service is the service-locator smell. Accessors, operators, and lambdas are
        // not ordinary methods and are left alone.
        if (method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        // The composition method (Program) and the builder's With(...) overloads legitimately take a service.
        if (CompositionPoint.Encloses(method))
        {
            return;
        }

        bool takesServiceParameter = method.Parameters.Any(
            parameter => BoundaryServices.IsOwnerInterface(parameter.Type as INamedTypeSymbol));
        if (takesServiceParameter)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.Name));
        }
    }
}
