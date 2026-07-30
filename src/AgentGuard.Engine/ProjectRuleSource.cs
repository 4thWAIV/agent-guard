// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The repository's own configurable protected paths, read from an optional <c>.agentguard/protected-paths.json</c>
/// (a JSON array of patterns). A missing file contributes no Rules; a malformed file throws, so composition
/// fails closed rather than silently protecting nothing. Each pattern is a bare file name, a <c>*.ext</c>
/// suffix, or a repo-relative path.
/// </summary>
internal sealed class ProjectRuleSource : IRuleSource
{
    private const string ConfigRelative = ".agentguard/protected-paths.json";

    private readonly IReadOnlyList<Rule> _rules;

    private ProjectRuleSource(IReadOnlyList<Rule> rules) => _rules = rules;

    /// <inheritdoc />
    public RuleOrigin Origin => RuleOrigin.Project;

    /// <inheritdoc />
    public IReadOnlyList<Rule> Rules => _rules;

    /// <summary>
    /// Creates the Project rule source, loading patterns from the optional project config file.
    /// </summary>
    /// <param name="canonicalizer">The canonicalizer used to resolve path patterns.</param>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <returns>The rule source, as its interface.</returns>
    internal static IRuleSource Create(IPathCanonicalizer canonicalizer, string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        string configPath = CoreSystemPaths.Absolute(projectRoot, ConfigRelative);
        var rules = new List<Rule>();
        if (!File.Exists(configPath))
        {
            return new ProjectRuleSource(rules);
        }

        string text = File.ReadAllText(configPath);
        List<string>? patterns = JsonSerializer.Deserialize<List<string>>(text)
            ?? throw new JsonException("Project protected-paths config deserialized to null.");
        IVerifier verifier = NoChangeVerifier.Create();
        foreach (string pattern in patterns)
        {
            rules.Add(new Rule(
                $"project:{pattern}",
                RuleOrigin.Project,
                PatternMatcherFactory.Create(canonicalizer, projectRoot, pattern),
                verifier));
        }

        return new ProjectRuleSource(rules);
    }
}
