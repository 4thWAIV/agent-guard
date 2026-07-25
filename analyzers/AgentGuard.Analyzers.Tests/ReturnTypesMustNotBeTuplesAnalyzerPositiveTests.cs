// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ReturnTypesMustNotBeTuplesAnalyzerPositiveTests
{
    [Fact]
    public async Task PublicMethodReturningTuple_IsReported()
    {
        const string source = """
            public class Sample
            {
                public (int, string) {|AG0002:Get|}()
                {
                    return (1, "a");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }

    [Fact]
    public async Task PrivateMethodReturningTuple_IsReported()
    {
        const string source = """
            public class Sample
            {
                private (int, string) {|AG0002:Get|}()
                {
                    return (1, "a");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }

    [Fact]
    public async Task MethodReturningNestedTuple_IsReported()
    {
        const string source = """
            using System.Collections.Generic;

            public class Sample
            {
                public List<(int, string)> {|AG0002:Get|}()
                {
                    return new List<(int, string)>();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }

    [Fact]
    public async Task PropertyReturningTuple_IsReported()
    {
        const string source = """
            public class Sample
            {
                public (int, string) {|AG0002:Pair|} => (1, "a");
            }
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }

    [Fact]
    public async Task DelegateReturningTuple_IsReported()
    {
        const string source = """
            public delegate (int, string) {|AG0002:Combine|}();
            """;

        await AnalyzerVerifier.VerifyAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);
    }
}
