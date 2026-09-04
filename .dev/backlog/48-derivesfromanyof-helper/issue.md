# 48 — Extract a shared WellKnownType.DerivesFromAnyOf helper (AG0112 + AG0026 duplicate a base-class walk)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/48

---

Two shipped analyzers hand-roll the same base-class-chain walk — "walk `type`/`current.BaseType`, test each ancestor against a `WellKnownType` candidate set" — with only variable names changed:

- `NoCachedNativeAuthContextAnalyzer` (AG0112) — `IsNativeHandleType`'s SafeHandle base check.
- `NoOsSkipInTestsAnalyzer` (AG0026) — `DerivesFromXunitTestAttribute`.

`WellKnownType.cs` is the project's single owner for type-identity matching but has no hierarchy-walk helper for either copy to call.

**Fix:** add `internal static bool DerivesFromAnyOf(ITypeSymbol? type, ImmutableArray<(string Namespace, string Name)> candidates)` to `WellKnownType.cs` (walk `type`/`current.BaseType`, call the existing `IsAnyOf` per level); route both analyzers through it, deleting the two hand-rolled loops.

Surfaced during the presence coverage refactor's RULE-PHASE (the AG0116 field-scan generalization). Deferred as tangential shipped-rule cleanup — Tim: "Okay, file an issue." Not a correctness bug; pure dedup.