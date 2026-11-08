# Replace personal authority references with defined human roles

GitHub issue: https://github.com/4thWAIV/agent-guard/issues/83

The live GitHub issue supersedes this filing copy.

## Work item
Matt's second work item, after the documentation cleanup. Start from current origin/dev, not main. This ticket records the work; do not perform the replacements as part of filing it.

## Required terminology
Replace personal references to Tim in governing documentation and agent/workflow instructions with the appropriate role for the context:

| Role | Meaning |
| --- | --- |
| Human Developer | An AgentGuard user/customer using AgentGuard in their own project, other than the AgentGuard project itself. |
| Agent Guard Contributor | A human developer working on AgentGuard with rights to edit its repository. |
| Agent Guard Maintainer | A human maintainer of the AgentGuard repository. Matt is now a maintainer. |
| Agent Guard Admin | A human admin of the AgentGuard repository. Tim currently holds this role. |

## Required work
Distinguish instructions for customers using AgentGuard in their projects from instructions for people developing AgentGuard itself. Classify each governing reference by its intended role; do not globally replace Tim with one term.

Use the four role names consistently. Where an existing instruction does not establish whether a contributor, maintainer, or admin should have the authority, present that specific choice for human resolution rather than inventing permissions.

Historical records attributing an actual decision or action to Tim are not role definitions; preserve those attributions.

## Starting locations
The current search found references in:
- .agents/skills/rails-decisions/SKILL.md
- .agents/skills/rails-run-a-workflow/SKILL.md
- .agents/skills/rails-solid-code/SKILL.md
- .dev/reference/best-practices-guide.md
- .agents/workflows/refute.js
- .agents/workflows/implement.js
- .agents/workflows/architecture.js
- .agents/workflows/tdd.js

Recheck current dev rather than treating this list as exhaustive. Include applicable prompts and error messages so agent instructions do not retain contradictory personal authority wording.

## Boundary
This is the role-terminology work item, not a redesign of workflow execution or approvals. Coordinate with [Guard process workflow](https://github.com/4thWAIV/agent-guard/issues/82) where it touches the same instructions.

## Completion
- Governing references use the appropriate defined role instead of treating Tim as every human actor.
- Customer-project instructions and AgentGuard-repository instructions are distinguishable.
- Any authority choice needed for the replacements is explicitly resolved, not silently assumed.
- Historical attribution remains accurate.
