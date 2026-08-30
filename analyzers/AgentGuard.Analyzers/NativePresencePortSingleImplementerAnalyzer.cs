// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a SECOND (or later) class implementing one of the internal per-OS native presence interfaces — the three
/// FLOW ports (<c>ILocalAuthentication</c> (macOS), <c>IWindowsUserPresence</c> (Windows), <c>IPolkitAuthority</c>
/// (Linux)) AND the per-OS native-OPS owners the coverage refactor adds one layer below them (<c>IObjCRuntime</c>
/// (macOS), <c>IWindowsHelloNativeOps</c>/<c>ICredentialPromptNativeOps</c> (Windows)). Each must have exactly one
/// implementer: the single class that touches its native library or the pinned D-Bus package
/// (per-os-native-behind-a-port). These interfaces live OUTSIDE the <c>ISystemServices</c> tree, so the container
/// single-owner rules (AG0025/AG0030) do not cover them — without this guard "exactly one class owns each native
/// library" would be a convention, not a build invariant, and a second, ungoverned presence path could form. Each is
/// internal to its own per-OS assembly, so in any one compilation only that assembly's interfaces are in scope; the
/// shared accumulate-and-report algorithm (<see cref="SecondImplementerGuard"/>) permits ONE implementer and reports the
/// rest. Preventive; none of the native-ops owners exists yet.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NativePresencePortSingleImplementerAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0114";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A per-OS native presence port must have exactly one implementer",
        messageFormat: "Type '{0}' is a second implementer of native presence port '{1}'; each port must have exactly one implementer — the single class that owns its native library or the pinned D-Bus package",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each internal per-OS native presence port — ILocalAuthentication (macOS), IWindowsUserPresence (Windows), IPolkitAuthority (Linux) — must have exactly one implementer, the single class that touches its native library or the pinned Tmds.DBus.Protocol package. These ports live outside the ISystemServices tree, so AG0025/AG0030 do not cover them; without this guard the one-owner-per-native-library invariant would be a convention, not a build error. The second and later implementer of any port is reported.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

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
        // The candidate set is the three per-OS FLOW ports PLUS the per-OS native-OPS owners the coverage refactor adds
        // (IObjCRuntime, IWindowsHelloNativeOps, ICredentialPromptNativeOps) — each an internal interface that must have
        // exactly one implementer, the single thin class that touches its native library. Each is internal to its own
        // assembly, so per-compilation resolution scopes the guard to the one interface in scope; the shared guard
        // permits ONE and reports the rest.
        SecondImplementerGuard.Register(
            context, PresenceContracts.AllNativePorts.AddRange(PresenceContracts.AllNativeOpsOwners), Rule, permittedImplementers: 1);
    }
}
