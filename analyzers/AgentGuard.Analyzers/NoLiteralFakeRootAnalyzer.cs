// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a compile-time literal passed at a fake-filesystem-root seed site — the <c>home</c>,
/// <c>currentDirectory</c>, or <c>tempDirectory</c> argument of <c>AgentGuard.TestHelpers.FakeEnvironment.Create</c>,
/// or the <c>tempRoot</c> argument where the copy-on-write overlay <c>AgentGuard.TestHelpers.InMemoryFileSystemStore</c>
/// is constructed. Those are the only places a fake root is seeded, and the copy-on-write simulator is correct on
/// every OS only when each takes a real-host or abstraction-derived value (through <c>IEnvironment</c>), never a
/// hardcoded string: a POSIX-shaped absolute literal (a bare <c>/</c>-rooted path) is not fully-qualified on Windows
/// and throws there, and a <c>C:\</c> literal would break macOS/Linux the same way — a break invisible on the
/// developer's machine and caught only in cross-OS CI.
/// <para>
/// The rule fires ONLY on a literal at those specific arguments, matched by the target member's identity (namespace +
/// name AND the declaring assembly through <see cref="WellKnownType.IsInAssembly"/>) AND the parameter name, so it
/// never inspects an arbitrary string and
/// cannot mistake data — a <c>/repo</c>-style JSON value, a route, a regex — for a path (no false positives by
/// construction). "Literal" is the semantic sense: a string literal OR a reference to a <c>const</c> (both are
/// compile-time constants the compiler folds), so a <c>const</c> such as the retired <c>DefaultFakeHome</c> is caught
/// exactly as a bare <c>"…"</c> would be. Only an argument the caller EXPLICITLY wrote is inspected, so a defaulted
/// <c>currentDirectory</c>/<c>tempDirectory</c> is never flagged, and a runtime value (a real-host read) is left
/// alone.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoLiteralFakeRootAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0035";

    private const string Category = "AgentGuard.Architecture";
    private const string FakeEnvironmentFactoryName = "Create";
    private const string StoreTempRootParameterName = "tempRoot";

    // The three FakeEnvironment.Create parameters that seed a filesystem root. Each must take a real-host or
    // abstraction-derived value, never a literal.
    private static readonly ImmutableArray<string> FakeEnvironmentRootParameters = ImmutableArray.Create(
        "home", "currentDirectory", "tempDirectory");

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A fake filesystem root must not be seeded with a compile-time literal",
        messageFormat: "The '{0}' argument seeds a fake filesystem root with a compile-time literal; pass a real-host value read through IEnvironment (GetHomeDirectory/GetTempDirectory) or another abstraction-derived value, never a hardcoded root (a POSIX-shaped literal is not fully-qualified on Windows and a C:\\ literal breaks macOS/Linux)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A fake filesystem root is seeded only at FakeEnvironment.Create (home/currentDirectory/tempDirectory) and where the InMemoryFileSystemStore overlay is built (tempRoot). Each must take a real-host or abstraction-derived value through IEnvironment so the copy-on-write simulator is fully-qualified on every OS; a compile-time literal (a string literal or a const reference) is a build error there. The rule matches only those named arguments of those two members, so a data string elsewhere is never inspected, and only a value the caller explicitly wrote is checked.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedRules;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(context => MemberUseScanner.Register(context, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (IsFakeEnvironmentCreate(context.Operation, member, type))
        {
            foreach (string parameterName in FakeEnvironmentRootParameters)
            {
                ReportIfLiteral(context, parameterName);
            }

            return;
        }

        if (TestHelperTypes.IsStoreConstruction(context.Operation, type))
        {
            ReportIfLiteral(context, StoreTempRootParameterName);
        }
    }

    // FakeEnvironment.Create in AgentGuard.TestHelpers — a static factory whose home/currentDirectory/tempDirectory
    // seed the fake environment's roots, matched by namespace + name AND the declaring assembly
    // (AgentGuard.TestHelpers) AND the method name through WellKnownType.IsInAssembly, so a type merely NAMED the same
    // in another assembly does not match.
    private static bool IsFakeEnvironmentCreate(IOperation operation, ISymbol member, INamedTypeSymbol type)
        => operation is IInvocationOperation
            && member is IMethodSymbol { IsStatic: true }
            && string.Equals(member.Name, FakeEnvironmentFactoryName, StringComparison.Ordinal)
            && WellKnownType.IsInAssembly(
                type, TestAssembly.TestHelpersName, TestHelperTypes.FakeEnvironmentTypeName, TestAssembly.TestHelpersName);

    private static void ReportIfLiteral(OperationAnalysisContext context, string parameterName)
    {
        IArgumentOperation? argument = SeedArgument.ExplicitLiteral(SeedArgument.Of(context.Operation), parameterName);
        if (argument is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Syntax.GetLocation(), parameterName));
        }
    }
}
