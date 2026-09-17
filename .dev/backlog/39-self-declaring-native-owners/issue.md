# 39 — Make the OS-native-call owner set self-declaring instead of a hard-coded analyzer list

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/39

---

## Problem

The set of classes allowed to make raw OS-native calls is a **hard-coded compile-time array of string pairs** in the analyzer source. AG0101 (`OsDivergentFilesystemOnlyInCrossPlatformAnalyzer`) resolves its owner via `OwnerClass.IsOwner`, whose owning-interface set is:

```csharp
// ContractInterfaces.cs
internal static readonly ImmutableArray<(string Namespace, string Name)> PlatformFileSystem =
    ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IPlatformFileSystem"));
```

Matching is two ordinal string comparisons (namespace display string + simple name) against the class's implemented interfaces. The same hard-coded-list shape backs the owner mechanism generally (`ContractInterfaces`, `OwnedPrimitives`).

Consequence: **every new capability that legitimately needs to make native calls requires editing that array, rebuilding the analyzer, and a human sign-off** — e.g. adding `IPresenceCheck` for the OS presence check. That recurs once per new OS-touching capability.

## Ask

Evaluate a self-declaring owner mechanism so a legitimately-owned native capability can opt in **without** an analyzer source edit — for example a marker interface or an `[OsNativeOwner]`-style attribute the per-OS owner class carries, matched by the analyzer.

## The constraint that makes this non-trivial

The current two-part conjunction in `OwnerClass.IsOwner` — (implements an owning interface) AND (compiled into a per-OS implementation assembly) — exists specifically to **block self-grants**: a class cannot hand itself native-call rights by implementing the interface in the wrong assembly. A naive marker reopens exactly that hole: a lazy AI grants itself native calls by adding the marker to a new class. Any self-declaring design must preserve the no-self-grant property (e.g. keep the assembly-gate half, and/or require the marker to sit on a class that also satisfies an independent structural check).

## Relationship to minting

The roadmap's crypto-minting/grant system is the intended systematic answer to this recurring cost: it turns "a human edits the analyzer" into "a human signs a grant that authorizes this owner" — auditable, revocable, and not self-grantable. This issue may be **subsumed by or designed alongside** the minting work rather than solved standalone. Decide whether to build the marker approach as an interim step or fold it into minting.

## Pointers
- `analyzers/AgentGuard.Analyzers/ContractInterfaces.cs` — the hard-coded owning-interface arrays.
- `analyzers/AgentGuard.Analyzers/OwnerClass.cs` — `IsOwner` conjunction (structural interface match + assembly gate).
- `analyzers/AgentGuard.Analyzers/OsDivergentFilesystemOnlyInCrossPlatformAnalyzer.cs` — AG0101, the OS-divergent/native-call owner rule.
- Triggered by: the OS presence-check contract (`.dev/inprocess/2026-08-23-os-presence-check/`), which extends the list to add `IPresenceCheck`.
