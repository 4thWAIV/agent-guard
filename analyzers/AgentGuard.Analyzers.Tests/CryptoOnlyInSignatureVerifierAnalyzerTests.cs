// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class CryptoOnlyInSignatureVerifierAnalyzerTests
{
    // A sibling that does NOT implement ISignatureVerifier, using the raw BouncyCastle Ed25519Signer. The stub
    // BouncyCastle type is declared in its real namespace so the full-name match (namespace + name) fires.
    private const string SiblingUsingSignerSource = """
        namespace Org.BouncyCastle.Crypto.Signers
        {
            public class Ed25519Signer { }
        }

        namespace App
        {
            public class Sample
            {
                public object Make() => new Org.BouncyCastle.Crypto.Signers.Ed25519Signer();
            }
        }
        """;

    // The owner class that implements ISignatureVerifier and uses the raw signer — exempt only in AgentGuard.Boundaries.
    private const string OwnerUsingSignerSource = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface ISignatureVerifier { }
        }

        namespace Org.BouncyCastle.Crypto.Signers
        {
            public class Ed25519Signer { }
        }

        namespace App
        {
            public sealed class Ed25519SignatureVerifier : AgentGuard.Abstractions.Contracts.ISignatureVerifier
            {
                public object Make() => new Org.BouncyCastle.Crypto.Signers.Ed25519Signer();
            }
        }
        """;

    [Fact]
    public async Task RawSigner_OutsideOwner_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<CryptoOnlyInSignatureVerifierAnalyzer>(
                SiblingUsingSignerSource, "AgentGuard.Engine"));

        Assert.Equal("AG0021", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Ed25519Signer", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RawPublicKeyParameters_OutsideOwner_IsReported()
    {
        const string source = """
            namespace Org.BouncyCastle.Crypto.Parameters
            {
                public class Ed25519PublicKeyParameters { }
            }

            namespace App
            {
                public class Sample
                {
                    public object Make() => new Org.BouncyCastle.Crypto.Parameters.Ed25519PublicKeyParameters();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<CryptoOnlyInSignatureVerifierAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0021", diagnostic.Id);
    }

    [Fact]
    public async Task RawSigner_InOwnerClassAndOwnerAssembly_IsExempt()
    {
        // Both halves of the conjunction pass: implements ISignatureVerifier AND compiled into AgentGuard.Boundaries.
        Assert.Empty(await AnalyzerRunner.RunAsync<CryptoOnlyInSignatureVerifierAnalyzer>(
            OwnerUsingSignerSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task RawSigner_InOwnerAssembly_ButNotOwnerClass_IsReported()
    {
        // AgentGuard.Boundaries is the owner assembly, but this sibling does not implement ISignatureVerifier, so its
        // raw signer is still RED — the assembly half alone never exempts.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<CryptoOnlyInSignatureVerifierAnalyzer>(
                SiblingUsingSignerSource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0021", diagnostic.Id);
    }

    [Fact]
    public async Task RawSigner_OwnerClass_InWrongAssembly_IsReported()
    {
        // Implementing ISignatureVerifier is not enough — the class must also compile into AgentGuard.Boundaries. In
        // AgentGuard.Engine the owner's raw signer is RED, so a class declaring ': ISignatureVerifier' cannot launder it.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<CryptoOnlyInSignatureVerifierAnalyzer>(
                OwnerUsingSignerSource, "AgentGuard.Engine"));
        Assert.Equal("AG0021", diagnostic.Id);
    }
}
