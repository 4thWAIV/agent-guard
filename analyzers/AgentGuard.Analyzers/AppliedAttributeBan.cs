// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single owner of "an applied attribute matched a banned identity set, so report a descriptor against it" — the
/// match-then-report body shared by the two attribute-ban rules: the native-interop location ban (AG0008, which
/// forbids <c>[DllImport]</c>/<c>[LibraryImport]</c> outside the per-OS libraries) and the compile-time-source-path
/// ban (AG0039, which forbids <c>[CallerFilePath]</c> anywhere). Extracted here (LESSON 1, DRY) so the two rules share
/// ONE copy of "cast the visited node to an attribute, match it against an identity set through
/// <see cref="AttributeIdentity.IsAnyOf"/>, and on a match report the descriptor with the attribute's qualified name"
/// instead of each spelling the same <see cref="Microsoft.CodeAnalysis.Diagnostics.SyntaxNodeAnalysisContext"/> body.
/// <para>
/// The ban is pure mechanism — which attribute identities are banned, and which descriptor to report, are the caller's
/// policy, passed in per rule. Each analyzer's registered <c>SyntaxNodeAction</c> becomes a thin call to
/// <see cref="Report"/> with its own identity set and <see cref="DiagnosticDescriptor"/>; AG0008 keeps its own
/// <c>CompilationStart</c> assembly gate and <c>GeneratedCodeAnalysisFlags.Analyze</c> registration on top.
/// </para>
/// This reuses the two existing owners rather than re-deriving either: <see cref="AttributeIdentity.IsAnyOf"/> owns the
/// resolve-first identity match with the syntactic fallback, and <see cref="AttributeSyntaxName"/> owns the syntactic
/// simple-name extraction and its <c>…Attribute</c> normalization for the reported name.
/// </summary>
internal static class AppliedAttributeBan
{
    /// <summary>
    /// Reports <paramref name="descriptor"/> against the applied attribute the context is visiting when that attribute
    /// is any of <paramref name="bannedIdentities"/>, formatted with the attribute's qualified <c>…Attribute</c> name;
    /// otherwise does nothing.
    /// </summary>
    /// <param name="context">The syntax-node analysis context whose node is the applied <see cref="AttributeSyntax"/>.</param>
    /// <param name="bannedIdentities">The (namespace, name) identities any of which the attribute may not be.</param>
    /// <param name="descriptor">The diagnostic to report when the attribute matches a banned identity.</param>
    internal static void Report(
        SyntaxNodeAnalysisContext context,
        ImmutableArray<(string Namespace, string Name)> bannedIdentities,
        DiagnosticDescriptor descriptor)
    {
        var attribute = (AttributeSyntax)context.Node;

        if (!AttributeIdentity.IsAnyOf(context.SemanticModel, attribute, bannedIdentities, context.CancellationToken))
        {
            return;
        }

        string qualifiedName = AttributeSyntaxName.FullName(AttributeSyntaxName.SimpleName(attribute.Name));
        context.ReportDiagnostic(Diagnostic.Create(descriptor, attribute.GetLocation(), qualifiedName));
    }
}
