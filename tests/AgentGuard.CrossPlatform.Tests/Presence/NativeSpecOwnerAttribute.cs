// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// Declares that the test class it is applied to is the <c>[Trait("Category","NativeSpec")]</c> pointed-integration spec
/// test that drives the named native-ops owner (guardrail-1-nativespec-convention-test). The convention test
/// (<see cref="NativeSpecCoverageConventionTests"/>) reads this to know which owner a spec test COVERS, matched against
/// the owners the per-OS production assembly marks REQUIRED with <see cref="RequiresNativeSpecTestAttribute"/>. Unlike
/// that placement-based marker, a spec test class has no structural relationship to the owner interface it drives, so it
/// must name the owner through <c>typeof(&lt;the internal interface&gt;)</c> — a rename is then a compile error, never a
/// silent miss. It also owns the two NativeSpec trait strings the guardrail matches on, so the spec test's <c>[Trait]</c>
/// and the convention test's matcher read one source and can never drift.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class NativeSpecOwnerAttribute : Attribute
{
    /// <summary>The xUnit trait NAME (<c>Category</c>) that tags a NativeSpec pointed-integration test. Owned here — the
    /// single place both the spec test's <c>[Trait]</c> and the convention test's matcher read — so a rename or typo is
    /// a compile error, never a silent mismatch (LESSON 1, DRY).</summary>
    internal const string CategoryTraitName = "Category";

    /// <summary>The xUnit trait VALUE (<c>NativeSpec</c>) that marks a native-ops pointed-integration test. Owned here
    /// alongside <see cref="CategoryTraitName"/> for the same reason.</summary>
    internal const string NativeSpecTraitValue = "NativeSpec";

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeSpecOwnerAttribute"/> class.
    /// </summary>
    /// <param name="ownerInterface">The native-ops owner interface the annotated spec test drives.</param>
    public NativeSpecOwnerAttribute(Type ownerInterface) => OwnerInterface = ownerInterface;

    /// <summary>Gets the native-ops owner interface the annotated spec test drives.</summary>
    public Type OwnerInterface { get; }
}
