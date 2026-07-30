// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The C# language Provider. Its Baseline protects every instance of the build-config files that are rarely
/// edited and whose silent change would weaken the golden build: <c>Directory.Build.props</c>,
/// <c>*.globalconfig</c>, <c>.editorconfig</c>, <c>stylecop.json</c>, and <c>global.json</c>. It deliberately
/// does not cover <c>.csproj</c>, <c>.sln</c>, or <c>Directory.Packages.props</c>, which a later build scanner
/// governs. Every Rule uses the default no-change Verifier.
/// </summary>
internal sealed class CSharpProvider : IProvider
{
    private readonly Baseline _baseline;

    private CSharpProvider(Baseline baseline) => _baseline = baseline;

    /// <inheritdoc />
    public string Language => "csharp";

    /// <inheritdoc />
    public IReadOnlyList<Rule> Rules => _baseline.Rules;

    /// <inheritdoc />
    public Baseline Baseline => _baseline;

    /// <summary>
    /// Creates the C# Provider, building its Rules from the shared no-change Verifier.
    /// </summary>
    /// <returns>The Provider, as its interface.</returns>
    internal static IProvider Create()
    {
        IVerifier verifier = NoChangeVerifier.Create();
        var rules = new List<Rule>
        {
            new("csharp:Directory.Build.props", RuleOrigin.Provider, FileNameMatcher.Create("Directory.Build.props"), verifier),
            new("csharp:*.globalconfig", RuleOrigin.Provider, FileExtensionMatcher.Create(".globalconfig"), verifier),
            new("csharp:.editorconfig", RuleOrigin.Provider, FileNameMatcher.Create(".editorconfig"), verifier),
            new("csharp:stylecop.json", RuleOrigin.Provider, FileNameMatcher.Create("stylecop.json"), verifier),
            new("csharp:global.json", RuleOrigin.Provider, FileNameMatcher.Create("global.json"), verifier),
        };
        return new CSharpProvider(new Baseline("csharp-recommended", rules));
    }
}
