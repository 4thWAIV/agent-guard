# Implement the installer — install / init / remove / doctor on one self-installing binary

First of three builds (installer, then the Touch ID gate, then config-file protection). It stands alone: it
depends on neither of the other two and changes no File Guard or protection code.

## The standard / what we're building

Non-negotiable: the contract interfaces under `src/AgentGuard.Engine/Abstractions/` and the analyzers under
`analyzers/` are frozen. Implement against them; do not change them. Do not change any File Guard rule, matcher,
scanner, verifier, or precheck/postcheck logic — this build adds the setup surface only. Every failure path
fails closed.

End state: four commands on one self-installing binary, plus the existing `guard hook`:

- `guard install` — the running binary copies its own published file into `~/.agentguard/versions/<version>/`,
  atomically points `~/.agentguard/current` at that version, ensures `~/.agentguard/bin/guard` resolves and
  `bin` is on PATH, and records the installed version + the binary's hash in `~/.agentguard/state.json`. Run by
  path the first time and for updates. Refuses to install an older version than the recorded one unless
  `--allow-downgrade`.
- `guard init` — in a repo, creates `.agentguard/`, wires the Claude Code hooks into `.claude/settings.json`,
  updates `.gitignore`, stamps the project with the guard version. Idempotent; always merges; safe to re-run.
- `guard remove` — removes the guard's own entries from `.claude/settings.json` and the guard's `.agentguard/`
  wiring, leaving all non-guard content intact.
- `guard doctor [--fix]` — checks every condition the other commands establish, reports each healthy / broken /
  cannot-verify, changes nothing by default; `--fix` repairs the structural conditions. Agent-runnable.

Publish the CLI as a **self-contained, single-file** binary, so "the binary" is exactly one file to copy, hash,
and replace.

## Success definition

All acceptance checks met; build, tests, analyzers green; and this end state holds: a clean machine goes from
`./guard install` to a resolvable, version-and-hash-recorded `guard`; a repo goes from `guard init` to live
Claude Code hooks that call the guard by its absolute path without altering the meaning of any existing settings;
`guard remove` cleanly un-wires; `guard doctor` gives a trustworthy per-condition verdict and refuses "healthy"
when it cannot verify a condition; every command is idempotent and atomic; and a hook run refuses when it cannot
resolve or verify its own install. Restating or weakening this definition to fit the result is a top-line
Lie-catcher finding.

## Surfaces

Check every claim against all of these: (1) the machine layout under `~/.agentguard/` — `versions/<v>`,
`current`, `bin/guard`, `state.json`; (2) the project `.agentguard/` — `config.json`, `state.json`, `grants/`,
the public-key path; (3) the guard's entries in `.claude/settings.json`, and its non-guard content; (4) the repo
`.gitignore`; (5) the running binary's resolvability and recorded hash; (6) the single set of condition checks
that install/init apply and doctor reports and repairs.

## What to do

Adopt **System.CommandLine 2.0** as the whole surface — `hook`, `install`, `init`, `remove`, `doctor` — and move
the existing `guard hook pre|post` onto it with its stdin/exit-code behavior unchanged (its tests still pass).
Put the setup logic in the Engine (or a new `AgentGuard.Setup` namespace within it), behind thin CLI handlers.

1. **One set of conditions + one creation helper, so install/init and doctor never re-derive the same logic.**
   Model each condition as an object with a **scope** (`Machine` or `Project`) that knows how to detect its state
   — `Ok`, `Broken(reason)`, or `CannotVerify(reason)` — and how to repair it. Put the imperative creation work
   (path math, the atomic symlink swap, the single-file copy/replace, the settings and gitignore merges) in one
   helper that install, init, remove, and doctor's repairs all call. `install`/`init` apply the relevant
   conditions (repair uses the helper).
2. **`guard install`.** Resolve the running binary with `Environment.ProcessPath`. Compute its SHA-256. If
   `versions/<version>/guard` exists with that same hash, skip (clean no-op). Otherwise copy the running file to
   a temp name under `versions/<version>/` and `rename` it over any existing `guard` there (atomic single-file
   replace; never overwrite the *running* image). Flip `current` atomically (write a temp symlink, `rename` over
   the old). Ensure `bin/guard` -> `current/guard`. Ensure `~/.agentguard/bin` is on PATH by appending one line
   to the user's shell profile (for zsh, `~/.zshrc`; detect the shell), idempotently — never a duplicate line.
   Write `state.json` (installed version + the SHA-256). Compare versions with **SemVer precedence including
   prerelease** (e.g. NuGet `NuGetVersion`, so `0.1.0-alpha` parses and `0.10.0 > 0.9.0`); refuse an older
   version unless `--allow-downgrade`. Updating means running the newer build's binary by path.
3. **Integrity self-check (gates HOOK runs only; report-only for writability).** On a `guard hook` run, the guard
   derives its install root and `state.json` from the **resolved binary path** (`Environment.ProcessPath`), never
   from `$HOME` (a hook may run with a different or absent `HOME`). It denies (fail closed) on an unresolvable
   `current`, or a missing/unreadable/mismatched `state.json` hash. It reports (does not block) if its own image
   is user-writable — blocking that is a later build; state this limit plainly and do not claim the hash check
   defends the binary against a same-user agent. The setup commands (`install`/`init`/`remove`/`doctor`) are
   exempt from this gate, so the first `./guard install` on a clean machine is not blocked by the record it is
   about to create.
4. **`guard init`.** Refuse if the machine is not installed (no resolvable `~/.agentguard/bin/guard`), telling the
   user to run `guard install` first. Create `.agentguard/`: `config.json` (enabled providers, default the C#
   provider; project protected-paths, default empty — scaffolding for a later build; nothing reads it yet), an
   empty `grants/`, a project `state.json` with the wired guard version, and place the committed public key if
   present (never generate one). Wire `.claude/settings.json` by adding `PreToolUse` and `PostToolUse` entries of
   the form `{ "matcher": <tools>, "hooks": [ { "type": "command", "command": "<abs>/bin/guard hook pre|post --host claude-code --agentguard-owned" } ] }`,
   the file-edit matcher taken from the Claude adapter's own edit-tool set (`Edit|Write|MultiEdit|NotebookEdit`)
   plus a separate `Bash` matcher, the command using the absolute installed path (never bare `guard`). The
   `--agentguard-owned` sentinel argument is how a guard entry is identified — path-independent, survives JSON
   round-trips, distinct from any user hook that merely invokes some `guard`. If the file exists, merge: preserve
   every non-guard key with identical values and no change in meaning (formatting need not be byte-identical);
   add the guard entries if absent; update a guard-owned entry (matched by the sentinel) whose path is stale;
   refuse with a clear message on a real conflict. Add `.protected-snapshots/` and `.agentguard/grants/` to
   `.gitignore` idempotently; never ignore `config.json` or the public key. All writes atomic (temp + rename);
   write only under the project root.
5. **`guard remove`.** Remove only the guard-owned entries from `.claude/settings.json` — matched by the
   `--agentguard-owned` sentinel — preserving all non-guard content and leaving valid JSON; remove the guard's
   `.agentguard/` wiring; leave the repo otherwise untouched. Idempotent; write only under the project root.
6. **`guard doctor`.** Read-only by default: run every in-scope condition's detect and print healthy /
   broken-with-reason / cannot-verify; change nothing; exit non-zero if any is broken. **Scope selection:** run
   the `Machine` conditions always; run the `Project` conditions only when the current directory is an
   initialized repo (has `.agentguard/`), so `doctor` on an installed machine outside a repo does not report the
   project wiring as broken. Conditions are structural: machine layout, `current`, `bin`, the PATH line, the
   binary hash; and in a repo, the guard's hook entries present and pointing at the absolute installed path, the
   gitignore lines, the version stamp, and that `config.json` exists and parses. Do NOT judge whether providers
   are "enabled" — nothing consumes `config.json` in this build, so that check would measure nothing; it belongs
   to the config-protection build. If a condition cannot be evaluated (an unreadable record) report
   cannot-verify, never healthy. `--fix` repairs the structural conditions through the shared helper; a
   missing/corrupt public key or a hash mismatch is reported ("re-run `guard install` from a trusted build"),
   never fabricated. `doctor` (incl. `--fix`) is agent-runnable and is NOT approval-gated; its writes are not
   undone because Post reverts only Provider/Project files, and `.claude/settings.json`/`config.json` are not in
   that set in this build.
7. **The approval placeholder.** `install`, `init`, and `remove` call one shared `RequireApproval(action)` before
   they mutate anything. In this build its implementation allows through (there is no OS check yet); the real
   Touch ID check is the next build and drops in behind this call with no change to the callers. `doctor` does
   not call it (agent self-heal). State plainly that this build ships those three commands ungated. Do NOT add
   any File Guard block for these commands here.
8. **Golden-build duplicate-code checks.** Add `Philips.CodeAnalysis.DuplicateCodeAnalyzer` at a tuned token
   threshold, and confirm SonarAnalyzer `S1192` and `S4144` are active as errors. The new code must pass them.

## What the agent MAY do

- Create the CLI handlers, the condition set + shared creation helper, and the tests.
- Add `System.CommandLine`, `Philips.CodeAnalysis.DuplicateCodeAnalyzer`, and a SemVer comparison package if
  needed (central versions); set the CLI project to self-contained single-file publish.
- Use throwaway `HOME`/project fixtures so install/init/remove/doctor run against disposable directories.

## What the agent MUST NOT do

- Change anything under `Abstractions/` or `analyzers/`, or any File Guard rule/matcher/scanner/verifier/
  precheck/postcheck. This build adds the setup surface only.
- Implement the OS Touch ID check, the signed macOS helper, `dev-cert`, config-file part-level protection, the
  change ledger, Codex wiring, grant minting, or `reset`. Those are other builds.
- Weaken/skip/xfail/delete any test; disable/downgrade/`NoWarn` any analyzer. Only permitted suppression: one
  commented `[SuppressMessage]` of `CA1031` per fail-closed boundary.
- Claim the integrity check defends the binary against a same-user agent, or claim `install`/`init`/`remove` are
  gated.
- Fail open: an unresolvable `current`, a hash mismatch, an unreadable record, or an indeterminate condition must
  deny or report cannot-verify.
- Move (not copy) the running binary; overwrite the running image; make a non-atomic `current` swap; compare
  versions by string; downgrade without the flag; alter the meaning of non-guard `.claude/settings.json` content.
- Write outside the allowed roots: `install`/`doctor --fix` only under `~/.agentguard` plus the one PATH profile
  line; `init`/`remove` only under the project root.
- Leave a `TODO`/`PENDING`/`NotImplementedException` in any command path; commit; write outside the repo except a
  test's throwaway HOME.
- Stop and escalate at any wall rather than deviate silently.

## Acceptance

Re-runnable; paste output + exit code. An adversary re-runs each.

1. `dotnet build AgentGuard.sln` = `0 Warning(s)` / `0 Error(s)`, duplicate-code checks active; the CLI publishes
   self-contained single-file.
2. `dotnet test AgentGuard.sln` = 0 failed; existing engine + 45 analyzer tests pass; `guard hook pre|post`
   behavior unchanged on the new surface.
3. Behavioral tests, each against a throwaway HOME/repo:
   a. `install` into a clean HOME builds `versions/<v>/guard`, points `current`, resolves `bin/guard`, writes a
      `state.json` whose hash matches the binary.
   b. version handling: a newer `install` flips `current` without breaking the prior version's file; same-version
      reinstall is a no-op that does not overwrite the running image; an older-version install is refused and
      `--allow-downgrade` permits it; the comparison orders `0.10.0 > 0.9.0` and parses `0.1.0-alpha`.
   c. a hook run denies on a hash mismatch, a missing/unreadable record, and an unresolvable `current`; the
      setup commands are not blocked at bootstrap.
   d. `init` refuses when the machine is not installed; with no `.claude/settings.json` creates it with
      absolute-path Pre/Post guard hooks (carrying the sentinel) for the edit and Bash matchers.
   e. `init` over an existing `.claude/settings.json` holding `permissions`/`sandbox`/unrelated hooks preserves
      every non-guard value and adds the guard entries; a second `init` adds no duplicates; a stale-path guard
      entry is updated (matched by the sentinel); a real conflict is refused.
   f. `init` adds the two runtime dirs to `.gitignore` idempotently and never ignores `config.json` or the key.
   g. the PATH-profile write targets the right shell profile, is idempotent, and adds no duplicate line.
   h. `doctor` on a healthy installed machine outside a repo reports all-healthy (project conditions skipped) and
      changes nothing; inside a healthy repo reports all-healthy; a mis-wired hook, a version drift, a hash
      mismatch, and an unreadable record (cannot-verify) are each reported correctly with a non-zero exit.
   i. `doctor --fix` repairs a stale hook path, a missing gitignore line, and a missing `config.json`; a hash
      mismatch is reported, not regenerated.
   j. `remove` deletes the guard entries (by sentinel) from `.claude/settings.json`, leaves non-guard settings
      intact and the file valid JSON.
   k. containment: `install`/`doctor --fix` touch only `~/.agentguard` plus the PATH line; `init`/`remove` only
      the repo.
4. `git diff --stat HEAD -- src/AgentGuard.Engine/Abstractions analyzers` is empty, and the diff shows no change
   to any File Guard rule/scanner/verifier/precheck/postcheck file.
5. `git status` shows no commit.

## Tier

FULL — new mutating code that installs and wires the guard and writes host config; a fake pass would ship a
broken installer, one that clobbers a user's settings, or a `doctor` that reports healthy on a broken install.

## Scope

Change scope only by editing this file before the run starts. In scope: `install`/`init`/`remove`/`doctor`, the
System.CommandLine surface, the shared creation helper + scoped condition set, the duplicate-code checks, and the
ungated approval placeholder. Out: the OS Touch ID check and signed helper (build two), config-file part-level
protection and any File Guard change (build three), plus minting/keys/reset/Codex/dev-cert.
