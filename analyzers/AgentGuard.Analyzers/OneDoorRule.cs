// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The shared body of a one-door rule: gate to the calling assembly, filter the use down to the guarded target, test
/// it against that rule's single permitted door, test the site where the gate requires it, and report naming EVERY
/// condition that failed. The three one-door rules — AG0040 (Engine into <c>AgentGuard.Boundaries</c>), AG0023 (into
/// the core <c>AgentGuard.CrossPlatform</c> assembly), and AG0029 (into a per-OS implementation assembly) — were the
/// same target-filter, door-test, report sequence spelled once each; that sequence lives here, and each rule supplies
/// only its descriptor and its own scope and door tests.
/// <para>
/// Two gates exist, and they are deliberately different. The <c>AgentGuard.Boundaries</c> gate carries NO caller
/// constraint — the pre-existing behaviour, preserved exactly, so the door may be called from anywhere in that
/// assembly and nothing outside Engine is loosened. The <c>AgentGuard.Engine</c> gate adds the caller constraint (the
/// door may be reached only from <c>AgentGuard.Engine.SystemServices.Create()</c>, matched by
/// <see cref="CompositionPoint.IsContainerFactoryMethod"/>) and adds TYPE-reference coverage through the two shared
/// lenses, <see cref="WrittenNameScanner"/> and <see cref="DeclaredTypeScanner"/>.
/// </para>
/// <para>
/// Every message is built the same way: <c>{0}</c> is the calling assembly and <c>{1}</c> is the list of conditions
/// that failed. Each rule's descriptor names its own door in the fixed text, so the failure list stays rule-neutral
/// and a reach that IS the door but sits outside <c>Create()</c> reports that its site is wrong — never the falsehood
/// that a reach to the door is not the door.
/// </para>
/// </summary>
internal static class OneDoorRule
{
    private const string CallSiteFailure = "the call site is not " + CompositionPoint.ContainerFactoryDescription;

    private const string ReferenceSiteFailure =
        "the reference site is not " + CompositionPoint.ContainerFactoryDescription;

    private const string NotTheDoor = "' is not the permitted door";

    private const string FailureSeparator = "; and ";

    /// <summary>
    /// Registers the single <c>AgentGuard.Engine</c> CALL gate: a member use that reaches the guarded assembly is
    /// reported unless it is the door AND the call site is <c>AgentGuard.Engine.SystemServices.Create()</c>. This is
    /// AG0040's whole shape — it guards calls into <c>AgentGuard.Boundaries</c> and adds no type-reference coverage,
    /// because Engine legitimately names Boundaries nowhere at all.
    /// </summary>
    /// <param name="context">The analysis context to register on.</param>
    /// <param name="rule">The descriptor to report.</param>
    /// <param name="isInScope">The rule's target filter — true when the used type belongs to the guarded assembly.</param>
    /// <param name="isDoorMember">The rule's door test for a used member.</param>
    internal static void RegisterEngineCallGate(
        AnalysisContext context,
        DiagnosticDescriptor rule,
        Func<INamedTypeSymbol, bool> isInScope,
        Func<ISymbol, INamedTypeSymbol, bool> isDoorMember)
    {
        context.RegisterCompilationStartAction(startContext => MemberUseScanner.RegisterForAssembly(
            startContext,
            EngineAssembly.Name,
            (operationContext, member, type) => InspectCall(
                operationContext, rule, member, type, isInScope, isDoorMember, requireContainerFactorySite: true)));
    }

    /// <summary>
    /// Registers BOTH gates of a rule that guards one target assembly from two consumers: the unchanged
    /// <c>AgentGuard.Boundaries</c> call gate, and the <c>AgentGuard.Engine</c> gate with its caller constraint and its
    /// two type-reference lenses. This is the shape AG0023 and AG0029 share; they differ only in which assembly is the
    /// target and which type is the door.
    /// <para>
    /// The two gates take SEPARATE target filters, deliberately. The Boundaries gate keeps each rule's pre-existing
    /// filter untouched, so no expectation outside Engine moves. The Engine-facing registrations — the call gate and
    /// BOTH type-reference lenses — take the filter that also admits a same-named decoy squatting the guarded
    /// namespace from a foreign assembly, so such a decoy lands in scope and then FAILS the door test instead of
    /// falling out of scope and being silently accepted. All three Engine-facing registrations receive the same
    /// filter, because a decoy reached as a written or carried TYPE is the same spoof as one reached by a call.
    /// </para>
    /// </summary>
    /// <param name="context">The analysis context to register on.</param>
    /// <param name="rule">The descriptor to report.</param>
    /// <param name="isInScopeFromBoundaries">The rule's target filter for the unchanged <c>AgentGuard.Boundaries</c>
    /// gate — true when a reached type belongs to the guarded assembly.</param>
    /// <param name="isInScopeFromEngine">The rule's target filter for every <c>AgentGuard.Engine</c>-facing
    /// registration — true when a reached type belongs to the guarded assembly OR could pose as this rule's door from
    /// a foreign one.</param>
    /// <param name="isDoorMember">The rule's door test for a used member.</param>
    /// <param name="isDoorType">The rule's door test for a referenced type.</param>
    internal static void RegisterBoundariesAndEngineGates(
        AnalysisContext context,
        DiagnosticDescriptor rule,
        Func<INamedTypeSymbol, bool> isInScopeFromBoundaries,
        Func<INamedTypeSymbol, bool> isInScopeFromEngine,
        Func<ISymbol, INamedTypeSymbol, bool> isDoorMember,
        Func<INamedTypeSymbol, bool> isDoorType)
    {
        context.RegisterCompilationStartAction(startContext =>
        {
            string? callingAssembly = startContext.Compilation.AssemblyName;

            MemberUseScanner.RegisterForAssembly(
                startContext,
                BoundaryAssembly.Name,
                (operationContext, member, type) => InspectCall(
                    operationContext,
                    rule,
                    member,
                    type,
                    isInScopeFromBoundaries,
                    isDoorMember,
                    requireContainerFactorySite: false));

            MemberUseScanner.RegisterForAssembly(
                startContext,
                EngineAssembly.Name,
                (operationContext, member, type) => InspectCall(
                    operationContext,
                    rule,
                    member,
                    type,
                    isInScopeFromEngine,
                    isDoorMember,
                    requireContainerFactorySite: true));

            WrittenNameScanner.RegisterForAssembly(
                startContext,
                EngineAssembly.Name,
                (nodeContext, symbol) => InspectWrittenName(nodeContext, rule, symbol, isInScopeFromEngine, isDoorType));

            DeclaredTypeScanner.RegisterForAssembly(
                startContext,
                EngineAssembly.Name,
                (declaredType, declaringSymbol, location) => TypeReferenceDiagnostic(
                    rule, location, callingAssembly, declaredType, declaringSymbol, isInScopeFromEngine, isDoorType));
        });
    }

    // A member use: the target filter first — a use that does not reach the guarded assembly at all is not this rule's
    // business, so neither the door nor the site is considered for it.
    private static void InspectCall(
        OperationAnalysisContext context,
        DiagnosticDescriptor rule,
        ISymbol member,
        INamedTypeSymbol type,
        Func<INamedTypeSymbol, bool> isInScope,
        Func<ISymbol, INamedTypeSymbol, bool> isDoorMember,
        bool requireContainerFactorySite)
    {
        if (!isInScope(type))
        {
            return;
        }

        bool doorFailed = !isDoorMember(member, type);
        bool siteFailed = requireContainerFactorySite
            && !CompositionPoint.IsContainerFactoryMethod(SymbolResolution.EnclosingSymbol(
                context.Operation.SemanticModel,
                context.Operation.Syntax,
                context.ContainingSymbol,
                context.CancellationToken));

        if (!doorFailed && !siteFailed)
        {
            return;
        }

        string subject = "the called member '" + MemberUseScanner.Describe(member, type) + NotTheDoor;
        context.ReportDiagnostic(Diagnostic.Create(
            rule,
            context.Operation.Syntax.GetLocation(),
            context.Compilation.AssemblyName,
            Failure(subject, doorFailed, siteFailed, CallSiteFailure)));
    }

    // A written name: only a TYPE reference is this half's business, because a member reach is already the call half's.
    private static void InspectWrittenName(
        SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor rule,
        ISymbol symbol,
        Func<INamedTypeSymbol, bool> isInScope,
        Func<INamedTypeSymbol, bool> isDoorType)
    {
        if (symbol is not ITypeSymbol referenced)
        {
            return;
        }

        Diagnostic? diagnostic = TypeReferenceDiagnostic(
            rule,
            context.Node.GetLocation(),
            context.Compilation.AssemblyName,
            referenced,
            SymbolResolution.EnclosingSymbol(
                context.SemanticModel, context.Node, context.ContainingSymbol, context.CancellationToken),
            isInScope,
            isDoorType);

        if (diagnostic is not null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    // A TYPE reference — a type the source wrote out, or a type a declaration carries. It is reported unless every
    // guarded type it reaches is this rule's door type AND the reference sits directly inside
    // AgentGuard.Engine.SystemServices.Create(). The whole type tree is walked through the shared TypeTree owner, so a
    // guarded type nested inside a generic argument, an array element, or a pointer target is caught; the two walks
    // are separate so the message can tell "reaches a guarded type that is not the door" from "reaches the door from
    // the wrong site".
    private static Diagnostic? TypeReferenceDiagnostic(
        DiagnosticDescriptor rule,
        Location location,
        string? callingAssembly,
        ITypeSymbol? referenced,
        ISymbol? containingSymbol,
        Func<INamedTypeSymbol, bool> isInScope,
        Func<INamedTypeSymbol, bool> isDoorType)
    {
        bool reachesNonDoor = TypeTree.Any(referenced, reached => isInScope(reached) && !isDoorType(reached));
        bool reachesDoor = TypeTree.Any(referenced, reached => isInScope(reached) && isDoorType(reached));
        bool siteFailed = reachesDoor && !CompositionPoint.IsContainerFactoryMethod(containingSymbol);

        if (!reachesNonDoor && !siteFailed)
        {
            return null;
        }

        string subject = "the referenced type '" + TypeTree.Describe(referenced) + NotTheDoor;
        return Diagnostic.Create(
            rule, location, callingAssembly, Failure(subject, reachesNonDoor, siteFailed, ReferenceSiteFailure));
    }

    // Names every condition that failed, and only those: the subject alone when the door test failed, the site alone
    // when the reached symbol IS the door but the site is wrong, and both joined when both failed.
    private static string Failure(string subject, bool subjectFailed, bool siteFailed, string siteFailure)
    {
        if (subjectFailed && siteFailed)
        {
            return subject + FailureSeparator + siteFailure;
        }

        return subjectFailed ? subject : siteFailure;
    }
}
