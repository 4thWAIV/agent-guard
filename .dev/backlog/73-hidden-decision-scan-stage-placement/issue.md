> The live GitHub issue supersedes this file. Read https://github.com/4thWAIV/agent-guard/issues/73 for the authoritative text.

# 73 — The hidden-decision scan is documented under GROUND but cannot run until a contract exists

## The problem

The hidden-decision scan is documented as part of GROUND and cannot run there.

`hidden-decision-scan.js` refuses to start without `{ contractPath, projectPath }` — it needs a contract file on disk. `rails-run-a-workflow` lists the scan under GROUND and names "the draft contract for the hidden-decision scan" among GROUND's own inputs. But `contract.md` is written by the CONTRACT stage, two stages after GROUND, and CONTRACT's input list includes "unresolved findings from the hidden-decision scan." Each stage waits on the other.

A second tell sits in the same paragraph: "No contract advances past CONTRACT with an unresolved forced choice" is written inside the GROUND section. It is a CONTRACT gate in the wrong place, which is likely how the scan came to be filed under GROUND at all.

## What it cost

The GROUND run for issue #63 completed its explorers and its prior-art ledger and then had nowhere to go. The scan is still pending after the stage it belongs to is finished, so GROUND cannot be closed and the orchestrator has to carry that in its head across stages.

## Two ways to fix it, and they are not equivalent

**Move it into CONTRACT.** The orchestrator drafts the contract, runs the scan over the draft, takes the findings to Tim, records the answers, and locks. This matches the script exactly as built and is a documentation change with no code behind it. The cost is that the contract is written, scanned, taken to the human, and then revised — and the contract is meant to be the locked record, not a draft that cycles.

**Move it to the end of DESIGN.** The scan runs against the selected approach and the existing decision record before any contract exists, so the contract is written once with every forced choice already answered. This needs a real code change: the script must take the DESIGN output and the decision record in place of `contractPath`, because without a contract there is no Decisions section to compare against. The comparison becomes "what does this approach force, and what does the record already answer," which is the same question against a different pair of inputs.

The second is the better sequencing and the more expensive fix. Pick deliberately rather than by which is cheaper.

## Surfaces

`.agents/skills/rails-run-a-workflow/SKILL.md` — the GROUND section's Role, Scripts and Inputs lines, the paragraph describing the scan, and the CONTRACT section's Inputs line.

`.agents/workflows/hidden-decision-scan.js` — the input validation and whatever the hunters are handed.

`.agents/workflows/design.js` — only if the scan moves to end-of-DESIGN and needs the design output in a particular shape.

## Done when

The scan runs at a stage where every input it needs already exists, the rail describes it in exactly one place with no stage listing an input another stage has not produced yet, the CONTRACT gate sentence lives in the CONTRACT section, and a pipeline run end to end leaves nothing pending after the stage that owns it.

