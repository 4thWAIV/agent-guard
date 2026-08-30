// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The shared gate-then-report block for the operation-scoped native-ops rules — AG0106 (no control flow in a
/// native-ops implementer) and AG0115 (no async orchestration in one). Both walk the identical tail at every report
/// site: require the analyzed operation to sit inside a native-ops owner (through
/// <see cref="PresenceContracts.IsInNativeOpsOwner"/>, the one shared owner check), then report the operation's own
/// syntax location with a rule-specific description. That tail is spelled exactly once here (LESSON 1, DRY) so neither
/// rule copies the other and no report site re-inlines the pair — mirroring <see cref="FieldOwnershipScan"/>, the same
/// once-and-only-once extraction for the two field-ban rules. Each caller computes only its own description string and
/// calls this; the owner gate and the <see cref="Diagnostic.Create(DiagnosticDescriptor, Location, object?[])"/> stay
/// here.
/// </summary>
internal static class NativeOpsOperationScan
{
    /// <summary>
    /// Reports the operation under analysis when it sits inside a native-ops owner (a class implementing
    /// <c>IObjCRuntime</c> in the macOS assembly or a Windows native-ops interface in the Windows assembly), locating the
    /// diagnostic at the operation's own syntax.
    /// </summary>
    /// <param name="context">The operation analysis context; its <see cref="OperationAnalysisContext.Operation"/> is reported.</param>
    /// <param name="rule">The descriptor to report, formatted with <paramref name="description"/>.</param>
    /// <param name="description">The rule-specific description of the offending construct.</param>
    internal static void ReportIfOwner(
        OperationAnalysisContext context,
        DiagnosticDescriptor rule,
        string description)
    {
        // Only inside a native-ops owner — a class implementing IObjCRuntime (macOS) or a Windows native-ops interface,
        // compiled into its per-OS assembly. The same owner check the relocated raw-interop exemption (AG0113/AG0101)
        // uses, so the two families of rule resolve the identical native-ops layer.
        if (!PresenceContracts.IsInNativeOpsOwner(context))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            rule, context.Operation.Syntax.GetLocation(), description));
    }
}
