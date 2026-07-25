// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ReturnTypesMustNotBeTuplesAnalyzerNegativeTests
{
    [Fact]
    public async Task MethodReturningNamedRecord_IsNotReported()
    {
        const string source = """
            public sealed record Point(int X, int Y);

            public class Sample
            {
                public Point Get()
                {
                    return new Point(1, 2);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }

    [Fact]
    public async Task MethodReturningKeyValuePair_IsNotReported()
    {
        const string source = """
            using System.Collections.Generic;

            public class Sample
            {
                public KeyValuePair<int, string> Get()
                {
                    return new KeyValuePair<int, string>(1, "a");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }

    [Fact]
    public async Task MethodWithTupleParameterButNonTupleReturn_IsNotReported()
    {
        const string source = """
            public class Sample
            {
                public int Get((int, string) value)
                {
                    return value.Item1;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }

    [Fact]
    public async Task MethodUsingTupleLocalButNonTupleReturn_IsNotReported()
    {
        const string source = """
            public class Sample
            {
                public int Get()
                {
                    (int a, int b) pair = (1, 2);
                    return pair.a + pair.b;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }

    [Fact]
    public async Task VoidMethod_IsNotReported()
    {
        const string source = """
            public class Sample
            {
                public void Do()
                {
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }
}
