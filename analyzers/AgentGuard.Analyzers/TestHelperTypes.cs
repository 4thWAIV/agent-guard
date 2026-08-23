// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// The simple names of the <c>AgentGuard.TestHelpers</c> types the seed-site rules key on, named once here so the
/// same literal is not repeated across the analyzers that match them. Each lives in the namespace AND the assembly
/// <see cref="TestAssembly.TestHelpersName"/>, so a rule pairs a name here with that identity through
/// <see cref="WellKnownType"/>. <see cref="StoreTypeName"/> is shared by two rule families — the guarded-construction
/// rule (AG0027) that pins the overlay's single construction site through <see cref="IsStoreConstruction"/>, and the two
/// seed-site rules (AG0035/AG0036) that read the overlay's <c>tempRoot</c>/<c>caseSensitive</c> seed at the single
/// overlay factory <c>SystemServicesBuilder.NewOverlay</c> (<see cref="IsOverlayFactory"/>). That factory call — the one
/// place a caller's value is visible, because <c>NewOverlay</c> forwards its own parameters to the constructor — is the
/// single seed site the two rules check; the constructor is only ever reached through <c>NewOverlay</c>, so the
/// constructor call never carries a literal and a check there could never fire.
/// </summary>
internal static class TestHelperTypes
{
    /// <summary>
    /// The shared copy-on-write overlay type <c>AgentGuard.TestHelpers.InMemoryFileSystemStore</c>. The single owner
    /// of this name: AG0027 pins its construction to <c>SystemServicesBuilder</c> through
    /// <see cref="IsStoreConstruction"/>, and AG0035/AG0036 name it as the return type of the overlay factory
    /// <c>NewOverlay</c> whose <c>tempRoot</c>/<c>caseSensitive</c> arguments they read for a literal seed.
    /// </summary>
    internal const string StoreTypeName = "InMemoryFileSystemStore";

    /// <summary>
    /// The test builder type <c>AgentGuard.TestHelpers.SystemServicesBuilder</c> whose private
    /// <see cref="OverlayFactoryMethodName"/> helper is the single overlay factory. AG0035/AG0036 read that call's
    /// <c>tempRoot</c> and <c>caseSensitive</c> arguments for a literal seed.
    /// </summary>
    internal const string OverlayBuilderTypeName = "SystemServicesBuilder";

    /// <summary>
    /// The single overlay factory helper <c>SystemServicesBuilder.NewOverlay</c>. It forwards its own
    /// <c>tempRoot</c>/<c>caseSensitive</c> parameters to the store constructor, so the caller's seed value is visible
    /// only at the call to it — never at the constructor — which is why AG0035/AG0036 inspect its arguments.
    /// </summary>
    internal const string OverlayFactoryMethodName = "NewOverlay";

    /// <summary>
    /// The environment fake type <c>AgentGuard.TestHelpers.FakeEnvironment</c>. AG0035 reads its <c>Create</c>
    /// factory's <c>home</c>/<c>currentDirectory</c>/<c>tempDirectory</c> arguments for a literal fake root.
    /// </summary>
    internal const string FakeEnvironmentTypeName = "FakeEnvironment";

    /// <summary>
    /// Gets a value indicating whether <paramref name="operation"/> constructs the shared copy-on-write overlay
    /// <c>AgentGuard.TestHelpers.InMemoryFileSystemStore</c> — an <see cref="IObjectCreationOperation"/> whose created
    /// type is that store, matched by namespace + name AND the declaring assembly
    /// (<see cref="TestAssembly.TestHelpersName"/>) through <see cref="WellKnownType.IsInAssembly"/>, so a type merely
    /// NAMED the same in another assembly does not match. This is the store-construction predicate the
    /// guarded-construction rule (AG0027) keys on to pin the store's one construction site to
    /// <c>SystemServicesBuilder</c>; the seed-site rules (AG0035/AG0036) do NOT read the constructor — they read the
    /// overlay factory <c>NewOverlay</c> (<see cref="IsOverlayFactory"/>) instead, the single site a caller's seed value
    /// is visible.
    /// </summary>
    /// <param name="operation">The operation under analysis.</param>
    /// <param name="type">The type <c>MemberUseScanner</c> resolved for the operation (the created type for a
    /// construction).</param>
    /// <returns><see langword="true"/> when the operation constructs the overlay store.</returns>
    internal static bool IsStoreConstruction(IOperation operation, INamedTypeSymbol type)
        => operation is IObjectCreationOperation
            && WellKnownType.IsInAssembly(
                type, TestAssembly.TestHelpersName, StoreTypeName, TestAssembly.TestHelpersName);

    /// <summary>
    /// Gets a value indicating whether <paramref name="operation"/> is a call to the single overlay factory
    /// <c>SystemServicesBuilder.NewOverlay</c> in <c>AgentGuard.TestHelpers</c> — an <see cref="IInvocationOperation"/>
    /// whose target is a static method named <see cref="OverlayFactoryMethodName"/> on that builder type, matched by
    /// namespace + name AND the declaring assembly (<see cref="TestAssembly.TestHelpersName"/>) through
    /// <see cref="WellKnownType.IsInAssembly"/>, mirroring the <c>FakeEnvironment.Create</c> identity match, so a
    /// same-named method in another assembly does not match. This is the single owner of the overlay-factory-call
    /// predicate the two seed-site rules share: AG0035 reads the call's <c>tempRoot</c> argument and AG0036 its
    /// <c>caseSensitive</c> argument for a literal seed. This — not the constructor — is where a caller's seed value is
    /// visible, because <c>NewOverlay</c> forwards its own <c>tempRoot</c>/<c>caseSensitive</c> parameters to the store
    /// constructor rather than a literal.
    /// </summary>
    /// <param name="operation">The operation under analysis.</param>
    /// <param name="member">The member <c>MemberUseScanner</c> resolved for the operation (the invoked method for an
    /// invocation).</param>
    /// <param name="type">The type <c>MemberUseScanner</c> resolved for the operation (the invoked method's containing
    /// type).</param>
    /// <returns><see langword="true"/> when the operation calls the overlay factory.</returns>
    internal static bool IsOverlayFactory(IOperation operation, ISymbol member, INamedTypeSymbol type)
        => operation is IInvocationOperation
            && member is IMethodSymbol { IsStatic: true }
            && string.Equals(member.Name, OverlayFactoryMethodName, StringComparison.Ordinal)
            && WellKnownType.IsInAssembly(
                type, TestAssembly.TestHelpersName, OverlayBuilderTypeName, TestAssembly.TestHelpersName);
}
