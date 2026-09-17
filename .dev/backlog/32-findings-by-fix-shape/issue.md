# 32 — Reclassify refute findings by fix-shape (keep the SOLID net that catches semantic DRY)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/32

---

**Observed 2026-08-20 (RULE-PHASE panel).** The SOLID adversary caught a real duplication the DRY adversary missed: AG0022 hardcoded the container-interface set instead of deriving it from the shared `BoundaryServices.ResolveTree` walk. The DRY adversary hunts TEXTUAL duplication (copied blocks/values/functions); this was SEMANTIC — the same knowledge ('what is a container') represented two different ways (a hardcoded list vs a derived walk), so the copy-hunt slid past it. The SOLID lens caught it as 'no single owner of the invariant.'

**Problem:** the SOLID lens mislabels these as SOLID when they are DRY. That costs review trust — a DRY finding is trusted on sight (the fix is always 'collapse the copies into one owner, both consumers reuse it'), a real SOLID finding needs a harder look; a mislabeled DRY makes the human scrutinize a simple fix.

**Tim's call:** do NOT remove the SOLID adversary — it catches semantic DRY the DRY adversary currently misses (baby/bathwater). Instead add a reclassification pass/skill, run after the refute panel, that relabels each finding by the fix-shape tell: fix = 'merge copies / derive from the one owner' => DRY; fix = 'split a class's two responsibilities' => SRP; a specific OCP/LSP/ISP/DIP mapping => that letter. Correct labels without losing coverage.

**Relates to #29** (semantic DRY — the DRY-rail redefinition + semantic/divergence pass). Once the DRY adversary catches semantic duplicates directly, the SOLID lens's overlap shrinks and the reclassify pass mostly confirms rather than corrects.