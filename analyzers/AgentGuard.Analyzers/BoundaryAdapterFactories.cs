// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The four — and only four — <c>AgentGuard.Boundaries</c> factory identities <c>AgentGuard.Engine</c> may call
/// (AG0040): the static <c>Create</c> method declared on <c>EnvironmentAdapter</c>, <c>ConsoleAdapter</c>,
/// <c>Ed25519SignatureService</c>, and <c>BuildInfoReader</c>, each in namespace AND assembly
/// <c>AgentGuard.Boundaries</c>. Every identity is matched on assembly, namespace, type name, method name, and
/// staticness together through the shared <see cref="WellKnownType.IsInAssembly"/> conjunction, so a same-named type
/// in another namespace or another assembly cannot pose as a door. A method's RETURN TYPE grants nothing: a fifth
/// Boundaries factory that hands back a service interface is rejected until this list is changed to name it, which is
/// an explicit rule change.
/// <para>
/// This follows the shape <see cref="PlatformFactory"/> established for the per-OS door — static plus method name plus
/// type identity — rather than sharing its body: the per-OS door and the boundary adapters are independent facts about
/// independent types that happen to share the mandated <c>Create</c> spelling today.
/// </para>
/// </summary>
internal static class BoundaryAdapterFactories
{
    /// <summary>
    /// The one-line description of this door used in a diagnostic message, spelled here beside the list it describes
    /// so the message and the test can never name different sets.
    /// </summary>
    internal const string Description =
        "one of the four permitted AgentGuard.Boundaries adapter factories (EnvironmentAdapter.Create, "
        + "ConsoleAdapter.Create, Ed25519SignatureService.Create, BuildInfoReader.Create)";

    /// <summary>
    /// The name of the static factory method every permitted adapter exposes — <c>Create</c>.
    /// </summary>
    private const string MethodName = "Create";

    /// <summary>
    /// The simple names of the four permitted factory types, each declared in namespace and assembly
    /// <c>AgentGuard.Boundaries</c>. Exactly these four; adding a fifth is a rule change.
    /// </summary>
    private static readonly ImmutableArray<string> FactoryTypeNames = ImmutableArray.Create(
        "EnvironmentAdapter",
        "ConsoleAdapter",
        "Ed25519SignatureService",
        "BuildInfoReader");

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/>, used on <paramref name="type"/>, is one of the four
    /// permitted boundary adapter factories. This is the call-site shape AG0040 uses, where the used member and the
    /// type it is used on arrive separately from <see cref="MemberUseScanner"/>.
    /// </summary>
    /// <param name="member">The used member symbol.</param>
    /// <param name="type">The type the member is used on.</param>
    /// <returns><see langword="true"/> when the use is a call to one of the four permitted factories.</returns>
    internal static bool Is(ISymbol member, INamedTypeSymbol type)
    {
        return member is IMethodSymbol { IsStatic: true } method
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, type)
            && string.Equals(method.Name, MethodName, StringComparison.Ordinal)
            && IsFactoryType(type);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is one of the four permitted factory types, matched on
    /// namespace, name, and declaring assembly together.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is one of the four permitted factories.</returns>
    private static bool IsFactoryType(INamedTypeSymbol type)
    {
        return FactoryTypeNames.Any(
            factoryTypeName =>
                WellKnownType.IsInAssembly(type, BoundaryAssembly.Name, factoryTypeName, BoundaryAssembly.Name));
    }
}
