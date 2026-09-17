// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class TypeTreeTests
{
    // Two ordinary named types. Every pointer, array and generic shape below is built from them with the COMPILER's
    // own symbol factories, so the pointer branch is proved without unsafe source: the fixture compiles under the
    // runner's default empty compiler-error expectation and no runner change is needed.
    private const string LeafTypesSource = """
        namespace Sample
        {
            public sealed class Guarded
            {
            }

            public sealed class Other
            {
            }
        }
        """;

    private const string SoughtLeaf = "Sample.Guarded";

    private const string OtherLeaf = "Sample.Other";

    private const string ListMetadataName = "System.Collections.Generic.List`1";

    [Fact]
    public async Task APointerToTheSoughtType_ReachesItsTarget()
    {
        Compilation compilation = await AnalyzerRunner.CompileAsync(LeafTypesSource);
        IPointerTypeSymbol root = compilation.CreatePointerTypeSymbol(SharedAnalyzerSources.TypeIn(compilation, SoughtLeaf));
        var offered = new List<string>();

        bool found = TypeTree.Any(root, Record(offered));

        Assert.Equal(SoughtLeaf + "*", root.ToDisplayString());
        Assert.True(found);
        Assert.Equal(new[] { SoughtLeaf }, offered);
    }

    [Fact]
    public async Task APointerToAnotherType_IsWalkedToItsTargetAndDoesNotMatch()
    {
        // The nonmatching pointer target, and the reason the walk RECORDS what it offers: a false answer alone cannot
        // tell a pointer whose target was reached and rejected from a pointer the walk never followed at all.
        Compilation compilation = await AnalyzerRunner.CompileAsync(LeafTypesSource);
        IPointerTypeSymbol root = compilation.CreatePointerTypeSymbol(SharedAnalyzerSources.TypeIn(compilation, OtherLeaf));
        var offered = new List<string>();

        bool found = TypeTree.Any(root, Record(offered));

        Assert.Equal(OtherLeaf + "*", root.ToDisplayString());
        Assert.False(found);
        Assert.Equal(new[] { OtherLeaf }, offered);
    }

    [Fact]
    public async Task AnArrayOfPointers_ReachesTheTargetThroughItsElementType()
    {
        Compilation compilation = await AnalyzerRunner.CompileAsync(LeafTypesSource);
        IArrayTypeSymbol root = compilation.CreateArrayTypeSymbol(
            compilation.CreatePointerTypeSymbol(SharedAnalyzerSources.TypeIn(compilation, SoughtLeaf)));
        var offered = new List<string>();

        bool found = TypeTree.Any(root, Record(offered));

        Assert.Equal(SoughtLeaf + "*[]", root.ToDisplayString());
        Assert.True(found);
        Assert.Equal(new[] { SoughtLeaf }, offered);
    }

    [Fact]
    public async Task AGenericCarryingAPointerArray_ReachesTheTargetThroughItsTypeArgument()
    {
        // The nesting the access rules depend on: generic argument, then array element, then pointer target. The
        // recorded order is the walk itself — the constructed generic first, the leaf inside it after.
        Compilation compilation = await AnalyzerRunner.CompileAsync(LeafTypesSource);
        INamedTypeSymbol root = SharedAnalyzerSources.TypeIn(compilation, ListMetadataName).Construct(
            compilation.CreateArrayTypeSymbol(
                compilation.CreatePointerTypeSymbol(SharedAnalyzerSources.TypeIn(compilation, SoughtLeaf))));
        var offered = new List<string>();

        bool found = TypeTree.Any(root, Record(offered));

        Assert.True(found);
        Assert.Equal(new[] { root.ToDisplayString(), SoughtLeaf }, offered);
        Assert.Contains(SoughtLeaf + "*[]", root.ToDisplayString(), StringComparison.Ordinal);
    }

    // The leaf test every walk above is driven with: it RECORDS each named type the walk offers it, then answers
    // whether that type is the one being sought. The record is what makes an assertion a proof of TRAVERSAL rather
    // than of a non-null constructed symbol — it names exactly which types the walk reached, and in what order.
    private static Func<INamedTypeSymbol, bool> Record(List<string> offered)
    {
        return named =>
        {
            string name = named.ToDisplayString();
            offered.Add(name);
            return string.Equals(name, SoughtLeaf, StringComparison.Ordinal);
        };
    }
}
