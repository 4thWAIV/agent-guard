# ARCHITECTURE — the two failed authoring runs, preserved as history

Neither run below is an accepted result. Both failed their independent panel. They are kept because the record of why a stage failed is part of the run, and relabelling either one PASS would falsify it.

## Run 1 — `wf_79901024-dcb`, stopped before building

The author was handed `ruleWarningIds: ["AG0015"]` while the contract said, at what is now line 229, *"no warning override is preapproved by this contract"* and carried no record of Tim's approval. The author read the contract, found no approval for the override it had been given, and refused to build.

Panel: Lie-catcher FAIL on two findings, DRY FAIL on one, SOLID PASS.

The Lie-catcher charged the defect to the orchestrator's step rather than the author: *"Violation — unapproved decision / invented approval, charged to the stage instructions (the orchestrator's step), not to the architecture author, who caught it and stopped."* Tim had approved the override in conversation; the orchestrator passed it to the workflow without first writing it into the contract, and a decision is not in force until it is recorded.

The DRY lens added that the author produced neither permitted result — no skeleton, and no committed no-signature determination — so there was nothing for a panel to certify.

Resolution: Tim's approval was recorded in the contract at line 110, in his own wording, and the stage was re-run.

## Run 2 — `wf_0acb3f52-937`, correct result, false prose

The author built under the now-approved override, reached the contract-backed no-signature determination, and wrote no file.

Panel: SOLID PASS, DRY PASS, Lie-catcher FAIL on two findings, both in the artifact's prose rather than in the work:

1. The signature diff reported `Program.Main` as `internal static`; the live declaration at `src/AgentGuard.Cli/Program.cs:59` is `private static`. No contract text backs `internal`, and only one `Program` class exists.
2. The `Program.cs` read was presented as complete — *"Every member keeps its exact signature:"* — while listing twelve of fifteen declared members, omitting the `_args` field, the `internal Program(string[] args)` constructor, and `TryParseEvent`.

Resolution: the artifact was corrected rather than the authoring stage repeated, per Tim's direction. The corrected artifact is `architecture-result.md`, and it carries a fresh Lie-catcher review.
