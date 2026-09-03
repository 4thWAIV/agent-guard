---
name: rails-solid-code
description: The SOLID design guardrail and the SOLID adversary's PRIMARY PASS/FAIL checklist — design and structure only. Load it when designing, when implementing, and when reviewing finished work.
---

# Rails: SOLID code

A rail against structural decay — designs and fixes that patch a symptom, bake in one case, or bolt a parallel path beside the one that should own the concern. Design and structure ONLY; duplication is the DRY rail's job, not this one. One source, three readers:

- **Architect** — follow every **Practice** while shaping the design.
- **Implementer** — follow every **Practice** while building; create no **Violation**.
- **SOLID adversary** — use every **Violation** as the PRIMARY checklist for the finished diff and its live ground truth.

**When SOLID and DRY conflict, SOLID wins.** Required changes are made SOLID-first; collapsing structure to remove a duplicate never justifies a symptom fix, a hardcoded specific, or a second path.

## The rails

### 1. Single owner of the invariant
- **Practice:** Every invariant/rule is enforced in exactly one module or process that owns it. Name that owner before changing anything.
- **Violation:** The invariant has no single owner — it is enforced, re-checked, or re-derived in more than one place — or the change enforces the rule somewhere other than its rightful owner.
- **Fix:** Locate the one rightful owner, put the enforcement there, and collapse the scattered copies of it into that owner.

### 2. Owner, not symptom
- **Practice:** Fix the invariant at its owner so every current and future manifestation is covered by the one change.
- **Violation:** The change patches one visible symptom — the failing case in front of you — while the owning module still admits the defect, so other manifestations remain.
- **Fix:** Trace the symptom back to the owning module/process and fix it there, not at the surface where it happened to show.

### 3. No hardcoded specifics
- **Practice:** Encode the general rule. Match on the structural property, not the concrete instance.
- **Violation:** A specific file, module, id, path, selector, text phrase, or observed current data pattern is baked into the logic in place of a general rule.
- **Fix:** Replace the literal with the general rule it is one instance of.

### 4. No unjustified second path
- **Practice:** Extend the existing path that already owns this concern.
- **Violation:** The change adds a second path beside an existing one without explaining, in writing, why the existing path cannot own it.
- **Fix:** Route through the existing owner — or state the specific reason it cannot own this and get that reason signed off before adding the parallel path.

### 5. No recurrence on the next case
- **Practice:** The design must be general enough that the same failure cannot reappear on the next file, module, input, or run.
- **Violation:** The same failure would recur on the next file, next module, next input, or next run — the work handles only the instance built against.
- **Fix:** Generalize to the whole set, and verify against an input other than the one that triggered the fix.

### 6. Gate wired in, not remembered
- **Practice:** The fix carries a guard that fails when the defect returns — a failing fixture, regression check, deterministic replay check, or exact proof command — and that guard is wired into the normal workflow (build/test/gate) so it runs without anyone remembering to.
- **Violation:** There is no failing fixture / regression check / replay check / proof command guarding the fix, OR the verifier exists but relies on a human remembering to run it instead of being wired into the normal workflow.
- **Fix:** Add the guard and wire it into the workflow the run already executes.

### 7. Full general seam — no one-case hack, no premature escape hatch
- **Practice:** For known-needed design, build the full general seam up front.
- **Violation:** A one-case hack or a deferred abstraction where the general seam was already known to be needed; OR a speculative escape hatch / extension point built "for later" with no real, approved need.
- **Fix:** Build the full seam now for the known need; delete the speculative hatch.

### 8. Dependencies point one way
- **Practice:** A lower, depended-upon assembly never needs a service injected from a higher assembly that depends on it. Keep the dependency flow one-directional by placing each service in the assembly that uses it. This rail enforces principle 2d (Boundary abstraction) of the best-practices guide (`.dev/reference/best-practices-guide.md`), which owns the rule.
- **Violation:** A service is injected UP — a dependent assembly hands it into the very assembly it depends on (for example `AgentGuard.Boundaries` passing a service into `AgentGuard.CrossPlatform`, which `Boundaries` references), or a rule is bypassed to permit the backwards call. This is a top-line finding.
- **Fix:** Reorder so the flow is one-directional — usually MOVE the needed service DOWN into the lower assembly that uses it. Reordering is the default; only Tim rules whether the reorder cost is too high, and any such violation needs Tim's personal, explicit sign-off (anywhere, any repo).

### 9. No language escape hatch without approval

- **Practice:** No language escape hatch (e.g. TypeScript `any`) without explicit approval and a stated reason it beats the alternatives.
- **Violation:** A language escape hatch is used without explicit approval and a stated reason it beats the alternatives.
- **Fix:** Remove the language escape hatch, or get the required explicit approval and record the reason it beats the alternatives.

## Verdict

Return PASS only when no Violation is confirmed. Return FAIL when any Violation is confirmed.
