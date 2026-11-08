// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Guards the runner's own guarantee: every compilation it builds produces exactly the compiler errors the caller
/// declared, by identifier and by occurrence count, and the declaration defaults to none. Without it, a test that
/// asserts no diagnostic passes whether the analyzer stayed quiet or the fixture never compiled, and every accept
/// case in the suite rests on that distinction. A fixture that is invalid on purpose declares its errors and is
/// still analyzed.
/// </summary>
public class AnalyzerRunnerTests
{
    // The assembly these runner tests compile into. NoClassInAbstractionsAnalyzer keys on the namespace a type is
    // declared in and never on the assembly, so the name is free to say what the compilation is.
    private const string SubjectAssemblyName = "RunnerFixture";

    // A public fake assembly and an internal one. The internal fake is the shape an accept case needs when the rule
    // under test is about reaching internals across an assembly boundary.
    private const string PublicReferenceSource = """
        namespace Fake
        {
            public static class Door
            {
                public static object Open() => null!;
            }
        }
        """;

    private const string InternalReferenceSource = """
        namespace Fake
        {
            internal static class Door
            {
                internal static object Open() => null!;
            }
        }
        """;

    private const string InternalReferenceWithGrantSource = """
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo("Caller")]

        namespace Fake
        {
            internal static class Door
            {
                internal static object Open() => null!;
            }
        }
        """;

    private const string CallDoorSource = """
        using Fake;

        public class Caller
        {
            public object Use() => Door.Open();
        }
        """;

    private const string BrokenSource = """
        public class Broken
        {
            public object Use() => ThisTypeDoesNotExist.Missing();
        }
        """;

    // Invalid on purpose AND analyzer-relevant at the same time — the shape the expectation exists for. The public
    // class in an .Abstractions namespace is what AG0001 reports, and the unresolved call in its body is the CS0103
    // its caller declares, so the compiler error is the fixture's scenario and the analyzer result is still the
    // thing under test.
    private const string AbstractionsClassWithUnresolvedCallSource = """
        namespace AgentGuard.Abstractions
        {
            public class Door
            {
                public object Open() => ThisTypeDoesNotExist.Missing();
            }
        }
        """;

    private const string CompilesCleanlySource = """
        public class Quiet
        {
            public object Use() => new object();
        }
        """;

    // Two DIFFERENT unresolved names in one member: the missing return type is CS0246 and the missing call target is
    // CS0103, so a caller can declare one identifier and leave the other undeclared.
    private const string TwoDifferentErrorsSource = """
        public class Broken
        {
            public MissingType Make() => ThisTypeDoesNotExist.Missing();
        }
        """;

    // The SAME unresolved name twice, so its occurrence count is two and declaring it once is the wrong count.
    private const string RepeatedErrorSource = """
        public class Broken
        {
            public object First() => ThisTypeDoesNotExist.Missing();

            public object Second() => ThisTypeDoesNotExist.Missing();
        }
        """;

    [Fact]
    public async Task RunAsync_OverSourceThatDoesNotCompile_Throws()
    {
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(BrokenSource));

        Assert.Contains("compiler error", failure.Message, StringComparison.Ordinal);
        Assert.Contains("CS0103", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompileAsync_OverSourceThatDoesNotCompile_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => AnalyzerRunner.CompileAsync(BrokenSource));
    }

    [Fact]
    public async Task RunWithReferenceAsync_WhenTheSubjectDoesNotCompile_Throws()
    {
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AnalyzerRunner.RunWithReferenceAsync<NoClassInAbstractionsAnalyzer>(
                BrokenSource, "Caller", PublicReferenceSource, "Fake"));

        Assert.Contains("Caller", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunWithReferenceAsync_WhenTheReferenceDoesNotCompile_Throws()
    {
        // Only the reference check can produce this message: it names the REFERENCED assembly as the one that failed,
        // and it is reached before the subject is compiled at all. Drop the reference check and the same call still
        // throws — the subject can no longer see the 'Fake' namespace the broken reference never declared — but that
        // failure names 'Caller' as the assembly and lists the subject's own errors, so asserting on the bare word
        // "Fake" (which the subject's CS0246 text also carries) would pass with the reference check deleted.
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AnalyzerRunner.RunWithReferenceAsync<NoClassInAbstractionsAnalyzer>(
                CallDoorSource, "Caller", BrokenSource, "Fake"));

        Assert.Contains("compiled into 'Fake'", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("compiled into 'Caller'", failure.Message, StringComparison.Ordinal);
        Assert.Contains("CS0103: not declared, but reported 1 time(s)", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunWithReferenceAsync_ReachingAnInternalWithNoGrant_Throws()
    {
        // The exact false pass this check exists to stop: an accept case asserting no diagnostic would pass here
        // because the compiler rejected the call with CS0122, not because the analyzer stayed quiet.
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AnalyzerRunner.RunWithReferenceAsync<NoClassInAbstractionsAnalyzer>(
                CallDoorSource, "Caller", InternalReferenceSource, "Fake"));

        Assert.Contains("CS0122", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunWithReferenceAsync_ReachingAnInternalThroughAGrant_ReturnsTheAnalyzerResult()
    {
        // The same call compiles once the fake carries the grant, so the empty result is the analyzer's answer.
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<NoClassInAbstractionsAnalyzer>(
            CallDoorSource, "Caller", InternalReferenceWithGrantSource, "Fake"));
    }

    [Fact]
    public async Task RunAsync_OverSourceThatCompiles_ReturnsTheAnalyzerResult()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(CallDoorSource.Replace(
            "using Fake;", "// no reference needed", StringComparison.Ordinal).Replace(
            "Door.Open()", "new object()", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task RunAsync_WhenTheDeclaredErrorsMatchTheActualErrors_ReturnsTheAnalyzerResult()
    {
        // The declared error is the one the fixture produces, so the run proceeds and the analyzer's own diagnostic
        // comes back — a deliberately invalid fixture is still analyzed.
        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(
            AbstractionsClassWithUnresolvedCallSource, SubjectAssemblyName, "CS0103"));

        Assert.Equal("AG0001", diagnostic.Id);
    }

    [Fact]
    public async Task RunAsync_WhenADeclaredErrorIsNotProduced_Throws()
    {
        // The fixture compiles, so the declared error never appeared and the test is not exercising what it says.
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(
                CompilesCleanlySource, SubjectAssemblyName, "CS0103"));

        Assert.Contains("CS0103: declared 1 time(s), but not reported", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WhenAnUndeclaredErrorIsProduced_Throws()
    {
        // CS0103 is declared and CS0246 is not, so the comparison is per identifier: the undeclared one fails the run
        // while the declared one is accepted.
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(
                TwoDifferentErrorsSource, SubjectAssemblyName, "CS0103"));

        Assert.Contains("CS0246: not declared, but reported 1 time(s)", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("CS0103: not declared", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WhenADeclaredErrorIsProducedMoreTimesThanDeclared_Throws()
    {
        // Same identifier, wrong count: the fixture broke in one more place than the test declared, so the analyzer
        // result over it is not the one the test author believed they were asserting on.
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(
                RepeatedErrorSource, SubjectAssemblyName, "CS0103"));

        Assert.Contains(
            "CS0103: declared 1 time(s), but reported 2 time(s)", failure.Message, StringComparison.Ordinal);
    }
}
