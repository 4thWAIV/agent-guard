# Implement the install / init / doctor / remove process, the OS-presence approval gate, and the change ledger

## The standard / what we're building

Non-negotiable: the contract interfaces under `src/AgentGuard.Engine/Abstractions/` and the analyzers under
`analyzers/` are frozen. Implement against them; do not change them. Every failure path fails closed.

End state: a person installs the guard on a machine, wires it into a repo, verifies/repairs/removes that wiring,
and every command that changes protected state is gated by a real OS presence check the agent cannot pass — on
one self-installing binary plus a small signed presence helper:

- `guard install` — machine step. The running binary copies its own published files into
  `~/.agentguard/versions/<version>/`, atomically points `~/.agentguard/current` at that version, ensures
  `~/.agentguard/bin/guard` resolves and `bin` is on PATH, and records the installed version + a binary hash in
  `~/.agentguard/state.json`. Run by path first time and for updates. **Refuses a downgrade** (older version than
  installed) unless `--allow-downgrade`.
- `guard init` — project step. Creates `.agentguard/`, wires the Claude Code hooks into `.claude/settings.json`,
  updates `.gitignore`, stamps the project version. Idempotent; always merges; safe to re-run.
- `guard remove` — project teardown. Removes the guard's wiring/config for a project, so turning the guard off
  is a deliberate command, not drift.
- `guard doctor [--fix]` — verifies every invariant, reports each healthy / broken / cannot-verify, mutates
  nothing by default; `--fix` repairs structural breakage.
- `guard dev-cert` — creates a self-signed code-signing identity in the login keychain for local development and
  signs the local presence helper; keeps all key material out of source control.

`install`, `init`, and `remove` route through one centralized approval gate that forces an OS presence check.
`doctor`/`doctor --fix` are agent-runnable (self-heal), made safe by the change ledger.

## Threat honesty (read first — this bounds the contract)

- The agent runs as the user, so it can write user-owned files and invoke commands. It **cannot present the
  user's biometric/passcode to the OS.** So the real gate on protected commands is an OS presence check, not a
  command-string block.
- `~/.agentguard/` sits outside every project root; the File Guard (which walks only under
  `CallEnvironment.ProjectRoot`) cannot cover it. The binary-integrity check is therefore **warn-only** (decision
  1a): the guard reports if its own resolved image is agent-writable or its hash record mismatches, but on a
  same-user machine it cannot fully defend the binary. Real closure (signed binary, signed records) is the crypto
  contract; do not claim the hash check defends against a same-user agent.
- The change ledger is safe on a same-user machine because the ledger lives in the Sealed store (agent tool-calls
  are denied there) and the guard only ever records the correct content it computes — never a disabled state.
  The residual (agent swaps the guard binary itself) is the 1a residual; crypto signs ledger entries later.

## Success definition

All acceptance checks met; build, tests, analyzers green; and this end state holds: a clean machine goes from
`./guard install` to a resolvable, integrity-recorded `guard`; a repo goes from `guard init` to live Claude Code
hooks calling the guard by absolute path without altering existing settings' meaning; `guard remove` cleanly
un-wires; `guard doctor` gives a trustworthy per-invariant verdict incl. that protection is enabled, and refuses
"healthy" when it cannot verify; `install`/`init`/`remove` each force an OS presence check that an unattended
agent cannot pass; `doctor --fix` self-heals — including protected wiring — without the guard reverting its own
repair, via the ledger; every command is idempotent and atomic; and the same-user residual is documented, not
hidden. Any restatement or weakening to fit the result is a top-line Lie-catcher finding.

## Surfaces

Check every claim against all of these: (1) `~/.agentguard/` machine layout; (2) project `.agentguard/`
(`config.json` incl. enabled providers, `state.json`, `grants/`, key path); (3) `.claude/settings.json` guard
entries and its non-guard content; (4) `.gitignore`; (5) binary integrity (resolved-image writability + hash);
(6) the approval gate (presence check reached before any protected mutation); (7) the change ledger (Sealed,
guard-only writer, content-bound, single-use); (8) the shared layout primitive + invariant set.

## What to do

Adopt **System.CommandLine 2.0** for the whole surface (`hook`, `install`, `init`, `remove`, `doctor`,
`dev-cert`); migrate existing `guard hook pre|post` onto it unchanged (its tests still pass). Setup logic lives
in the Engine (or an `AgentGuard.Setup` namespace); thin CLI handlers invoke it.

1. **Layout primitive + invariant set (DRY seams).** (a) A layout primitive owning path derivation, atomic
   symlink flip, publish/copy, and the settings/gitignore merges — the imperative creation logic install/init/
   remove and doctor's repairs all call. (b) An invariant set, each `{ Name, Scope: Machine|Project,
   Detect(ctx) -> Ok | Broken(reason) | CannotVerify(reason), Repair(ctx) }`. install/init apply theirs (Repair
   uses the primitive), doctor runs every Detect, doctor `--fix` runs Repair on Broken ones.
2. **`guard install`.** Resolve via `Environment.ProcessPath`; if `versions/<version>` already holds a
   byte-identical binary, skip the copy; else copy the publish output into a temp dir under `versions/` and
   `rename` into place — never move, never overwrite the running image. Atomic `current` flip (temp symlink +
   `rename`). Ensure `bin/guard`; idempotent PATH-profile append (detect shell, no dup line). Write `state.json`
   (version + SHA-256). Refuse a downgrade unless `--allow-downgrade`. Updating = run the newer build by path.
3. **Integrity self-check (warn-only, 1a).** On every run the guard reports (via the host channel) if its
   resolved image is user-writable or its hash mismatches `state.json`; a missing/unreadable record or
   unresolvable `current` denies (fail closed). `install` is exempt at bootstrap; that is why `install` is
   presence-gated.
4. **Centralized approval gate.** One `RequireApproval(action)` that `install`, `init`, and `remove` call before
   any mutation; on failure/denial the command refuses and changes nothing. Its implementation raises an **OS
   presence prompt** behind a host-presence seam. macOS implementation now: a small **`net9.0-macos` presence
   helper** using the first-party `LocalAuthentication` binding (`LAContext.EvaluatePolicyAsync`,
   `DeviceOwnerAuthentication`), embedded Info.plist, signed by `guard dev-cert` locally; the `net9.0` `guard`
   binary invokes it and reads the result. Windows implementation is **spec-only** here: a `net9.0-windows`
   helper using `UserConsentVerifier.RequestVerificationForWindowAsync` via the `UserConsentVerifierInterop`
   HWND shim — put it behind the same seam, build it when Windows is a live target. Defense-in-depth: the
   PreToolUse hook also denies an agent-issued `install`/`init`/`remove` so the agent can't even trigger the
   prompt; the OS check is the real wall.
5. **`guard init`.** Presence-gated. Refuse if the machine isn't installed. Create `.agentguard/` (`config.json`:
   enabled providers default C#, project protected-paths default empty; empty `grants/`; project `state.json`
   with wired version; place the committed public key if present, never generate one). Wire
   `.claude/settings.json`: add `PreToolUse`/`PostToolUse` entries `{ "matcher": <tools>, "hooks": [ { "type":
   "command", "command": "<abs>/bin/guard hook pre|post --host claude-code" } ] }`, file-edit matcher from the
   adapter's edit-tool set (`Edit|Write|MultiEdit|NotebookEdit`) plus a `Bash` matcher, absolute path, stable
   guard marker. If the file exists, MERGE preserving every non-guard key with identical values and no semantic
   change (not necessarily byte-identical formatting); add guard entries if absent; update a stale guard entry;
   refuse on a genuine conflict. `.gitignore` += `.protected-snapshots/`, `.agentguard/grants/`, idempotent,
   never ignore `config.json`/key. Atomic writes; only under the project root. Every protected-file write it
   makes is recorded in the ledger (item 8).
6. **Protect `.agentguard/config.json`.** Add it to the built-in System rules (permitted File-Guard change #1),
   so the agent can't hollow the provider set through a hooked call.
7. **`guard doctor`.** Read-only default: run every Detect; print healthy / broken-with-reason / cannot-verify;
   mutate nothing; non-zero exit if any broken. Invariants MUST include **protection-enabled** (a present-but-
   empty provider set is Broken, never healthy), not just structural presence. `--fix` runs Repair via the
   layout primitive; it is agent-runnable, and its protected-file repairs are recorded in the ledger so the guard
   does not revert them. A missing/corrupt key or a hash mismatch is reported ("re-run `guard install` from a
   trusted build"), never fabricated. `doctor`/`doctor --fix` are NOT approval-gated.
8. **The change ledger (permitted File-Guard change #2).** A first-class authorized-change subsystem in the
   Sealed store, not a flag in the revert path. Record: `{ target canonical path, expected new-content SHA-256,
   issuing command, timestamp, single-use }`. Before the guard writes a protected file (init/doctor --fix), it
   writes a ledger entry for the intended content. The File Guard's Post authorization gains the ledger as a
   second source alongside grants: a protected-file change whose current content matches an unconsumed entry for
   that path is authorized (consume it, no revert); otherwise the existing Coverage→Conformance→revert path runs
   unchanged. Only the guard binary writes entries (Sealed store denies agent tool-calls); entries are
   content-bound and single-use.
9. **`guard remove`.** Presence-gated, agent-Pre-denied. Un-wire `.claude/settings.json` (remove only the guard
   entries, preserve the rest), remove `.agentguard/` config/wiring as specified, leave the repo clean. Its
   protected-file writes are ledger-recorded.
10. **`guard dev-cert`.** Create a self-signed code-signing identity in the login keychain; sign the local
    presence helper; ensure no key/cert material is written into the repo (any exported artifact is gitignored).
    Local dev only; document that beta/GA use a real Developer ID cert + notarization (a later distribution step,
    not this contract).
11. **Golden-build DRY guardrails.** Add `Philips.CodeAnalysis.DuplicateCodeAnalyzer` at a tuned threshold;
    confirm SonarAnalyzer `S1192`/`S4144` active as errors; the new code passes them.

## What the agent MAY do

- Create the CLI handlers; the layout primitive + invariant library; the approval-gate + host-presence seam +
  the `net9.0-macos` presence helper; the change-ledger subsystem; the two permitted File-Guard changes
  (`config.json` rule, Post ledger-authorization); `guard dev-cert`; and tests.
- Add `System.CommandLine`, `Philips.CodeAnalysis.DuplicateCodeAnalyzer`, and the `net9.0-macos` target for the
  helper (central versions).
- Use throwaway `HOME`/project fixtures; drive at least one settings.json repair through the live hook pipeline;
  test the approval gate and the ledger against a fake presence provider / fake clock.

## What the agent MUST NOT do

- Change anything under `Abstractions/` or `analyzers/`. The only permitted Engine changes to protection are the
  two named: the `config.json` System rule and the Post ledger-authorization source.
- Weaken/skip/xfail/delete any test; disable/downgrade/`NoWarn` any analyzer. Only permitted suppression: one
  commented `[SuppressMessage]` of `CA1031` per fail-closed boundary.
- Claim the integrity check or the ledger defends against a same-user agent, or hide the Threat-honesty residual.
- Let a protected command mutate anything before the approval gate returns success.
- Fail open: unverifiable install, unresolvable `current`, hash mismatch, unreadable record, indeterminate
  invariant, or a failed presence check must deny / report cannot-verify.
- Move (not copy) the running binary; overwrite the running image; non-atomic `current` swap; downgrade without
  the flag; alter the meaning of non-guard `settings.json` content.
- Write outside allowed roots: `install`/`doctor --fix`/`dev-cert` only under `~/.agentguard` + the login
  keychain + the PATH line; `init`/`remove` only under the project root.
- Write a ledger entry from anywhere but the guard binary's own process; authorize non-guard content via the
  ledger; make ledger entries reusable (they are single-use, content-bound).
- Generate/read/place a private grant key; mint/verify grants; implement `reset`; implement Codex wiring; build
  the Windows helper (spec only); build the beta-distribution / real-cert-signing path.
- Leave a `TODO`/`PENDING`/`NotImplementedException` in any command path; commit; write outside the repo except a
  test's throwaway HOME.
- Stop and escalate at any wall rather than deviate silently.

## Acceptance

Re-runnable; paste output + exit code. An adversary re-runs each.

1. `dotnet build AgentGuard.sln` = `0 Warning(s)` / `0 Error(s)`, DRY analyzers active, `net9.0-macos` helper
   builds.
2. `dotnet test AgentGuard.sln` = 0 failed; existing engine + 45 analyzer tests pass; `hook pre|post` unchanged.
3. Behavioral tests, each against throwaway HOME/repo with a fake presence provider:
   a. `install` clean HOME builds layout, points `current`, resolves `bin/guard`, `state.json` hash matches.
   b. newer `install` flips `current` without breaking the prior; same-version reinstall no-op / no self-
      overwrite; an older-version `install` is refused, and `--allow-downgrade` permits it.
   c. guard denies on hash mismatch, missing/unreadable record, unresolvable `current`.
   d. `install`/`init`/`remove` call `RequireApproval` and abort with no mutation when the presence check fails;
      succeed when it passes.
   e. an agent-issued (hooked) `install`/`init`/`remove` is denied at Pre; `doctor`/`doctor --fix` are allowed.
   f. `init` refuses when machine not installed; with no settings.json creates absolute-path hooks; over an
      existing settings.json (permissions/sandbox/unrelated hooks) preserves every non-guard value and adds guard
      entries; re-run adds no dupes; a genuine conflict is refused.
   g. `init` gitignore idempotent, never ignores config.json/key.
   h. PATH-profile write shell-correct, idempotent, no duplicate line.
   i. `doctor` healthy setup → all-healthy, mutates nothing; mis-wired hook, version drift, hash mismatch,
      unreadable record (→ cannot-verify), empty provider set (→ broken) each reported correctly, non-zero exit.
   j. `doctor --fix` repairs a stale hook in `.claude/settings.json` **through the live hook path** and the
      guard does NOT revert it (ledger entry consumed); repairs gitignore + missing config; a hash mismatch is
      reported, not regenerated.
   k. ledger: a guard-recorded change to a protected file is accepted at Post; an identical-looking change with
      NO ledger entry is reverted; a consumed entry does not authorize a second change.
   l. `remove` un-wires the guard entries and leaves non-guard settings intact.
   m. `guard dev-cert` produces a keychain identity and signs the helper; no key/cert material appears under the
      repo; git status shows none.
   n. containment: install/doctor --fix/dev-cert touch only `~/.agentguard` + keychain + PATH line; init/remove
      only the repo.
4. `git diff --stat HEAD -- src/AgentGuard.Engine/Abstractions analyzers` is empty.
5. `git status` shows no commit.

## Tier

FULL — new mutating code that installs and activates the guard, forces the OS approval gate, and adds a store
subsystem and two File-Guard authorization changes; a fake pass would ship a broken/insecure installer, a
bypassable gate, or a ledger that blesses tampering.

## Scope

Change scope only by editing this file before the run starts. In scope: `install`/`init`/`remove`/`doctor`/
`dev-cert`, Claude wiring, the approval gate + macOS presence helper (+ Windows spec), the change ledger, the two
permitted File-Guard changes, System.CommandLine, DRY analyzers. Out: Codex wiring, minting/keys/reset, the
Windows helper build, beta distribution + real-cert signing, and the crypto contract's Sealed/signed closure of
the same-user residual.
