// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Finds the argument bound to a named parameter of an invocation and answers whether the caller
/// wrote a compile-time literal there. This is the one home for the seed-site inspection the two literal-seed rules
/// share: AG0035 (no literal fake root at <c>FakeEnvironment.Create</c>/the overlay's <c>tempRoot</c>) and AG0036
/// (no literal case-mode at the overlay's <c>caseSensitive</c>) both match a target member, then ask this helper for
/// the named argument the caller actually passed.
/// </summary>
internal static class SeedArgument
{
    /// <summary>
    /// Gets the arguments of an invocation, or an empty array for any other operation. Both seed sites the literal-seed
    /// rules check — the <c>FakeEnvironment.Create</c> invocation and the <c>NewOverlay</c> factory invocation — are
    /// invocations, read through this one accessor.
    /// </summary>
    /// <param name="operation">The operation whose arguments are read.</param>
    /// <returns>The operation's arguments, or an empty array when it is not an invocation.</returns>
    internal static ImmutableArray<IArgumentOperation> Of(IOperation operation)
    {
        return operation switch
        {
            IInvocationOperation invocation => invocation.Arguments,
            _ => ImmutableArray<IArgumentOperation>.Empty,
        };
    }

    /// <summary>
    /// Returns the argument the caller EXPLICITLY passed for the parameter named <paramref name="parameterName"/>
    /// when its value is a compile-time constant — a string/bool literal OR a reference to a <c>const</c> — or
    /// <see langword="null"/> otherwise. An omitted optional argument is skipped
    /// (<see cref="ArgumentKind.DefaultValue"/>), so a defaulted <c>currentDirectory</c>/<c>tempDirectory</c> is
    /// never mistaken for a caller-written seed; and a runtime value (a method call, a local, a field read that is
    /// not <c>const</c>) has no <see cref="Optional{T}.HasValue"/> constant and is left alone. That is exactly the
    /// "a real-host or abstraction-derived value, never a literal" line the seed-site rules draw.
    /// </summary>
    /// <param name="arguments">The operation's arguments.</param>
    /// <param name="parameterName">The parameter name whose argument is inspected.</param>
    /// <returns>The literal argument, or <see langword="null"/> when the parameter was omitted or given a
    /// non-constant value.</returns>
    internal static IArgumentOperation? ExplicitLiteral(
        ImmutableArray<IArgumentOperation> arguments, string parameterName)
    {
        foreach (IArgumentOperation argument in arguments)
        {
            if (argument.ArgumentKind == ArgumentKind.Explicit
                && string.Equals(argument.Parameter?.Name, parameterName, StringComparison.Ordinal)
                && argument.Value.ConstantValue.HasValue)
            {
                return argument;
            }
        }

        return null;
    }

    /// <summary>
    /// Reports <paramref name="rule"/> at the argument the caller EXPLICITLY passed for the parameter named
    /// <paramref name="parameterName"/> when that argument is a compile-time literal, and does nothing when it was
    /// omitted or given a runtime value. This is the one report path both literal-seed rules share — AG0035 (fake root:
    /// <c>home</c>/<c>currentDirectory</c>/<c>tempDirectory</c> and the overlay's <c>tempRoot</c>) and AG0036 (case
    /// mode: the overlay's <c>caseSensitive</c>) — after each has matched its target member. The diagnostic is placed
    /// on the argument's syntax and carries <paramref name="messageArgs"/>, so a rule whose message names the offending
    /// parameter (AG0035) passes it through and a rule whose message is fixed (AG0036) passes none.
    /// </summary>
    /// <param name="context">The operation analysis context whose operation is inspected and on which the diagnostic
    /// is reported.</param>
    /// <param name="rule">The descriptor to report when a literal is found.</param>
    /// <param name="parameterName">The parameter name whose argument is inspected.</param>
    /// <param name="messageArgs">The values that fill the rule's message format placeholders, if any.</param>
    internal static void ReportIfLiteral(
        OperationAnalysisContext context, DiagnosticDescriptor rule, string parameterName, params object[] messageArgs)
    {
        IArgumentOperation? argument = ExplicitLiteral(Of(context.Operation), parameterName);
        if (argument is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(rule, argument.Syntax.GetLocation(), messageArgs));
        }
    }
}
