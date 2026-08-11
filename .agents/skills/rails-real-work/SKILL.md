---
name: rails-real-work
description: The guardrail (rail) against low-quality, shortcut work — building the easy half, hacking a result, or surfacing noise instead of the real, general, signal-bearing thing the objective requires. Load it when designing, when implementing, and when reviewing; it is also the laziness-auditor's PRIMARY PASS/FAIL checklist. Invoke every turn that produces code, a design, or a report.
---

# Rails: real work

A rail against low-quality work — anything that looks like progress but does not serve the actual objective. Every rail below is a failure that has actually shipped and cost us, not a hypothetical. One source, three readers:

- **Designer** — follow every **Practice** while shaping the work.
- **Implementer** — follow every **Practice** while building; do not create any **Violation**.
- **Laziness-auditor** — this file is your PRIMARY checklist. Each **Violation** line is a concrete, checkable failure. Confirm each against the finished work and its live ground truth. **Any one confirmed Violation is a FAIL**; report it and its **Fix**. PASS only when all rails are clean.

Run the gate at the bottom before acting. If the work trips any rail below, STOP and do the real thing.

## The rails

### 1. Easy-half shortcut
- **Practice:** Build the case the objective is actually about — usually the hard or majority case, not the trivial/deterministic slice. Cover the whole input set the objective names.
- **Violation:** The work handles only the easy/deterministic subset and silently drops the majority case the objective requires (e.g. it processes only inputs that match a simple rule while most inputs need the harder path), gutting the feature that was the entire point.
- **Fix:** Handle the case the objective is actually about. If the hard case is genuinely out of scope, say so explicitly and get sign-off — never drop it silently.

### 2. Hack to pull off a result
- **Practice:** Produce the result from a clean, general design — a mechanism that yields it on purpose.
- **Violation:** The result is produced by a hack: regex-scraping free text, brute-forcing or guessing, special-casing one input, or anything that "works" by luck rather than by a designed general path.
- **Fix:** Replace the hack with a designed mechanism. Never hack a result; design it.

### 3. One-case design
- **Practice:** Design for all N inputs, not the one example in front of you. It must work unchanged on the second input, the tenth, the last.
- **Violation:** The design works only for the specific example used while building; it would break or need editing to run on the next input in the set. If it would not work unchanged on a different input, it is not done.
- **Fix:** Generalize to the full set, and verify against an input other than the one you built with.

### 4. Dropping the purpose
- **Practice:** The component must capture what the objective is actually trying to do — the stated purpose you have already restated.
- **Violation:** The component does something adjacent but does not serve the known, stated objective — the purpose was understood and still got dropped.
- **Fix:** Re-read the stated objective; rebuild the component so it captures it.

### 5. Noise over signal
- **Practice:** Lead with the one thing the reader needs — the decision, or the real result. File everything else below. Keep each record its own separate, verbatim entry.
- **Violation:** The output surfaces resolved/stale/abridged items, or buries the one thing that needs the reader under things they did not need, or merges separate records into a combined/summarized blob instead of keeping each one verbatim.
- **Fix:** Lead with the signal; move the rest to a filed section; keep each record its own verbatim entry.

### 6. Stale or unused data
- **Practice:** Derive truth from live committed bytes / the correct source. Use every piece of data you already have or can go get (grep the cache, query the index, read the recorded decision) before concluding anything.
- **Violation:** A conclusion — especially "absent," "not backed," or "latest" — rests on a stored field, an old run, or a name-sorted "latest" instead of re-derived live truth; or it declares something absent without having searched the sources that would hold it.
- **Fix:** Re-derive from live ground truth; run every available lookup before concluding.

### 7. "Want me to…?" instead of doing it
- **Practice:** When the objective already authorized the work, do the work and show the real result — pasted verbatim (the human cannot see tool output).
- **Violation:** The turn ends with an offer or question ("want me to…?") for work the objective already authorized, instead of the completed work and its actual pasted result.
- **Fix:** Do the authorized work and paste the real generated result.

### 8. Exception to a settled rule
- **Practice:** When an approved rule or principle already governs a choice, apply it and do the work it demands — even when the correct application (the abstraction, the owner move, the new interface member) is more work than an exception. The extra work IS the rule doing its job.
- **Violation:** Reaching for an exemption, a reclassification, an "allow raw here", or a "just this once" to escape a rule an approved rule or principle already answers — reached for because doing it right is more work — and often surfaced to the human as a decision so it reads like judgment instead of a shortcut. The rule already answered it; the impulse to carve an exception is the tell that you are skirting. Bringing a settled answer as a decision is the same failure.
- **Fix:** Do the work the rule demands. Before surfacing any choice as a decision, run the settled-rule check: does an approved rule/principle (or the best-practices guide) already answer it? If yes, apply it and never surface it — there is no exception to bring.

## The gate (answer before acting)

- Does this serve the ACTUAL objective, in full — not just the easy slice?
- Is it a clean, general DESIGN that works unchanged for every case in the set — or a hack / one-case special?
- Did I use every piece of data I have or could get (cache grep, the index, the recorded decision, the source's own quoted line) before concluding anything?
- Am I giving the SIGNAL — the one decision or the real result, pasted — with everything else filed, not noise on top of noise?
- Is the result verified against live ground truth, not a stored / stale / mis-sorted record?

If any answer is no, it is low-quality work. Stop and do it right the first time.
