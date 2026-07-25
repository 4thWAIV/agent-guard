// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class AbstractionsMustBeInterfacesAnalyzerPositiveTests
{
    [Fact]
    public async Task PublicClassInAbstractionsNamespace_IsReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public class {|AG0001:Widget|}
                {
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<AbstractionsMustBeInterfacesAnalyzer>(source);
    }
}
