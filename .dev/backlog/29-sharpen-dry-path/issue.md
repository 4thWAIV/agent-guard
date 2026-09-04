# 29 — Sharpen the DRY path (the most-leveraged adversary): design-time reuse instructions + semantic/divergence catch

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/29

---

Converged with Tim 2026-08-14, motivated by a real incident: in a rule-phase the construction analyzer re-derived the namespace+assembly identity check that `CompositionPoint` already owned (dropping the assembly anchor), opening a self-grant false-positive hole — and it passed the DRY adversary, because DRY hunts literal copy-paste and this was a *diverged semantic duplicate*.

**1. Redefine DRY (rails-dry-code).** From 'no duplicated block' to the real standard: *every piece of knowledge has one authoritative representation.* Consequences: a semantic duplicate (same responsibility, two implementations) is a violation even when the text differs; a duplicate that currently AGREES is the WORST case (it will drift — someone updates one copy and not the other), not exempt; a duplicate that DISAGREES is an active bug now plus over-complex. The adversary fails ANY second representation; 'they currently agree' is no defense.

**2. Design-time DRY — proactive, highest leverage.** Run a prior-art-ledger-driven DRY at DESIGN, against the EXISTING code, to find what the drafted design would make a downstream agent re-implement, and write explicit 'reuse X' instructions into the contract before it locks. This is the same code-level DRY run early to fix the instructions — NOT a separate 'design duplication' check. It is what would have prevented the incident: `CompositionPoint`'s identity check already existed; the contract should have said 'the construction rule reuses CompositionPoint's identity predicates,' and the rule-gen agent (which follows the contract) would have reused instead of re-derived. Limit: it only catches this if the design surfaces the concern at the right grain ('identity-matching has one owner, every rule reuses it').

**3. Sharpened catch — reactive, the backstop.** Duplication is born at every creative phase (design, rule-phase, implement), so the catch runs after each. KEEP the literal copy-paste pass exactly as-is — it's cheap, high-volume, nails the common junk (the fast front line). ADD a second pass that hunts by RESPONSIBILITY (using the capability lenses — CodeGraph/lore/grep, the same as the prior-art-ledger): for each concept the change touches, is it now represented in more than one place, and do the copies agree? Diff near-duplicates and surface divergence first — the divergence is where the bug hides.

**Also (shift-left generally):** add a SOLID/DRY review at contract/design time to catch design-level duplication and mislaid responsibility before code exists; keep the rule-phase/implement adversary as the backstop.

Priority: Tim ranks DRY the single most important adversary ('if I could keep the AI DRY I remove 90% of the crap bug-riddled code it generates').

Files: `.agents/skills/rails-dry-code/SKILL.md` (definition), `.agents/workflows/refute.js` (DRY lens: add the semantic/divergence pass after the literal pass), the DESIGN/contract path (design-time reuse-instruction hook), `.agents/workflows/prior-art-ledger.js` (the substrate both reuse and the semantic catch stand on).