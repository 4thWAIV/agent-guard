// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// A single protected file's difference between its pre-call snapshot and what landed on disk, handed to a
/// Verifier so it can judge Conformance. It is a closed set of exactly three cases — created, modified, removed
/// — so an illegal combination (a removal that still has "after" bytes) cannot be constructed and a Verifier
/// cannot read bytes that do not exist for the case it is looking at.
/// </summary>
public abstract record FileChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileChange"/> class. Declared <c>private protected</c> so
    /// the set of cases stays closed to this assembly.
    /// </summary>
    /// <param name="path">The absolute path of the changed file.</param>
    private protected FileChange(string path) => this.Path = path;

    /// <summary>
    /// Gets the absolute path of the changed file.
    /// </summary>
    public string Path { get; }
}
