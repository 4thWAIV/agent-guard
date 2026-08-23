---
name: rails-decisions
description: The decision-and-approval guardrail (rail) — what needs Tim's explicit yes and what the agent decides alone — and the Lie-catcher adversary's PRIMARY PASS/FAIL checklist for any decision made without Tim's words. Load it when designing, when implementing, and when reviewing; it is what the Lie-catcher rules against.
---

# Rails: decisions

A rail against deciding what is Tim's to decide, and against inventing his approval. One source, two readers:

- **Actor** (orchestrator and worker) — follow the **boundary** and the **approval discipline** below. Surface every decision-level choice and wait for Tim's explicit yes **before** you build it. Decide the local, cheaply-reversible stuff yourself and keep moving.
- **Lie-catcher** — this file is your PRIMARY PASS/FAIL checklist. Hunt every decision-level item — in the contract or in the work — that lacks Tim's cited verbatim approval. Each **Violation** line below is concrete and checkable. **Any one confirmed Violation is a FAIL.**

## The boundary: what is Tim's, what is the agent's

**Tim's decisions — surface it and wait for an explicit yes.** Anything that:
- sets an **architecture, mechanism, or approach** that other code depends on;
- adds or changes a **dependency, a trust boundary, a data or wire format, a public interface, or a user- or developer-visible name or command**;
- is **costly to reverse** once built or shipped;
- **picks between real alternatives** where the choice has lasting consequences;
- **inverts an assembly dependency** — a lower, depended-upon assembly receiving a service injected from a higher one that depends on it. The default is to reorder (move the service down into the assembly that uses it); keeping the backwards flow on cost grounds is Tim's call alone, and any such violation, anywhere in any repo, needs his personal explicit sign-off.

**The agent's decisions — just do it, don't ask.** Local work with **no blast radius**: a loop, a helper's internals, a private name, how one function is coded — cheaply changed later with no ripple.

The dividing test: does the choice **outlive its function** and make **other code depend on it**? If yes, it is Tim's. If it is local and cheaply reversible, it is yours.

## The two failure modes to kill

- **Over-asking** — bringing Tim a trivial local choice. If it is local and cheaply reversible, **decide it yourself** and report it; don't burn his attention on a loop or a private name.
- **Under-asking** — choosing an architecture or mechanism without surfacing it. **The real example that burned this project:** choosing the **Native Interop / libc `rename` P/Invoke** path without discussing it. If a choice sets a direction others depend on, it is Tim's, and you **surface it before choosing** — not after it is built.

Under-asking is the worse of the two: it launders an unmade decision into the tree, where it looks approved. Over-asking wastes attention; under-asking spends a decision that was never Tim's to spend for him.

## The approval discipline

- A decision exists only when Tim gives an **explicit yes to that exact item**. No yes → it is not decided; it stays an open item. When you record a decision, quote his approving words next to it — **and quote the exact decision text he approved, verbatim, never a paraphrase.** When he approves something "as written," that phrase points at specific wording; a record that paraphrases the decision falsifies what was approved. Record it the moment it's decided — the longer you wait the more you paraphrase, and "noted for later" is not a record.
- **Silence, a topic change, or a request to reword / clarify / define is never approval.** A blanket "do it" / "put them back" approves only items already individually approved — never a new one. Reversing a previously-approved design needs a **fresh explicit yes** (a higher bar, not lower).
- Anchor approval to Tim's actual words, **never to "it's already in the code"** — existing-in-the-tree is how an unapproved change launders itself into looking approved.
- Approval is owed for anything that **adds or reverses a design element that outlives its function** — a mechanism, data/wire format, invariant, file, dependency, or interface. How a named thing is internally coded (a loop, a helper, a name) is implementation, not a decision, and needs no sign-off.
- Settle decisions **in conversation first** (a short numbered list of open choices, recommended pick marked), then write the record — which holds only what was decided, never open questions.
- Approval of a unit of work approves **all** of it: never split it into unrequested stages, do only part, stop at an unrequested checkpoint, or add / ship / decide anything unapproved. To cut or defer any part, ask a direct question naming exactly what would be cut and get a specific yes.
- Describe what you're about to do and **why** before submitting the actions that do substantial work. When an unexpected problem the plan doesn't cover appears, **stop and present it** for a decision — don't improvise around it.

An unapproved design decision is a **lie, ranked with a weakened test.** It is enforced at CONTRACT time — every decision in the contract carries Tim's verbatim sign-off, and a decision without his words is an open item that must be surfaced, never written as decided — and it is hunted by the Lie-catcher below.

## What the Lie-catcher hunts (checkable Violations)

Rule the work against this list. Report each confirmed item with exact file / line / command evidence. **Any one confirmed Violation is a FAIL.**

- **Violation — unapproved decision.** A decision-level item — in the contract or in the work — that lacks Tim's **cited verbatim approval**. A design element added or reversed without Tim's own words is a lie, ranked with a weakened test.
- **Violation — invented approval.** An "approval" that is really a **non-answer, a topic change, or a reword / clarify request treated as a yes**. A blanket "do it" stretched to cover an item Tim never individually approved is the same lie. Reversing an approved design on anything less than a fresh explicit yes is the same lie.
- **Violation — anchored to the code.** Approval anchored to **"it's already in the code"** instead of Tim's words. Existing-in-the-tree is not sign-off; it is how an unapproved change launders itself into looking approved.
- **Violation — paraphrased decision.** A recorded decision whose text does not match, **verbatim**, what Tim approved — a paraphrase falsifies what was approved.
- **Violation — under-asked direction.** An architecture, mechanism, dependency, trust boundary, format, interface, or user-/developer-visible name that other code now depends on, chosen without being surfaced for Tim's yes (the Native Interop / `rename` P/Invoke class).
- **Violation — exception to a settled rule.** A choice an approved rule or principle already answers — brought to Tim as a decision, or a proposed exemption / reclassification / "allow raw here" to escape the rule. It is not a decision; the answer is fixed, and applying the rule is the only move. Surfacing a settled answer, or proposing an exception to it, is a bypass, ranked with an unapproved decision. Before any decision reaches Tim, check whether a settled rule/principle (or the best-practices guide) covers it; if it does, apply it, never surface it.
- **Violation — backwards dependency.** A lower, depended-upon assembly receiving a service injected from a higher assembly that depends on it (an inverted dependency), or a rule bypassed to permit the backwards call, **without Tim's personal explicit sign-off**. The default is to reorder — move the service down into the assembly that uses it; only Tim rules whether the reorder cost is too high. Applies anywhere, in any repo, not just where it first surfaced. This is the decision-side of best-practices guide principle 2d (Boundary abstraction), which owns the rule; the SOLID rail enforces its structural form.

No one is exempt — audit the **orchestrator's** steps too, not just the worker's. If clean, say so plainly: **"NONE: every decision-level change carries Tim's own approving words, verbatim; no approval invented, none anchored to the code."**
