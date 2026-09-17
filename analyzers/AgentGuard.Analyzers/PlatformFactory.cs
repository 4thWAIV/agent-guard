// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The platform factory identity — the static <c>Create</c> method on the <c>PlatformServices</c> type in the
/// <c>AgentGuard.CrossPlatform</c> namespace. This is the self-building container factory the
/// <c>container-is-one-class-with-its-own-create</c> mandate makes the one per-OS door, replacing the separate
/// <c>Platform</c> factory. Two rules test for this same factory from different angles (AG0010 checks the factory's
/// own declaration returns the <c>IPlatformServices</c> container, AG0029 checks that the one call
/// <c>AgentGuard.Boundaries</c> makes into a per-OS assembly is this factory), so the "is this PlatformServices.Create"
/// test lives here once rather than being re-spelled per rule. Each match anchors on full type identity (namespace +
/// name via <see cref="WellKnownType"/>) AND a static method named <c>Create</c>, a conjunction: a method merely
/// NAMED <c>Create</c>, or a <c>PlatformServices</c> type in another namespace, cannot self-grant the match.
/// </summary>
internal static class PlatformFactory
{
    /// <summary>
    /// The simple name of the platform container factory type — <c>PlatformServices</c>, declared in the
    /// <see cref="CrossPlatformBoundary.RootName"/> namespace. <c>PlatformServices.Create()</c> is the mandated
    /// self-building shape (a parameterless static factory returning <c>IPlatformServices</c>, on the per-OS
    /// <c>PlatformServices</c> class), so it is the one door — the same shape as <c>SystemServices.Create()</c>.
    /// </summary>
    private const string TypeName = "PlatformServices";

    /// <summary>
    /// The name of the static factory method — <c>Create</c>.
    /// </summary>
    private const string MethodName = "Create";

    /// <summary>
    /// Gets a value indicating whether <paramref name="method"/> is the platform factory itself — the static
    /// <c>Create</c> method declared on the <c>PlatformServices</c> type in <c>AgentGuard.CrossPlatform</c>. This is
    /// the declaration-site shape AG0010 uses when it walks method symbols to find the factory whose return type it
    /// must check.
    /// </summary>
    /// <param name="method">The method symbol to test.</param>
    /// <returns><see langword="true"/> when the method is the static <c>PlatformServices.Create</c> factory.</returns>
    internal static bool Is(IMethodSymbol method)
    {
        return method.IsStatic
            && string.Equals(method.Name, MethodName, StringComparison.Ordinal)
            && IsFactoryType(method.ContainingType);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/>, used on <paramref name="type"/>, is a call to the
    /// platform factory — the static <c>Create</c> on the <c>PlatformServices</c> type in
    /// <c>AgentGuard.CrossPlatform</c>. This is the call-site shape AG0029 uses when it inspects a member use from
    /// <c>AgentGuard.Boundaries</c> or <c>AgentGuard.Engine</c> into a per-OS assembly, where the used member and the
    /// type it is used on arrive separately.
    /// </summary>
    /// <param name="member">The used member symbol.</param>
    /// <param name="type">The type the member is used on.</param>
    /// <returns><see langword="true"/> when the use is a call to the static <c>PlatformServices.Create</c> factory.</returns>
    internal static bool Is(ISymbol member, INamedTypeSymbol type)
    {
        // Delegate to the IMethodSymbol shape so the identity conjunction lives in exactly one body (mirrors
        // OwnerClass.EnclosingType). For every operation kind AG0029 inspects, `type` is the member's containing
        // type; assert that and reuse Is(method).
        return member is IMethodSymbol method
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, type)
            && Is(method);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is the platform container factory TYPE itself — the
    /// <c>PlatformServices</c> type in the <c>AgentGuard.CrossPlatform</c> namespace — regardless of which member of
    /// it is being reached. This is the door-TYPE identity AG0029's Engine gate tests when it inspects a written or
    /// carried reference to a per-OS type rather than a call, so the one <c>PlatformServices</c> identity is spelled
    /// here once and the type-reference half cannot drift from the call half. The assembly is deliberately NOT part of
    /// this conjunction: the type is declared in each of the three per-OS implementation assemblies under the same
    /// namespace, so the CALLER supplies the assembly half of the door's identity. AG0029 conjoins this check with
    /// <see cref="CrossPlatformBoundary.IsPerOsImplementationAssembly"/>, which is how a same-named decoy from a
    /// foreign assembly is rejected even where that rule's Engine-facing scope deliberately admits it.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is the <c>PlatformServices</c> platform container factory.</returns>
    internal static bool IsFactoryType(INamedTypeSymbol? type)
    {
        return WellKnownType.Is(type, CrossPlatformBoundary.RootName, TypeName);
    }
}
