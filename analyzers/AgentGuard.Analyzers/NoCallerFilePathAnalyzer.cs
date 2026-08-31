// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports the <c>System.Runtime.CompilerServices.CallerFilePathAttribute</c> applied to any parameter, production and
/// test alike, with no owner exemption. The reach is every assembly the custom analyzers are wired into, which is every
/// project EXCEPT the two analyzer projects themselves: <c>AgentGuard.Analyzers</c> and <c>AgentGuard.Analyzers.Tests</c>
/// set <c>AgentGuardIsAnalyzerProject</c>, and <c>Directory.Build.props</c> skips the custom-analyzer wiring for those,
/// so this rule cannot fire inside them. <c>[CallerFilePath]</c> embeds the compiler's
/// compile-time source path into the value the caller receives; under a deterministic CI build
/// (<c>Deterministic=true</c> with <c>ContinuousIntegrationBuild=true</c>) Roslyn rewrites that path to
/// <c>/_/...</c>, so any file located from it is missing on CI while it resolves on the developer's machine — a break
/// invisible locally and caught only in cross-runner CI. Reach the application base directory through the owned
/// <c>IEnvironment.GetBaseDirectory()</c> instead of capturing a source path.
/// <para>
/// Scoped to exactly <c>CallerFilePathAttribute</c> — never its <c>CallerMemberName</c>, <c>CallerLineNumber</c>, or
/// <c>CallerArgumentExpression</c> siblings, which leak no path. The attribute is matched by RESOLVED type through the
/// shared <see cref="AttributeIdentity.IsAnyOf"/> resolver, so a user-declared <c>CallerFilePath</c> attribute in
/// another namespace is not mistaken for the BCL one; the syntactic fallback inside the resolver still catches a
/// genuinely unresolved attribute.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoCallerFilePathAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0039";

    private const string Category = "AgentGuard.Architecture";

    /// <summary>
    /// The (namespace, name) identity of <c>System.Runtime.CompilerServices.CallerFilePathAttribute</c> — the one
    /// attribute this rule recognizes. Single owner of the identity; passed to <see cref="AttributeIdentity.IsAnyOf"/>
    /// so a user-declared <c>CallerFilePath</c> attribute in another namespace is not mistaken for the BCL one, and so
    /// the <c>CallerMemberName</c>/<c>CallerLineNumber</c>/<c>CallerArgumentExpression</c> siblings are never matched.
    /// </summary>
    private static readonly ImmutableArray<(string Namespace, string Name)> CallerFilePathAttribute =
        ImmutableArray.Create((KnownNamespaces.SystemRuntimeCompilerServices, "CallerFilePathAttribute"));

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A compile-time source path must not be captured with [CallerFilePath]",
        messageFormat: "'{0}' captures a compile-time source path that a deterministic CI build rewrites to '/_/...'; reach the application base directory through the owned IEnvironment.GetBaseDirectory() instead of a compile-time source path",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The [CallerFilePath] attribute embeds the compiler's source path into the value the caller receives. Under a deterministic CI build (Deterministic=true with ContinuousIntegrationBuild=true) that path is remapped to /_/..., so any file located from it reads as missing on CI while it resolves on the developer's machine. Reach the application base directory through the owned IEnvironment.GetBaseDirectory() instead of capturing a compile-time source path. Scoped to CallerFilePathAttribute only — its CallerMemberName/CallerLineNumber/CallerArgumentExpression siblings leak no path and are not affected — and applies production and test alike, with no owner exemption, in every assembly the custom analyzers are wired into (every project except AgentGuard.Analyzers and AgentGuard.Analyzers.Tests, which set AgentGuardIsAnalyzerProject and so are skipped by the custom-analyzer wiring in Directory.Build.props).");

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

        // No CompilationStart gate and no assembly exemption: the ban is repo-wide, production and test alike. Register
        // directly on every applied attribute and match [CallerFilePath] by resolved type.
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    // Thin call to the shared match-then-report owner: report Rule against any applied [CallerFilePath] and leave every
    // other attribute alone. The resolve-first match (so a user attribute of the same simple name in another namespace
    // does not fire, and the CallerMemberName/CallerLineNumber/CallerArgumentExpression siblings never match) lives in
    // AppliedAttributeBan, shared with AG0008.
    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context) =>
        AppliedAttributeBan.Report(context, CallerFilePathAttribute, Rule);
}
