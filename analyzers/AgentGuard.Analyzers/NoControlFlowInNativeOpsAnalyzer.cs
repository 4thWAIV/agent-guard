// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports control flow inside a per-OS native-OPS owner — a class implementing <c>IObjCRuntime</c> (macOS assembly) or
/// one of the Windows native-ops interfaces (<c>IWindowsHelloNativeOps</c>/<c>ICredentialPromptNativeOps</c>). The
/// coverage refactor pushes every decision, mapping, and branch INTO the fake-testable orchestrator (the flow port) and
/// leaves the native-ops class thin: a straight-line sequence of raw native calls with, at most, <c>try</c>/<c>finally</c>
/// for native cleanup. Any branching or looping construct — an <c>if</c> or ternary (<c>?:</c>), a <c>for</c>/
/// <c>foreach</c>/<c>while</c>/<c>do</c> loop, a <c>switch</c> statement or expression, a null-coalescing <c>??</c>/
/// <c>??=</c>, a conditional-access <c>?.</c>, a short-circuit <c>&amp;&amp;</c>/<c>||</c> (structurally identical to
/// <c>?:</c> — the right operand is evaluated only conditionally), or a pattern combinator <c>is X and Y</c>/
/// <c>is A or B</c> (the same conditional branch spelled as a pattern instead of an operator) — is testable logic that would hide behind the one class
/// we do not fake, so it is a build error here. A <c>try</c>/<c>catch</c> is exempt (it carries no decision the orchestrator can own; it
/// only keeps a native fault from unwinding), which is why <see cref="OperationKind.Try"/> is not among the reported
/// kinds. This rule constrains only the CONTENTS of a native-ops implementer; the class stays behind its interface and
/// DI-mockable. Preventive; no native-ops owner exists yet, so it compiles clean and its RED is proven by a fixture.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoControlFlowInNativeOpsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0106";

    private const string Category = "AgentGuard.Architecture";

    // The branching and looping operation kinds a native-ops implementer may not contain. OperationKind.Conditional
    // covers both an `if` statement and a ternary `?:`; OperationKind.Loop covers for/foreach/while/do. try/catch
    // (OperationKind.Try) is deliberately absent — it is exempt.
    private static readonly ImmutableArray<OperationKind> ControlFlowKinds = ImmutableArray.Create(
        OperationKind.Conditional,
        OperationKind.Loop,
        OperationKind.Switch,
        OperationKind.SwitchExpression,
        OperationKind.Coalesce,
        OperationKind.CoalesceAssignment,
        OperationKind.ConditionalAccess);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A native-ops implementer must contain no control flow",
        messageFormat: "Control flow '{0}' is inside a native-ops implementer; branching and looping logic belongs in the fake-testable orchestrator (the flow port), not the thin native-ops class (try/catch is the one exempt construct)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class implementing a per-OS native-ops interface (IObjCRuntime, IWindowsHelloNativeOps, ICredentialPromptNativeOps) must be a straight-line sequence of raw native calls with at most try/finally for native cleanup. Any branching or looping construct — an if or ternary, a for/foreach/while/do loop, a switch statement or expression, a null-coalescing ?? or ??=, a conditional-access ?., a short-circuit && or || (structurally identical to ?:), or a pattern combinator (is X and Y / is A or B) — is testable decision logic that must live in the fake-testable orchestrator (the flow port), never behind the one class that is not faked. A try/catch is exempt. The native-ops class stays behind its interface and DI-mockable; this rule constrains only its implementation contents.");

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
        // The native-ops owners live only in the macOS and Windows per-OS assemblies; the shared gate-then-register
        // helper (owned once next to PresenceContracts.InMacOsOrWindows) skips every other compilation.
        PresenceContracts.RegisterInMacOsOrWindows(context, Register);
    }

    private static void Register(CompilationStartAnalysisContext context)
    {
        context.RegisterOperationAction(Inspect, ControlFlowKinds);
        context.RegisterOperationAction(InspectBinary, OperationKind.Binary);
        context.RegisterOperationAction(InspectBinaryPattern, OperationKind.BinaryPattern);
    }

    private static void Inspect(OperationAnalysisContext context)
    {
        NativeOpsOperationScan.ReportIfOwner(context, Rule, Describe(context.Operation.Kind));
    }

    private static void InspectBinary(OperationAnalysisContext context)
    {
        var binary = (IBinaryOperation)context.Operation;

        // Only the short-circuit conditional operators are branching control flow: `&&`/`||` are structurally identical
        // to `?:` (the right operand is evaluated only conditionally), so conditional branching under a different operator
        // spelling is the same testable decision logic. Arithmetic, relational, and bitwise binary operators carry no
        // branch and are left alone. OperationKind.Binary covers every binary operator, so the operator kind is filtered
        // here rather than by a coarser registered kind.
        if (binary.OperatorKind != BinaryOperatorKind.ConditionalAnd
            && binary.OperatorKind != BinaryOperatorKind.ConditionalOr)
        {
            return;
        }

        NativeOpsOperationScan.ReportIfOwner(context, Rule, DescribeBinary(binary.OperatorKind));
    }

    private static void InspectBinaryPattern(OperationAnalysisContext context)
    {
        var pattern = (IBinaryPatternOperation)context.Operation;

        // A pattern combinator (`is X and Y`, `is A or B`) is the same conditional branch as `&&`/`||`, only spelled as a
        // pattern instead of an operator: the disjunction/conjunction is evaluated as a decision over the input. Catching
        // only the operator spelling would let a native-ops implementer hide the identical decision logic behind a
        // pattern combinator. A plain non-combinator pattern (`x is X`) carries no And/Or branch, so the operator kind is
        // filtered here — mirroring InspectBinary — and only And/Or is reported. `not` (a unary pattern) is not a binary
        // combinator and never reaches this action.
        if (pattern.OperatorKind != BinaryOperatorKind.And
            && pattern.OperatorKind != BinaryOperatorKind.Or)
        {
            return;
        }

        NativeOpsOperationScan.ReportIfOwner(context, Rule, DescribeBinaryPattern(pattern.OperatorKind));
    }

    private static string DescribeBinary(BinaryOperatorKind kind)
    {
        return kind == BinaryOperatorKind.ConditionalAnd ? "&&" : "||";
    }

    private static string DescribeBinaryPattern(BinaryOperatorKind kind)
    {
        return kind == BinaryOperatorKind.And ? "pattern and" : "pattern or";
    }

    private static string Describe(OperationKind kind)
    {
        return kind switch
        {
            OperationKind.Conditional => "if / ?:",
            OperationKind.Loop => "loop",
            OperationKind.Switch => "switch",
            OperationKind.SwitchExpression => "switch expression",
            OperationKind.Coalesce => "??",
            OperationKind.CoalesceAssignment => "??=",
            OperationKind.ConditionalAccess => "?.",
            _ => kind.ToString(),
        };
    }
}
