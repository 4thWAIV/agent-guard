// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports OS branching — a call to <c>OperatingSystem.Is*</c> or <c>RuntimeInformation.IsOSPlatform</c>, or a
/// platform <c>#if</c> — declared outside the <c>AgentGuard.CrossPlatform.*</c> platform implementation
/// libraries. OS-agnostic code must stay free of platform branches; platform-specific behavior is absorbed by
/// the per-OS implementation selected in the csproj and reached through <c>IPlatformServices</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoOsBranchingOutsideCrossPlatformAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0009";

    private const string Category = "AgentGuard.Architecture";
    private const string SystemNamespace = "System";
    private const string InteropNamespace = "System.Runtime.InteropServices";
    private const string OperatingSystemTypeName = "OperatingSystem";
    private const string RuntimeInformationTypeName = "RuntimeInformation";
    private const string OsPlatformQueryMethodName = "IsOSPlatform";
    private const string OperatingSystemQueryPrefix = "Is";

    private static readonly ImmutableArray<string> PlatformSymbolPrefixes = ImmutableArray.Create(
        "WINDOWS", "LINUX", "OSX", "MACOS", "MACCATALYST", "ANDROID", "IOS", "TVOS", "BROWSER", "FREEBSD", "WASI");

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "OS branching is not allowed outside the AgentGuard.CrossPlatform libraries",
        messageFormat: "OS branching ('{0}') is not allowed outside the AgentGuard.CrossPlatform.* libraries; keep OS-agnostic code free of platform branches and reach platform-specific behavior through IPlatformServices",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Calling OperatingSystem.Is* or RuntimeInformation.IsOSPlatform, or writing a platform #if, is not allowed outside the AgentGuard.CrossPlatform.* platform implementation libraries. Platform differences are absorbed by the per-OS implementations selected in the csproj, so consuming code stays OS-agnostic.");

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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (CrossPlatformBoundary.IsCrossPlatformLibrary(context.Compilation))
        {
            return;
        }

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxTreeAction(AnalyzePlatformDirectives);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return;
        }

        INamedTypeSymbol containingType = method.ContainingType;
        if (containingType is null)
        {
            return;
        }

        bool isOperatingSystemBranch = WellKnownType.Is(containingType, SystemNamespace, OperatingSystemTypeName)
            && method.Name.StartsWith(OperatingSystemQueryPrefix, StringComparison.Ordinal);

        bool isRuntimeInformationBranch = WellKnownType.Is(containingType, InteropNamespace, RuntimeInformationTypeName)
            && string.Equals(method.Name, OsPlatformQueryMethodName, StringComparison.Ordinal);

        if (isOperatingSystemBranch || isRuntimeInformationBranch)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), $"{containingType.Name}.{method.Name}"));
        }
    }

    private static void AnalyzePlatformDirectives(SyntaxTreeAnalysisContext context)
    {
        SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
        var platformDirectives = root
            .DescendantNodesAndSelf(descendIntoTrivia: true)
            .OfType<ConditionalDirectiveTriviaSyntax>()
            .Where(directive => ContainsPlatformSymbol(directive.Condition));

        foreach (ConditionalDirectiveTriviaSyntax directive in platformDirectives)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, directive.GetLocation(), directive.ToString().Trim()));
        }
    }

    private static bool ContainsPlatformSymbol(ExpressionSyntax? condition)
    {
        return condition is not null
            && condition.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Any(identifier => IsPlatformSymbol(identifier.Identifier.ValueText));
    }

    private static bool IsPlatformSymbol(string name)
    {
        foreach (string prefix in PlatformSymbolPrefixes)
        {
            if (string.Equals(name, prefix, StringComparison.Ordinal))
            {
                return true;
            }

            if (name.Length > prefix.Length
                && name.StartsWith(prefix, StringComparison.Ordinal)
                && (name[prefix.Length] == '_' || char.IsDigit(name[prefix.Length])))
            {
                return true;
            }
        }

        return false;
    }
}
