# 36 — Enforce the run-record invariant: fail if inprocess/ holds a run-record for shipped work

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/36

---

rails-run-a-workflow now says (Provenance + REPORT) to move a run-record inside the shipping PR before merge, and to sweep `.dev/inprocess/` for stragglers whose work already shipped. That is enforced by hand today.

Add mechanical enforcement — a structural test or CI check that fails the build when any `.dev/inprocess/<run>/` run-record is for work already marked done/shipped, so a forgotten move cannot merge.

Design need: a machine-readable link between a run-record and its status. The MANIFEST is currently prose tables, so this needs either a parseable status field per run-record folder or a machine-readable MANIFEST section. Decide that data contract first.

Context: this came up when the 2026-08-11-lockdown-and-coverage umbrella shipped to dev but its run-record sat in inprocess/ and had to be filed in a later direct push (commits 87a038f, 89dd82b). Related: #21 (enforce the workflow pipeline).