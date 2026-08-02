# How to write a contract

A contract is the instructions for the agent that does the work, plus the checks that prove it is done. Nothing else belongs in it.

## The one test for every line

Does this line tell the agent WHAT TO DO, what it MUST NOT do, or HOW IT IS CHECKED? If not, delete it. If the human would read it and ask "what value does this add?", it is already noise — cut it before they have to.

## The sections, in order

1. **Title** — one line naming the job. No "1 of 2", no status, no framing.
2. **The standard / what we're building** — the target end state, stated plainly. If there is a non-negotiable rule, lead with it. Say WHAT the end state is, never WHY it is currently broken or how we got here.
3. **Success definition** — the human's standing definition verbatim (ALL criteria met AND no errors in the system as a result of the change) plus this run's specific expected end state. A contract without this is invalid — the gate has nothing to rule against. Any restatement or weakening of it to fit the result is a top-line Lie-catcher finding.
4. **Surfaces** — every store or file the change touches (each place the same value lives), enumerated, so adversaries refute against a written list at contract time, not a post-mortem.
5. **Reuse ledger** — the output of the GROUND prior-art search: every capability this change needs, each marked `reuse <file:line>` (an existing owner already does it), `extract <copies>` (it exists in more than one place with no single owner — collapse them into one), or `new` (no lens found it). A `new` mark is valid only when every lens — CodeGraph, lore semantic search, grep — came back empty, and it must say so. The contract forbids building a `reuse` or `extract` capability fresh. No ledger, no contract.
6. **What to do** — the concrete work: the real files, functions, data, claim ids, byte offsets. Name them; they are the checkable things.
7. **What the agent MAY do** — the permitted actions, when scoping helps.
8. **What the agent MUST NOT do** — the hard boundaries. Anti-cheat lives here: no weakening, skipping, or deleting tests; no commit; no scope expansion; stop and report on any wall.
9. **Acceptance** — numbered checks, each with the command or proof. Where cheating is a risk, the check must be re-derivable by someone other than the worker — an adversary reading the bytes, or a script the human runs. The worker's own word is not proof.
10. **Tier** — one line: FULL / LITE / CONTRACT-ONLY, plus one clause of why. One line, never a paragraph.
11. **Scope line** — "Change scope only by editing this file before the run starts."

## Cut every one of these — they are noise

- "This is contract 1 of 2 / the other is at <path>." Sequencing is the operator's job, not the agent's. No cross-references between contracts.
- Background: why it is broken, the history, how we got here. The agent needs the target, not the story.
- A tier justification paragraph — one line only.
- An opening narrative that then repeats the sections below. Say each thing once.
- The same constraint stated in two places (a description AND a "design decisions" list) — merge into one.
- Editorializing ("the only way to pass is to cheat", "the highest-blast-radius thing in the system").
- Meta framing ("agree or edit these", "in one breath", "the ends-with-one bar").
- Reassurance, motivation, pitch.

## Voice

- Plain English. No coined or hyphenated terms — write "both verbatim and contiguous", not "verbatim-contiguous".
- One idea per line.
- Concise is not the goal; CLEAR is. Do not pad into noise; do not compress until it cannot be read.
- Every requirement must be verifiable. If you cannot write a check for it, it is not a contract term.
- Every artifact the contract points at — a script, a check, a file — must already exist or be a listed deliverable in the same contract. No dangling references to something that only ran ad-hoc in conversation.

## Last step before it goes to the human

Re-read the whole thing and delete anything that is not an instruction, a boundary, or a check. Do this yourself so the human never has to read a line of noise.
