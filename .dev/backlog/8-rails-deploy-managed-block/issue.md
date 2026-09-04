# 8 — Deploy the rails skill-set into other repos via a self-injected managed block

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/8

---

## What
Install agent-guard's rails skill-set and rules into other repos, so the rules travel with the tool instead of being copied by hand.

## Mechanism (decided)
agent-guard injects its **own managed rules-block on install**, mirroring how CodeGraph injects its `<!-- CODEGRAPH_START -->` block:
- **presence-locked, position-flexible** — the block may move within the target file, but it must stay present.
- On install, agent-guard writes/refreshes this managed block; on a guarded repo it enforces the block's presence.

## Scope
- Install the `.agents/skills/` rails set (rails-read-me + the six rails) into a target repo, symlinked to `.claude/skills/` as here.
- Inject the managed rules-block into the target's agent-config file (the global/project rules), the CodeGraph way.

## Source
Decision #1 in `.dev/inprocess/2026-08-07-rules-and-process-reorg/RESUME.md` (recorded before it was lost to a context reset). This is the "deploy-skill-set-to-projects" item the MANIFEST marks as "issue owed".