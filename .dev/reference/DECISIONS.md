# Agent-governance port — decisions & verified facts (working record)

## Goal & threat model
- Port the 4thWAIV agent-governance system into this repo (branch `dev`) to stop a LAZY AI from cheating the golden build by disabling analysis.
- Threat = lazy cheater, NOT a malicious attacker. Make the easy cheats harder; iterate on where the AI actually goes; do not over-engineer for extreme measures.
- Keep the original core's functionality, but improve structure (one CLI) and drop pieces that do not serve THIS threat. Justify every kept piece against the threat — never "keep it because 4thWAIV had it."

## Settled decisions
- Runtimes: BOTH Claude and Codex (not a choice).
- Keypair: reuse 4thWAIV's. Copy its public key in; point the `op://` reference at `op://4thWAIV_Engineering/ProtectedFilesBypassSigningKey/notesPlain`. No new keypair.
- Engine runtime: **C#/.NET** (LOCKED 2026-07-24, supersedes the earlier `bun`/TypeScript choice). The rationale is the threat model, NOT speed: a C# engine is harder for a lazy AI — which is most fluent in TypeScript — to quietly subvert, and raising the effort and visibility bar to tamper with the guard's own logic is the entire point. Speed was an earlier wrong framing of mine, since the 600-second hook timeout makes startup latency irrelevant either way. Consequence: the engine is a fresh C# implementation, and the 4thWAIV TypeScript engine is a behavioral reference to port from, NOT a tree to copy verbatim.
- One consolidated CLI named `guard`. Write verbs are human-only (on the invocation-block); read verbs are agent-OK.
  - `guard hooks`: `setup`, `verify` (wiring hard-fail only). NO `baseline`.
  - `guard grant`: signed write-grants (renamed from 4thWAIV's `bypass`). `mint`/`extend`/`add`/`revoke` human-only; `list`/`show`/`verify` agent-OK. All seven kept.
  - `guard exempt`: `sign` (human-only; the `mint-current` analog, folds in config-level `severity=none` with line-level `#pragma`), `verify` (agent-OK; the build/CI gate), `show` (agent-OK). `mint-infrastructure-globs` deferred until a blanket-exempt path is needed.
- DROP the committed baseline (`protected-baseline.json`, `hooks-baseline`). It is redundant for our threat; the runtime drift-revert has the teeth.
- DON'T add the Codex `config.toml` verify check. `.codex/config.toml` is already write-blocked (`HARDCODED_PROTECTED_GLOBS`), so the AI cannot flip the `codex_hooks` master switch.
- .NET enforcement = "analysis enforces analysis", two build-time pieces:
  - A Roslyn analyzer (reuses our `PRO0001` analyzer infra) for the in-code / editorconfig cheats: `#pragma warning disable`, `[SuppressMessage]`, a protected rule at `severity = none`.
  - An MSBuild structural scanner (the `verify-config` analog) for the `.sln`/`.csproj`/`.props` cheats: flipped toggle, deleted analyzer ref, dropped project, `NoWarn`.
- File guard split:
  - WRITE-BLOCK (rarely edited): `Directory.Build.props`, `**/*.globalconfig`, `.editorconfig`, `stylecop.json` (plus the engine).
  - SCANNER-GUARD, not write-blocked (routinely edited): `**/*.csproj`, `Directory.Packages.props`, `Proteus.sln`. Build-time scanner only (option B); the write-time value-aware guard is a deferred upgrade.
- The write-block is CONFIG-DRIVEN, and this is OSS-shaped (we ship this tool for others' repos). `protected-paths.json` holds every consuming-project path AND the runtime wiring (`.claude/settings.json`, `.codex/config.toml`, `.codex/hooks.json`); the four C# config files are just this repo's entries; a user extends it for their language by adding globs, never by changing code. The ONLY thing hardcoded in source is the guard's own shipped code — `agent-tools/**` plus the two Layer-A hook files — so the AI cannot disable the guard's own code, and the guard hardcodes nothing about any consuming project. Both Layer-A guards read `protected-paths.json` (no hardcoded `LOCKED_FILES`).
- A `guard grant` scopes each capability by a single file, a glob, or `*`; grants can be narrow or broad.
- `.csproj`/`.sln`/`Directory.Packages.props` are NOT write-blocked (option B). ALL their edits — any tool, including `dotnet remove` and `dotnet sln remove` — are ALLOWED at write-time; the build scanner (later unit) catches a removed analyzer or dropped project. No `dotnet`-command authorization or blocking. Only the WRITE-BLOCKED config (Directory.Build.props, `*.globalconfig`, `.editorconfig`, stylecop.json) needs a `guard grant` to edit.

## Deferred backlog (over-engineering for a lazy AI; revisit on evidence)
- tree-sitter shell normalizer for the invocation-block. The raw-regex version is kept as-is; a lazy AI won't wrap commands in `bash -c`.
- value-aware pre-write guard for `.csproj` (the "A" upgrade to option B).
- gating the `BYPASS_TEST_PRIVATE_KEY_PEM` env hatch to test-only.

## Open decisions — ALL CLOSED
- Must-sign breadth: DECIDED **A** — structural (toggles, analyzer refs, project membership) + `PRO*` + correctness/security (`CA*`/`S*`/`MA*`); style `SA*` stays freely disableable. Starting set; promote/demote families later on evidence.
- CLI review DONE: `guard hooks` (setup/verify), `guard grant` (mint/extend/add/revoke + list/show/verify), `guard exempt` (sign/verify/show). Verb for the mint-current analog = `sign`.
- Engine runtime was reopened and is now CLOSED as **C#/.NET** (see the Settled-decisions entry). Both `contract-OLD-proteus-port.md` and this doc's copy-verbatim assumptions are now doubly stale: they target proteus AND assume a TypeScript copy. The next step is a fresh contract for a STANDALONE OSS tool with a C# engine, a language-agnostic core, the multi-language `protected-paths.json` schema, and a fixture project per language (TypeScript, Python, C#, Rust).

## Verified facts about 4thWAIV (from the code, not the docs)
- Config/build files are WRITE-BLOCKED (protected-files) — NOT "edit freely." Routine edits go through authorized commands (`bun add`) and `scripts-modify` (structure-aware, `scripts` key only).
- Guarded twice, independently: write-time block AND build-time lint gate (`require-signed-exemption` rule + `verify-config`).
- Signing tool `mint-current` (`eslint-plugin-fourth-waiv/cli/verbs/mint-current.ts`): runs lint, filters to protected-rule violations, mints ids, inserts `fw-exempt` tags, signs the batch; `--dry-run` previews. No hand-authoring of exemption JSON. Siblings `mint-config`, `mint-infrastructure-globs`.
- Committed baseline (`protected-baseline.json`): 58-path sha256 manifest. `hooks-verify`'s WIRING check hard-fails and does NOT use the baseline; the CONTENT-hash check is WARN-only (exit 0) and only over `agent-tools/{hooks,lib,scripts}`. Distinct from the runtime drift-revert (`baseline.ts` → gitignored `.protected-snapshots/` byte snapshots keyed by `tool_use_id`).
- `.codex/config.toml` is write-blocked precisely because it is the `codex_hooks` master switch (`protection-config.ts:67-69,87`).
- Roslyn analyzers cannot see MSBuild toggles or refs. The `dotnet` CLI cannot set properties, items, or metadata — only basic package/reference/sln add, without metadata (so it cannot even do our analyzer wiring).

## Corrections already made (do not repeat)
- "edit freely" was WRONG — config is write-blocked.
- "verify fails CI on protected-file drift" was WRONG — the content check is warn-only and engine-source-only.
- "the Codex `config.toml` verify is a real gap" was WRONG — `config.toml` is write-blocked.
- A contract holds ONLY instructions, boundaries, and checks — no rationale. The Phase-1 core-port contract is at `./contract.md`.
