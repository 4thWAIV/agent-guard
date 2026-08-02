# Protect the guard's own config files with per-region modes

## Approved decisions (each carries Tim's exact words)



1. **The guard keeps its own hooks canonical and does not police hooks you add.** It can't tell a hook the AI added from one you added, so it only re-asserts its own groups and leaves every other hook alone. Tim: *"HOW do you know they are not legit?"*
2. **Identify the guard's own hook groups by their command** — a call to the guard binary running `hook pre`/`post`, which the guard computes — not by any marker. This supersedes every `--agentguard-owned` reference in the body below. Tim: *"Should not 'exactly' the guard's hooks be a call to the guard CLI ... is that not a better way to locate it."*
3. **Remove the `--agentguard-owned` marker** (`HookCommand.OwnedFlag`) and every use of it. Tim: *"GET RID OF THESE --agentowned BULLSHIT ... that you put in with no ... APPROVAL or even discussion with me."*
4. **config.json protection stays as designed** — mode-1 structure/defaults, mode-2 `protectedPaths` reverted unless a grant covers it. (Already in the framework body below; listed so the set is complete.) - Tim indicates NOW as I (Tim) write this by hand that this is approved.
5. **The region check runs before the fingerprint gate**, so a `protectedPaths` change is reverted rather than denied-without-revert in the two-process case. See What-to-do 7 and Acceptance 9. -- Tim approves now by hand.
6. **Add `.agentguard/config.json` to the pre Bash-scan** so a shell command naming it is blocked at Pre like the other core files. See What-to-do 8 and Acceptance 11.  -- Tim agrees.
7. **Broaden the hook matcher** to `Edit|Write|MultiEdit|NotebookEdit|Bash|Monitor|PowerShell|mcp__.*`, so MCP, Monitor, and PowerShell tool calls also fire the hooks (all verified real in the Claude Code tools reference). See What-to-do 9 and Acceptance 10. -- I Timothy approve this.

Everything below this section is the framework design Tim ordered and previously approved, unchanged except where a decision above modifies it.

## The standard / what we're building

The guard watches `.claude/settings.json` and `.agentguard/config.json` after every tool call and restores its own parts if they changed without authorization, leaving all other content untouched.

The existing Pre-block is unchanged and stays: a direct write whose target names one of these files is still denied before it runs. This build is purely **additive** — it adds the after-the-call check, which catches the change Pre never saw (a shell command that writes the file without naming it) and undoes only the guard's own region.

Each protected config file declares a **region map**: a list of regions, each with a **mode**. A region not declared is unprotected.

- **Mode 1 — Canonical.** The guard computes the correct value. The check is "is the region what it should be?", not "did it change?". Restore rewrites the canonical value. The guard's own edits (`guard init`, `guard doctor --fix`) pass by construction because their output *is* the canonical value — no snapshot, no per-call context, no command trust needed.
- **Mode 2 — Backup-protected.** Security-critical but user-set, so no canonical value exists. The check is "did the region change from the pre-call backup without a covering grant?". Restore reverts the region to the pre-call backup. Only a signed grant authorizes a change.
- **Mode 3 — Unprotected.** Everything not declared. Never checked, never touched.

Region maps:
- `settings.json` = mode 1: the guard hook groups (identified by their command — a call to the guard binary with `hook pre`/`post`; see decision 2). Everything else is mode 3.
- `config.json` = mode 1: the file's structure/defaults (a valid object with a `protectedPaths` array; `guard init`/`doctor` regenerate this when missing). Mode 2: the **contents** of `protectedPaths`. Everything else is mode 3.

Both restores are **partial**: rewrite or revert only the failing region; preserve every other byte of the file.

The read/write layer is a **general seam** across formats. A **region** is `{ id, mode, locator, canonical? }`: `id` a stable name; `mode` 1/2/3; `locator` an address that **only the per-format adapter interprets** (the shared verifier/diff/restore treat it as opaque); `canonical` a value provider for mode-1 regions only (the guard's "what it should be" logic — per-region, never per-format). The locator means whatever the format needs — a JSON key path, an XPath (`.csproj`), a TOML key, a `.sln` section, a JSONL record selector — and the adapter also **normalizes** the value it reads so key-order/whitespace differences are not changes. This build ships the **JSON** adapter only; XML / TOML / YAML / INI / `.sln` each add one adapter later (see #1 for `.js`/`.ts`), with the verifier/diff/restore reused unchanged. Supporting another language (C#, TypeScript, Python, Rust) is then region-map **data**, not code — until it brings a format with no adapter yet, which is one new adapter.

## Success definition

All acceptance checks pass; `dotnet build` = 0 warnings / 0 errors; `dotnet test` = 0 failed (the existing 76 engine + 45 analyzer tests plus the new config-protection tests); analyzers clean. AND this end state holds: `.claude/settings.json`'s guard hooks and `.agentguard/config.json`'s `protectedPaths` are checked after every tool call and restored per the three modes — mode 1 rewrites to the guard's canonical value (so `doctor --fix` and `init` survive), mode 2 reverts to the pre-call backup unless a grant covers it, mode 3 is never touched; the Pre-block is unchanged; the region/adapter seam is general with only the JSON adapter built; none of the reuse-ledger owners are duplicated; nothing under `Abstractions/` or `analyzers/` changed. Any restatement or weakening of this to fit the result is a top-line Lie-catcher finding.

## Surfaces

Check every claim against all of these:
1. `.claude/settings.json` — the guard hook region and all non-guard content.
2. `.agentguard/config.json` — its structure/defaults, the `protectedPaths` contents, and any other user keys.
3. The pre-call snapshot store — the mode-2 backup source.
4. The region-map registry — which files, regions, and modes are watched (new).
5. The grant / coverage store — mode-2 authorization.
6. The after-check membership — the whole-file scanner set plus the region-map registry.

## Reuse ledger (from the prior-art-ledger run, 2026-07-31)

| Capability | Ruling | Owner to reuse |
|---|---|---|
| Read config text (absent/present/unreadable) | REUSE | `SafeRead.TryReadText`; `ClaudeSettings.Read` for settings |
| Parse + re-serialize JSON preserving the rest | REUSE | `ClaudeSettingsWiring` (`JsonNode.Parse` tree → `ToJsonString(IndentedOptions)`) |
| Rewrite only the guard's part, keep user content | REUSE | `ClaudeSettingsWiring.AddGuardEntries` / `RewriteGuardGroups` |
| Canonical "is the settings region correct?" check | REUSE | `ClaudeSettingsWiring.Inspect` (Ok/Missing/Stale/Malformed) |
| Canonical config default | REUSE | `CreationHelper.EnsureConfig` (`ProjectConfig.Default()`) |
| Produce a restore effect carrying corrected bytes | REUSE | `RestoreFileEffect`; built by `FileGuard.BuildEffect`, written by `PrivilegedWriter` |
| Snapshot a file before the call (mode-2 backup) | REUSE | `FileGuard.CaptureAsync` → `WriteSnapshotAsync` |
| A change is covered by a signed grant (mode-2 auth) | REUSE | `CoverageChecker.FindAuthorizing` / `GrantStore` |
| **Region-scoped diff** (region vs backup, ignoring other content) | NEW | — |
| **Region-aware verifier** (per-region mode-1 canonical / mode-2 backup+grant) | NEW | — |
| **Region-map declaration** per protected config file | NEW | — |

Any capability marked NEW must be re-checked by the DRY adversary against every lens before it is accepted as new.

## What to do

1. **Region-map declaration.** Each protected config file declares its regions, each a `{ id, mode, locator, canonical? }`. `settings.json`: region `guard-hooks` (mode 1; locator = the guard hook groups; canonical = `ClaudeSettingsWiring`'s computed hooks). `config.json`: region `structure` (mode 1; canonical = `ProjectConfig.Default()` shape) and `protected-paths` (mode 2; locator = `$.protectedPaths`). Anything not declared is mode 3.
2. **Per-format region adapter.** A format adapter does exactly two things, addressing a region **by its opaque `locator`**: **read the region's value** (normalized, so key-order/whitespace are not changes), and **rewrite that one region to a new value while preserving every other byte**. Ship one **JSON** adapter now — the settings-hooks case reuses `ClaudeSettingsWiring`; the config case reads/writes the `protectedPaths` region. The region map, the region-aware verifier, the region diff, and the restore are all **format-blind** — they only call the adapter — so adding a format (`.csproj` XML, `.sln`, TOML, YAML) is a new adapter with the verify/diff/restore reused unchanged. No verify/diff/restore code is duplicated per format.
3. **Region-aware verifier** (a new `IVerifier`). For each declared region: mode 1 → compare the current region to the guard's canonical value (reuse `Inspect` for settings, `ProjectConfig.Default()` shape for config structure); mode 2 → compare the current region to the pre-call backup and require a covering grant. Deny only when a protected region is wrong/unauthorized; allow all mode-3 content.
4. **Restore.** Mode 1 → rewrite the canonical region (partial). Mode 2 → revert the region to the pre-call backup (partial). If the file is unparseable/deleted so no region can be read, fall back to restoring the whole pre-call snapshot. All restores carried by `RestoreFileEffect`.
5. **After-check membership = two sources unioned.** The after-check watches the existing whole-file set (the Provider/Project build-config files — any change reverts, no region map, `NoChangeVerifier` as today) **plus** the files in a new **region-map registry** (`settings.json`, `config.json` — region-aware verifier). The scanner already emits the first set from rule origin; it also consults the region-map registry (a new #2 construct, populated by setup) for the second. Each file runs the verifier its source assigns: whole-file for the origin set, region-aware for the registry set. No change to `RuleOrigin` or the frozen `Rule`, and no hardcoded filenames — a file joins the after-check by being registered with a region map.
6. **Guard-own-edit authorization is implicit.** No new `ApprovalGate` or command-trust *authorization* path: mode-1 canonical checking makes `init`/`doctor --fix` pass because their output is canonical; mode-2 `protectedPaths` is grant-gated and the guard's own commands never write it. Identifying the guard's own hook groups by their command (decisions 2–3, item 10) is identification, not authorization. **This replaces the "change ledger" subsystem from the retired install/init/doctor contract — no Sealed-store ledger is needed; the canonical check is the mechanism.**
7. **Region check before the fingerprint gate.** Reverting `protectedPaths` also changes the ruleset, so a `protectedPaths` change moves the ruleset fingerprint between the pre-hook and post-hook processes. `FileGuard.PostcheckAsync` currently returns Deny with no revert whenever that fingerprint changed. For a region-map registry file, run the region check and restore independent of, and ahead of, the fingerprint gate, so a `protectedPaths` change is reverted rather than denied-without-revert. The fingerprint gate stays in place for the whole-file diff path.
8. **Add `config.json` to the pre Bash-scan.** Add `CoreSystemPaths.ProjectConfigRelative` (`.agentguard/config.json`) to `CoreSystemPaths.BashReferenceTokens`, so a Bash command that names it is denied at Pre like the snapshot store, grant store, grant public key, and `settings.json`.
9. **Broaden the hook matcher.** In `ClaudeSettingsWiring`, the guard's Pre and Post groups fire on `Edit|Write|MultiEdit|NotebookEdit|Bash|Monitor|PowerShell|mcp__.*`. `Monitor` and `PowerShell` are real command-running tools, and MCP tool calls fire the hooks but their `mcp__…` names were unmatched; sub-agents already fire the parent hooks, so no change is needed there.
10. **Identify the guard's hook groups by command; remove the marker.** Change `IsGuardGroup` from matching `HookCommand.OwnedFlag` to matching the guard's own command (the guard binary invoked with `hook pre`/`post`). Remove `HookCommand.OwnedFlag` and its emission in `HookCommand.ForEvent`. Inspect, rewrite, and strip continue to find the guard's groups by that command.

## What the agent MAY do

- Reuse and extend the owners named in the ledger.
- Add the general seam interface plus one JSON implementation.

## What the agent MUST NOT do

- Build a new config reader/writer/parser that duplicates `ClaudeSettingsWiring`, `SafeRead`, `ClaudeSettings`, `RestoreFileEffect`, the snapshot store, or the grant/coverage machinery.
- Implement XML/TOML/YAML/INI in this build.
- Add an `ApprovalGate` or command-trust *authorization* path (mode-1 canonical replaces it). Identifying the guard's own hook groups by their command (decision 2) is identification, not authorization, and is required by the marker removal.
- Weaken, skip, or delete any existing test. No commit. Stop and escalate at any wall.
- Touch the frozen `Abstractions/**` or `analyzers/**` without a separate contract.

## Acceptance

Each an xUnit acceptance test (naming: `Acceptance_ConfigProtection_*`), Pre then mutate then Post, asserting the Verdict and the exact bytes.

1. A shell command drifts `settings.json`'s guard hooks (Pre saw a command that did not name the file) → reverted to canonical hooks at Post; user content preserved. (A direct `Edit` to `settings.json` stays Pre-blocked — existing behavior, not this build.)
2. AI runs `guard doctor --fix` inside a tool call, changing the guard hooks legitimately → **not** reverted (canonical check passes); user content preserved.
3. AI empties `config.json` `protectedPaths` (mode 2) with no grant → reverted to the pre-call backup; any mode-3 keys preserved.
4. A change to `protectedPaths` covered by a valid grant → allowed, not reverted.
5. A shell command adds an unrelated key to `config.json` (mode 3) → allowed at Post, untouched.
6. AI deletes `settings.json` entirely → restored (canonical hooks re-created / whole-file fallback).
7. AI corrupts `config.json` to non-JSON → restored from the whole-file pre-call snapshot fallback.
8. `guard init` creating `config.json` with default `protectedPaths` inside a tool call → not reverted.
9. Emptying `config.json` `protectedPaths` when the pre-image snapshot and the post state carry **different ruleset fingerprints** (the two-process case, where the post-hook computes the fingerprint from the already-changed config) → the region is still reverted to the pre-call backup, NOT denied-without-revert. This is the case that fails today; it must pass.
10. A tool whose name matches `mcp__…` writes a protected file → the Post hook fires and the change is reverted (the broadened matcher sees it).
11. A Bash command that names `.agentguard/config.json` → denied at Pre (config.json is in the Bash-scan token list).
12. An extra hook appended inside the guard's own group → removed when the group is re-asserted to canonical; hooks the user added elsewhere are preserved. (This is the case the old health-label check missed.)

## Tier

FULL — a mutating, security-critical change (this is the core defense against the guard being silently disabled); all six roles.

## Scope

Change scope only by editing this file before the run starts.
