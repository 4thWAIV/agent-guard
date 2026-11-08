# The five readiness findings, resolved against the corrected contract

The readiness check returned `ready: false` on five mismatches; its complete output is `readiness-output.json`. Tim then recorded the missing authorization and scope entries in `contract.md`. Each finding below is resolved by naming the contract text that now carries it. The readiness check is not re-run; `rails-run-a-workflow` forbids a second run to confirm a fix landed.

## Finding 1, open-item — the ARCHITECTURE no-signature determination was never accepted

Resolved. `contract.md`, Decisions / Recorded conversation authorizations, first entry: Tim's instruction of 2026-09-16 04:17:55, "Accept the reviewed ARCHITECTURE no-signature result and proceed to TDD using:" followed by `tdd-worker-instructions.md`. The same entry states that "The pending-acceptance text in `architecture-result.md` predates this authorization and remains historical."

`architecture-result.md` still reads "awaiting Tim's acceptance ... TDD has not been authorized" in its Status and Handoff sections. The contract labels that text historical rather than current, so the file is left as the historical record it is. The REFUTE panel is told this, so a reviewer reading that file rules on it with the contract entry in hand.

## Finding 2, boundary — twenty-two entries staged against "Do not ... stage, commit, push"

Resolved. Same section, last entry: "Tim explicitly authorized the checkpoint commit and subsequent staging baseline. Codex performed the checkpoint commit `a12e58b` and staged the baseline at Tim's direction. Preserve that index during implementation and review. The prohibition on agents staging or committing without authorization does not invalidate these explicitly requested operations and grants no permission for further staging, commits or pushes."

## Finding 3, decision — the final TDD delivery was reviewed by DRY alone

Resolved. Same section, fifth entry: Tim's instruction of 2026-09-16 19:27:55, "Run one L2 correction of the three DRY violations, followed by one independent DRY-only review. This is the review scope authorized for this correction." The entry scopes that exception to the factory-call, string-comparison and wrong-namespace extractions, records that `dry-fix-review-dry.md` reviews those changes, records that the earlier test-quality and Lie-catcher reviews remain reviews of the Acceptance 25 delivery, and states that "The full L1 implementation review remains required."

## Finding 4, decision — four TDD rounds ran with no written authorization

Resolved. Same section carries a dated instruction for each round: 2026-09-16 16:04:23 for the L3 correction, 17:20:16 for the second correction of the delivery report, 18:43:32 for the Acceptance 25 round, and 19:27:55 for the DRY-fix round.

## Finding 5, surface — two changed analyzer files absent from Surfaces and the Reuse ledger

Resolved. Both are now in Surfaces:

> - `analyzers/AgentGuard.Analyzers/TypeTree.cs` — share diagnostic type-name formatting through `Describe(ITypeSymbol?)` for the approved access rules.
> - `analyzers/AgentGuard.Analyzers/CrossPlatformBoundary.cs` — share the core-assembly check and foreign-assembly namespace-decoy check through `IsCoreAssembly` and `IsSharedNamespaceDecoy`, preserving the genuine core and per-OS factory permissions.

And both have a prior-art-ledger row:

> - `extract analyzers/AgentGuard.Analyzers/TypeTree.cs` — `Describe(ITypeSymbol?)` owns the repeated type-description formatting used by `EngineInternalsOneDoorAnalyzer` and `OneDoorRule`.
> - `extract analyzers/AgentGuard.Analyzers/CrossPlatformBoundary.cs` — `IsCoreAssembly` owns the core-assembly comparison; `IsSharedNamespaceDecoy` composes the existing namespace and assembly identity checks once for the Engine gates of AG0023 and AG0029. Each rule still applies its own permitted-factory identity check and rejects an imitation of its own factory.

The ledger section closes with: "The two extraction entries above reconcile completed RULE-PHASE corrections with this ledger. They do not claim that a separate prior-art-ledger tool run occurred before those corrections, and do not change the approved access restrictions."
