// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports every call site of <c>IEnvironment.GetTempDirectory()</c> — the owned abstraction over
/// <c>Path.GetTempPath()</c>, which Sonar's <c>S5443</c> cannot see through (ags5443-temp-root-back-door-rule). Sonar
/// <c>S5443</c> fires only on the raw <c>Path.GetTempPath()</c>, and that raw read lives in exactly one place —
/// <c>EnvironmentAdapter.GetTempDirectory</c>, where it is owned (AG0020) and its one Sonar hit is suppressed and
/// approved (<c>s5443-suppression-approved</c>). Every CALLER of the <c>GetTempDirectory()</c> abstraction then receives
/// the shared, publicly-writable temp root while Sonar sees nothing: that is the back door, and this rule closes it by
/// flagging the callers so a use of the shared temp root must be justified with a suppression, exactly as Sonar would
/// force at the raw call. The diagnostic id keeps the <c>S5443</c> lineage visible.
/// <para>
/// The target is matched by the containing interface's full identity (<c>AgentGuard.Abstractions.Contracts.IEnvironment</c>,
/// through the shared <see cref="ContractInterfaces.Environment"/> identity) AND the method name, and every USE is
/// flagged — a direct call OR a method-group/delegate capture (<c>Func&lt;string&gt; g = env.GetTempDirectory;</c>),
/// matched by member identity regardless of operation kind, the same way the sibling AG0017 matches a captured
/// container factory, so the capture-then-invoke shape cannot reopen the back door. It does NOT touch the raw
/// <c>Path.GetTempPath()</c> — owned and pinned by <see cref="PathPurityAnalyzer"/> (AG0020) to
/// <c>EnvironmentAdapter</c> — nor <c>IDirectoryWriter.CreateTempSubdirectory</c>, which creates a uniquely-named atomic
/// subdirectory the caller owns rather than the shared root (which is why Sonar has no rule against it).
/// </para>
/// <para>
/// One owner is exempt (temp-root-owner-exemption): the test <c>SystemServicesBuilder</c> in
/// <c>AgentGuard.TestHelpers</c> is copy-on-write over the real host, so it reads the real temp root through
/// <c>GetTempDirectory()</c> once to seed the in-memory fake — the same single-owner shape the temp and random
/// primitives already use. Inside <c>SystemServicesBuilder</c> the call is allowed; everywhere else it stays a build
/// error. There is otherwise no caller in <c>src</c> or <c>tests</c>, so the rule remains preventive: silent as shipped
/// except for the one owner, and firing the moment any other caller reaches for the raw temp root.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TempRootBackDoorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer. It keeps the Sonar <c>S5443</c> lineage visible: this is
    /// the owned equivalent that closes the abstraction back door <c>S5443</c> cannot see through.
    /// </summary>
    public const string DiagnosticId = "AGS5443";

    private const string Category = "AgentGuard.Architecture";
    private const string GetTempDirectoryMethodName = "GetTempDirectory";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "IEnvironment.GetTempDirectory() must not be called; it hands back the shared publicly-writable temp root",
        messageFormat: "'{0}' returns the shared publicly-writable temp root (the back door Sonar S5443 cannot see through the IEnvironment abstraction); create a uniquely-named subdirectory with IDirectoryWriter.CreateTempSubdirectory instead, or justify the shared root with a suppression",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every call site of IEnvironment.GetTempDirectory() is a build error. Sonar S5443 flags only the raw Path.GetTempPath(), which lives in the one owned reader EnvironmentAdapter.GetTempDirectory (its Sonar hit suppressed and approved); every caller of the GetTempDirectory() abstraction then receives the shared, publicly-writable temp root while Sonar sees nothing. This rule is the owned equivalent that closes that back door by flagging the callers, so a use of the shared temp root must be justified with a suppression exactly as Sonar would force at the raw call. It does not touch the raw Path.GetTempPath (owned by AG0020) or IDirectoryWriter.CreateTempSubdirectory (a uniquely-named atomic subdirectory the caller owns). Zero call sites exist today, so the rule is preventive.");

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
        // Every USE of the abstraction is the back door — a direct call OR a method-group/delegate capture
        // (Func<string> g = env.GetTempDirectory;) — matched by member identity regardless of operation kind, the same
        // way the sibling AG0017 matches a captured container factory, so the capture-then-invoke shape cannot reopen
        // the back door. The target is IEnvironment.GetTempDirectory, matched by the containing interface's full
        // identity (namespace + name via the shared ContractInterfaces.Environment) AND the method name, so a
        // same-named method on another type is never flagged and the raw Path.GetTempPath (owned by AG0020) is out of
        // scope entirely.
        if (member is IMethodSymbol
            && string.Equals(member.Name, GetTempDirectoryMethodName, StringComparison.Ordinal)
            && WellKnownType.IsAnyOf(type, ContractInterfaces.Environment))
        {
            // temp-root-owner-exemption (AGS5443): the test SystemServicesBuilder is the ONE owner licensed to read
            // the real host temp root through the abstraction to seed the copy-on-write fake; the back-door ban stands
            // everywhere else, including the Program composition caller.
            if (CompositionPoint.EnclosesTestBuilder(context.ContainingSymbol))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
        }
    }
}
