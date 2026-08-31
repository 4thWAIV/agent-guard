// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single-owner-class exemption shared by the OS-primitive boundary rules — the consolidated owner rule (AG0011),
/// which resolves every primitive's owner through <see cref="OwnedPrimitives"/>, and the OS-divergent rule (AG0101).
/// A raw primitive call is allowed in exactly one place — the class that implements the owning interface, compiled
/// into the assembly where that owner lives (owners-live-at-lowest-consumer) — and nowhere else, not even in another
/// class of the same assembly or the same class in another assembly. The exemption is a CONJUNCTION, resolved once in
/// <see cref="IsOwner"/>: the enclosing type is an owner of the primitive (it implements one of the owning interfaces,
/// matched structurally through the semantic model against the type's <see cref="ITypeSymbol.AllInterfaces"/> by full
/// name) AND the compilation compiles into an owner assembly (matched by <see cref="InAssembly"/> against a shared name
/// constant, or by a <see cref="CrossPlatformBoundary"/> predicate scoped to the owner's assemblies — e.g.
/// <see cref="CrossPlatformBoundary.IsPerOsImplementationAssembly"/> for AG0101's per-OS owner). Both halves are
/// required, so a class cannot self-grant by declaring <c>: IFileReader</c> in the wrong assembly. This is the one
/// place that conjunction lives, so the rules do not each spell it out.
/// </summary>
internal static class OwnerClass
{
    /// <summary>
    /// Resolves the named type that encloses the analyzed operation — the class, struct, or record whose member
    /// body (or field/property initializer) contains the operation. A nested type resolves to the nested type, not
    /// its outer type, so only the exact class that implements the owning interface is ever exempt.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <returns>The enclosing named type, or <see langword="null"/> when there is none.</returns>
    internal static INamedTypeSymbol? EnclosingType(OperationAnalysisContext context)
    {
        return EnclosingType(context.ContainingSymbol);
    }

    /// <summary>
    /// Resolves the named type that encloses <paramref name="containingSymbol"/> — the symbol itself when it is a
    /// named type, otherwise its containing type. The one place this "symbol → enclosing named type" step lives, so
    /// callers that hold a symbol rather than an operation context (for example the AG0017 composition-point walk)
    /// do not re-derive it.
    /// </summary>
    /// <param name="containingSymbol">The symbol whose enclosing named type is wanted.</param>
    /// <returns>The enclosing named type, or <see langword="null"/> when there is none.</returns>
    internal static INamedTypeSymbol? EnclosingType(ISymbol containingSymbol)
    {
        return containingSymbol as INamedTypeSymbol ?? containingSymbol.ContainingType;
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="enclosingType"/> implements any of the
    /// <paramref name="owningInterfaces"/>, matched by full name (namespace + simple name) against the type's
    /// <see cref="ITypeSymbol.AllInterfaces"/>. This is the structural half of the owner exemption: the class that
    /// implements the owning interface. It is never sufficient on its own — <see cref="IsOwner"/> pairs it with the
    /// assembly gate.
    /// </summary>
    /// <param name="enclosingType">The type that encloses the operation, from <see cref="EnclosingType(OperationAnalysisContext)"/>.</param>
    /// <param name="owningInterfaces">The (namespace, name) pairs of the interfaces whose implementer is exempt.</param>
    /// <returns><see langword="true"/> when the enclosing type implements one of the owning interfaces.</returns>
    internal static bool Implements(
        INamedTypeSymbol? enclosingType, ImmutableArray<(string Namespace, string Name)> owningInterfaces)
    {
        return enclosingType is not null
            && enclosingType.AllInterfaces.Any(implemented => WellKnownType.IsAnyOf(implemented, owningInterfaces));
    }

    /// <summary>
    /// Gets a value indicating whether an invoked (or referenced) member is <paramref name="memberName"/> declared on
    /// one of the <paramref name="owningInterfaces"/> OR on a type that implements one of them — the single
    /// "is this a call to member M on a type implementing interface I" recognition, owned here on top of
    /// <see cref="Implements"/> so no rule reinvents it. <paramref name="declaringType"/> is the member's
    /// <see cref="ISymbol.ContainingType"/>: when the reference goes through the interface it is the interface itself
    /// (matched by <see cref="WellKnownType.IsAnyOf"/>), and when it goes through a CONCRETE reference it is the
    /// implementer (matched by <see cref="Implements"/>, which walks the type's <see cref="ITypeSymbol.AllInterfaces"/>).
    /// Both resolve to the same interface member, so a call reached through a concrete-typed reference is caught the same
    /// as one through the interface-typed reference — closing the concrete-reference bypass. Whether such a call is legal
    /// is the caller's policy, applied on top.
    /// </summary>
    /// <param name="member">The invoked or referenced member.</param>
    /// <param name="declaringType">The type that declares the member (the member's containing type).</param>
    /// <param name="memberName">The simple name the member must have.</param>
    /// <param name="owningInterfaces">The (namespace, name) pairs of the interfaces the member must be declared on, or
    /// that the declaring type must implement.</param>
    /// <returns><see langword="true"/> when the member is the named member on one of the interfaces or on an implementer of one.</returns>
    internal static bool IsInterfaceMemberInvocation(
        ISymbol member,
        INamedTypeSymbol? declaringType,
        string memberName,
        ImmutableArray<(string Namespace, string Name)> owningInterfaces)
    {
        return string.Equals(member.Name, memberName, StringComparison.Ordinal)
            && (WellKnownType.IsAnyOf(declaringType, owningInterfaces) || Implements(declaringType, owningInterfaces));
    }

    /// <summary>
    /// Builds the assembly gate for the single-owner-assembly primitives: a predicate that is satisfied only when
    /// the compilation's assembly name is exactly <paramref name="ownerAssemblyName"/>. The name-equality comparison
    /// lives here once so the owner rule's per-primitive gates (AgentGuard.CrossPlatform for the filesystem/GUID
    /// owners, AgentGuard.Boundaries for the environment/console/signature/build-info owners) do not each spell it
    /// out; each is cached in a static field, so no delegate is allocated per analyzed operation.
    /// </summary>
    /// <param name="ownerAssemblyName">The exact assembly name where the owner class lives (a shared name constant,
    /// never a re-spelled literal).</param>
    /// <returns>A predicate that is <see langword="true"/> only for that assembly.</returns>
    internal static Func<Compilation, bool> InAssembly(string ownerAssemblyName)
    {
        return compilation => string.Equals(compilation.AssemblyName, ownerAssemblyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets a value indicating whether the analyzed operation is inside the single owner of its primitive — the
    /// conjunction that every OS-primitive boundary rule shares. Both halves are required: the compilation compiles
    /// into an owner assembly (<paramref name="compilesIntoOwnerAssembly"/>) AND the enclosing type is an owner of
    /// the primitive — it implements one of the <paramref name="owningInterfaces"/>. Requiring both closes the
    /// self-grant hole: implementing the owning interface in the wrong assembly is not exempt.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="owningInterfaces">The (namespace, name) pairs of the interfaces whose implementer is exempt.</param>
    /// <param name="compilesIntoOwnerAssembly">The assembly gate — the compilation must compile into an owner
    /// assembly. Built with <see cref="InAssembly"/> for the single-owner-assembly rules, or a
    /// <see cref="CrossPlatformBoundary"/> predicate scoped to the owner's assemblies, such as
    /// <see cref="CrossPlatformBoundary.IsPerOsImplementationAssembly"/> for AG0101's per-OS owner.</param>
    /// <returns><see langword="true"/> when the operation is inside the owner class in the owner assembly.</returns>
    internal static bool IsOwner(
        OperationAnalysisContext context,
        ImmutableArray<(string Namespace, string Name)> owningInterfaces,
        Func<Compilation, bool> compilesIntoOwnerAssembly)
    {
        if (!compilesIntoOwnerAssembly(context.Compilation))
        {
            return false;
        }

        INamedTypeSymbol? enclosingType = EnclosingType(context);
        return Implements(enclosingType, owningInterfaces);
    }
}
