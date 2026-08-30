// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a compile-time string constant whose VALUE names a polkit authentication-agent registration or spawn —
/// <c>"org.freedesktop.PolicyKit1.AuthenticationAgent"</c>, <c>"RegisterAuthenticationAgent"</c>,
/// <c>"RegisterAuthenticationAgentWithOptions"</c>, or <c>"pkttyagent"</c> — anywhere in production source
/// (<c>src/**</c>). The guard uses only an authentication agent already registered by the trusted login session; it
/// must never create, spawn, or register one (no-self-answerable-agent), because a self-registered agent could answer
/// its own presence prompt. This matches by the literal or <c>const</c> VALUE — a genuinely new shape: AG0035 matches
/// by PARAMETER name and never inspects a value, and no existing rule compares a constant's value against a banned set.
/// It scopes to exact literal/const values, not a substring in unrelated text. The process-spawn half is also covered
/// by the live <c>Process</c> ban; acceptance's <c>grep</c> is the independent backstop. Preventive; nothing names
/// these today.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoAuthenticationAgentRegistrationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0111";

    private const string Category = "AgentGuard.Architecture";

    // The exact banned VALUES — the polkit authentication-agent D-Bus interface, the two register calls, and the
    // agent-spawning helper. Matched by whole-string equality against a string constant's value, never a substring.
    private static readonly ImmutableHashSet<string> BannedValues = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "org.freedesktop.PolicyKit1.AuthenticationAgent",
        "RegisterAuthenticationAgent",
        "RegisterAuthenticationAgentWithOptions",
        "pkttyagent");

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "The guard must never register or spawn a polkit authentication agent",
        messageFormat: "String constant '{0}' names a polkit authentication-agent registration or spawn; the guard uses only the login session's already-registered agent and must never create, register, or spawn one (no-self-answerable-agent)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A compile-time string constant whose value is org.freedesktop.PolicyKit1.AuthenticationAgent, RegisterAuthenticationAgent, RegisterAuthenticationAgentWithOptions, or pkttyagent is a build error in production source. The guard relies only on an authentication agent already registered by the trusted login session; registering or spawning its own agent would let it answer its own presence prompt. Matched by the literal or const value, scoped to exact values.");

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
        // Production only (src/**). A test may name these values in a fixture without shipping them.
        if (TestAssembly.IsTestSystemAssembly(context.Compilation))
        {
            return;
        }

        // A written string literal (including a const declaration's initializer) is caught here.
        context.RegisterOperationAction(AnalyzeLiteral, OperationKind.Literal);

        // A reference to a const declared elsewhere (whose initializer literal is not in this compilation) is caught
        // through the field reference's folded constant value.
        context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
    }

    private static void AnalyzeLiteral(OperationAnalysisContext context)
    {
        var literal = (ILiteralOperation)context.Operation;
        ReportIfBanned(context, literal.ConstantValue, literal.Syntax.GetLocation());
    }

    private static void AnalyzeFieldReference(OperationAnalysisContext context)
    {
        var reference = (IFieldReferenceOperation)context.Operation;
        if (reference.Field.IsConst)
        {
            ReportIfBanned(context, reference.ConstantValue, reference.Syntax.GetLocation());
        }
    }

    private static void ReportIfBanned(OperationAnalysisContext context, Optional<object?> constant, Location location)
    {
        if (constant.HasValue && constant.Value is string value && BannedValues.Contains(value))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, value));
        }
    }
}
