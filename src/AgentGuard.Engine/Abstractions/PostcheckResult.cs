// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// What a Postcheck returns: a <see cref="Verdict"/> plus the ordered <see cref="Effect"/>s the Engine must
/// execute. This is a named type rather than a tuple because tuple return types are disallowed (AG0002).
/// </summary>
/// <param name="Verdict">The aggregate judgment for the call.</param>
/// <param name="Effects">The side effects, in order, for the Engine to carry out.</param>
public sealed record PostcheckResult(Verdict Verdict, IReadOnlyList<Effect> Effects)
{
    /// <summary>
    /// Creates a result that allows the call and requests no effects.
    /// </summary>
    /// <returns>An allow result with an empty effect list.</returns>
    public static PostcheckResult Allow() => new(Verdict.Allow(), Array.Empty<Effect>());
}
