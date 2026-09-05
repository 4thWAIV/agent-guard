> The live GitHub issue supersedes this file. Read https://github.com/4thWAIV/agent-guard/issues/72 for the authoritative text.

# 72 — Replace the binary auto-resolve boundary with a confidence number and a hidden threshold

## Why

Today an agent resolving a forced choice makes a binary call: auto-resolve it against an approved principle, or leave it open for Tim. A binary is a boundary an agent can learn to sit just inside, and it hides how sure the agent actually was.

Replace it with a number the agent must produce, and do not tell the agent where the review threshold falls.

## The change

**Every `autoResolved` record carries a required numeric confidence, strictly greater than 0 and strictly less than 100.** Banning both ends forces a real estimate: 100 is a claim of certainty no judgement call earns, and 0 is a refusal to engage.

**The threshold is never published to the agent.** With no boundary to hug there is nothing to game. The filter runs on our side, after the results come back.

## Companion rule, without which the number does not help

**One claim per record.** A resolution containing an "and" is two records with two numbers.

This is the lesson from the run that prompted this. Two explorers welded a shape decision and a placement decision into one sentence. The shape was correct and any honest confidence on it is high; the placement was Tim's to make. A single number on that record comes back high and tells us nothing, because the confidence in the first claim carries the second along for free. A third explorer facing the same question split it correctly of its own accord, so the line is findable.

## Calibration — set the threshold from data, do not pick one now

A model's stated confidence clusters high and does not spread like a real probability. An absolute cut chosen in advance returns either everything or nothing. Run it for several runs first, look at the distribution, and consider filtering on relative position within a run rather than an absolute value until there is enough data to justify one.

## One representation of "how sure"

The prior-art ledger already returns a `confidence` field, as the string `high`/`medium`/`low`. If a numeric field is added, decide whether the ledger's converges on it or stays deliberately separate. Two ways of saying how sure something is, in the same pipeline, is the duplication this project fails work over.

## Surfaces

`rails-decisions` owns the `autoResolved` contract and states the four required fields. The result schemas that enforce it live in `.agents/workflows/ground.js` and `.agents/workflows/design.js`, with the hidden-decision filter in `.agents/workflows/hidden-decision-scan.js`. The Lie-catcher in `.agents/workflows/refute.js` verifies the records.

## Done when

Every `autoResolved` record carries a confidence strictly between 0 and 100 and exactly one claim, the schema rejects a record that does not, the threshold lives outside anything an agent reads, and the ledger's confidence field either uses the same representation or is documented as deliberately different.

