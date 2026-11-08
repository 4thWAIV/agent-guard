# Readiness mismatches — proposed contract wording, awaiting Tim's approval

The readiness check returned `ready: false` on five mismatches. Its complete output is `readiness-output.json`. Nothing below is written into `contract.md`; every paragraph is a draft in the orchestrator's words, for Tim to approve, edit or reject. Two of the five need a ruling he has not yet given.

## Needs a ruling — the ARCHITECTURE no-signature determination was never accepted

`architecture-result.md` line 5 still reads: "The no-signature determination is awaiting Tim's acceptance. This revision was reviewed: DRY PASS and Lie-catcher PASS, recorded in `architecture-review-round3.json`. TDD has not been authorized." Its Handoff section repeats "report the stage result for Tim's approval before TDD." Four TDD authoring rounds and IMPLEMENT ran after that, and the contract's Decisions section records no ARCHITECTURE acceptance.

The determination itself is that the relocation needs no new application interface, DTO or container member, so ARCHITECTURE wrote no skeleton: `skeletonFiles: []` and `perType: []`. The contract supports it — "Preserve the existing `ISystemServices` interface and runtime behavior", and IMPLEMENT owns the move.

Proposed Decision, for approval:

> Tim accepted the ARCHITECTURE no-signature determination, presented as written:
>
> - The relocation requires no new application interface, DTO, enum or container member, so ARCHITECTURE's result is `skeletonFiles: []` and `perType: []`. That result stands accepted, and TDD and IMPLEMENT were authorized to proceed on it.

Once approved, `architecture-result.md`'s Status and Handoff sections need updating so the live record stops saying TDD was never authorized.

## Needs a ruling — two analyzer files were changed that the contract's Surfaces section does not list

RULE-PHASE changed `analyzers/AgentGuard.Analyzers/TypeTree.cs` and `analyzers/AgentGuard.Analyzers/CrossPlatformBoundary.cs` at commit `a12e58b`. Neither is in the Surfaces list. `CrossPlatformBoundary.cs` appears nowhere in the contract.

What was added:

- `TypeTree.Describe(ITypeSymbol? root)`, 23 lines. Its own doc comment names its consumers: AG0041 and the shared `OneDoorRule` body behind AG0040, AG0023 and AG0029.
- `CrossPlatformBoundary.IsCoreAssembly(string?)` and `CrossPlatformBoundary.IsSharedNamespaceDecoy(INamedTypeSymbol)`, 45 lines. AG0023's and AG0029's Engine gates use them to set scope and to admit a same-named decoy so each rule's door test can reject it. That is the mechanism Acceptance 5, 21 and 25 turn on.

Both are shared analyzer owners with three or four consumers each, and neither has a prior-art-ledger row. A DRY reviewer that finds a changed shared owner with no Surfaces entry and no ledger row fails on the missing row.

Proposed Surfaces entries, for approval:

> - `analyzers/AgentGuard.Analyzers/TypeTree.cs` — add `Describe`, the one owner of the type-description text the access diagnostics report.
> - `analyzers/AgentGuard.Analyzers/CrossPlatformBoundary.cs` — add `IsCoreAssembly` and `IsSharedNamespaceDecoy`, the core-assembly scope test and the same-named-decoy admission the AG0023 and AG0029 Engine gates use.

Proposed Reuse ledger rows, for approval:

> - `extract analyzers/AgentGuard.Analyzers/TypeTree.cs` — the type-description text each access diagnostic reports was spelled at the report sites. `Describe` is its one owner, consumed by AG0041 and by the shared `OneDoorRule` body behind AG0040, AG0023 and AG0029.
> - `extract analyzers/AgentGuard.Analyzers/CrossPlatformBoundary.cs` — the core-assembly test and the same-named-decoy admission belong with the existing CrossPlatform boundary identity rather than in either rule, so AG0023 and AG0029 share one owner.

## Already ruled in conversation, not yet in the contract — the staged baseline

The contract's "What the agent MUST NOT do" section says "Do not ... stage, commit, push, or open a PR", and twenty-two entries sit in git's index. Tim staged them himself and said so. The only record of that is the orchestrator's panel instruction, which touches no file and which no adversary can check.

Proposed Decision, for approval:

> Tim recorded the staged baseline, presented as written:
>
> - Tim staged the twenty-two entries now in git's index himself — the six analyzer test files and the sixteen run records. They are the baseline every later stage preserves. The prohibition on staging continues to bind every agent; it does not bind Tim.

## Already ruled in conversation, not yet in the contract — the final TDD delivery's single review

The contract's "What to do" step 5 requires test-quality, DRY and Lie-catcher reviews of TDD. The final authoring round, the DRY-fix round of three test files, has one review record on disk: `dry-fix-review-dry.md`. The test-quality and Lie-catcher PASS verdicts are against the earlier Acceptance 25 delivery, which is the same test code without those three extractions.

Proposed Decision, for approval:

> Tim accepted the TDD stage, presented as written:
>
> - The TDD stage stands accepted. Its final delivery, the DRY-fix round's three test files, was reviewed by the DRY adversary alone, and Tim accepted it on that review. The test-quality and Lie-catcher PASS verdicts recorded for TDD are against the Acceptance 25 delivery.

## Already ruled in conversation, not yet in the contract — the four TDD rounds ran without a written authorization

The contract's "What to do" step 9 says "Any further fix round requires Tim's approval after Codex reviews the findings and proposed corrections." `tdd-result.md` records under `notes` that no authorization record exists in the work folder for the correction round, the second correction round, the Acceptance 25 round or the DRY-fix round. The Acceptance 25 Lie-catcher raised this and it is still unwritten.

Proposed Decision, for approval:

> Tim recorded the TDD fix rounds, presented as written:
>
> - The four TDD authoring rounds — the original round, the correction, the Acceptance 25 round and the DRY-fix round — ran without a written authorization record in this work folder. Tim accepted them as delivered. The requirement that a fix round carry his approval continues to apply to every later round.
