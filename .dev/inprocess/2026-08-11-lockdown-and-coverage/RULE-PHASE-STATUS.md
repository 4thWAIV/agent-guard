# RULE-PHASE status — STOPPED for Tim (2026-08-12, overnight)

**Not clean after 4 fix rounds. Nothing committed. All analyzer work is in the working tree, uncommitted, on branch `clr-primitive-lockdown`.**

## Why stopped (not "run more rounds")
Tim's overnight rule: get a clean RULE-PHASE, then commit + IMPLEMENT + REFUTE; **if it stalls, stop and leave a report rather than force a fake-clean.** Round 4 hit two stop conditions at once:
1. The **Lie-catcher flipped to FAIL** (it had passed rounds 1–3). It caught a rigged proof: the rule tests validate against the wrong namespace, so the suite is green while the rule is dead against the real interfaces.
2. The root defect is a **repeat of the LESSON-1 identity-match class** — round 2 was the CLI assembly name (`guard` vs `AgentGuard.Cli`), round 4 is the owner-interface namespace. The rule-gen keeps making the exact error the contract's own lessons exist to prevent, so it wants Tim's eyes, not another unattended round.

## The convergence trail (each round's findings were NEW, none survived)
- Round 1: SOLID x2 + DRY x2 (composition-point assembly name; AG0025 implements-check; AG0019 & AG0032 re-spelled literals). Lie-catcher PASS.
- Round 2: SOLID x1 + DRY x1 (AG0024 weaker composition check; AG0025 re-derived implements). Lie-catcher PASS.
- Round 3: SOLID x1 + DRY x2 (AG0025 TypeKind.Class gate; bespoke type walker; test-system predicate). Lie-catcher PASS.
- Round 4 (STOP): SOLID x2 + DRY x1 + **Lie-catcher FAIL x1** — all one root cause + two small riders.

## The remaining defects (round 4)
1. **ROOT (SOLID + Lie-catcher): the boundary rules match the WRONG owner-interface namespace.** `KnownNamespaces.AgentGuardAbstractions = "AgentGuard.Abstractions"` (flat), used by `BoundaryServices.OwnerInterfaces` / `OwnerClass.Implements` for AG0011/12/14/16/25/101. The contract's `namespace-match-fix` mandates the boundary owner interfaces live under, and be matched at, **`AgentGuard.Abstractions.Contracts`** (the `.Contracts` convention `IGuard` uses). `WellKnownType.Is` is an exact namespace match, so once IMPLEMENT relocates the interfaces to `.Abstractions.Contracts`, no owner is ever recognized → the owner classes stay permanently RED → the build can never go green. The analyzer TESTS use the flat `namespace AgentGuard.Abstractions { interface IFileReader }`, matching the buggy constant, so they're green against the bug's own shape, not the contract's — the rigged proof the Lie-catcher flagged.
2. **SOLID: `NoCoverageOptOutAnalyzer` reuses `KnownNamespaces.AgentGuardAbstractions` (a NAMESPACE constant) as an ASSEMBLY-NAME literal** — conflating two invariants that happen to share the string `AgentGuard.Abstractions`.
3. **DRY: `TestAssembly.IsTestSystemAssembly`'s disjunction is copied inline in `TestHelpersOnlyInTestsAnalyzer`** instead of calling the one shared owner (and that owner's doc comment falsely claims it's spelled once).

## The fix (precise, contract-specified — for the next round or a hand-fix)
- Set the interface-match namespace to **`AgentGuard.Abstractions.Contracts`** (a purpose-specific constant), keeping a **separate** constant for the assembly-name literal `AgentGuard.Abstractions`.
- Rewrite the rigged analyzer test fixtures to declare their sample interfaces under the **real** `AgentGuard.Abstractions.Contracts`, plus a red-then-clean probe proving AG0025 fires against a second implementer at the real namespace.
- Point `NoCoverageOptOutAnalyzer` at the assembly-name constant, not the namespace one.
- Fold the `TestHelpersOnlyInTestsAnalyzer` disjunction into the one `TestAssembly.IsTestSystemAssembly` owner.

## What IS good (do not re-litigate)
- Analyzer assembly builds 0/0; 197 analyzer tests green.
- Whole-tree RED is real: committed config turns the boundary violations into build errors (12 errors, all in `PlatformFileSystemShared.cs` at the CrossPlatform layer): AG0011 x?, AG0014, AG0020, AG0101 — the standing forcing-function for the IMPLEMENT worker.
- The critical round-2 fix (composition-point matches the real `guard` assembly) is in and unit-proven.
- Every other rule (AG0011–0024, 0028–0032, 0101, the AG0014/0017 edits, the AG0015 tightening) is written, RED-proven or preventive, and clean.
