// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a raw <c>System.Console</c> use — a call such as <c>Console.WriteLine</c> or a read of <c>Console.Out</c>,
/// <c>Console.Error</c>, or <c>Console.In</c> — made anywhere but the single owner class that implements
/// <c>AgentGuard.Abstractions.Contracts.IConsole</c>. The one exemption is the class implementing <c>IConsole</c> AND compiled
/// into <c>AgentGuard.Boundaries</c> (where the <c>ConsoleAdapter</c> lives); the raw use is a build error in another
/// class of the same assembly and in the same class self-granting from another assembly. Every other type writes and reads the console
/// through <c>IConsole</c> pulled off <c>ISystemServices</c>, which mirrors <c>System.Console</c>'s shape, so console
/// output is captured in tests instead of hitting the real streams.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsoleOnlyInBoundariesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0016";

    private const string Category = "AgentGuard.Architecture";

    // The owning interface whose single implementing class is the only place a raw System.Console use is allowed
    // (one-owner-class-per-primitive). Matched structurally by full name against the enclosing type's implemented
    // interfaces, never by a class-name literal.
    private static readonly ImmutableArray<(string Namespace, string Name)> OwningInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IConsole"));

    // The owner assembly: AgentGuard.Boundaries, where the ConsoleAdapter lives — nothing below Boundaries consumes
    // the console, so it stays there (owners-live-at-lowest-consumer). Half of the conjunction OwnerClass.IsOwner
    // applies: implementing IConsole in any OTHER assembly does not exempt. Reuses the shared BoundaryAssembly.Name
    // constant; cached once so no per-operation allocation.
    private static readonly Func<Compilation, bool> InOwnerAssembly =
        OwnerClass.InAssembly(BoundaryAssembly.Name);

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Raw System.Console use must live only in the class implementing IConsole",
        messageFormat: "Raw console use '{0}' is outside the single owner class implementing IConsole; write and read the console through IConsole pulled off ISystemServices",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A use of System.Console is allowed only in the single class that implements AgentGuard.Abstractions.Contracts.IConsole — not merely somewhere in its assembly. Every other type writes and reads the console through IConsole on ISystemServices so console output is captured in tests instead of hitting the real streams.");

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
        // The single owner class that implements IConsole AND compiles into AgentGuard.Boundaries is the only place
        // the raw use is allowed (one-owner-class-per-primitive + owners-live-at-lowest-consumer); everywhere else —
        // a sibling class in the same assembly, or the same class self-granting in another assembly — is RED.
        if (OwnerClass.IsOwner(context, OwningInterfaces, InOwnerAssembly))
        {
            return;
        }

        if (WellKnownType.Is(type, KnownNamespaces.System, "Console"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }
}
