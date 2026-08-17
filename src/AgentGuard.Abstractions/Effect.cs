// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// A privileged side effect a Guard requests and only the Engine executes, so every protected-path write
/// stays in one audited place. It is a closed set — only the effects declared in this assembly exist — so the
/// Engine's effect executor can be exhaustive and no Guard can introduce an effect it does not handle.
/// </summary>
public abstract record Effect
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Effect"/> class. Declared <c>private protected</c> so the
    /// set of effects stays closed to this assembly.
    /// </summary>
    /// <param name="path">The absolute path the effect acts on.</param>
    private protected Effect(string path) => this.Path = path;

    /// <summary>
    /// Gets the absolute path the effect acts on.
    /// </summary>
    public string Path { get; }
}
