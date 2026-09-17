# 70 — Give GROUND and DESIGN a written brief

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/70

---

`ground.js` and `design.js` start their agents with no statement of what the work is for. The explorers receive only their area's focus, and `design.js` receives a goal typed into a tool call rather than a file. The purpose and the settled decisions live only in the GitHub issue, which no stage reads.

This change gives both scripts three inputs — the issue number, the work folder path, and an optional scope line when a run covers only part of an issue — and renders them into a brief that opens every agent prompt either script builds. The live GitHub issue is the authority; the folder holds background.

## Done when

Both scripts refuse to launch without the issue number and folder path, every prompt either builds carries the brief, `design.js` no longer takes or records a goal, and the brief's shared code is byte-identical in both.
