---
name: rails-solid-code
description: The SOLID design guardrail and the SOLID adversary's PRIMARY PASS/FAIL checklist — design and structure only. Load it when designing, when implementing, and when reviewing finished work.
---

# Rails: SOLID code

A rail against structural decay — designs and fixes that patch a symptom, bake in one case, or bolt a parallel path beside the one that should own the concern. Design and structure ONLY; duplication is the DRY rail's job, not this one. One source, three readers:

- **Architect** — follow every **Practice** while shaping the design.
- **Implementer** — follow every **Practice** while building; create no **Violation**.
- **SOLID adversary** — this file is your PRIMARY PASS/FAIL checklist. Each **Violation** line is a concrete, checkable structural defect; confirm each against the finished diff and its live ground truth. **Any one confirmed Violation is a FAIL.** Report blocking Violations first with exact file/line and the **Fix**; list which proof commands passed or were not run; name what you attempted to refute and how (an adversary that lists no refutation attempts is rubber-stamping). PASS only when every rail is clean.

**When SOLID and DRY conflict, SOLID wins.** Required changes are made SOLID-first; collapsing structure to remove a duplicate never justifies a symptom fix, a hardcoded specific, or a second path.

If the code's state makes SOLID hard in a way the plan didn't cover, STOP and present it — do not improvise around it.

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

### 7. Never weaken a requirement to fit the code
- **Practice:** Grow the code (or the tool) until it meets the requirement as stated.
- **Violation:** The change narrows, softens, or reinterprets a requirement so it matches what the code already does. This is a top-line finding.
- **Fix:** Restore the full requirement and grow the code to meet it.

### 8. Full general seam — no one-case hack, no premature escape hatch
- **Practice:** For known-needed design, build the full general seam up front.
- **Violation:** A one-case hack or a deferred abstraction where the general seam was already known to be needed; OR a speculative escape hatch / extension point built "for later" with no real, approved need.
- **Fix:** Build the full seam now for the known need; delete the speculative hatch. Expansion happens on a real, approved need — never before, never deferred past a known one.

### 9. Take the correct approach, not the cheaper wrong one
- **Practice:** When a correct approach and a cheaper wrong one are both visible, take the correct one and report it.
- **Violation:** The cheaper wrong approach was shipped — often disguised as a neutral "your call" flag when the right choice was actually clear.
- **Fix:** Implement the correct approach and report it. Reserve flagging for a genuine unresolved trade-off.

### 10. Dependencies point one way
- **Practice:** A lower, depended-upon assembly never needs a service injected from a higher assembly that depends on it. Keep the dependency flow one-directional by placing each service in the assembly that uses it. This rail enforces best-practices guide principle 2d (Boundary abstraction), which owns the rule.
- **Violation:** A service is injected UP — a dependent assembly hands it into the very assembly it depends on (for example `AgentGuard.Boundaries` passing a service into `AgentGuard.CrossPlatform`, which `Boundaries` references), or a rule is bypassed to permit the backwards call. This is a top-line finding.
- **Fix:** Reorder so the flow is one-directional — usually MOVE the needed service DOWN into the lower assembly that uses it. Reordering is the default; only Tim rules whether the reorder cost is too high, and any such violation needs Tim's personal, explicit sign-off (anywhere, any repo).

## The gate (answer before acting)

- Does exactly one module/process own this invariant, and did I fix the owner — not a symptom?
- Is the logic a general rule, with no file / id / path / selector / text / data-pattern hardcoded — and would it hold on the next file, input, and run?
- Did I extend the existing path, or add a second one only with a written, signed-off reason the existing path cannot own it?
- Is the fix guarded by a fixture / regression / replay / proof command wired into the normal workflow, not left to human memory?
- Did I meet the requirement as written (never weakened it), build the full general seam (no one-case hack, no speculative hatch), and take the correct approach rather than the cheaper wrong one?
- When SOLID and DRY pulled apart, did SOLID win?
- Do all dependencies point one way — no service injected up from a dependent into the assembly it depends on?

If any answer is no, the structure is wrong. STOP and fix it before acting. For the SOLID adversary: any one confirmed Violation is a FAIL.
