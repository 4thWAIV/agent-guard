// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single-owner-class exemption shared by every OS-primitive boundary rule (AG0011, AG0012, AG0014, AG0016,
/// AG0101). A raw primitive call is allowed in exactly one place — the class that implements the owning interface,
/// compiled into the assembly where that owner lives (owners-live-at-lowest-consumer) — and nowhere else, not even
/// in another class of the same assembly or the same class in another assembly. The exemption is a CONJUNCTION,
/// resolved once in <see cref="IsOwner"/>: the enclosing type is an owner of the primitive (it implements one of the
/// owning interfaces, matched structurally through the semantic model against the type's
/// <see cref="ITypeSymbol.AllInterfaces"/> by full name, or — for AG0101 — an additional named-helper owner) AND the
/// compilation compiles into an owner assembly (matched by <see cref="InAssembly"/> against a shared name constant,
/// or by <see cref="CrossPlatformBoundary.IsCrossPlatformLibrary"/> for the platform set). Both halves are required,
/// so a class cannot self-grant by declaring <c>: IFileReader</c> in the wrong assembly, and an assembly cannot
/// self-grant by declaring a same-named helper. This is the one place that conjunction lives, so the five rules do
/// not each spell it out.
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
    /// Builds the assembly gate for the four single-owner-assembly rules: a predicate that is satisfied only when
    /// the compilation's assembly name is exactly <paramref name="ownerAssemblyName"/>. The name-equality comparison
    /// lives here once so AG0011/AG0012/AG0014/AG0016 do not each spell it out; each rule caches the returned
    /// predicate in a static field, so no delegate is allocated per analyzed operation.
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
    /// the primitive — it implements one of the <paramref name="owningInterfaces"/>, or the optional
    /// <paramref name="additionalOwner"/> predicate matches it (AG0101's shared helper). Requiring both closes the
    /// self-grant hole: implementing the owning interface in the wrong assembly is not exempt, and declaring a
    /// same-named helper in the wrong assembly is not exempt.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="owningInterfaces">The (namespace, name) pairs of the interfaces whose implementer is exempt.</param>
    /// <param name="compilesIntoOwnerAssembly">The assembly gate — the compilation must compile into an owner
    /// assembly. Built with <see cref="InAssembly"/> for the single-owner-assembly rules, or
    /// <see cref="CrossPlatformBoundary.IsCrossPlatformLibrary"/> for the platform set.</param>
    /// <param name="additionalOwner">An optional extra owner-type predicate for a non-interface owner (AG0101's
    /// <c>PlatformFileSystemShared</c> helper); <see langword="null"/> for the interface-only rules.</param>
    /// <returns><see langword="true"/> when the operation is inside the owner class in the owner assembly.</returns>
    internal static bool IsOwner(
        OperationAnalysisContext context,
        ImmutableArray<(string Namespace, string Name)> owningInterfaces,
        Func<Compilation, bool> compilesIntoOwnerAssembly,
        Func<INamedTypeSymbol?, bool>? additionalOwner = null)
    {
        if (!compilesIntoOwnerAssembly(context.Compilation))
        {
            return false;
        }

        INamedTypeSymbol? enclosingType = EnclosingType(context);
        return Implements(enclosingType, owningInterfaces)
            || (additionalOwner is not null && additionalOwner(enclosingType));
    }
}
