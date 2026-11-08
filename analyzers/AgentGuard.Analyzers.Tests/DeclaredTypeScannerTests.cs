// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// The carried-type lens has TWO entry points and they register different sets. AG0006 keeps exactly the four
/// DECLARATION positions it always had; the access rules' gated entry point adds the return type of an INVOKED
/// member. AG0006's own seven tests remain the proof of the four declaration positions, unmodified, so the only
/// behaviour proved here is the position that is new and the split that keeps AG0006's diagnostics unchanged.
/// </summary>
public class DeclaredTypeScannerTests
{
    private const string DeclaringMethod = "Declared";

    // The consumer reaches the guarded type through a method IT declares, and then INVOKES that method. Three
    // registrations see it and each reports at its own place: the written name in the signature, the declared return
    // type of the method, and the return type carried by the invocation. Nothing else in the fixture reaches Engine,
    // so the third diagnostic exists only if the invoked-return position is registered.
    private const string InvokedReturnBody =
        "        internal static object Made() => " + DeclaringMethod + "();\n\n"
        + "        private static EngineInternal " + DeclaringMethod + "() => null!;";

    // The AG0006 fixture, in the same shape: a contract implementation is the DECLARED return type of one method and
    // the INVOKED return type at the call to it. AG0006 must see the first and not the second.
    private const string ContractImplementationType = "Sample.Impl.Widget";

    private const string ContractImplementationSource = """
        namespace Sample.Abstractions.Contracts
        {
            public interface IWidget
            {
            }
        }

        namespace Sample.Impl
        {
            using Sample.Abstractions.Contracts;

            public sealed class Widget : IWidget
            {
            }

            public static class Holder
            {
                public static Widget Declared() => null!;

                public static object Invoked() => Declared();
            }
        }
        """;

    [Fact]
    public async Task TheGatedEntryPoint_ScansTheReturnTypeOfAnInvokedMember()
    {
        string source = SharedAnalyzerSources.GatedConsumer(
            SharedAnalyzerSources.EngineUsing, InvokedReturnBody, string.Empty);

        ImmutableArray<Diagnostic> diagnostics =
            await SharedAnalyzerSources.RunAgainstFakeEngineAsync<EngineInternalsOneDoorAnalyzer>(
                source, SharedAnalyzerSources.GuardAssemblyName);

        diagnostics.AssertAllReported(EngineInternalsOneDoorAnalyzer.DiagnosticId);

        // The three places, read off the diagnostics' own source spans. Only the INVOCATION span can come from the
        // invoked-return registration: the written-name lens reports the name node and the declaration registration
        // reports the method's own identifier, so neither can produce it.
        diagnostics.AssertSpans(source, "EngineInternal", DeclaringMethod, DeclaringMethod + "()");

        Assert.All(
            diagnostics,
            diagnostic => Assert.Contains(SharedAnalyzerSources.EngineInternalTypeName, diagnostic.Message(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheDeclarationEntryPoint_DoesNotScanTheReturnTypeOfAnInvokedMember()
    {
        // The other half of the split, proved against the one rule that drives the declaration entry point alone.
        // The fixture really does invoke a member whose return type is the contract implementation — asserted from
        // the semantic model, so "AG0006 does not report there" cannot pass by the call being absent — and AG0006
        // still reports exactly once, at the DECLARATION.
        SemanticModel model = await SharedAnalyzerSources.SemanticModelAsync(ContractImplementationSource);
        var invoked = Assert.IsAssignableFrom<IMethodSymbol>(
            model.GetSymbolInfo(SharedAnalyzerSources.OnlyNode<InvocationExpressionSyntax>(model.SyntaxTree)).Symbol);
        Assert.Equal(ContractImplementationType, invoked.ReturnType.ToDisplayString());

        Diagnostic reported = Assert.Single(
            await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeReferencedAnalyzer>(
                ContractImplementationSource));

        reported.AssertReported(ContractConcreteTypeMustNotBeReferencedAnalyzer.DiagnosticId);
        Assert.Equal(DeclaringMethod, AnalyzerRunner.SpanText(ContractImplementationSource, reported));
    }
}
