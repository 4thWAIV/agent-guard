// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0026 (os-skip-ban-rule): in a test compilation, a type deriving from xUnit's FactAttribute/TheoryAttribute is a
/// build error — that custom-attribute subclass is exactly how an OS-conditional skip is reintroduced under a new name
/// (the deleted PosixOnlyFactAttribute). The xUnit bases live in a REFERENCED assembly here, as they do in reality, so
/// xUnit's own TheoryAttribute : FactAttribute (metadata, not source) is never flagged and only a source-declared
/// subclass fires.
/// </summary>
public class NoOsSkipInTestsAnalyzerTests
{
    // Stand-ins for xUnit's attributes in a separate reference assembly (like the real xunit.core), so they are
    // metadata — the analyzer visits only source types, so these are not flagged themselves.
    private const string XunitReference = """
        namespace Xunit
        {
            public class FactAttribute : System.Attribute { }
            public class TheoryAttribute : FactAttribute { }
        }
        """;

    [Fact]
    public async Task FactSubclass_InTestAssembly_IsReported()
    {
        // The PosixOnlyFactAttribute shape: a custom attribute deriving from FactAttribute. RED in a test compilation.
        const string source = """
            namespace App
            {
                public sealed class PosixOnlyFactAttribute : Xunit.FactAttribute { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunWithReferenceAsync<NoOsSkipInTestsAnalyzer>(
            source, "AgentGuard.Cli.Tests", XunitReference, "xunit.core"));
        Assert.Equal("AG0026", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("PosixOnlyFactAttribute", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheorySubclass_InTestAssembly_IsReported()
    {
        // A custom Theory attribute is caught too (TheoryAttribute derives from FactAttribute; either base matches).
        const string source = """
            namespace App
            {
                public sealed class OsTheoryAttribute : Xunit.TheoryAttribute { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunWithReferenceAsync<NoOsSkipInTestsAnalyzer>(
            source, "AgentGuard.Cli.Tests", XunitReference, "xunit.core"));
        Assert.Equal("AG0026", diagnostic.Id);
        Assert.Contains("OsTheoryAttribute", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TransitiveFactSubclass_IsReported()
    {
        // A subclass any number of levels above FactAttribute is caught — the base chain is walked. The intermediate and
        // the leaf both derive from FactAttribute, so both fire.
        const string source = """
            namespace App
            {
                public class MidFactAttribute : Xunit.FactAttribute { }
                public sealed class LeafFactAttribute : MidFactAttribute { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunWithReferenceAsync<NoOsSkipInTestsAnalyzer>(
            source, "AgentGuard.Cli.Tests", XunitReference, "xunit.core");

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0026", diagnostic.Id));
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("LeafFactAttribute", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PlainClassAndReferencedXunitTypes_AreNotReported()
    {
        // A plain class that is not a Fact/Theory subclass is clean, and the referenced Xunit.TheoryAttribute :
        // FactAttribute (metadata, not source) is never flagged.
        const string source = """
            namespace App
            {
                public sealed class Sample { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<NoOsSkipInTestsAnalyzer>(
            source, "AgentGuard.Cli.Tests", XunitReference, "xunit.core"));
    }

    [Fact]
    public async Task FactSubclass_InNonTestAssembly_IsNotReported()
    {
        // The rule is scoped to test compilations. A Fact subclass in a shipping assembly is not flagged (and could not
        // exist anyway — no xUnit reference), so the scan stays off shipping code.
        const string source = """
            namespace App
            {
                public sealed class PosixOnlyFactAttribute : Xunit.FactAttribute { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<NoOsSkipInTestsAnalyzer>(
            source, "AgentGuard.Engine", XunitReference, "xunit.core"));
    }
}
