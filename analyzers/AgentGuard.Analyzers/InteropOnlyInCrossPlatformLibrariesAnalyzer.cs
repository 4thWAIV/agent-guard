// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a native-interop P/Invoke declaration — a method carrying <c>[DllImport]</c> or <c>[LibraryImport]</c> —
/// declared outside the <c>AgentGuard.CrossPlatform.*</c> platform implementation libraries. Native interop lives
/// only inside those per-OS libraries, behind the locked interfaces; the rest of the codebase stays OS-agnostic
/// and consumes platform capabilities through <c>IPlatformServices</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InteropOnlyInCrossPlatformLibrariesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0008";

    private const string Category = "AgentGuard.Architecture";
    private const string InteropNamespace = "System.Runtime.InteropServices";
    private const string AttributeSuffix = "Attribute";
    private const string DllImportAttributeName = "DllImportAttribute";
    private const string LibraryImportAttributeName = "LibraryImportAttribute";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Native interop must live only in the AgentGuard.CrossPlatform libraries",
        messageFormat: "Native interop '{0}' is declared outside the AgentGuard.CrossPlatform.* libraries; P/Invoke lives only in the per-OS platform implementation libraries, behind the locked interfaces",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A method carrying [DllImport] or [LibraryImport] may be declared only inside the AgentGuard.CrossPlatform.* platform implementation libraries. All other assemblies consume platform capabilities through IPlatformServices and stay free of native interop.");

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

        // Analyze (but do not report in) generated code. A [LibraryImport] partial method's implementation is
        // emitted by the source generator with [GeneratedCode], and Roslyn associates the user-authored partial
        // declaration with that generated output; under GeneratedCodeAnalysisFlags.None the attribute on the
        // user's declaration is never visited, so the P/Invoke would slip through. Analyze visits it, while
        // omitting ReportDiagnostics keeps the generator's own emitted marshalling stubs (in .g.cs) from firing.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (CrossPlatformBoundary.IsCrossPlatformLibrary(context.Compilation))
        {
            return;
        }

        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        string qualifiedName = FullAttributeName(SimpleName(attribute.Name));
        if (!string.Equals(qualifiedName, DllImportAttributeName, StringComparison.Ordinal)
            && !string.Equals(qualifiedName, LibraryImportAttributeName, StringComparison.Ordinal))
        {
            return;
        }

        // The [LibraryImport] source generator can leave the attribute unresolved in the post-generation
        // semantic model, so the syntactic name is the reliable signal. The semantic model is used only to
        // reject a same-named attribute declared in a different namespace: if the symbol resolves to a type
        // that is NOT the interop attribute, it is not native interop and is left alone.
        if (context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is IMethodSymbol constructor
            && !WellKnownType.Is(constructor.ContainingType, InteropNamespace, DllImportAttributeName)
            && !WellKnownType.Is(constructor.ContainingType, InteropNamespace, LibraryImportAttributeName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, attribute.GetLocation(), qualifiedName));
    }

    private static string SimpleName(NameSyntax name)
    {
        return name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => name.ToString(),
        };
    }

    private static string FullAttributeName(string simpleName)
    {
        return simpleName.EndsWith(AttributeSuffix, StringComparison.Ordinal)
            ? simpleName
            : simpleName + AttributeSuffix;
    }
}
