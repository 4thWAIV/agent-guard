// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.CrossPlatform.Linux;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The policy-integrity test (acceptance #7): the shipped <c>eng/polkit/agentguard-presence.policy</c> declares all
/// three scopes as exactly <c>auth_self</c> (no <c>_keep</c>, no <c>yes</c> — a fresh challenge every call), and its XML
/// action id equals the single-owner C# const <c>PolkitAction.Id</c>, so the id lives in exactly two places and never
/// drifts. The file is read through the owned <see cref="IFileReader"/> (no raw filesystem call) and located at the
/// application base directory reported by <see cref="IEnvironment.GetBaseDirectory"/>, where the build copies the shipped
/// policy beside the test assembly — a runtime anchor with no reliance on the compile-time source path. Compiled only on
/// the Linux CI leg (it reaches the internal <c>PolkitAction.Id</c>).
/// </summary>
public sealed class PolkitPolicyIntegrityTests
{
    [Fact]
    public void ShippedPolicy_DeclaresAllThreeScopesAuthSelf_NoKeepNoYes_AndActionIdMatchesTheConst()
    {
        ISystemServices services = SystemServicesBuilder.Real().Build();
        IFileReader reader = services.FileSystem.GetFileReader();
        string policyPath = Path.Combine(services.Environment.GetBaseDirectory(), "agentguard-presence.policy");

        reader.Exists(policyPath).Should().BeTrue("the polkit policy must be shipped in the repo");

        XElement action = ParseSingleAction(reader.ReadAllText(policyPath));
        action.Attribute("id")!.Value.Should().Be(PolkitAction.Id);

        XElement defaults = action.Element("defaults")!;
        defaults.Element("allow_any")!.Value.Should().Be("auth_self");
        defaults.Element("allow_inactive")!.Value.Should().Be("auth_self");
        defaults.Element("allow_active")!.Value.Should().Be("auth_self");
    }

    // Parses the policy XML DTD-safely (polkit policies carry a DOCTYPE) and returns the single <action> element.
    private static XElement ParseSingleAction(string xml)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
        using var textReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(textReader, settings);
        XDocument document = XDocument.Load(xmlReader);
        return document.Descendants("action").Should().ContainSingle().Subject;
    }
}
