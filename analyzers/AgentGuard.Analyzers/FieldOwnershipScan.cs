// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The shared symbol-scan skeleton for the presence-layer state-ban rules — AG0112 (no cached native auth-context
/// handle field in a native presence port) and AG0116 (no field, auto-property, or field-like event in a native-ops
/// implementer). Every one of those checks walks the identical tail: require the symbol's containing type to implement
/// one of the owning interfaces (through <see cref="OwnerClass.Implements"/>, the one shared membership walk), apply a
/// rule-specific predicate, and report the member by name. That tail is spelled exactly once here (LESSON 1, DRY) in the
/// symbol-kind-agnostic <see cref="Report{TSymbol}"/> so neither rule — nor AG0116's own field/property/event actions —
/// copies the other; each supplies its own owner set, predicate, and descriptor. Fields also skip a <c>const</c> (a
/// compile-time literal that holds no live state), so the field callers route through <see cref="ReportField"/>, the
/// thin const-skipping wrapper over the shared tail. The AG0116 auto-property and field-like-event state each surface
/// through their own symbol kind (a compiler-synthesized backing field is implicitly declared and never reaches a field
/// action), so those actions call <see cref="Report{TSymbol}"/> directly with <c>IPropertySymbol</c>/<c>IEventSymbol</c>
/// and their own reportability predicate.
/// The owning interfaces are internal, but a test fake in <c>AgentGuard.CrossPlatform.Tests</c> can implement one
/// through the verified InternalsVisibleTo grants, so internal-ness alone does NOT keep this scan off a test-assembly
/// fake; the production-assembly gate (macOS/Windows) is required. A single-field-action rule (AG0112) registers through
/// <see cref="RegisterField"/>,
/// which bakes <see cref="PresenceContracts.RegisterInMacOsOrWindows"/> into the registration so the gate cannot be
/// omitted at the consumer; AG0116 registers its four member actions together behind that same gate in its own
/// <c>OnCompilationStart</c>.
/// </summary>
internal static class FieldOwnershipScan
{
    /// <summary>
    /// Registers the shared const-skipping field scan for a single-field-ban rule (AG0112), gated to the macOS or
    /// Windows PRODUCTION assembly. The production-assembly gate is baked in here — the field action is registered only
    /// inside <see cref="PresenceContracts.RegisterInMacOsOrWindows"/> — so a consumer cannot register the field scan
    /// while forgetting the gate: an owning interface is internal but a <c>AgentGuard.CrossPlatform.Tests</c> fake can
    /// implement it through InternalsVisibleTo, and without the gate a native-handle-holding test fake would misfire.
    /// The registered action defers to the <see cref="ReportField"/> tail (const skip, owner gate, report by name).
    /// </summary>
    /// <param name="context">The analysis context from the analyzer's <c>Initialize</c>.</param>
    /// <param name="owningInterfaces">The (namespace, name) pairs whose implementer this rule constrains.</param>
    /// <param name="isReportableField">The rule-specific predicate deciding whether the (non-const, owned) field violates.</param>
    /// <param name="rule">The descriptor to report, formatted with the field name.</param>
    internal static void RegisterField(
        AnalysisContext context,
        ImmutableArray<(string Namespace, string Name)> owningInterfaces,
        Func<IFieldSymbol, bool> isReportableField,
        DiagnosticDescriptor rule)
    {
        // Bake the production-assembly gate into the registration: the field action is registered ONLY when the
        // compilation is the macOS or Windows per-OS production library (PresenceContracts.RegisterInMacOsOrWindows), so
        // no consumer can register it ungated and misfire on a native-handle-holding test fake in AgentGuard.CrossPlatform.Tests.
        context.RegisterCompilationStartAction(startContext =>
            PresenceContracts.RegisterInMacOsOrWindows(
                startContext,
                gated => gated.RegisterSymbolAction(
                    fieldContext => ReportField(fieldContext, owningInterfaces, isReportableField, rule),
                    SymbolKind.Field)));
    }

    /// <summary>
    /// Reports the symbol under analysis when it is declared on a type implementing one of
    /// <paramref name="owningInterfaces"/> and satisfies <paramref name="isReportable"/>. The symbol-kind-agnostic
    /// tail — gate to an owning-interface implementer, apply the predicate, report by name — shared by every
    /// presence-layer state-ban action (field, auto-property, field-like event) so none re-inlines it.
    /// </summary>
    /// <typeparam name="TSymbol">The analyzed symbol kind (a field, property, or event).</typeparam>
    /// <param name="context">The symbol analysis context; its <see cref="SymbolAnalysisContext.Symbol"/> is the member.</param>
    /// <param name="owningInterfaces">The (namespace, name) pairs whose implementer this rule constrains.</param>
    /// <param name="isReportable">The rule-specific predicate deciding whether the (owned) member violates.</param>
    /// <param name="rule">The descriptor to report, formatted with the member name.</param>
    internal static void Report<TSymbol>(
        SymbolAnalysisContext context,
        ImmutableArray<(string Namespace, string Name)> owningInterfaces,
        Func<TSymbol, bool> isReportable,
        DiagnosticDescriptor rule)
        where TSymbol : ISymbol
    {
        var symbol = (TSymbol)context.Symbol;

        // Only a member declared on a class that implements one of the owning interfaces. The owning interface is
        // internal but reachable from AgentGuard.CrossPlatform.Tests through InternalsVisibleTo, so a match CAN occur on a
        // test-assembly fake; internal-ness alone is not the assembly gate. The production-assembly gate (macOS/Windows)
        // is baked into the shared registration — RegisterField for a single-field rule, AG0116's own
        // RegisterInMacOsOrWindows for its four member actions — so it cannot be omitted at a consumer.
        if (!OwnerClass.Implements(symbol.ContainingType, owningInterfaces))
        {
            return;
        }

        if (!isReportable(symbol))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(rule, symbol.Locations[0], symbol.Name));
    }

    /// <summary>
    /// Reports the field under analysis when it is a non-<c>const</c> field, declared on a type implementing one of
    /// <paramref name="owningInterfaces"/>, that satisfies <paramref name="isReportableField"/>. The const skip is the
    /// only field-specific step; it defers to the shared <see cref="Report{TSymbol}"/> tail for the owner gate and the
    /// report, so the two field-ban rules (AG0112, AG0116) neither copy the tail nor re-spell the const skip.
    /// </summary>
    /// <param name="context">The field symbol analysis context; its <see cref="SymbolAnalysisContext.Symbol"/> is the field.</param>
    /// <param name="owningInterfaces">The (namespace, name) pairs whose implementer this rule constrains.</param>
    /// <param name="isReportableField">The rule-specific predicate deciding whether the (non-const, owned) field violates.</param>
    /// <param name="rule">The descriptor to report, formatted with the field name.</param>
    internal static void ReportField(
        SymbolAnalysisContext context,
        ImmutableArray<(string Namespace, string Name)> owningInterfaces,
        Func<IFieldSymbol, bool> isReportableField,
        DiagnosticDescriptor rule)
    {
        var field = (IFieldSymbol)context.Symbol;

        // A const is a compile-time literal that holds no live state; only a non-const field can.
        if (field.IsConst)
        {
            return;
        }

        Report(context, owningInterfaces, isReportableField, rule);
    }
}
