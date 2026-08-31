// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a dodge of the owned prompt resource at a production presence call site — an argument of a
/// <c>PresenceRequest</c> construction or an <c>IPresenceCheck.Check</c> invocation. The prompt a human sees is one
/// reviewed, action-specific string held as an owned resource keyed by <c>SetupVerb</c>
/// (os-dialog-text-owned-resource); resolving it through that resource's accessor, not baking the text in at the call,
/// keeps the three verb strings reviewed and single-owned. A dodge is any text fixed at the call that bypasses the
/// owned resource: a bare string literal, OR a reference to a <c>const</c> / <c>static readonly</c> string field — both
/// caught through the shared <see cref="OwnedSourceArgument"/> detector. A value read through the owned
/// <c>PresenceDialogText</c> accessor (an indexer or method keyed by <c>SetupVerb</c>) is the sanctioned shape and is
/// left alone, as is a runtime value (a parameter or local) that may carry the resolved text. A test assembly
/// (<c>*.Tests</c>) is exempt. Preventive; <c>PresenceRequest</c>/<c>IPresenceCheck</c>/<c>PresenceDialogText</c> do not
/// exist yet.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoInlinePromptLiteralAtPresenceCallAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0109";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A presence prompt must come from the owned per-verb resource, not baked in at the call",
        messageFormat: "The prompt at this presence call site is baked in (a bare literal or a const/static-readonly field); resolve it through the owned PresenceDialogText accessor keyed by SetupVerb (os-dialog-text-owned-resource)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "At a production presence call site — a PresenceRequest construction or an IPresenceCheck.Check invocation — the prompt text must be resolved through the reviewed, action-specific owned PresenceDialogText resource keyed by SetupVerb, never baked in at the call. A bare string literal OR a reference to a const/static-readonly string field is caught; a value read through the owned accessor, or a runtime value that may carry the resolved text, is left alone. A test assembly is exempt.");

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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (TestAssembly.IsTestSystemAssembly(context.Compilation))
        {
            return;
        }

        MemberUseScanner.Register(context, Inspect);
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (!IsPresenceCallSite(member, type))
        {
            return;
        }

        // A dodge is text baked in at the call (a literal or a const/static-readonly field) that does NOT come through
        // the owned PresenceDialogText accessor — the one recognized, reviewed prompt source. The shared
        // OwnedSourceArgument detector answers both halves.
        foreach (IArgumentOperation argument in ArgumentsOf(context.Operation)
            .Where(argument => OwnedSourceArgument.IsDodge(argument.Value, PresenceContracts.PresenceDialogText)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Syntax.GetLocation()));
        }
    }

    private static ImmutableArray<IArgumentOperation> ArgumentsOf(IOperation operation)
    {
        // The two presence call sites are the Check invocation and the PresenceRequest object creation; both carry the
        // prompt as an argument, read here through the same accessor.
        return operation switch
        {
            IInvocationOperation invocation => invocation.Arguments,
            IObjectCreationOperation creation => creation.Arguments,
            _ => ImmutableArray<IArgumentOperation>.Empty,
        };
    }

    private static bool IsPresenceCallSite(ISymbol member, INamedTypeSymbol type)
    {
        // A PresenceRequest construction, or an IPresenceCheck.Check invocation — the two places a prompt string is
        // handed toward the boundary.
        if (member is IMethodSymbol { MethodKind: MethodKind.Constructor }
            && WellKnownType.IsAnyOf(type, PresenceContracts.PresenceRequest))
        {
            return true;
        }

        return PresenceContracts.IsPresenceCheckInvocation(member, type);
    }
}
