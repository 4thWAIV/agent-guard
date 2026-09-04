# .dev — how work is organized

One folder per work item, created once and never duplicated. Filing a GitHub issue creates it at `.dev/backlog/<issue-number>-<slug>/`, and that same folder travels with the work through three folders. Two more hold things that are not a work item.

- **backlog/** — planned work, not started. Each work item's folder holds `issue.md` with the issue body, which the live GitHub issue always supersedes, plus the design / sketch / plan for something we intend to build.
- **inprocess/** — the active work. When a backlog item starts, its folder moves here: the contract, run-records, adversary verdicts, agent results — everything about the current work that is not code — lives inside it while it is in flight. A run never creates a second folder.
- **completed/** — finished work. When a work item ships, its folder moves here. `run-records/` holds the contract + adversary verdicts + report for each shipped build; `designs/` holds design / spec docs whose subject shipped.
- **reference/** — living documents, not tied to one work item: the decisions log (`DECISIONS.md`) and standing decision records.
- **archive/** — non-workflow historical material. e.g. `original-specs/`, the TypeScript agent-governance build guide that agent-guard was ported from.

Lifecycle: **backlog → inprocess → completed.** `reference/` and `archive/` sit outside that flow. `rails-run-a-workflow` owns when the folder moves, what a run keeps inside it, and how work the human starts without an issue gets its folder.
