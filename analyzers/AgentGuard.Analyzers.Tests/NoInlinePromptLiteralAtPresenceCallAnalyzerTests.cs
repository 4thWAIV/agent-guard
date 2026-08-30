// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0109: at a production presence call site (a PresenceRequest construction or an IPresenceCheck.Check invocation)
/// the prompt must be resolved through the owned PresenceDialogText accessor keyed by SetupVerb — never baked in at the
/// call as a bare string literal OR as a const/static-readonly string field. A read through the owned accessor, or a
/// runtime value (a parameter/local), is left alone. A test assembly is exempt.
/// </summary>
public class NoInlinePromptLiteralAtPresenceCallAnalyzerTests
{
    // The PresenceRequest stub is the shared owner SharedAnalyzerSources.PresenceContract (spelled once, consumed by
    // the three presence-rule test classes).
    private const string PresenceContract = SharedAnalyzerSources.PresenceContract;

    // The owned dialog-text resource in the gate's AgentGuard.Setup namespace — the single owner PresenceContracts
    // pins on. A prompt read through its keyed accessor (here the For(verb) method) is the ONE sanctioned source, so
    // the analyzer leaves it alone.
    private const string PresenceDialogTextResource = """
        namespace AgentGuard.Setup
        {
            internal static class PresenceDialogText
            {
                internal static string For(string verb) => verb;
            }
        }
        """;

    [Fact]
    public async Task LiteralPromptAtConstruction_InProduction_IsReported()
    {
        string source = PresenceContract + """

            namespace AgentGuard.Setup
            {
                using AgentGuard.Abstractions.Contracts;
                internal static class Gate
                {
                    internal static PresenceRequest Build() => new PresenceRequest("Approve installing the guard?");
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoInlinePromptLiteralAtPresenceCallAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0109", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task ConstFieldPromptAtConstruction_InProduction_IsReported()
    {
        // FIX 2: a const string holding the prompt text dodges the owned resource just as a bare literal does.
        string source = PresenceContract + """

            namespace AgentGuard.Setup
            {
                using AgentGuard.Abstractions.Contracts;
                internal static class Gate
                {
                    private const string Prompt = "Approve installing the guard?";
                    internal static PresenceRequest Build() => new PresenceRequest(Prompt);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoInlinePromptLiteralAtPresenceCallAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0109", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task StaticReadonlyFieldPromptAtConstruction_InProduction_IsReported()
    {
        // FIX 2: a static-readonly string field baked with the prompt text is a dodge too.
        string source = PresenceContract + """

            namespace AgentGuard.Setup
            {
                using AgentGuard.Abstractions.Contracts;
                internal static class Gate
                {
                    private static readonly string Prompt = "Approve installing the guard?";
                    internal static PresenceRequest Build() => new PresenceRequest(Prompt);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoInlinePromptLiteralAtPresenceCallAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0109", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task OwnedAccessorPromptAtConstruction_IsNotReported()
    {
        // FIX 2: the owned-accessor path stays clean — a prompt resolved through PresenceDialogText's keyed accessor is
        // the ONE sanctioned source.
        string source = PresenceContract + PresenceDialogTextResource + """

            namespace AgentGuard.Setup
            {
                using AgentGuard.Abstractions.Contracts;
                internal static class Gate
                {
                    internal static PresenceRequest Build(string verb) => new PresenceRequest(PresenceDialogText.For(verb));
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoInlinePromptLiteralAtPresenceCallAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task ResolvedPromptAtConstruction_IsNotReported()
    {
        // A runtime value (a parameter that may carry the resolved text) is not a baked-in dodge, so it is left alone.
        string source = PresenceContract + """

            namespace AgentGuard.Setup
            {
                using AgentGuard.Abstractions.Contracts;
                internal static class Gate
                {
                    internal static PresenceRequest Build(string resolved) => new PresenceRequest(resolved);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoInlinePromptLiteralAtPresenceCallAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task LiteralPrompt_InTestAssembly_IsNotReported()
    {
        string source = PresenceContract + """

            namespace AgentGuard.Tests
            {
                using AgentGuard.Abstractions.Contracts;
                internal static class Fixture
                {
                    internal static PresenceRequest Build() => new PresenceRequest("any test prompt");
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoInlinePromptLiteralAtPresenceCallAnalyzer>(source, "AgentGuard.Tests"));
    }
}
