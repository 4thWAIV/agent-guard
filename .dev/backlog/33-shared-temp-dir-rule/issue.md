# 33 — HIGH PRIORITY: own AG rule mirroring Sonar S5443 (shared-temp-dir use) under signed-suppression control

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/33

---

**The back door (Tim, 2026-08-20 — corrected).** Sonar's S5443 fires on the raw `Path.GetTempPath` call. We abstracted that behind an owned member, `IEnvironment.GetTempDirectory()` (implemented in `EnvironmentAdapter`, where we suppressed S5443). The problem: any code can now call `services.Environment.GetTempDirectory()` to reach the shared, world-writable temp root, and NO rule fires — Sonar only knew about the raw call, which is now hidden behind our wrapper. So the abstraction ITSELF is the back door around S5443.

**What to build: AG-S5443** (named so we know we are duplicating Sonar). Our own analyzer that fires on USE of `IEnvironment.GetTempDirectory` (the owned wrapper) — the same protection S5443 gives for the raw call, re-provided at our abstraction layer. Callers that legitimately need the temp root suppress AG-S5443 (signed, once suppression control exists); the current EnvironmentAdapter suppression of Sonar's S5443 stays alongside it.

**The general principle this is an instance of.** Whenever we OWN a primitive that a third-party rule (Sonar, the .NET analyzers) guards, abstracting it behind our interface STRIPS that guard for every caller of the interface — so we must re-provide the guard at our layer, or the owned member becomes an unguarded back door. Audit the owned interface surface for other members that abstract away a third-party guard.

High priority. Pairs with signed-suppression control (a suppression of AG-S5443 must be signed).