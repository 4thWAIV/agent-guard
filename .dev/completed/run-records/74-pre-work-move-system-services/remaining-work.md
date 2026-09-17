# Remaining work — assigned to TDD, not to RULE-PHASE

Tim's direction: keep the direct helper, pointer-traversal and caller-identity tests assigned to TDD in the remaining-work record. RULE-PHASE proves the rules with violating and compliant fixtures; these three prove helper behaviour and belong to the test author.

## Direct helper tests

`analyzers/AgentGuard.Analyzers.Tests/SymbolResolutionTests.cs` — the shared resolution owner. Acceptance 26. Cover a type name, a well-formed attribute name that binds the constructor while denoting the type, a malformed attribute with a required constructor argument omitted that binds nothing while still denoting the type, a member name proving the recovery tier does not run after a successful bind, and the unresolved cases that must yield nothing.

`analyzers/AgentGuard.Analyzers.Tests/DeclaredTypeScannerTests.cs` — the carried-type lens, where AG0006's seven existing tests do not already prove the behaviour. Acceptance 26. AG0006 remains the proof for the four declaration positions it always had; only the invoked-return-type position the access rules added needs its own coverage.

## Pointer traversal

`analyzers/AgentGuard.Analyzers.Tests/TypeTreeTests.cs` — Acceptance 27. `TypeTree.Any` has two callers and no tests, so its pointer branch is unproven. Exercise pointer targets using compiler-created type symbols and the existing compilation helper: a pointer type, an array of pointers, and a generic carrying a pointer. This proves traversal, not unsafe-source compilation; no runner change is needed and no new expected-compiler-error exception is introduced.

## Caller identity

Acceptance 25's direct wrong-assembly check of `CompositionPoint`. It is currently proved only indirectly, by `TimeMustUseTimeProviderAnalyzerTests.TimeProviderSystem_InSameNamedContainerInAnotherAssembly_IsReported`, which exercises the same identity conjunction on the same values through a different rule. TDD makes it direct: resolve a same-named `Create` from a compilation named something else and assert the composition-point predicate is false.
