// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class AbstractionsMustBeInterfacesAnalyzerNegativeTests
{
    [Fact]
    public async Task InterfaceInAbstractionsAndClassElsewhere_AreNotReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public interface IWidget
                {
                }
            }

            namespace Sample.Implementation
            {
                public class Widget
                {
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<AbstractionsMustBeInterfacesAnalyzer>(source);
    }
}
