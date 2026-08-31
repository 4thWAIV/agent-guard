// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a native auth-context handle held as a field in a per-OS native presence-port implementer — a class
/// implementing <c>ILocalAuthentication</c> (macOS) or <c>IWindowsUserPresence</c> (Windows). Every presence check
/// must force a FRESH human interaction: a fresh <c>LAContext</c> per macOS check, a fresh Hello request per Windows
/// check (fresh-interaction-no-cached-yes). Caching the native handle (the <c>LAContext</c>/Hello-request pointer, an
/// <c>IntPtr</c>/<c>nint</c>/<c>UIntPtr</c>, or a <c>SafeHandle</c>) in an instance or static field would let a later
/// check reuse a prior authorization — a cached "yes" unobservable from above the seam, the macOS/Windows analogue of
/// the polkit no-cached-yes rule. The handle must be created per call and released, never stored. A <c>const</c> is
/// exempt (it cannot hold a live handle). Its field scan registers through the shared, production-assembly-gated
/// <see cref="FieldOwnershipScan.RegisterField"/>
/// (mirroring AG0116's <see cref="PresenceContracts.RegisterInMacOsOrWindows"/> gate), so it fires ONLY inside the macOS
/// and Windows PRODUCTION assemblies and is silent elsewhere — including <c>AgentGuard.CrossPlatform.Tests</c>, where a
/// native-handle-holding presence-port fake legitimately exists through the verified InternalsVisibleTo grants (the
/// internal port interface is implementable there, so internal-ness alone would not keep the scan off it). Preventive;
/// the native ports do not exist yet.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoCachedNativeAuthContextAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0112";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A native presence port must not cache a native auth-context handle in a field",
        messageFormat: "Field '{0}' caches a native auth-context handle in a presence-port implementer; create the handle per call so each check forces a fresh interaction, never reusing a prior authorization",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In a class implementing ILocalAuthentication or IWindowsUserPresence, a native auth-context handle (an IntPtr/nint/UIntPtr/nuint or a SafeHandle holding the LAContext or Hello request) must not be stored in an instance or static field: a cached handle could reuse a prior authorization, a cached 'yes' unobservable from above the seam. The handle is created per Check call and released. A const is exempt.");

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

        // The production-assembly gate (macOS/Windows), the const skip, the native-presence-port owner gate, and the
        // report-by-name are the shared, gated field-scan skeleton (FieldOwnershipScan.RegisterField, which bakes in
        // PresenceContracts.RegisterInMacOsOrWindows so this rule cannot register ungated and misfire on a
        // native-handle-holding presence-port fake in AgentGuard.CrossPlatform.Tests). This rule supplies only its owner
        // set (the native presence ports) and its native-handle predicate.
        FieldOwnershipScan.RegisterField(context, PresenceContracts.NativePresencePorts, IsCachedHandleField, Rule);
    }

    // A cached native auth-context handle is the narrower field this rule bans (unlike AG0116, which bans every field).
    private static bool IsCachedHandleField(IFieldSymbol field) => IsNativeHandleType(field.Type);

    private static bool IsNativeHandleType(ITypeSymbol type)
    {
        // A raw native pointer handle: IntPtr/UIntPtr (nint/nuint are the same symbols).
        if (type.SpecialType == SpecialType.System_IntPtr || type.SpecialType == SpecialType.System_UIntPtr)
        {
            return true;
        }

        // A function pointer or unsafe pointer to native memory.
        if (type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer)
        {
            return true;
        }

        // A SafeHandle (or any subclass) wrapping the native handle.
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (WellKnownType.Is(current as INamedTypeSymbol, KnownNamespaces.SystemRuntimeInteropServices, "SafeHandle"))
            {
                return true;
            }
        }

        return false;
    }
}
