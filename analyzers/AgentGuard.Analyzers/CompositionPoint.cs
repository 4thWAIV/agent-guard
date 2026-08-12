// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The two places the wiring of the OS services is allowed to happen — the single <c>Program</c> composition method
/// in <c>AgentGuard.Cli</c> and the test <c>SystemServicesBuilder</c> in <c>AgentGuard.TestHelpers</c>. Several rules
/// carve out exactly these two spots (AG0017 pins <c>SystemServices.Create()</c> to them, AG0024 lets a service type
/// be held there, AG0031 lets a service be a parameter there), so the "is this the composition point" test lives here
/// once rather than being re-spelled per rule. Each match anchors on full type identity (namespace + name via
/// <see cref="WellKnownType"/>) AND the assembly, a conjunction: a type merely NAMED <c>Program</c> or
/// <c>SystemServicesBuilder</c> in another namespace or another assembly cannot self-grant the exemption.
/// </summary>
internal static class CompositionPoint
{
    private const string CompositionTypeName = "Program";
    private const string BuilderTypeName = "SystemServicesBuilder";

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is one of the two composition points — the
    /// <c>Program</c> type in <c>AgentGuard.Cli</c> or the <c>SystemServicesBuilder</c> type in
    /// <c>AgentGuard.TestHelpers</c>.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is a composition point.</returns>
    internal static bool IsCompositionType(INamedTypeSymbol? type)
    {
        return IsProgramInCli(type) || IsBuilderInTestHelpers(type);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="containingSymbol"/> is enclosed by a composition point —
    /// walking the enclosing named types so a member (or a nested type) declared inside the <c>Program</c>
    /// composition method or the <c>SystemServicesBuilder</c> is recognised.
    /// </summary>
    /// <param name="containingSymbol">The symbol whose enclosing types are walked.</param>
    /// <returns><see langword="true"/> when a composition point encloses the symbol.</returns>
    internal static bool Encloses(ISymbol containingSymbol)
    {
        for (INamedTypeSymbol? enclosing = OwnerClass.EnclosingType(containingSymbol);
             enclosing is not null;
             enclosing = enclosing.ContainingType)
        {
            if (IsCompositionType(enclosing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProgramInCli(INamedTypeSymbol? enclosing)
    {
        // The two identity signals are DISTINCT and matched separately: the Program TYPE by its namespace
        // (AgentGuard.Cli) + name, and the ASSEMBLY by the CLI's real compiled name ("guard", set by
        // <AssemblyName>guard</AssemblyName>). Using the root namespace for the assembly check would never match the
        // real Program and would permanently, falsely flag the one legitimate composition point (LESSON 1).
        return WellKnownType.Is(enclosing, CliAssembly.RootNamespace, CompositionTypeName)
            && string.Equals(enclosing?.ContainingAssembly?.Name, CliAssembly.CompiledName, StringComparison.Ordinal);
    }

    private static bool IsBuilderInTestHelpers(INamedTypeSymbol? enclosing)
    {
        return WellKnownType.Is(enclosing, TestAssembly.TestHelpersName, BuilderTypeName)
            && string.Equals(enclosing?.ContainingAssembly?.Name, TestAssembly.TestHelpersName, StringComparison.Ordinal);
    }
}
