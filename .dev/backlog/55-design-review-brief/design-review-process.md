# DRAFT — the Design Review process

**Status: DRAFT, not codified.** Trial it on real decisions; if it works, codify it into `rails-run-a-workflow` (alongside the L1/L2/L3 model) and delete this file.

The Design Review is the heavyweight case-making the human calls — or an agent triggers — when a decision needs more than a quick answer. Its whole job is to let the human rule on the *merits*, not fight through bad communication.

## When it fires (the bar that keeps it from being noise)
Only a decision that is **forced, lasting, and costly to reverse** and is **not** already settled by the rules or by best practice — a locked-interface change, a new lasting design element in a governed place, a genuine design fork. Obvious and best-practice choices are taken and recorded, never escalated. The human can also call it directly at any time: "design review on X."

## The brief — one fixed shape, so the case is always made the same clean way
1. **The ask** — one line: exactly what is being decided.
2. **The problem** — the forcing thing, in the plainest words, first. The one fact that makes this a decision at all is the problem (e.g. "the OS-specific libraries must use this shared code, and they can't depend on the Engine"). Lead with that fact, never with what the thing is. No parenthetical asides or name-clarifications wedged mid-sentence — if it is worth saying, it is its own sentence. If you cannot name a forcing thing, it is not a Design Review; it gets taken.
3. **The code** — the actual interface, abstract prototype, or class signature the decision is about, shown verbatim. No code shown means the prep is not done and the review is bounced.
4. **The options** — the two or three real alternatives, one line each, the recommended one marked. Never lazy-versus-right.
5. **Why the pick** — the one Chief-Architect reason it wins.
6. **Cost of the wrong call** — what it locks in, who it burns, how hard to undo.
7. **Blast radius** — what it touches: files, interfaces, callers — so the human sees the scope.

## The rules that stop the communication from forcing a "no"
- Problem before pick, always.
- The ask and the pick are one line each.
- No backreferences to earlier conversation — the brief stands alone.
- Full human sentences.
- One bounded round: the human approves, picks, rejects, or asks for one specific thing more — not an open debate.

## Who it binds
The orchestrator (L3) and every delegated agent (L1 and L2). When any of them hits an escalation-worthy fork, it makes the case in exactly this shape — never a raw dump, never a bad-COM push to make the human cave.

## Outcome
The human's ruling is recorded as a decision in their own words in the contract, and the work proceeds.
