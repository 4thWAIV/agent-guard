// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// The simple names of the <c>AgentGuard.TestHelpers</c> types the seed-site rules key on, named once here so the
/// same literal is not repeated across the analyzers that match them. Both live in the namespace AND the assembly
/// <see cref="TestAssembly.TestHelpersName"/>, so a rule pairs a name here with that identity through
/// <see cref="WellKnownType"/>. <see cref="StoreTypeName"/> is shared by three rules — the guarded-construction rule
/// (AG0027) that pins the overlay's single construction site, and the two seed-site rules (AG0035/AG0036) that read
/// the overlay constructor's <c>tempRoot</c> and <c>caseSensitive</c> arguments — so the name has one owner.
/// </summary>
internal static class TestHelperTypes
{
    /// <summary>
    /// The shared copy-on-write overlay type <c>AgentGuard.TestHelpers.InMemoryFileSystemStore</c>. The single owner
    /// of this name: AG0027 pins its construction to <c>SystemServicesBuilder</c>, and AG0035/AG0036 read its
    /// constructor's <c>tempRoot</c> and <c>caseSensitive</c> arguments for a literal seed.
    /// </summary>
    internal const string StoreTypeName = "InMemoryFileSystemStore";

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
    /// NAMED the same in another assembly does not match. The single owner of the
    /// store-construction predicate: the guarded-construction rule (AG0027) that pins the store's one construction
    /// site, and the two seed-site rules that read the store constructor's <c>tempRoot</c> (AG0035) and
    /// <c>caseSensitive</c> (AG0036) arguments, all key on this same shape — so it is written once here beside
    /// <see cref="StoreTypeName"/>.
    /// </summary>
    /// <param name="operation">The operation under analysis.</param>
    /// <param name="type">The type <c>MemberUseScanner</c> resolved for the operation (the created type for a
    /// construction).</param>
    /// <returns><see langword="true"/> when the operation constructs the overlay store.</returns>
    internal static bool IsStoreConstruction(IOperation operation, INamedTypeSymbol type)
        => operation is IObjectCreationOperation
            && WellKnownType.IsInAssembly(
                type, TestAssembly.TestHelpersName, StoreTypeName, TestAssembly.TestHelpersName);
}
