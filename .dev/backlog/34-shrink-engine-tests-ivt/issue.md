# 34 — Shrink the Engine->Tests IVT to SystemServices.Create only (test grant-shrink)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/34

---

**Context.** `SystemServices.Create` is the one thing IVT genuinely exists for — the composition root you cannot construct the container without. But `AgentGuard.Tests` reaches many more Engine internals through the `Engine -> Tests` grant. The pipeline reaches were removed in the test migration; the remaining reaches are the setup/serialization surface, and they fall into two kinds:

1. **DRY-value reuse of the guard's layout/wire contract** — reached only to avoid duplicating a literal:
   - `CoreSystemPaths.ProjectConfigRelative`/`ClaudeSettingsRelative` (the config + settings paths; 38 references, 2 constants).
   - `ClaudeSettingsWiring.ToolMatcher` (the tool matcher, deliberately test-facing).
   - `ProjectConfig.ProtectedPathsWireKey` (the config JSON key).
   These are the guard's layout/wire contract, not implementation. **Fix:** expose the user-facing paths, the matcher, and the config schema key on a small public surface both production and tests use.

2. **White-box tests of internal setup/serialization logic:**
   - `ProjectConfig` (the config DTO), `SetupCommands` (install/init/doctor), `ClaudeSettingsWiring.AddGuardEntries`/`Inspect`, `ShellProfile.Resolve`, `SnapshotSerializer`, `SetupJson`.
   The setup commands are already exercised through the CLI (`Program.Run install/init/doctor`), so those tests can be **black-box** (write `config.json`, drive the CLI, assert the resulting files) with no grant. A pure serializer round-trip may be the one genuinely-white-box unit; assess each.

**Goal:** after this, `Engine -> Tests` holds nothing but what `SystemServices.Create` (via `Boundaries -> TestHelpers`) already covers — ideally the `Engine -> Tests` grant is removed entirely, or reduced to a single documented entrypoint. Deferred deliberately (Tim approved finishing the migration first, 2026-08-21); this is its own focused pass, not part of the current run.