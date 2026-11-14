# Where these two results came from

`author-result.json` is the RULE-PHASE author's own result, copied without change from the last agent result in the fix round's journal, `wf_65e741e9-f59/journal.jsonl`, written 12 November 19:38. It is the round that applied the fixes in `../fixes-for-the-two-failed-rules.md`, and it carries the three rules, their violating and compliant fixtures, the build proof, and rule 1's impact on existing code: fifteen literals in `ts/metalang/app.tsx` that IMPLEMENT clears.

`../tdd/result.json` is the TDD stage result, copied without change from the completed run `wf_453d9fce-e9f`: the three test files, the RED proof, fifteen coverage rows and a PASS from each of the three independent reviewers.

Neither file is composed or abridged. The workflow runtime keeps each agent's result in its journal and the stage result in its return value, and nothing in the repository held either one, so they are written here where the next stage reads them as file paths.
