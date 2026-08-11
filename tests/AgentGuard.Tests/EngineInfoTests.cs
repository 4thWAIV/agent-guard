// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Engine;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public class EngineInfoTests
{
    [Fact]
    public void Name_ReturnsEngineAssemblyName()
    {
        EngineInfo.Name.Should().Be("AgentGuard.Engine");
    }
}
