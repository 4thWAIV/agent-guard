// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single recognizer for "does this argument value come from the ONE owned accessor, or is it a dodge" — the
/// shape the owned-resource rules (AG0109, the inline-prompt ban) key off, extracted here (LESSON 1, DRY) as reusable
/// boundary boilerplate so future literal-vs-owned-source rules answer the same question against their OWN owned
/// accessor rather than re-spelling the literal/const/field detection. A sanctioned value comes from an owned accessor
/// (an <see cref="IInvocationOperation"/> or <see cref="IPropertyReferenceOperation"/> on one of the owned resource
/// types — a keyed accessor such as the <c>SetupVerb</c>-keyed <c>PresenceDialogText</c>); a dodge is a text baked in
/// at the call — a bare string literal, or a reference to a <c>const</c> / <c>static readonly</c> string field — which
/// bypasses the owned resource. A runtime value (a parameter, a local, or another call) is neither: it may carry the
/// resolved value at runtime, so it is left alone, exactly as the pre-fix rule left every non-literal alone. Which
/// owned accessor and which call site a rule cares about is the caller's policy, applied on top; the recognition of
/// "owned accessor vs baked-in constant text" lives here once.
/// </summary>
internal static class OwnedSourceArgument
{
    /// <summary>
    /// Gets a value indicating whether <paramref name="value"/> is read from one of the owned accessors — an
    /// invocation or property/indexer reference whose declaring type is one of <paramref name="ownedAccessorTypes"/>.
    /// This is the POSITIVE, sanctioned shape: the prompt (or other reviewed text) resolved from its owned resource.
    /// Kept separate from <see cref="IsInlineConstantText"/> so an owned accessor is never mistaken for a dodge even
    /// when the owned member is itself exposed as a constant-like read.
    /// </summary>
    /// <param name="value">The argument value operation.</param>
    /// <param name="ownedAccessorTypes">The (namespace, name) pairs of the owned resource types whose accessor is sanctioned.</param>
    /// <returns><see langword="true"/> when the value comes from an owned accessor.</returns>
    internal static bool IsFromOwnedAccessor(
        IOperation value, ImmutableArray<(string Namespace, string Name)> ownedAccessorTypes)
    {
        IOperation unwrapped = Unwrap(value);
        return unwrapped switch
        {
            IInvocationOperation invocation =>
                WellKnownType.IsAnyOf(invocation.TargetMethod.ContainingType, ownedAccessorTypes),
            IPropertyReferenceOperation property =>
                WellKnownType.IsAnyOf(property.Property.ContainingType, ownedAccessorTypes),
            _ => false,
        };
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="value"/> is a baked-in constant string source — a string
    /// literal, or a reference to a <c>const</c> or <c>static readonly</c> string field. These are the dodge shapes:
    /// text fixed at (or effectively at) compile time rather than resolved at runtime from the owned resource. A
    /// runtime value — a parameter, a local, an instance field, or another method call — is not constant text and is
    /// not caught here.
    /// </summary>
    /// <param name="value">The argument value operation.</param>
    /// <returns><see langword="true"/> when the value is a literal or a const/static-readonly string field reference.</returns>
    internal static bool IsInlineConstantText(IOperation value)
    {
        IOperation unwrapped = Unwrap(value);
        if (unwrapped is ILiteralOperation { ConstantValue.HasValue: true } literal
            && literal.ConstantValue.Value is string)
        {
            return true;
        }

        return unwrapped is IFieldReferenceOperation { Field: { } field }
            && field.Type.SpecialType == SpecialType.System_String
            && (field.IsConst || (field.IsStatic && field.IsReadOnly));
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="value"/> is a dodge of the owned-source requirement — baked-in
    /// constant text (<see cref="IsInlineConstantText"/>) that does NOT come from an owned accessor
    /// (<see cref="IsFromOwnedAccessor"/>). This is the composite the owned-resource rules report on: an owned-accessor
    /// read is clean, a runtime value is clean, and only a literal / const / static-readonly-string field written at
    /// the call is a dodge.
    /// </summary>
    /// <param name="value">The argument value operation.</param>
    /// <param name="ownedAccessorTypes">The (namespace, name) pairs of the owned resource types whose accessor is sanctioned.</param>
    /// <returns><see langword="true"/> when the value is a baked-in constant text that is not an owned-accessor read.</returns>
    internal static bool IsDodge(
        IOperation value, ImmutableArray<(string Namespace, string Name)> ownedAccessorTypes)
    {
        return !IsFromOwnedAccessor(value, ownedAccessorTypes) && IsInlineConstantText(value);
    }

    // Peels implicit/explicit conversions off an argument value so a conversion wrapped around the literal or the
    // owned-accessor read does not hide it. Parentheses are transparent in the C# operation tree, so only conversions
    // need peeling.
    private static IOperation Unwrap(IOperation value)
    {
        IOperation current = value;
        while (current is IConversionOperation conversion)
        {
            current = conversion.Operand;
        }

        return current;
    }
}
