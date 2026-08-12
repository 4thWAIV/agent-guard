// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw BouncyCastle Ed25519 use — <c>Ed25519Signer</c> or <c>Ed25519PublicKeyParameters</c> — made
/// anywhere but the single owner class that implements <c>AgentGuard.Abstractions.Contracts.ISignatureVerifier</c>. Grant
/// signature verification is a stateful sequence (<c>Init</c>/<c>BlockUpdate</c>/<c>VerifySignature</c>), so it is
/// not input-deterministic and cannot be a pure static call; it is hidden behind a purpose-built interface — not a
/// 1:1 wrapper of the library — so no BouncyCastle type crosses the boundary and the fail-closed guarantee lives in
/// the one owner (signature-verify-behind-isignatureverifier). The owner is the class implementing
/// <c>ISignatureVerifier</c> AND compiled into <c>AgentGuard.Boundaries</c> (BouncyCastle is managed and OS-uniform,
/// and its only consumer, <c>GrantStore</c>, sits above both layers). Every other type verifies through
/// <c>ISignatureVerifier</c> pulled off <c>ISystemServices</c>. <c>SHA256.HashData</c> is a pure, input-deterministic
/// function of its bytes, so it is not claimed here.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CryptoOnlyInSignatureVerifierAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0021";

    private const string Category = "AgentGuard.Architecture";
    private const string BouncyCastleSignersNamespace = "Org.BouncyCastle.Crypto.Signers";
    private const string BouncyCastleParametersNamespace = "Org.BouncyCastle.Crypto.Parameters";

    // The owning interface whose single implementing class is the only place a raw BouncyCastle Ed25519 use is allowed
    // (one-owner-class-per-primitive). Matched structurally by full name against the enclosing type's implemented
    // interfaces, never by a class-name literal.
    private static readonly ImmutableArray<(string Namespace, string Name)> OwningInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractionsContracts, "ISignatureVerifier"));

    // The banned BouncyCastle Ed25519 types, matched by full name (namespace + name) so a same-named type in another
    // namespace is not caught. The Ed25519Signer sequence and the Ed25519PublicKeyParameters both hide inside the
    // owner; no BouncyCastle type crosses ISignatureVerifier.
    private static readonly ImmutableArray<(string Namespace, string Name)> BannedTypes = ImmutableArray.Create(
        (BouncyCastleSignersNamespace, "Ed25519Signer"),
        (BouncyCastleParametersNamespace, "Ed25519PublicKeyParameters"));

    // The owner assembly: AgentGuard.Boundaries, where the ISignatureVerifier owner lives — BouncyCastle is managed
    // and OS-uniform and the only consumer sits above both layers, so nothing pulls it into CrossPlatform
    // (owners-live-at-lowest-consumer). Half of the conjunction OwnerClass.IsOwner applies: implementing
    // ISignatureVerifier in any OTHER assembly does not exempt. Cached once so no per-operation allocation.
    private static readonly Func<Compilation, bool> InOwnerAssembly =
        OwnerClass.InAssembly(BoundaryAssembly.Name);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Raw BouncyCastle Ed25519 must live only in the class implementing ISignatureVerifier",
        messageFormat: "Raw BouncyCastle Ed25519 use '{0}' is outside the single owner class implementing ISignatureVerifier; verify signatures through ISignatureVerifier pulled off ISystemServices",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A use of the BouncyCastle Ed25519Signer or Ed25519PublicKeyParameters is allowed only in the single class that implements AgentGuard.Abstractions.Contracts.ISignatureVerifier — not merely somewhere in its assembly. Every other type verifies grant signatures through ISignatureVerifier on ISystemServices, so no BouncyCastle type crosses the boundary and the fail-closed guarantee lives in the one owner. SHA256.HashData is pure and stays legal.");

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
        // The single owner class that implements ISignatureVerifier AND compiles into AgentGuard.Boundaries is the
        // only place the raw BouncyCastle use is allowed; everywhere else — a sibling class in the same assembly, or
        // the same class self-granting in another assembly — is RED.
        if (OwnerClass.IsOwner(context, OwningInterfaces, InOwnerAssembly))
        {
            return;
        }

        if (WellKnownType.IsAnyOf(type, BannedTypes))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }
}
