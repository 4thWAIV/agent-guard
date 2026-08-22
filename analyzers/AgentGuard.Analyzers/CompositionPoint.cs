// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The designated construction sites the rules carve out, held here once so a "is this the right site" test is not
/// re-spelled per rule. Three distinct concepts live here, each anchored on full type identity (namespace + name via
/// <see cref="WellKnownType"/>) AND the assembly — a conjunction, so a type merely NAMED the same in another namespace
/// or assembly cannot self-grant the exemption:
/// <list type="bullet">
/// <item>the <b>callers</b> — the single <c>Program</c> composition method in <c>AgentGuard.Cli</c> and the test
/// <c>SystemServicesBuilder</c> in <c>AgentGuard.TestHelpers</c>: the only places <c>SystemServices.Create()</c> may be
/// <em>called</em> (AG0017), a service type may be held (AG0024), or a service may be a parameter (AG0031);</item>
/// <item>the <b>construction site</b> — <c>SystemServices</c> in <c>AgentGuard.Boundaries</c> (whose <c>Create()</c>
/// wires the container) and the test <c>SystemServicesBuilder</c>: the only places a direct <c>TimeProvider.System</c>
/// acquisition is legal (AG0015, clock-legal-in-create-and-builder);</item>
/// <item>the <b>wrapper factory</b> — <c>FileInfoFactory</c> in <c>AgentGuard.CrossPlatform</c>: the only place an
/// <c>AbstractedFileInfo</c>/<c>AbstractedDirectoryInfo</c> wrapper may be constructed (AG0033).</item>
/// </list>
/// </summary>
internal static class CompositionPoint
{
    private const string CompositionTypeName = "Program";
    private const string BuilderTypeName = "SystemServicesBuilder";
    private const string ContainerFactoryTypeName = "SystemServices";
    private const string WrapperFactoryTypeName = "FileInfoFactory";

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is one of the two composition callers — the
    /// <c>Program</c> type in <c>AgentGuard.Cli</c> or the <c>SystemServicesBuilder</c> type in
    /// <c>AgentGuard.TestHelpers</c>.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is a composition caller.</returns>
    internal static bool IsCompositionType(INamedTypeSymbol? type)
    {
        return IsProgramInCli(type) || IsBuilderInTestHelpers(type);
    }

    /// <summary>
    /// Gets a value indicating whether a composition caller (<c>Program</c> in the CLI or the test
    /// <c>SystemServicesBuilder</c>) encloses <paramref name="containingSymbol"/>.
    /// </summary>
    /// <param name="containingSymbol">The symbol whose enclosing types are walked.</param>
    /// <returns><see langword="true"/> when a composition caller encloses the symbol.</returns>
    internal static bool Encloses(ISymbol containingSymbol)
    {
        return EnclosedBy(containingSymbol, IsCompositionType);
    }

    /// <summary>
    /// Gets a value indicating whether the test <c>SystemServicesBuilder</c> in <c>AgentGuard.TestHelpers</c> — and it
    /// alone, NOT the <c>Program</c> composition caller — encloses <paramref name="containingSymbol"/>. This is the one
    /// owner licensed to read the shared OS temp root through <c>IEnvironment.GetTempDirectory()</c> to seed the
    /// copy-on-write fake (temp-root-owner-exemption, AGS5443): the builder is copy-on-write over the real host, so it
    /// passes the real temp root through once here and the AGS5443 ban stands everywhere else.
    /// </summary>
    /// <param name="containingSymbol">The symbol whose enclosing types are walked.</param>
    /// <returns><see langword="true"/> when the test <c>SystemServicesBuilder</c> encloses the symbol.</returns>
    internal static bool EnclosesTestBuilder(ISymbol containingSymbol)
    {
        return EnclosedBy(containingSymbol, IsBuilderInTestHelpers);
    }

    /// <summary>
    /// Gets a value indicating whether the clock construction site — <c>SystemServices</c> in
    /// <c>AgentGuard.Boundaries</c> (whose <c>Create()</c> wires the container) or the test
    /// <c>SystemServicesBuilder</c> — encloses <paramref name="containingSymbol"/>. This is where a direct
    /// <c>TimeProvider.System</c> acquisition is legal (AG0015): the clock is wired into the container there, unlike
    /// the composition CALLERS above, which only call the already-built factory.
    /// </summary>
    /// <param name="containingSymbol">The symbol whose enclosing types are walked.</param>
    /// <returns><see langword="true"/> when the construction site encloses the symbol.</returns>
    internal static bool EnclosesConstructionSite(ISymbol containingSymbol)
    {
        return EnclosedBy(containingSymbol, IsConstructionSiteType);
    }

    /// <summary>
    /// Gets a value indicating whether the wrapper factory — <c>FileInfoFactory</c> in
    /// <c>AgentGuard.CrossPlatform</c> — encloses <paramref name="containingSymbol"/>. This is the only place an
    /// <c>AbstractedFileInfo</c>/<c>AbstractedDirectoryInfo</c> wrapper may be constructed (AG0033), so every caller
    /// stays on the mockable factory seam.
    /// </summary>
    /// <param name="containingSymbol">The symbol whose enclosing types are walked.</param>
    /// <returns><see langword="true"/> when the wrapper factory encloses the symbol.</returns>
    internal static bool EnclosesWrapperFactory(ISymbol containingSymbol)
    {
        return EnclosedBy(containingSymbol, IsWrapperFactoryType);
    }

    private static bool EnclosedBy(ISymbol containingSymbol, Func<INamedTypeSymbol?, bool> isSite)
    {
        for (INamedTypeSymbol? enclosing = OwnerClass.EnclosingType(containingSymbol);
             enclosing is not null;
             enclosing = enclosing.ContainingType)
        {
            if (isSite(enclosing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsConstructionSiteType(INamedTypeSymbol? type)
    {
        return IsSystemServicesInBoundaries(type) || IsBuilderInTestHelpers(type);
    }

    private static bool IsWrapperFactoryType(INamedTypeSymbol? type)
    {
        // FileInfoFactory in AgentGuard.CrossPlatform, matched by type identity AND its compiled assembly through the
        // shared WellKnownType.IsInAssembly conjunction, so a merely same-named type elsewhere cannot self-grant the
        // wrapper-construction exemption.
        return WellKnownType.IsInAssembly(
            type, CrossPlatformBoundary.RootName, WrapperFactoryTypeName, CrossPlatformBoundary.RootName);
    }

    private static bool IsSystemServicesInBoundaries(INamedTypeSymbol? type)
    {
        // SystemServices in AgentGuard.Boundaries — the type whose Create() acquires and wires the clock. Anchored on
        // namespace + name AND the compiled assembly name (both are AgentGuard.Boundaries) through the shared
        // WellKnownType.IsInAssembly conjunction.
        return WellKnownType.IsInAssembly(
            type, BoundaryAssembly.Name, ContainerFactoryTypeName, BoundaryAssembly.Name);
    }

    private static bool IsProgramInCli(INamedTypeSymbol? enclosing)
    {
        // The two identity signals are DISTINCT and matched separately by the shared WellKnownType.IsInAssembly
        // conjunction: the Program TYPE by its namespace (AgentGuard.Cli) + name, and the ASSEMBLY by the CLI's real
        // compiled name ("guard", set by <AssemblyName>guard</AssemblyName>). Using the root namespace for the assembly
        // check would never match the real Program and would permanently, falsely flag the one legitimate composition
        // point (LESSON 1).
        return WellKnownType.IsInAssembly(
            enclosing, CliAssembly.RootNamespace, CompositionTypeName, CliAssembly.CompiledName);
    }

    private static bool IsBuilderInTestHelpers(INamedTypeSymbol? enclosing)
    {
        return WellKnownType.IsInAssembly(
            enclosing, TestAssembly.TestHelpersName, BuilderTypeName, TestAssembly.TestHelpersName);
    }
}
