// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a production invocation of <c>IPresenceCheck.Check</c> made anywhere but the approval gate
/// (<c>AgentGuard.Setup.ApprovalGate</c>). The gate is the single place a presence check is raced against the 60-second
/// timeout, its reason mapped to a CLI line, and the fail-closed policy applied; a command, handler, or other type
/// calling <c>Check</c> directly would bypass the timeout and the reason handling. In a production assembly
/// (<c>src/**</c>) the only legal caller is the gate. A test assembly (<c>*.Tests</c>) is exempt: the per-OS native
/// smoke in <c>AgentGuard.CrossPlatform.Tests</c> calls the real <c>Check</c> directly with its own test prompt to
/// prove the boundary returns a non-approved reason on a headless runner. Preventive; <c>IPresenceCheck</c> does not
/// exist yet.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PresenceCheckOnlyFromApprovalGateAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0108";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "IPresenceCheck.Check may be invoked only from the approval gate in production",
        messageFormat: "IPresenceCheck.Check is invoked outside the approval gate; only AgentGuard.Setup.ApprovalGate may call Check in production, so the 60-second timeout and reason handling are never bypassed",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In a production assembly (src/**) IPresenceCheck.Check may be invoked only from AgentGuard.Setup.ApprovalGate, the single place the check is raced against the 60-second fail-closed timeout and its reason mapped to a CLI line. A test assembly (*.Tests) is exempt so the per-OS native smoke can call the real Check directly with its own prompt.");

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
        // Production only. A test assembly (or the test-helpers assembly) is exempt: the CrossPlatform.Tests native
        // smoke calls the real Check directly.
        if (TestAssembly.IsTestSystemAssembly(context.Compilation))
        {
            return;
        }

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol target = invocation.TargetMethod;

        // The Check operation on IPresenceCheck — the shared recognizer matches by the interface identity and the method
        // name, so a same-named Check on an unrelated type is not caught.
        if (!PresenceContracts.IsPresenceCheckInvocation(target, target.ContainingType))
        {
            return;
        }

        // The one legal caller: the approval gate.
        INamedTypeSymbol? enclosing = OwnerClass.EnclosingType(context.ContainingSymbol);
        if (WellKnownType.Is(enclosing, PresenceContracts.ApprovalGateNamespace, PresenceContracts.ApprovalGateName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation()));
    }
}
