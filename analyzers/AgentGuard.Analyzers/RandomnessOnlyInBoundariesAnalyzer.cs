// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw randomness call — a use of <c>System.Random</c>, <c>Guid.NewGuid()</c>, or
/// <c>RandomNumberGenerator</c> — made anywhere but the single owner class that implements
/// <c>AgentGuard.Abstractions.Contracts.IGuidFactory</c>. The one place randomness enters the codebase is the temporary-file
/// name built from a fresh GUID; it is abstracted behind <c>IGuidFactory</c> (the same way time is abstracted behind
/// <c>TimeProvider</c>) so tests are deterministic. The raw <c>Guid.NewGuid()</c> lives only in the class that
/// implements <c>IGuidFactory</c> — the <c>GuidFactory</c> adapter, which sits in <c>AgentGuard.CrossPlatform</c>
/// (guid-seam-lives-in-crossplatform) — and is a build error even in another class of the same assembly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RandomnessOnlyInBoundariesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0014";

    private const string Category = "AgentGuard.Architecture";

    // The impure Guid-factory method set — every non-deterministic Guid factory, not just NewGuid. .NET 10 added
    // CreateVersion7/CreateVersion1, equally non-deterministic, which a name-only check on "NewGuid" would miss
    // (ag0014-edit-impure-guid-set); a future CreateVersionN added deliberately is never legal by omission.
    // IGuidFactory grows no method for these — they are banned with no owner. Building a Guid from bytes or parsing
    // text stays legal (those names are not in this set).
    private static readonly ImmutableHashSet<string> ImpureGuidFactoryMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "NewGuid",
        "CreateVersion7",
        "CreateVersion1");

    // The owning interface whose single implementing class is the only place a raw randomness call is allowed
    // (one-owner-class-per-primitive). Matched structurally by full name against the enclosing type's implemented
    // interfaces, never by a class-name literal.
    private static readonly ImmutableArray<(string Namespace, string Name)> OwningInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IGuidFactory"));

    // The owner assembly: AgentGuard.CrossPlatform, because the GuidFactory adapter lives there
    // (guid-seam-lives-in-crossplatform) — PlatformFileSystemShared needs a GUID and CrossPlatform cannot receive a
    // service from Boundaries above it. Half of the conjunction OwnerClass.IsOwner applies: implementing IGuidFactory
    // in any OTHER assembly does not exempt. Reuses the shared RootName constant; cached once so no per-operation
    // allocation.
    private static readonly Func<Compilation, bool> InOwnerAssembly =
        OwnerClass.InAssembly(CrossPlatformBoundary.RootName);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Raw randomness must live only in the class implementing IGuidFactory",
        messageFormat: "Raw randomness '{0}' is outside the single owner class implementing IGuidFactory; obtain a GUID through IGuidFactory pulled off ISystemServices so tests stay deterministic",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A use of System.Random, Guid.NewGuid(), or RandomNumberGenerator is allowed only in the single class that implements AgentGuard.Abstractions.Contracts.IGuidFactory — not merely somewhere in its assembly. Every other type obtains a GUID through IGuidFactory on ISystemServices, keeping randomness at one seam and tests deterministic.");

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
        context.RegisterCompilationStartAction(context => MemberUseScanner.Register(context, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        // The single owner class that implements IGuidFactory AND compiles into AgentGuard.CrossPlatform is the only
        // place the raw call is allowed (one-owner-class-per-primitive + owners-live-at-lowest-consumer); everywhere
        // else — a sibling class in the same assembly, or the same class self-granting in another assembly — is RED.
        if (OwnerClass.IsOwner(context, OwningInterfaces, InOwnerAssembly))
        {
            return;
        }

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

        // The impure Guid-factory set (Guid.NewGuid / CreateVersion7 / CreateVersion1) — every non-deterministic
        // factory. Building a GUID from bytes or parsing text stays legal.
        return WellKnownType.Is(type, KnownNamespaces.System, "Guid")
            && ImpureGuidFactoryMethods.Contains(member.Name);
    }
}
