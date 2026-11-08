# Attention: the one control of the eight with a worked design

Of Tim's eight controls, attention is the only one this repository has designed. The design is `.dev/inprocess/82-guard-process-workflow/human-checkins-proposal.md`, which states in its own first lines: "Not approved. This proposes where human interaction adds value, how the system gives strong signal instead of noise, and which inventories are closed so no agent can extend them."

It is about when a system stops and asks a person to decide. If Denice's user-facing model has a notion of attention, this is the material to start from, and it is not approved.

## The governing principle

A human adds value at a check-in only when all three hold:

1. The answer is genuinely his — taste, priority, risk tolerance, or authority that cannot be derived from the code.
2. Being wrong is expensive to reverse later.
3. He can actually detect the defect from what is in front of him.

The document says the third is the one that gets ignored, and the one that decides everything else. Its example: a person reads an eight-line test plan and says nothing here covers Windows cancellation, which is a defect caught in thirty seconds. The same person reads forty written test methods and does not notice the missing one, because absence is invisible in volume. The information is present in both cases and perceivable in only one.

The document draws one conclusion from that, and corrects an earlier version of itself in the process: "This is why the plan stages exist, and an earlier version of this document was wrong to collapse them. The plan is the artefact a human can audit. The output of the work is not."

The line it settles on: he approves intent, machines verify conformance.

## What a person can and cannot detect

| can detect | cannot detect from the artefact |
|---|---|
| wrong problem or wrong scope — the only source of truth for it | whether the code is correct; tests establish that |
| wrong approach — holds the system history and the unwritten constraints | whether something was duplicated; a search establishes that |
| a missing item in a short enumerated list | whether a claim's evidence is real; comparing claim to capture establishes that |
| a decision being taken that was not theirs to take | whether every required reviewer ran; mechanical |
| which way a genuine tradeoff should go | whether a result's structure and references are valid; mechanical |

The rule attached to it: everything in the right column must never reach him, because sending it does not add a safeguard, it adds volume that hides the left column.

## The shape of a request for a decision

Every check-in is the same three-part exchange, and no agent composes any part of it.

The **sign request** carries `Kind` (which check-in, from a closed list), `Content` (the decision block, rendered from saved results), `Sources` (a reference to every document version the content was drawn from), `Locks` (what approving will fix, named explicitly), `Outcomes` (approve, reject, or return to a named stage), and `RequestHash` (a hash over the exact rendered bytes plus the source list).

The **signed approval** carries `RequestHash` (the hash of exactly what was shown), `Outcome`, `Presence` (the operating system's presence result that authorised it), and `Signature` over the request hash and the outcome.

A **checkpoint** is created on approval only. A rejection or a return is recorded as a decision and locks nothing, because nothing was agreed.

Guard is what runs all of this; the proposal defines no new agent and no new result file, and no agent composes any part of a request or a page. Most of the mechanism also already exists: `CertifiedResults<T>` already carries the results, the hash and the signature, and a signer, a verifier and a key are already in the signing service and the grant public key. What is missing is the decision record, not the cryptography.

Three properties are given as the reason it is worth building. The signature covers what was shown rather than a pointer to a document that can change, so if a source changes afterwards the stored hash no longer reproduces and the system reports a stale checkpoint rather than an approval. It makes "it is already in the code, so it must have been approved" unprovable, turning a judgement call into a hash comparison. And an agent cannot manufacture an approval, because it cannot produce the signature and cannot author the content that was signed.

## The change it is actually asking for

The proposal's own ask, at its line 219, is not the list of check-ins but what the list replaces. The recorded default it would replace is approval on every stage transition, quoted there: "Human approval is required for every transition by default, including forward transitions, self-loops, and returns to earlier stages," which across sixteen stages is sixteen or more approvals before any correction loop. The proposal keeps every stage, including the plan stages, and moves approval off the transitions and onto seven check-ins and two interrupts. A stage with no check-in still runs, still validates and still records; it simply does not stop for anyone. Because the current default forbids that, it needs Tim's approval naming each transition that becomes mechanical, and that list has never been written.

The counts below are the seven check-ins. The two interrupts — a contract change and a waiver — are counted among the triggers.

## When it stops

Two check-ins stop on every run, because one authorises starting and the other authorises finishing: **scope**, at the end of the conversation and before any work is registered, and **accept**, at the gate.

Five more stop only when their stage is required, and which stages are required is fixed in advance by the level the work runs at, chosen once by the person at the scope check-in: **approach**, **contract**, **rule plan**, **test plan** and **surface**. The surface check-in is the one where interfaces are shown as code rather than as prose describing them.

Seven triggered check-ins are predicates over saved state rather than stages: an unresolved reuse question, a required result absent, a reviewer verdict absent, an outcome differing from its locked expectation, an edit outside the agreed scope, a contract change an agent asked for, and a waiver of a confirmed finding. Six of the seven are computable with no agent involved. The seventh, contract change, depends on an agent choosing to raise it, so it is paired with edit-outside-scope, which catches mechanically the case where an agent proceeded instead of asking.

A pending triggered check-in cannot be cleared by an agent, by a passing build, or by a later successful result. It clears on a signed outcome.

The cost, by level, each row conditional as the source writes it: seven stops at the most rigorous level where no rule phase is skipped, five at the middle one where design and tests are both needed, two at the lightest, plus triggers in each case.

## The six mechanics that make a stop carry signal

- **Prediction against outcome.** A predicted result sitting next to the actual result. Agreement is noise and collapses to one line; a mismatch, or an unknown, is the entire signal.
- **Absence rendered as an item.** The system knows the required set, so a missing result appears as a row reading required and absent, never as a gap in a list. A person cannot see nothing; they can see a row saying nothing arrived.
- **Bounded size, and overflow is itself the signal.** A check-in that does not fit on one screen has already failed. If the test plan runs past its cap, the finding is that the scope is too large and should be split. The cap is the mechanism, not a formatting preference.
- **Difference, not restatement.** On any return or second attempt, show what changed against what was already approved. Re-presenting an approved thing in full teaches a reader to skim, and skimming is how a rubber stamp happens.
- **Claims paired with their evidence, one level down.** Each claim links to the capture, the source location, or the failing check. Most will never be opened; that the link must exist and must resolve is what makes the claim honest, and it is checkable mechanically.
- **Exceptions only, by default.** Everything that passed, agreed or validated is one collapsed line. The open view holds what disagrees with expectation, what is absent, and what needs a decision.

## What never reaches a person

Counts, percentages, stage completion and agent tallies. Anything an agent wrote about its own work or its own diligence. Anything verifiable mechanically. Anything that matched its expectation. Any item whose answer changes no downstream action. Any status at all.

The test for any proposed check-in, and for any line inside one: if the answer were wrong, would something break that no reviewer, test or mechanical check would catch? If no, it is noise and it comes out.

## Closed lists, and the one that matters most

Each list is closed, a value outside it is rejected, and an agent needing a new value stops and asks. Five are closed already: the reuse determination, the run status, the reviewers, the execution levels, and the expected outcome of a planned case.

Five are named as still needing a closed list: the agent role, the kind of check-in, the approval outcome, the kind of result, and the stage.

The document singles out the agent role, which is currently a bare string on a C# record: a free-text role is how an agent invents a task type, and inventing task types is the inflation the document exists to stop. No issue tracks it. Issue 83 in the backlog is a different thing — replacing references to Tim by name in the governing documents with the role for the context — and it says in as many words that it does not introduce agent roles.

## What the design says it still needs

A closed set of agent roles replacing the free-text one. The approval record itself — which check-in, which person, what was presented, what was decided, against which document versions — because presence checking exists and the decision record does not. A type for the rule plan, to match the one the test plan already has. A type for the exploration result. One stage-by-level requirement table replacing three records and their seven booleans, covering all sixteen stages rather than eleven. And the list of transitions that become mechanical, for approval.

One stage is recorded as genuinely undefined rather than merely unapproved: review, which the subprocess inventory records as having no subprocess defined, unknown, and not zero required work.
