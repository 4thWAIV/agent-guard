# 41 — Structure refute/judgment outcomes with a data model (the SOLID net catches DRY the DRY net misses)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/41

---

The three review adversaries return free-text findings labeled by each adversary's own lens. In practice the SOLID net legitimately catches semantic / one-owner DRY the DRY net misses (see #32, #29), so findings must be reclassified by fix-shape before reporting — today that is manual relay.

Ask (Tim): give the judgment outcomes a real data model — finding -> fix-shape -> reclassified principle (SRP/DRY/ISP/...) -> severity -> verdict -> owning file -> dedup key across adversaries — so reclassification (#32) and cross-adversary dedup are systematic, not manual. Come back and rework the review-outcome structure on top of that model.

Related: #32 (reclassify by fix-shape), #29 (sharpen the DRY path).

## Automatic evidence capture and decision-focused reports

Extend the structured review outcomes with automatic capture of each verification command, its complete output, its actual exit code, and the tested snapshot identity in the work item's folder. Retention must not depend on an agent manually copying terminal output into a report.

Use the structured outcomes and retained evidence to generate a short report highlighting failures, decisions needing approval, scope changes, and missing proof, with links to the underlying records. Keep optional detail below the fold so the user and assisting agents do not need to read every transcript to find what needs attention.

Summarization must not silently discard findings. Preserve the underlying evidence and individual findings, including when reports group duplicate findings across reviewers.
