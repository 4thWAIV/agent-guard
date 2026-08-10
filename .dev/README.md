# .dev — how work is organized

Work flows through three folders. Two more hold things that are not a work item.

- **backlog/** — planned work, not started. Each item is the design / sketch / contract for something we intend to build.
- **inprocess/** — the active work. When a backlog item starts, its files move here: the contract, run-records, adversary verdicts, agent results — everything about the current work that is not code — lives here while it is in flight.
- **completed/** — finished work. When a work item is done, its files move here. `run-records/` holds the contract + adversary verdicts + report for each shipped build; `designs/` holds design / spec docs whose subject shipped.
- **reference/** — living documents, not tied to one work item: the decisions log (`DECISIONS.md`) and standing decision records.
- **archive/** — non-workflow historical material. e.g. `original-specs/`, the TypeScript agent-governance build guide that agent-guard was ported from.

Lifecycle: **backlog → inprocess → completed.** `reference/` and `archive/` sit outside that flow.
