// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Confines the managed D-Bus primitive to its single owner and bans the reflection-based high-level API outright. The
/// low-level <c>Tmds.DBus.Protocol</c> package (the pinned polkit transport) may be used only in the class implementing
/// <c>AgentGuard.CrossPlatform.Linux.IPolkitAuthority</c>, in the Linux implementation assembly — the one owner of the
/// polkit <c>CheckAuthorization</c> call — so a second, ungoverned presence path cannot form through the same boundary
/// primitive that AG0008/AG0011/AG0101 (native/filesystem) all miss. The high-level <c>Tmds.DBus</c> namespace (the
/// reflection-based proxy API) is banned everywhere: it is not single-file/AOT-clean and must never be used. This
/// reuses the single-owner conjunction <see cref="OwnerClass.IsOwner"/> (owning interface <c>IPolkitAuthority</c> +
/// the Linux assembly gate), as its own rule separate from the BCL-primitive table. Preventive; nothing references
/// Tmds today, and the owner (<c>TmdsPolkitAuthority</c>) is authored at IMPLEMENT.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DbusOnlyInPolkitAuthorityAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0110";

    private const string Category = "AgentGuard.Architecture";

    /// <summary>The low-level managed D-Bus namespace root (the pinned polkit transport), confined to the owner.</summary>
    private const string LowLevelDbusNamespace = "Tmds.DBus.Protocol";

    /// <summary>The high-level, reflection-based D-Bus namespace, banned everywhere (not single-file/AOT-clean).</summary>
    private const string HighLevelDbusNamespace = "Tmds.DBus";

    private static readonly Func<Compilation, bool> InLinuxOwnerAssembly = PresenceContracts.InLinux;

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Managed D-Bus must live only in the polkit authority owner; the high-level Tmds.DBus API is banned",
        messageFormat: "Managed D-Bus type '{0}' is used outside its owner; the low-level Tmds.DBus.Protocol lives only in the IPolkitAuthority owner (AgentGuard.CrossPlatform.Linux), and the reflection-based high-level Tmds.DBus namespace is banned everywhere",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The low-level Tmds.DBus.Protocol package (the pinned polkit transport) may be used only in the class implementing AgentGuard.CrossPlatform.Linux.IPolkitAuthority, compiled into the Linux implementation assembly — the single owner of the polkit CheckAuthorization call, so no second ungoverned presence path forms through the D-Bus primitive. The reflection-based high-level Tmds.DBus namespace is banned everywhere: it is not single-file/AOT-clean.");

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
        // The low-level transport (Tmds.DBus.Protocol and any sub-namespace) — confined to the IPolkitAuthority owner
        // in the Linux assembly. Checked first because it is a more specific prefix than the high-level namespace.
        if (WellKnownType.IsInNamespaceTree(type, LowLevelDbusNamespace))
        {
            if (!OwnerClass.IsOwner(context, PresenceContracts.PolkitAuthority, InLinuxOwnerAssembly))
            {
                Report(context, member, type);
            }

            return;
        }

        // The high-level reflection API (Tmds.DBus and any sub-namespace that is not the Protocol tree) — banned
        // everywhere, owner or not.
        if (WellKnownType.IsInNamespaceTree(type, HighLevelDbusNamespace))
        {
            Report(context, member, type);
        }
    }

    private static void Report(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Operation.Syntax.GetLocation(), MemberUseScanner.Describe(member, type)));
    }
}
