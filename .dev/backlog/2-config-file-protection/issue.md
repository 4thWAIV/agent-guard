# 2 — Protect the guard's own config files after a call (partial restore of settings.json + config.json)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/2

---

The guard's own wiring and config — `.claude/settings.json` and `.agentguard/config.json` — are not fully protected today. `settings.json` is only blocked before a tool runs, which a shell command can evade, disabling the guard's hooks; `config.json` has no protection at all, so one shell write can empty it and silently turn protection off.

**Required:** watch both files after every tool call. Before the call, snapshot the guard's own parts of each file. After the call, if the guard's parts changed without authorization, restore just those parts and leave all of the user's other content untouched.

**Mechanism:** a shared read/write layer that reads and rewrites one part of a config file by understanding its format as data — JSON now; XML, TOML, YAML, INI/editorconfig as needed (see #1 for `.js`/`.ts`). Each protected file declares its format and which parts belong to the guard; the check compares only the guard's parts and rebuilds the corrected file.

The guard's own legitimate edits (`guard init`, `guard doctor --fix`) are authorized so its protection does not revert them.

This is the core defense against the guard being silently disabled. Third of three builds (installer, presence gate, this).