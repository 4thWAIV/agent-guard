// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions.Contracts;

/// <summary>
/// A language plugin that bundles a set of default Rules and the Verifiers those Rules use. Providers are
/// compiled in for v1; C#, TypeScript, and Rust ship to start, with Python following.
/// </summary>
public interface IProvider
{
    /// <summary>
    /// Gets the identifier of the language this Provider governs.
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Gets every Rule this Provider ships. Each Rule carries the Verifier it uses, so the Verifiers are not
    /// exposed separately.
    /// </summary>
    IReadOnlyList<Rule> Rules { get; }

    /// <summary>
    /// Gets the Provider's opt-in recommended default subset of its <see cref="Rules"/>, applied as a template
    /// when the Provider is enabled — the way a lint "recommended" set works.
    /// </summary>
    Baseline Baseline { get; }
}
