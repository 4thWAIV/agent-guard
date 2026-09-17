// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The designated construction sites the rules carve out, held here once so a "is this the right site" test is not
/// re-spelled per rule. Four distinct concepts live here, each anchored on full type identity (namespace + name via
/// <see cref="WellKnownType"/>) AND the assembly — a conjunction, so a type merely NAMED the same in another namespace
/// or assembly cannot self-grant the exemption:
/// <list type="bullet">
/// <item>the <b>callers</b> — the single <c>Program</c> composition method in <c>AgentGuard.Cli</c> and the test
/// <c>SystemServicesBuilder</c> in <c>AgentGuard.TestHelpers</c>: the only places <c>SystemServices.Create()</c> may be
/// <em>called</em> (AG0017), a service type may be held (AG0024), or a service may be a parameter (AG0031);</item>
/// <item>the <b>construction site</b> — <c>SystemServices</c> in <c>AgentGuard.Engine</c> (whose <c>Create()</c>
/// wires the container) and the test <c>SystemServicesBuilder</c>: the only places a direct <c>TimeProvider.System</c>
/// acquisition is legal (AG0015, clock-legal-in-create-and-builder). This is a TYPE-level test: anywhere inside the
/// container class qualifies;</item>
/// <item>the <b>container factory method</b> — the static <c>Create()</c> on <c>SystemServices</c> in
/// <c>AgentGuard.Engine</c> and nothing else: the one method from which Engine may reach into
/// <c>AgentGuard.Boundaries</c> (AG0040), the core <c>AgentGuard.CrossPlatform</c> assembly (AG0023), or a per-OS
/// implementation assembly (AG0029), and the one Engine internal the CLI and the test helpers may reach (AG0041).
/// This is a METHOD-level test and is deliberately narrower than the type-level construction site above: another
/// method on the same class does NOT qualify;</item>
/// <item>the <b>wrapper factory</b> — <c>FileInfoFactory</c> in <c>AgentGuard.CrossPlatform</c>: the only place an
/// <c>AbstractedFileInfo</c>/<c>AbstractedDirectoryInfo</c> wrapper may be constructed (AG0033).</item>
/// </list>
/// </summary>
internal static class CompositionPoint
{
    /// <summary>
    /// The human-readable name of the one container factory method — <c>AgentGuard.Engine.SystemServices.Create()</c>
    /// — as it is spelled in a diagnostic message. Held next to <see cref="IsContainerFactoryMethod"/>, the predicate
    /// that decides it, so the required site is described in exactly the terms the rules test for and the text is
    /// spelled once for AG0040, AG0023, AG0029, and AG0041.
    /// </summary>
    internal const string ContainerFactoryDescription =
        EngineAssembly.Name + "." + ContainerFactoryTypeName + "." + ContainerFactoryMethodName + "()";

    private const string CompositionTypeName = "Program";
    private const string BuilderTypeName = "SystemServicesBuilder";
    private const string ContainerFactoryTypeName = "SystemServices";
    private const string WrapperFactoryTypeName = "FileInfoFactory";

    // The name of the container's own build point. Each door-identity owner spells the factory method name it pins
    // (PlatformFactory spells it for PlatformServices.Create, BoundaryAdapterFactories for the four adapter
    // factories): the mandated container-is-one-class-with-its-own-create shape happens to name them all Create, but
    // they are independent facts about independent types, and collapsing them would let a change to one silently
    // retarget the others.
    private const string ContainerFactoryMethodName = "Create";

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
    /// <c>AgentGuard.Engine</c> (whose <c>Create()</c> wires the container) or the test
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

    /// <summary>
    /// Gets a value indicating whether <paramref name="symbol"/> IS the one container factory method — the static
    /// <c>Create()</c> declared on the <c>SystemServices</c> type in namespace AND assembly
    /// <c>AgentGuard.Engine</c> — matched on assembly, namespace, type name, method name, and staticness together, so
    /// a same-named method on a decoy type in another namespace or assembly cannot self-grant the exemption. This is
    /// the narrowest reading of "directly inside <c>SystemServices.Create()</c>": the symbol handed in is the call
    /// site's OWN containing symbol, and a lambda or a local function nested in <c>Create()</c>'s body is a distinct
    /// method symbol (<see cref="MethodKind.LambdaMethod"/>/<see cref="MethodKind.LocalFunction"/>), so a call written
    /// inside one is NOT permitted. Widening that takes an explicit rule change.
    /// </summary>
    /// <param name="symbol">The symbol to test — a call site's containing symbol, or a resolved invocation target.</param>
    /// <returns><see langword="true"/> when the symbol is the container factory method itself.</returns>
    internal static bool IsContainerFactoryMethod(ISymbol? symbol)
    {
        return symbol is IMethodSymbol { IsStatic: true, MethodKind: MethodKind.Ordinary } method
            && string.Equals(method.Name, ContainerFactoryMethodName, StringComparison.Ordinal)
            && IsContainerFactoryType(method.ContainingType);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is the container type <c>SystemServices</c> in
    /// namespace AND assembly <c>AgentGuard.Engine</c>. Exposed so a rule can test a WRITTEN reference to the
    /// container type (AG0041's written-name lens) against the same identity the method-level caller test uses.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is the relocated container.</returns>
    internal static bool IsContainerFactoryType(INamedTypeSymbol? type)
    {
        // SystemServices in AgentGuard.Engine — the type whose Create() acquires the clock and wires the container.
        // Anchored on namespace + name AND the compiled assembly name (both are AgentGuard.Engine) through the shared
        // WellKnownType.IsInAssembly conjunction.
        return WellKnownType.IsInAssembly(
            type, EngineAssembly.Name, ContainerFactoryTypeName, EngineAssembly.Name);
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
        return IsContainerFactoryType(type) || IsBuilderInTestHelpers(type);
    }

    private static bool IsWrapperFactoryType(INamedTypeSymbol? type)
    {
        // FileInfoFactory in AgentGuard.CrossPlatform, matched by type identity AND its compiled assembly through the
        // shared WellKnownType.IsInAssembly conjunction, so a merely same-named type elsewhere cannot self-grant the
        // wrapper-construction exemption.
        return WellKnownType.IsInAssembly(
            type, CrossPlatformBoundary.RootName, WrapperFactoryTypeName, CrossPlatformBoundary.RootName);
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
