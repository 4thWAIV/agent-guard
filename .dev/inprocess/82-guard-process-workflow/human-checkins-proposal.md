# Human check-ins and locked inventories — proposal

Not approved. This proposes where human interaction adds value, how the system gives strong signal instead of noise, and which inventories are closed so no agent can extend them. It defines no new agents and no new result files.

## The governing principle

A human adds value at a check-in only when all three hold:

1. The answer is genuinely his — taste, priority, risk tolerance, or authority that cannot be derived from the code.
2. Being wrong is expensive to reverse later.
3. **He can actually detect the defect from what is in front of him.**

The third is the one that gets ignored, and it is the one that decides everything else.

A human reads an eight-line test plan and says "nothing here covers Windows cancellation." That is a defect caught in thirty seconds, before any test is written. The same human reads forty written test methods and does not notice the missing one, because absence is invisible in volume. The information is present in both cases; it is perceivable in only one.

This is why the plan stages exist, and an earlier version of this document was wrong to collapse them. The plan is the artefact a human can audit. The output of the work is not.

**He approves intent. Machines verify conformance.**

## What the human can and cannot detect

| He can detect | He cannot detect from the artefact |
|---|---|
| Wrong problem or wrong scope — he is the only source of truth for it | Whether the code is correct — tests establish that |
| Wrong approach — he holds the system history and the constraints that are not written down | Whether something was duplicated — a search establishes that |
| A missing item in a short enumerated list — the plan-stage case | Whether a claim's evidence is real — Guard comparing claim to capture establishes that |
| A decision being taken that was not his to take | Whether every required reviewer ran — mechanical |
| Which way a genuine tradeoff should go | Whether a result's structure and references are valid — mechanical |

Everything in the right column must never reach him. Sending it to him does not add a safeguard; it adds volume that hides the left column.

## The sign-request mechanism

Every check-in is the same three-part exchange. Guard runs it; no agent composes any part of it.

**Sign request** — Guard builds it from saved results and sends it to the human:

| Field | Content |
|---|---|
| `Kind` | Which check-in, from the closed list |
| `Content` | The decision block, rendered by Guard from saved results |
| `Sources` | A `VersionedFileReference` for every document version the content was drawn from |
| `Locks` | What approving will fix, named explicitly |
| `Outcomes` | `Approve`, `Reject`, `ReturnTo(stage)` |
| `RequestHash` | Hash over the exact rendered bytes plus the source list |

**Signed approval** — what comes back:

| Field | Content |
|---|---|
| `RequestHash` | The hash of exactly what was shown |
| `Outcome` | One of the offered outcomes |
| `Presence` | The OS presence result that authorised it |
| `Signature` | Over the request hash and the outcome |

**Checkpoint** — created on `Approve` only. A `Reject` or `ReturnTo` is recorded as a decision but locks nothing, because nothing was agreed.

Three properties make this worth building.

The signature covers **what was shown**, not a pointer to a mutable document. If a source document changes afterwards, the stored hash no longer reproduces and Guard reports a stale checkpoint rather than an approval.

It makes "it is already in the code, so it must have been approved" unprovable. That claim is a named violation in `rails-decisions`; this turns it from a judgement call into a hash comparison.

An agent cannot manufacture an approval, because it cannot produce the signature and cannot author the content that was signed.

`CertifiedResults<T>` in `CertifiedResults.cs` already has the shape — `Results`, `Hash`, `Signature`. It needs a signer, a verifier and a key, which the grant-token infrastructure already provides in `Ed25519SignatureService` and `.agentguard/grant-public-key`.

## Mandatory check-ins — always stop

Two stop on every run at every level, because one authorises starting and the other authorises finishing.

### 1. Scope

| | |
|---|---|
| **When** | End of CONVERSATION, before any assignment is registered |
| **He sees** | The work, its exclusions, the target platforms, the execution level |
| **Locks** | `Description`, `Scope`, `Exclusions`, `TargetPlatforms`, `Level` |
| **To change later** | A new signed Scope check-in. Guard marks every result whose sources are invalidated |

Nothing can be registered without this, so there is no run to govern until it is signed.

### 2. Accept

| | |
|---|---|
| **When** | GATE — `Required` at L1, L2 and L3 |
| **He sees** | Every confirmed finding with its adjudication, every planned case whose outcome differed from its expectation, and the acceptance checks against their actual results |
| **Locks** | The run's outcome, and the result set as the accepted work |
| **To change later** | Nothing. A later change is new work with its own run |

Everything that passed and agreed is one collapsed line. The open view is findings, mismatches and absences.

## Level-determined check-ins — stop if the stage runs

These are not optional in the sense of anyone choosing at the time. Each is mandatory exactly when its stage is `Required`, which `WorkflowLevel` fixes when the level is set at check-in 1. The decision is made once, by the human, in advance.

### 3. Approach — when `DesignStage` is `Required`

| | |
|---|---|
| **He sees** | The chosen approach, the alternatives it was chosen over, and every decision GROUND surfaced that no approved principle settled |
| **Locks** | The selected approach and each surfaced decision as answered |
| **To change later** | A new signed Approach check-in |

### 4. Contract — when `ContractStage` is `Required`

| | |
|---|---|
| **He sees** | The binding scope, the edit scope, and the acceptance checks |
| **Locks** | The contract. This is the checkpoint every later stage is measured against |
| **To change later** | The contract-change interrupt, which is itself a signed check-in |

### 5. Rule plan — when `RuleStage` is `Required`

| | |
|---|---|
| **He sees** | Each rule to be added, one line, with what it would make fail and the expected red |
| **Locks** | The rule set for this run |
| **To change later** | A new signed Rule-plan check-in |

### 6. Test plan — when `TestStage` is `Required`

| | |
|---|---|
| **He sees** | The planned cases: `Id`, what it exercises, `Expectation` of `Pass`/`Fail`/`Unknown`, and `Reason` |
| **Locks** | The case set and each expectation |
| **To change later** | A new signed Test-plan check-in. An expectation changed after the fact without one is the clearest cheat this design can catch |

Locking the expectation before the code exists is what makes the later comparison meaningful. An unlocked expectation can be edited to match whatever happened.

### 7. Surface — when `ArchitectureStage` is `Required`

| | |
|---|---|
| **He sees** | The interface definitions as code, not prose describing them |
| **Locks** | The signatures that tests and implementation will be written against |
| **To change later** | A new signed Surface check-in, which invalidates tests written against the old shape |

## Triggered check-ins — deterministic predicates

Each trigger is a predicate over saved state. Guard evaluates it; no agent decides whether to raise it, and no agent can suppress it. A triggered check-in that is pending blocks the stage transition.

| Trigger | Predicate | He decides |
|---|---|---|
| **Unresolved reuse** | any `PriorArtEvaluationResult.Decision is UnresolvedReuseDecision` | Answer the question, or authorise the follow-up research |
| **Required result absent** | a registered assignment with no run in `Completed`, or a run in `Interrupted` or `Failed` | Resume, reassign, or accept the gap |
| **Reviewer verdict absent** | a reviewer `Required` by the level with no recorded verdict | The same. This is the defect where a missing result is discarded and the run passes |
| **Expectation mismatch** | any planned case whose actual outcome differs from its locked `Expectation`, or whose `Expectation` is `Unknown` | Whether the surprise is a code defect or a wrong expectation |
| **Edit outside scope** | a changed file not matched by the contract's edit scope | Extend the scope, or revert the edit |
| **Contract change** | an agent submitted a `ContractChangeRequest` and stopped | Add it to the contract, or refuse and redirect the work |
| **Waiver** | a confirmed finding with a proposed disposition of waived, or a rule-exception request | Whether it stands unfixed |

Six of the seven are computable with no agent involvement at all. Only **contract change** depends on an agent choosing to raise it — so it is paired with **edit outside scope**, which catches mechanically the case where an agent proceeded instead of asking.

A pending triggered check-in cannot be cleared by an agent, by a passing build, or by a later successful result. It clears on a signed outcome.

## What this costs per run

| Level | Stops |
|---|---|
| L1, no rule skip | 7 mandatory and level-determined, plus triggers |
| L2, design and tests needed | 5, plus triggers |
| L3 | 2 — scope and accept — plus triggers |

Compared with the recorded default of approval on every transition, including self-loops and returns, across sixteen stages.

## How the system produces strong signal

Six mechanics. Guard implements all of them; no agent composes a page for the human.

**Prediction against outcome.** The highest-signal artefact available is a predicted result sitting next to the actual result. Agreement is noise and collapses to one line. A mismatch, or an `Unknown`, is the entire signal. This is what `PlannedCase.Expectation` is for, and it is what the RULE and TEST stages' evaluation steps investigate.

**Absence rendered as an item.** Guard knows the required set — every registered assignment, every enabled reviewer, every selected platform, every planned case. A missing result appears as a row reading required and absent, never as a gap in a list. A human cannot see nothing; he can see a row that says nothing arrived.

**Bounded size, and overflow is itself the signal.** A check-in that does not fit on one screen has already failed. If the test plan runs past its cap, the finding is not a longer page — it is that the scope is too large and should be split. The cap is the mechanism, not a formatting preference.

**Difference, not restatement.** On any return or second attempt he sees what changed against what he already approved. Re-presenting an approved thing in full teaches him to skim, and skimming is how a rubber stamp happens.

**Claims paired with their evidence, one level down.** Each claim links to the capture, the source location, or the failing check. He will not read most of them. The fact that the link must exist and must resolve is what makes the claim honest, and it is checkable mechanically.

**Exceptions only, by default.** Everything that passed, agreed, or validated is one collapsed line. The open view contains what disagrees with expectation, what is absent, and what needs a decision.

## What never reaches him

- Counts, percentages, stage completion, agent tallies.
- Anything an agent wrote about its own work or its own diligence.
- Anything verifiable mechanically.
- Anything that matched its expectation.
- Any item whose answer changes no downstream action.
- Any status at all. Status lives in Workflow and he can go look.

The test for any proposed check-in, and for any line inside one: **if he answered it wrong, would something break that no reviewer, test or mechanical check would catch?** If no, it is noise and it comes out.

## Locked inventories

Each list is closed. Guard rejects a value outside it, and an agent needing a new value stops and asks. These are enums, so extending one is a code change a human reviews.

| Inventory | Members | Status |
|---|---|---|
| Reuse determination | `reuse`, `extend`, `extract`, `new`, `Unresolved` | Locked — both additions approved |
| Agent run status | `Running`, `Completed`, `Interrupted`, `Failed` | Locked in `AgentRun.cs` |
| Reviewer | Prove-It, SOLID, DRY, Laziness, Lie-catcher | Locked in `WorkflowLevel.cs` |
| Execution level | L1, L2, L3 | Locked in `WorkflowLevel.cs` |
| Expected outcome | `Pass`, `Fail`, `Unknown` | Locked in `IPlan.cs` |
| Agent role | **needs a closed list** — `AgentAssignment.Role` is a bare string | Open |
| Check-in kind | the seven above plus two interrupts | Open |
| Approval outcome | `Approve`, `Reject`, `ReturnTo(stage)` | Open |
| Result kind | one per stage that produces a result | Open |
| Stage | the existing sixteen | Open — see below |

`AgentAssignment(string Role)` is the one that matters most. A free-text role is how an agent invents a task type, and inventing task types is the inflation this document exists to stop.

## On the stage count

The stages are not the problem; the per-transition approval is. The recorded default is "Human approval is required for every transition by default, including forward transitions, self-loops, and returns to earlier stages," which across sixteen stages is sixteen-plus approvals before any correction loop, most of them on things he cannot detect anything in.

The proposal is to keep the stages, including the plan stages, and move approval off transitions and onto the seven check-ins and two interrupts. A stage with no check-in still runs, still validates, still records — it simply does not stop for him. Because the current default forbids that, it needs his approval naming each transition that becomes mechanical.

That leaves one stage genuinely undefined rather than merely unapproved: REVIEW, which the subprocess inventory records as "No subprocess defined. Unknown, **not zero required work**."

## What this needs that does not exist

- A closed `AgentRole` enum replacing the free-text role.
- The approval record: which check-in, which human, what was presented, what was decided, against which document versions. Presence checking exists; the decision record does not.
- The rule plan type. The test plan has `PlannedCase`; rules have no equivalent.
- The exploration result type — DESIGN's main input, still unschemed.
- One stage-by-level requirement table replacing the three `WorkflowLevel` records and their seven booleans, covering all sixteen stages rather than eleven.
- The list of transitions that become mechanical, for approval.
