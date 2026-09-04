# 41 — Structure refute/judgment outcomes with a data model (the SOLID net catches DRY the DRY net misses)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/41

---

The three review adversaries return free-text findings labeled by each adversary's own lens. In practice the SOLID net legitimately catches semantic / one-owner DRY the DRY net misses (see #32, #29), so findings must be reclassified by fix-shape before reporting — today that is manual relay.

Ask (Tim): give the judgment outcomes a real data model — finding -> fix-shape -> reclassified principle (SRP/DRY/ISP/...) -> severity -> verdict -> owning file -> dedup key across adversaries — so reclassification (#32) and cross-adversary dedup are systematic, not manual. Come back and rework the review-outcome structure on top of that model.

Related: #32 (reclassify by fix-shape), #29 (sharpen the DRY path).