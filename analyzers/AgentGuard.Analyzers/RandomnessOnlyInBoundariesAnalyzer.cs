// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw randomness call — a use of <c>System.Random</c>, <c>Guid.NewGuid()</c>, or
/// <c>RandomNumberGenerator</c> — made outside the <c>AgentGuard.Boundaries</c> adapter assembly. The one place
/// randomness enters the codebase is the temporary-file name built from a fresh GUID; it is abstracted behind
/// <c>IGuidFactory</c> (the same way time is abstracted behind <c>TimeProvider</c>) so tests are deterministic.
/// The raw <c>Guid.NewGuid()</c> lives only in the <c>Boundaries</c> <c>GuidFactory</c> adapter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RandomnessOnlyInBoundariesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0014";

    private const string Category = "AgentGuard.Architecture";
    private const string NewGuidMethodName = "NewGuid";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Raw randomness must live only in AgentGuard.Boundaries",
        messageFormat: "Raw randomness '{0}' is outside AgentGuard.Boundaries; obtain a GUID through IGuidFactory pulled off ISystemServices so tests stay deterministic",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A use of System.Random, Guid.NewGuid(), or RandomNumberGenerator is allowed only in the AgentGuard.Boundaries adapter assembly. Every other assembly obtains a GUID through IGuidFactory on ISystemServices, keeping randomness at one seam and tests deterministic.");

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
        context.RegisterCompilationStartAction(
            context => MemberUseScanner.RegisterUnless(context, BoundaryAssembly.IsBoundariesLibrary, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (IsBanned(member, type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }

    private static bool IsBanned(ISymbol member, INamedTypeSymbol type)
    {
        // System.Random — any use (construction or a static member such as Random.Shared).
        if (WellKnownType.Is(type, KnownNamespaces.System, "Random"))
        {
            return true;
        }

        // RandomNumberGenerator — any use.
        if (WellKnownType.Is(type, KnownNamespaces.SystemSecurityCryptography, "RandomNumberGenerator"))
        {
            return true;
        }

        // Guid.NewGuid() — the non-deterministic factory. Building a GUID from bytes or parsing text stays legal.
        return WellKnownType.Is(type, KnownNamespaces.System, "Guid")
            && string.Equals(member.Name, NewGuidMethodName, StringComparison.Ordinal);
    }
}
