// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a platform factory — a static <c>Create</c> method on the <c>Platform</c> type in the
/// <c>AgentGuard.CrossPlatform</c> namespace — whose return type is not the <c>IPlatformServices</c> container.
/// The factory must always hand back the container so new capabilities can be added without restructuring how
/// services are located or changing callers; it must never be collapsed to a bare service such as
/// <c>IPlatformFileSystem</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PlatformFactoryMustReturnContainerAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0010";

    private const string Category = "AgentGuard.Architecture";
    private const string FactoryTypeName = "Platform";
    private const string FactoryMethodName = "Create";
    private const string ContainerTypeName = "IPlatformServices";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Platform.Create must return the IPlatformServices container",
        messageFormat: "Platform.Create must return the IPlatformServices container, not '{0}'; the factory hands back the container so capabilities can grow without changing callers",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The platform factory Platform.Create() must return the IPlatformServices container, never a bare service such as IPlatformFileSystem. Returning the container keeps a later capability addition from restructuring how services are located or breaking callers.");

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

        if (!method.IsStatic
            || !string.Equals(method.Name, FactoryMethodName, StringComparison.Ordinal))
        {
            return;
        }

        if (!WellKnownType.Is(method.ContainingType, CrossPlatformBoundary.RootName, FactoryTypeName))
        {
            return;
        }

        if (!WellKnownType.Is(method.ReturnType as INamedTypeSymbol, CrossPlatformBoundary.RootName, ContainerTypeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.ReturnType.ToDisplayString()));
        }
    }
}
