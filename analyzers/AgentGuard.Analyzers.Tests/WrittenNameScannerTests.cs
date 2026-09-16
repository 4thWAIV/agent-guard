// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class WrittenNameScannerTests
{
    // One fixture carrying every shape the documentation exclusion has to get right: a plain cref, a GENERIC cref
    // whose name node sits under a type-argument list, an OPERATOR cref whose names sit in a cref parameter list, the
    // same identifier written in ordinary code, and a preprocessor-directive name — the construct the rejected
    // IsPartOfStructuredTrivia alternative would also have exempted.
    private const string DocumentedSource = """
        using System;

        namespace Sample
        {
            public class Guarded
            {
                public static Guarded operator +(Guarded left, Guarded right) => left;
            }

            public class Cache<T>
            {
            }

            /// <summary>
            /// Links <see cref="Guarded"/>, <see cref="Cache{Guarded}"/> and
            /// <see cref="Guarded.operator +(Guarded, Guarded)"/>.
            /// </summary>
            public static class Documented
            {
        #if GUARDED
                public static object Excluded() => null;
        #endif

                public static Type Written() => typeof(Guarded);
            }
        }
        """;

    [Fact]
    public async Task EveryNameInsideACref_IsADocumentationReference()
    {
        // The plain cref, the generic cref's type argument, and the operator cref's container and parameter names are
        // all excluded.
        Compilation compilation = await AnalyzerRunner.CompileAsync(DocumentedSource);
        List<SimpleNameSyntax> inCrefs = NamesIn(compilation, "Guarded", inCref: true);

        Assert.Equal(5, inCrefs.Count);
        Assert.All(inCrefs, name => Assert.True(WrittenNameScanner.IsDocumentationReference(name)));
    }

    [Fact]
    public async Task TheGenericAndOperatorCrefShapes_AreBothCovered()
    {
        // The rejected parent-KIND alternative would have missed both of these: the generic cref's name sits under a
        // type-argument list, and the operator cref's names sit under a cref parameter list.
        Compilation compilation = await AnalyzerRunner.CompileAsync(DocumentedSource);
        List<SimpleNameSyntax> inCrefs = NamesIn(compilation, "Guarded", inCref: true);

        Assert.Contains(inCrefs, name => name.Ancestors().OfType<TypeArgumentListSyntax>().Any());
        Assert.Contains(inCrefs, name => name.Ancestors().OfType<CrefParameterListSyntax>().Any());
    }

    [Fact]
    public async Task TheSameNameWrittenInCode_IsNotADocumentationReference()
    {
        Compilation compilation = await AnalyzerRunner.CompileAsync(DocumentedSource);
        List<SimpleNameSyntax> inCode = NamesIn(compilation, "Guarded", inCref: false);

        Assert.NotEmpty(inCode);
        Assert.All(inCode, name => Assert.False(WrittenNameScanner.IsDocumentationReference(name)));
    }

    [Fact]
    public async Task APreprocessorDirectiveName_IsNotADocumentationReference()
    {
        // IsPartOfStructuredTrivia is true here, which is exactly why it was rejected as the discriminator: it would
        // have exempted a construct no rule names.
        Compilation compilation = await AnalyzerRunner.CompileAsync(DocumentedSource);
        SimpleNameSyntax directiveName = Assert.Single(NamesIn(compilation, "GUARDED", inCref: false));

        Assert.True(directiveName.IsPartOfStructuredTrivia());
        Assert.False(WrittenNameScanner.IsDocumentationReference(directiveName));
    }

    [Fact]
    public async Task ACrefName_ResolvesToTheSameSymbolAsTheCodeName()
    {
        // The positive control: a cref name is a live, resolvable name node the lens would report but for the
        // exclusion — it binds to the very same type the code reference binds to.
        SemanticModel model = await SharedAnalyzerSources.SemanticModelAsync(DocumentedSource);

        SimpleNameSyntax inCref = NamesIn(model.Compilation, "Guarded", inCref: true)[0];
        SimpleNameSyntax inCode = NamesIn(model.Compilation, "Guarded", inCref: false)[0];

        ISymbol? fromCref = SymbolResolution.Symbol(model, inCref, CancellationToken.None);
        ISymbol? fromCode = SymbolResolution.Symbol(model, inCode, CancellationToken.None);

        Assert.NotNull(fromCref);
        Assert.True(SymbolEqualityComparer.Default.Equals(fromCref, fromCode));
    }

    private static List<SimpleNameSyntax> NamesIn(Compilation compilation, string identifier, bool inCref)
    {
        return compilation.SyntaxTrees.Single()
            .GetRoot()
            .DescendantNodes(descendIntoTrivia: true)
            .OfType<SimpleNameSyntax>()
            .Where(name => string.Equals(name.Identifier.ValueText, identifier, StringComparison.Ordinal))
            .Where(name => (name.FirstAncestorOrSelf<CrefSyntax>() is not null) == inCref)
            .ToList();
    }
}
