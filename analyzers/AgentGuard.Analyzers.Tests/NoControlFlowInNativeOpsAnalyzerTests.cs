// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0106 (no-control-flow-in-native-ops): a class implementing a per-OS native-ops interface (IObjCRuntime and the two
/// Windows native-ops interfaces) must be straight-line raw calls with at most try/catch for cleanup — no if/ternary,
/// loop, switch, null-coalescing, or conditional-access. Branching logic belongs in the fake-testable orchestrator. The
/// native-ops owner interface stub is the shared owner SharedAnalyzerSources.ObjCRuntimeNativeOps; the implementer sits
/// in a second MacOS namespace block. Preventive against production; its RED is proven here by a violating fixture.
/// </summary>
public class NoControlFlowInNativeOpsAnalyzerTests
{
    private const string NativeOps = SharedAnalyzerSources.ObjCRuntimeNativeOps;

    [Fact]
    public async Task IfStatementInNativeOps_IsReported()
    {
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Pick(bool flag)
                    {
                        if (flag) { return 1; }
                        return 0;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task TernaryInNativeOps_IsReported()
    {
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Pick(bool flag) => flag ? 1 : 0;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task ConditionalAndInNativeOps_IsReported()
    {
        // `&&`/`||` are structurally identical short-circuit branches to `?:` (the right operand is evaluated only
        // conditionally), so conditional branching under a different operator spelling is the same testable decision
        // logic. This is the SOLID-3 gap the fix closes.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal bool Both(bool a, bool b) => a && b;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task ConditionalOrInNativeOps_IsReported()
    {
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal bool Either(bool a, bool b) => a || b;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task NonConditionalBinaryInNativeOps_IsNotReported()
    {
        // A non-short-circuit binary operator carries no branch: `&` (bitwise/logical AND) evaluates both operands
        // unconditionally, so it is not control flow and is left alone. This pins the operator-kind filter so the binary
        // action does not over-report arithmetic/relational/bitwise operators.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal bool Both(bool a, bool b) => a & b;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task PatternAndCombinatorInNativeOps_IsReported()
    {
        // A pattern combinator (`is X and Y` / `is A or B`) is the same conditional branch as `&&`/`||`, only spelled as
        // a pattern instead of an operator — the And/Or disjunction is evaluated as a decision over the input. Catching
        // only the operator spelling would let the identical decision logic hide behind a pattern. This is the `and`
        // spelling (BinaryPattern And).
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal bool InRange(int code) => code is 1 and 2;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task PatternOrCombinatorInNativeOps_IsReported()
    {
        // The `or` spelling of a pattern combinator (BinaryPattern Or) — the disjunction is a decision over the input,
        // the same conditional branch as `||`, pinned separately from the `and` spelling above.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal bool InSet(int code) => code is 1 or 2;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task NonCombinatorPatternInNativeOps_IsNotReported()
    {
        // A plain non-combinator pattern (`x is X`) carries no And/Or branch: it is a single match, not a decision over
        // a disjunction/conjunction. This pins the operator-kind filter on the pattern action so it does not over-report
        // ordinary type-test patterns.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal bool IsText(object value) => value is string;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task LoopInNativeOps_IsReported()
    {
        // foreach — one of the four loop spellings, all of which are OperationKind.Loop.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Sum(int[] values)
                    {
                        int total = 0;
                        foreach (int value in values) { total += value; }
                        return total;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task ForLoopInNativeOps_IsReported()
    {
        // The `for` spelling of a loop — also OperationKind.Loop, pinned independently so no single loop spelling slips.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Count(int n)
                    {
                        int total = 0;
                        for (int i = 0; i < n; i++) { total += i; }
                        return total;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task WhileLoopInNativeOps_IsReported()
    {
        // The `while` spelling of a loop — also OperationKind.Loop.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Count(int n)
                    {
                        int total = 0;
                        while (total < n) { total++; }
                        return total;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task DoLoopInNativeOps_IsReported()
    {
        // The `do` spelling of a loop — also OperationKind.Loop.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Count(int n)
                    {
                        int total = 0;
                        do { total++; } while (total < n);
                        return total;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task SwitchStatementInNativeOps_IsReported()
    {
        // A switch STATEMENT is OperationKind.Switch (distinct from the switch expression below, OperationKind.
        // SwitchExpression) — both spellings are named control-flow categories and each is pinned by its own fixture.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Map(int code)
                    {
                        switch (code)
                        {
                            case 0: return 1;
                            default: return 2;
                        }
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task SwitchExpressionInNativeOps_IsReported()
    {
        // A switch EXPRESSION is OperationKind.SwitchExpression — the second switch spelling, pinned separately from the
        // switch statement above.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Map(int code) => code switch { 0 => 1, _ => 2 };
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task NullCoalescingInNativeOps_IsReported()
    {
        // `??` is OperationKind.Coalesce.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal string Text(string? value) => value ?? "";
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task NullCoalescingAssignmentInNativeOps_IsReported()
    {
        // `??=` is OperationKind.CoalesceAssignment — the assignment spelling of null-coalescing, pinned separately.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal string Text(string? value)
                    {
                        value ??= "";
                        return value;
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task ConditionalAccessInNativeOps_IsReported()
    {
        // A null-conditional `?.` / `?[]` is OperationKind.ConditionalAccess — the right side is evaluated only when the
        // receiver is non-null, so it is a conditional branch and a named control-flow category.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int? Length(string? value) => value?.Length;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0106", diagnostic.Id);
    }

    [Fact]
    public async Task TryCatchInNativeOps_IsNotReported()
    {
        // try/catch is the ONE exempt construct — it carries no decision the orchestrator can own, it only keeps a native
        // fault from unwinding. A native-ops owner may guard its raw calls without tripping the rule.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal void Run(Action native)
                    {
                        try { native(); }
                        catch (Exception) { }
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task StraightLineInNativeOps_IsNotReported()
    {
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Add(int left, int right) => left + right;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task ControlFlowOutsideNativeOps_IsNotReported()
    {
        // A class that does NOT implement a native-ops interface is not this rule's concern — control flow is normal there.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class Ordinary
                {
                    internal int Pick(bool flag) => flag ? 1 : 0;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task NativeOpsInterfaceInWrongAssembly_IsNotReported()
    {
        // The native-ops owner is exempt from AG0113/AG0101 only in its per-OS assembly; symmetrically the guardrails
        // scope to that assembly. A class implementing IObjCRuntime compiled into a non-macOS assembly is not matched
        // (the interface is internal to the macOS assembly in production; this proves the assembly gate).
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int Pick(bool flag) => flag ? 1 : 0;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoControlFlowInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
    }
}
