# 1 — Add .js/.ts config-file support to part-level file protection

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/1

---

The guard protects only its own sections of a config file: before a tool runs it snapshots the file, and after, if the guard's sections changed without permission, it restores just those sections and leaves the rest of the user's file alone.

The shared read/write layer supports data formats — JSON, JSONL, XML, TOML, YAML, and INI/editorconfig. Adding a format means registering a reader that can read a named part and write a corrected part back.

`.js`/`.ts` config files (eslint.config.js, vite.config.ts, next.config.js, etc.) are excluded for now because they are code, not data: you can't read one setting and rewrite the rest without parsing the file into a syntax tree and re-emitting it with a formatting-preserving rewriter. Whole-file locking is not acceptable — users legitimately edit these configs.

This issue tracks building that code-config reader so `.js`/`.ts` configs get the same part-level protection as the data formats.