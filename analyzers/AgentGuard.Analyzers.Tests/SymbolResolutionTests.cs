// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class SymbolResolutionTests
{
    // One fixture carrying every resolution the two consumers ask for, and it COMPILES: a qualified type name whose
    // left segment binds a NAMESPACE and whose right segment binds the TYPE, a GENERIC name, a well-formed applied
    // attribute (whose node binds the CONSTRUCTOR, not the type), and an invocation whose bound method and whose type
    // info disagree. The malformed-attribute recovery tier is NOT re-fixtured here: it is already proved by the
    // existing InteropOnlyInCrossPlatformLibrariesAnalyzerTests.MalformedSameNamedDllImportAttribute_… and
    // NativeCallbackBodyMustBeGuardedAnalyzerTests.MalformedUserDefinedUnmanagedCallersOnly_… tests, whose inputs,
    // assertions and declared compiler errors stay untouched, so no second malformed-fixture exception is introduced.
    private const string ResolutionSource = """
        using System;

        namespace Sample
        {
            [AttributeUsage(AttributeTargets.Class)]
            public sealed class MarkerAttribute : Attribute
            {
            }

            public sealed class Cache<T>
            {
            }

            [Marker]
            public sealed class Guarded
            {
            }

            public static class Holder
            {
                public static Guarded Make() => null!;

                public static object Typed() => typeof(Sample.Guarded);

                public static object Generic() => typeof(Cache<Guarded>);

                public static object Used() => Make();
            }
        }
        """;

    private const string SampleNamespace = "Sample";

    private const string GuardedType = "Sample.Guarded";

    private const string HolderType = "Sample.Holder";

    private const string MarkerName = "Marker";

    [Fact]
    public async Task Symbol_OnAWrittenTypeName_ResolvesThatType()
    {
        SemanticModel model = await ModelAsync();

        ISymbol? resolved = SymbolResolution.Symbol(model, TypeSegment(model), CancellationToken.None);

        Assert.Equal(GuardedType, Display(resolved));
    }

    [Fact]
    public async Task Symbol_OnANamespaceSegment_ResolvesTheNamespaceItself()
    {
        // Every simple name in the tree is offered to the written-name lens, and the leading segments of a qualified
        // name bind NAMESPACES. This is the resolution behind that: the segment binds, and it binds a namespace.
        SemanticModel model = await ModelAsync();

        ISymbol? resolved = SymbolResolution.Symbol(model, NamespaceSegment(model), CancellationToken.None);

        Assert.Equal(SampleNamespace, Display(resolved));
        Assert.IsAssignableFrom<INamespaceSymbol>(resolved);
    }

    [Fact]
    public async Task NamedType_OnANamespaceSegment_ResolvesNothing()
    {
        // The other half of the same fact: a namespace is not a named TYPE at any tier, so a rule asking for the type
        // a name denotes gets nothing back for a namespace rather than an outer or enclosing type.
        SemanticModel model = await ModelAsync();

        Assert.Null(SymbolResolution.NamedType(model, NamespaceSegment(model), CancellationToken.None));
    }

    [Fact]
    public async Task Symbol_OnAGenericName_ResolvesTheConstructedType()
    {
        // A guarded name written as a generic: the node resolves to the CONSTRUCTED type, carrying the argument the
        // source wrote, which is what lets a rule's leaf test see the argument as well as the definition.
        SemanticModel model = await ModelAsync();

        ISymbol? resolved = SymbolResolution.Symbol(model, OnlyNode<GenericNameSyntax>(model), CancellationToken.None);

        var constructed = Assert.IsAssignableFrom<INamedTypeSymbol>(resolved);
        Assert.Equal("Sample.Cache<" + GuardedType + ">", constructed.ToDisplayString());
        Assert.Equal(GuardedType, Assert.Single(constructed.TypeArguments).ToDisplayString());
        Assert.Equal("Sample.Cache<T>", constructed.OriginalDefinition.ToDisplayString());
    }

    [Fact]
    public async Task Symbol_OnAMemberName_ResolvesThatMember()
    {
        SemanticModel model = await ModelAsync();
        ExpressionSyntax invoked = OnlyNode<InvocationExpressionSyntax>(model).Expression;

        ISymbol? resolved = SymbolResolution.Symbol(model, invoked, CancellationToken.None);

        Assert.Equal(HolderType + ".Make()", Display(resolved));
    }

    [Fact]
    public async Task NamedType_OnAWrittenTypeName_ResolvesThroughTheBoundTypeItself()
    {
        // Tier two: the node binds the TYPE, so no containing-type or type-info recovery is involved.
        SemanticModel model = await ModelAsync();
        SimpleNameSyntax written = TypeSegment(model);

        Assert.IsAssignableFrom<INamedTypeSymbol>(model.GetSymbolInfo(written, CancellationToken.None).Symbol);
        Assert.Equal(
            GuardedType,
            Display(SymbolResolution.NamedType(model, written, CancellationToken.None)));
    }

    [Fact]
    public async Task NamedType_OnAnAppliedAttribute_ResolvesTheAttributeTypeThroughItsConstructor()
    {
        // Tier one, and why it exists: an attribute node does not bind its type at all — it binds the CONSTRUCTOR —
        // so the containing type is what the attribute rules match on.
        SemanticModel model = await ModelAsync();
        AttributeSyntax applied = MarkerAttributeNode(model);

        var bound = Assert.IsAssignableFrom<IMethodSymbol>(
            model.GetSymbolInfo(applied, CancellationToken.None).Symbol);

        Assert.Equal(MethodKind.Constructor, bound.MethodKind);
        Assert.Equal(
            SampleNamespace + "." + MarkerName + "Attribute",
            Display(SymbolResolution.NamedType(model, applied, CancellationToken.None)));
    }

    [Fact]
    public async Task NamedType_OnAnInvocation_StopsAtTheBoundMethodAndNeverReachesTheTypeInfoTier()
    {
        // The tier ORDER, pinned where the tiers disagree: the invocation binds Make, so tier one answers with Make's
        // CONTAINING type, and the type-info recovery tier — which would answer with the RETURN type — does not run.
        SemanticModel model = await ModelAsync();
        InvocationExpressionSyntax invocation = OnlyNode<InvocationExpressionSyntax>(model);

        Assert.Equal(GuardedType, Display(model.GetTypeInfo(invocation, CancellationToken.None).Type));
        Assert.Equal(
            HolderType,
            Display(SymbolResolution.NamedType(model, invocation, CancellationToken.None)));
    }

    private static Task<SemanticModel> ModelAsync()
    {
        return SharedAnalyzerSources.SemanticModelAsync(ResolutionSource);
    }

    // The fixture's one qualified name, `Sample.Guarded`, read from its two ends: the left segment denotes the
    // namespace and the right segment denotes the type. One expression, so the two resolutions are proved against the
    // same written name rather than against two fixtures that might differ in something else.
    private static NameSyntax NamespaceSegment(SemanticModel model)
    {
        return OnlyNode<QualifiedNameSyntax>(model).Left;
    }

    private static SimpleNameSyntax TypeSegment(SemanticModel model)
    {
        return OnlyNode<QualifiedNameSyntax>(model).Right;
    }

    private static AttributeSyntax MarkerAttributeNode(SemanticModel model)
    {
        return SharedAnalyzerSources.OnlyNode<AttributeSyntax>(
            model.SyntaxTree,
            attribute => string.Equals(attribute.Name.ToString(), MarkerName, StringComparison.Ordinal));
    }

    private static T OnlyNode<T>(SemanticModel model)
        where T : SyntaxNode
    {
        return SharedAnalyzerSources.OnlyNode<T>(model.SyntaxTree);
    }

    private static string Display(ISymbol? symbol)
    {
        return Assert.IsAssignableFrom<ISymbol>(symbol).ToDisplayString();
    }
}
