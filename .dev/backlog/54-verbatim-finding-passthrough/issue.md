# 54 — Fix-round scripts must pass adversary findings through verbatim, not through an orchestrator-composed string

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/54

---

## What is broken

`rule-phase.js`, `architecture.js`, and `tdd.js` each accept a single free-text `refutation` argument that the orchestrator composes by hand, and paste it into a fix-round preamble that tells the agent: *"Apply EXACTLY these confirmed, human-approved fixes — nothing more."* (`rule-phase.js:81`, `architecture.js:82`, `tdd.js:82`; `implement.js:49` has the same shape under the name `instruction`.)

Nothing connects that text to the adversary verdicts the previous round actually produced. The findings already exist as structured data — each verdict carries `findings` with `summary`, `evidence`, and `fix` — but the fix round never receives them. Whatever the orchestrator types becomes the entire truth the next agent sees, and the prompt then stamps it as human-approved whether or not it is.

## How it burned us

In the Linux policy-path run (contract `.dev/inprocess/2026-08-23-os-presence-check/contract-linux-policy-path.md`), the RULE-PHASE duplication adversary's fix ended with:

> This touches InteropOnlyInCrossPlatformLibrariesAnalyzer.cs, a file outside this contract's declared rule-phase surface, so the collapse needs a scope note/sign-off before landing — flag that, don't silently expand scope.

The orchestrator wrote the fix-round directive by hand, dropped that closing sentence, and launched 12.4 seconds later. The rule-gen agent never saw the condition. A shipped analyzer outside the contract's surface was rewritten with no sign-off, and it was not caught until REFUTE, hours later.

Full forensics: `.dev/inprocess/2026-08-23-os-presence-check/run-linux-policy-path/11-forensic-audit/`.

## The fix

Split one field into two.

1. The fix round takes the prior round's `verdicts` array as its own argument and renders every finding verbatim — lens, summary, evidence, fix — into the preamble, from the structured data rather than from a string. There is then no point at which the orchestrator can edit an adversary's words in transit.
2. The orchestrator's own direction stays, but as a separate, clearly-labelled section that sits **on top of** the findings and never replaces them — the shape `implement.js` already uses for its "additional direction" block.
3. The preamble stops asserting the fixes are human-approved unless an approval argument is actually passed. Today that claim is unconditional, and it was false when it mattered.

Four scripts, the same edit in each; roughly thirty lines total, no new files, no change to any agent's job.

## Sequencing

Tim's next task is an audit of how we communicate with agents, moving to a structured JSON schema (related: #41, which asks for finding → fix-shape → severity → verdict as a data model). This fix is one field becoming structured data and is a strict subset of that work. Build it inside that audit rather than twice.