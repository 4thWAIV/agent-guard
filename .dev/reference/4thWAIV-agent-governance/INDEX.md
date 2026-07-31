# Agent-Governance Build Guide — INDEX

## What this system is

The agent-governance system is a TypeScript/JS sidecar that hooks an AI coding runtime (Claude Code and Codex CLI) and enforces a single invariant: **the AI can never authorize its own exception — only the human holding the Ed25519 private key can.** Every protected write, blocked command, and lint-disable is gated by that invariant. The agent can propose, but it cannot forge a signed token, mint an exemption, or shrink the in-source protected core. Unauthorized writes to protected paths are reverted byte-for-byte to their pre-call content. This guide tells a fresh AI agent how to rebuild that system in another repo.

## How to use this guide

1. Load **this INDEX only**.
2. Find your task in the router table below.
3. Open **only** the one file that row points to — pull it into context, build that piece, then come back here for the next.

Do not bulk-load every file. Each topic file is a self-contained forward-construction spec. The router is your map; the build order at the bottom is your sequence.

## Router

| If you need to… (problem / task) | Read this file | What you'll get |
|---|---|---|
| Understand **why** the system exists — the one invariant and its five guarantees, before any code | `threat-model.md` | The governing mental model: no forgery, no self-minting, config can't shrink the in-source core, token+snapshot stores are un-bypassable Tier 1, unauthorized writes reverted — plus the five non-negotiables any port must preserve |
| See **how it all fits together** — config, policy module, hooks, dispatcher, signing seam, audit log, two runtimes | `architecture.md` | The whole system skeleton: the two runtime configs that spawn the dispatcher, its parse/discover/run/aggregate loop and three-value HookOutput contract, the shared policy module, the two-pass snapshot→revert data flow |
| Build the **engine core** — `types.ts` (the hook + token contract) and `dispatcher.ts` (the single hook entry point every runtime spawns) | `dispatcher.md` | The forward-construction spec for both files: the `HookInput`/`HookContext`/`HookOutput`/`Hook`/`HostPayload`/`Token`/`BypassCapability` contracts, the `argv[2]→VALID_EVENT_TYPES` map, `readHostPayload()` (stdin-to-EOF, empty→exit 0, unparseable→exit 2), the exact snake_case→camelCase normalization, `discoverHookFiles()`, the deny/warn aggregation rule, and default-deny-on-failure. **Build this before any hook — the hooks only run because the dispatcher discovers and invokes them.** |
| Build the **shared protected-path policy** every hook imports | `policy-module.md` | `protection-config.ts` + `paths.ts` + `workspace-discovery.ts`: the two glob tiers, `effectiveProtectedGlobs` union, cached config loader/parser, the three matchers (`isProtectedPath`, `argsCoversTarget`, `commandStartsWith`), `DRIFT_RELEVANT_TOOLS`, and the shell-aware write-target extractor |
| Build the **PreToolUse block hook** — decide to block a write before it happens | `protected-files-block.md` | A hook returning allow/deny/allow_with_warning: file-edit vs Bash target extraction, Tier-1-before-bypass ordering, default-deny on tokenizer failure, and pre-call base64 content snapshot capture under `.protected-snapshots/` |
| Build the **PostToolUse drift safety net** — revert unauthorized changes after a write | `drift-detect-and-revert.md` | A hook that reads the pre-call snapshot, diffs the protected tree by `tool_use_id`, and reverts changed/removed files to their pre-call bytes and deletes created files — plus the `baseline.ts` snapshot-store helpers and test matrix |
| Understand **why a write was blocked or reverted** | `protected-files-block.md` (blocked) / `drift-detect-and-revert.md` (reverted) | The exact decision paths: what makes a target protected, when Tier-1 wins over a bypass, and how post-call drift is detected and undone |
| Build the **signed bypass-token subsystem** — let a human authorize an exception | `signing-and-bypass.md` | Keypair one-off, token JSON + canonical-signing scheme, the public-key verify library (`bypassAllows`), capability/guard/args scoping, TTL, JSONL audit log, and the human-only `op read` mint/extend/add/revoke/list/show/verify CLI with its exit-code contract |
| Build the **Bash-command block guard** — block invocations by regex | `invocation-block.md` | A PreToolUse hook matching commands against `invocation-blocked.json`, denying (exit 2) unless a signed token scoped to `invocation_block:bash` covers it, and treating the authorized-prefix list as irrelevant to blocking |
| Build the **lint-disable enforcement + signed-exemption gate** | `lint-gate.md` | A plain-JS ESLint rule rejecting unsigned disable directives (missing-record/anchor, file-mismatch, over-count), the shared Ed25519 sign/verify crypto, the exemption record + tuple schema, the mint CLI + verify-config, and the turbo build/lint coupling validator |
| **Wire the engine into both runtimes** (Claude Code + Codex CLI) | `hook-wiring.md` | All five wiring artifacts (`.claude/settings.json`, `.claude/hooks/protect-locked-files.js`, `.codex/hooks.json`, `.codex/config.toml`, `.codex/hooks/protect-locked-files.py`), the dispatcher stdin-JSON/exit-2 contract, and `hooks-setup`/`hooks-verify` |
| **Stand the whole system up in a fresh repo** — exact ordered commands | `bootstrap.md` | The install sequence: generate + commit the Ed25519 public key (private key to a secret store), author the three config JSONs, wire the dispatcher hooks, write the baseline, land a green `hooks:verify` |
| **Guard a C#, Rust, Go, or other non-JS repo** | `guarding-other-languages.md` | Why four guarantees are language-blind (the engine is a JS sidecar hooking the runtime, not a per-language port) and the only per-language work: swap the linter and write a suppression scanner reusing the unchanged TS signer/verifier |
| Know **which files to copy, author, generate, or drop** when porting | `file-manifest.md` | A four-column packing list: `agent-tools/`/eslint-plugin lib files to copy verbatim, config JSONs + `.claude`/`.codex` wiring to hand-author, artifacts (keypair, baseline) to generate, and 4thWAIV-specific rules/docs to delete |

## Recommended build order

Build bottom-up: the engine core (`types.ts` + `dispatcher.ts`) is what every hook runs inside, and the policy module is imported by every hook — both come before the guards. Wiring and bootstrap come last because they assume the pieces exist.

1. **`threat-model.md`** — read first; internalize the invariant and five guarantees so every later choice can be judged against it. (No code.)
2. **`architecture.md`** — read for the full skeleton and data flow before writing anything. (No code.)
3. **`dispatcher.md`** — build `types.ts` + `dispatcher.ts`, the engine core. Build this BEFORE the hooks: a hook only executes because the dispatcher discovers and invokes it, so there is nothing to run the guards in until the engine exists.
4. **`policy-module.md`** — build `protection-config.ts` + `paths.ts` + `workspace-discovery.ts`. Everything downstream imports these.
5. **`signing-and-bypass.md`** — build the keypair, token format, verify library, and mint CLI. The hooks call `bypassAllows`, so the crypto must exist before them.
6. **`protected-files-block.md`** — build the PreToolUse block hook (consumes the policy module + bypass verify; captures pre-call snapshots).
7. **`drift-detect-and-revert.md`** — build the PostToolUse drift hook + `baseline.ts` (consumes the snapshots the block hook writes).
8. **`invocation-block.md`** — build the Bash-command block guard (reuses the policy module + bypass verify).
9. **`lint-gate.md`** — build the ESLint rule + exemption crypto + turbo coupling validator (reuses the shared Ed25519 sign/verify).
10. **`hook-wiring.md`** — author the five runtime wiring artifacts that spawn the engine core in both runtimes.
11. **`bootstrap.md`** — run the ordered install sequence to stand the wired system up in the fresh repo.

Cross-cutting references — read as needed, not in sequence:
- **`file-manifest.md`** — consult alongside any build step to know whether a given file is copied, authored, generated, or dropped.
- **`guarding-other-languages.md`** — read before step 9 (the lint gate) if the target repo is not JS/TS. The four path-and-command guarantees need no per-language work; only the lint gate does, and even it keeps the crypto core in one place. Includes the build-to-lint coupling analog every non-turbo build system needs.
