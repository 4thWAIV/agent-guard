// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// Guardrail 1 (guardrail-1-nativespec-convention-test) — a build-failing convention test, NOT a Roslyn analyzer:
/// "Add a build-failing convention test that requires every per-OS native-ops owner to have its own
/// <c>[Trait("Category","NativeSpec")]</c> spec test, so a native-ops class sitting at 0% can never again hide under an
/// assembly total that still clears 75%." For the per-OS presence assembly on the CURRENT leg it fails when a required
/// native-ops owner lacks its NativeSpec spec test. The required owners are read from the production interfaces
/// themselves — every interface the per-OS production assembly marks with <see cref="RequiresNativeSpecTestAttribute"/>
/// (macOS <c>IObjCRuntime</c>, Windows <c>ICredentialPromptNativeOps</c>, Linux <c>IPolkitAuthority</c>) — so the owner
/// list lives ON the owner types, never in a separate test-side list that could drift. A spec test declares the owner it
/// covers with <c>[NativeSpecOwner(typeof(&lt;owner&gt;))]</c> and carries a NativeSpec-category test. It goes RED on the
/// Windows and Linux legs until their spec tests are added, and is green on macOS, whose
/// <see cref="MacOsObjCRuntimeSpecTests"/> already exists.
/// </summary>
public sealed class NativeSpecCoverageConventionTests
{
    private const string TraitAttributeFullName = "Xunit.TraitAttribute";

    // The per-OS production presence assemblies are named AgentGuard.CrossPlatform.<OS>; exactly one is referenced on
    // the current leg (RID-selected). The shared contract assembly AgentGuard.CrossPlatform is excluded by the trailing
    // dot, and the test assembly itself never appears in its own referenced-assembly set (guarded against regardless).
    private const string PerOsAssemblyPrefix = CrossPlatformName + ".";
    private const string CrossPlatformName = "AgentGuard.CrossPlatform";
    private const string TestAssemblyName = CrossPlatformName + ".Tests";

    /// <summary>
    /// Every native-ops owner the per-OS production assembly marks <see cref="RequiresNativeSpecTestAttribute"/> must be
    /// covered by a <c>[NativeSpecOwner(typeof(&lt;owner&gt;))]</c> test class that carries a
    /// <c>[Trait("Category","NativeSpec")]</c> test — otherwise a native-ops class could sit at 0% and hide under an
    /// assembly total that still clears the 75% floor.
    /// </summary>
    [Fact]
    public void EveryRequiredNativeOpsOwner_HasItsOwnNativeSpecTest()
    {
        Assembly testAssembly = typeof(NativeSpecCoverageConventionTests).Assembly;
        Assembly perOsAssembly = ResolvePerOsProductionAssembly(testAssembly);

        // The required owner IS the interface the marker is placed on — the attribute carries no argument, so it can
        // never name a different owner than where it sits (a copy-paste typo is structurally impossible).
        IReadOnlyList<Type> requiredOwners = perOsAssembly
            .GetTypes()
            .Where(type => type.IsDefined(typeof(RequiresNativeSpecTestAttribute), inherit: false))
            .Distinct()
            .ToList();

        // The per-OS production assembly must mark its native-ops owner (the [RequiresNativeSpecTest]-tagged interface).
        // An empty set would mean the platform selection or the marking is broken and the guardrail would otherwise pass
        // vacuously, so it is a failure.
        requiredOwners.Should().NotBeEmpty(
            "the per-OS production presence assembly on this leg must mark its native-ops owner with "
            + $"[RequiresNativeSpecTest] (assembly '{perOsAssembly.GetName().Name}')");

        HashSet<Type> coveredOwners = testAssembly
            .GetTypes()
            .Where(HasNativeSpecTest)
            .SelectMany(type => type.GetCustomAttributes<NativeSpecOwnerAttribute>())
            .Select(attribute => attribute.OwnerInterface)
            .ToHashSet();

        IReadOnlyList<Type> uncovered = requiredOwners
            .Where(owner => !coveredOwners.Contains(owner))
            .ToList();

        uncovered.Should().BeEmpty(
            "every native-ops owner marked [RequiresNativeSpecTest] must have its own "
            + "[Trait(\"Category\",\"NativeSpec\")] spec test — owner(s) still missing one: "
            + string.Join(", ", uncovered.Select(owner => owner.FullName)));
    }

    // Resolves the one per-OS production presence assembly referenced on the current leg — the RID-selected
    // AgentGuard.CrossPlatform.<OS> impl the test project imports. Exactly one is expected: zero would mean the platform
    // selection is broken (the guardrail would pass vacuously), and more than one would mean two legs' impls leaked into
    // one build; both are failures, not a silently-skipped check.
    private static Assembly ResolvePerOsProductionAssembly(Assembly testAssembly)
    {
        List<AssemblyName> perOsReferences = testAssembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name is not null
                && reference.Name.StartsWith(PerOsAssemblyPrefix, StringComparison.Ordinal)
                && !string.Equals(reference.Name, TestAssemblyName, StringComparison.Ordinal))
            .ToList();

        perOsReferences.Should().ContainSingle(
            "the test project references exactly one per-OS presence implementation assembly on the current leg — "
            + "found: " + string.Join(", ", perOsReferences.Select(reference => reference.Name)));

        return Assembly.Load(perOsReferences[0]);
    }

    // A class covers an owner only when it BOTH carries [NativeSpecOwner(typeof(owner))] AND has a real
    // NativeSpec-category test, so a bare [NativeSpecOwner] on a non-spec class cannot satisfy the requirement.
    private static bool HasNativeSpecTest(Type type)
    {
        if (!type.GetCustomAttributes<NativeSpecOwnerAttribute>().Any())
        {
            return false;
        }

        return HasNativeSpecTrait(type.GetCustomAttributesData())
            || type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Any(method => HasNativeSpecTrait(method.GetCustomAttributesData()));
    }

    // Reads xUnit's [Trait("Category","NativeSpec")] by its constructor arguments (the attribute exposes no public
    // name/value properties), matched by full name so it is robust across xUnit versions. The Category / NativeSpec
    // strings come from the single owner NativeSpecOwnerAttribute so the matcher and the spec test's [Trait] can never
    // drift (LESSON 1, DRY).
    private static bool HasNativeSpecTrait(IEnumerable<CustomAttributeData> attributes)
    {
        return attributes.Any(attribute =>
            string.Equals(attribute.AttributeType.FullName, TraitAttributeFullName, StringComparison.Ordinal)
            && attribute.ConstructorArguments.Count == 2
            && string.Equals(attribute.ConstructorArguments[0].Value as string, NativeSpecOwnerAttribute.CategoryTraitName, StringComparison.Ordinal)
            && string.Equals(attribute.ConstructorArguments[1].Value as string, NativeSpecOwnerAttribute.NativeSpecTraitValue, StringComparison.Ordinal));
    }
}
