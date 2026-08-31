// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    // Thin call to the shared match-then-report owner: report Rule against any applied [DllImport]/[LibraryImport] in a
    // non-cross-platform assembly (the CompilationStart gate above has already excluded the per-OS libraries). The
    // resolve-first match — falling back to the syntactic name only for the [LibraryImport] source-generator case where
    // the attribute does not bind, so a same-named user attribute in another namespace is left alone — lives in
    // AppliedAttributeBan, shared with AG0039. The interop-attribute identity set is owned once by PInvoke.
    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context) =>
        AppliedAttributeBan.Report(context, PInvoke.InteropAttributes, Rule);
}
