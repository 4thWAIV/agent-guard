# Wiring the Engine into Both Runtimes

This file tells you how to connect the governance engine to the two agent runtimes it protects — Anthropic **Claude Code** and OpenAI **Codex CLI** — so that the same file-protection, invocation-block, and drift-revert logic fires in both. It covers the runtime config files, the two lightweight standalone guards, the stdin-JSON / exit-2 contract the dispatcher speaks, and the installer/verifier that keeps the wiring correct.

The engine itself is documented elsewhere and must be built first: the dispatcher's parse/normalize/discover/run/aggregate loop and the hook contract are in **`dispatcher.md`**; the protected-path tiers are in `policy-module.md`; bypass tokens in `signing-and-bypass.md`; drift snapshots in `drift-detect-and-revert.md`. This file is only about **wiring**: what config declares what, and where Claude and Codex diverge.

---

## The two layers, per runtime

Each runtime is wired with **two independent PreToolUse layers plus, for the dispatcher, a PostToolUse layer**. They are redundant by design.

**Layer A — a standalone "locked-files" guard.** A tiny self-contained script with a hardcoded short list of infra files. No TypeScript, no dispatcher, no bypass tokens. One per runtime, in the runtime's own language:
- Claude: `.claude/hooks/protect-locked-files.js` (Node)
- Codex: `.codex/hooks/protect-locked-files.py` (Python)

**Layer B — the shared dispatcher.** The real engine. Both runtimes shell out to `bun .../agent-tools/lib/dispatcher.ts <event>`, which fans out to every `*.ts` under `agent-tools/hooks/<event_type>/`. This is the extensible, config-driven, bypass-aware system.

```
                Claude Code                          Codex CLI
                ───────────                          ─────────
 Layer A   .claude/hooks/protect-locked-files.js    .codex/hooks/protect-locked-files.py
           declared in .claude/settings.json        declared in .codex/config.toml
           exit 2 + stderr  → block                 JSON permissionDecision:"deny" on stdout → block

 Layer B   bun "…/dispatcher.ts" <event>            bun agent-tools/lib/dispatcher.ts <event>
           declared in .claude/settings.json        declared in .codex/hooks.json
                       │                                       │
                       └──────────────┬────────────────────────┘
                                      ▼
                        agent-tools/lib/dispatcher.ts   ← ONE engine, both runtimes
                                      │
             discovers *.ts under agent-tools/hooks/<event_type>/
```

Wiring the whole system into a new repo therefore means writing **five artifacts**: `.claude/settings.json`, `.claude/hooks/protect-locked-files.js`, `.codex/hooks.json`, `.codex/config.toml`, and `.codex/hooks/protect-locked-files.py` — and then running the installer to generate/refresh the two JSON configs and the baseline.

---

## The dispatcher contract (both runtimes speak this)

Both runtimes invoke the dispatcher the same way. It is the single decision boundary; everything downstream is engine internals.

**Invocation.** `bun agent-tools/lib/dispatcher.ts <event_type>` where `<event_type>` is `pre_tool_use` or `post_tool_use` (snake case — it is also the subdirectory name under `agent-tools/hooks/`). See `VALID_EVENT_TYPES` in `dispatcher.ts`.

**Input — stdin JSON.** The host CLI writes its tool-event payload to stdin as one JSON object. `readHostPayload()` reads it to EOF, trims, and `JSON.parse`s it. The fields the dispatcher consumes (`HostPayload`):
- `tool_name` — e.g. `"Edit"`, `"Write"`, `"apply_patch"`, `"Bash"`
- `tool_input` — the tool's arguments object (`file_path`, `command`, etc.)
- `tool_use_id` — the per-call id used to key the drift snapshot
- `cwd` — becomes the `projectDir` for path resolution
- `session_id`

Empty stdin → `readHostPayload()` returns `null` → the CLI entry exits **0** (host didn't actually run a tool). Non-object or unparseable JSON → exit **2** with a parse-error line on stderr.

**Output — exit code + stderr.** This is the contract the runtimes act on:

| Aggregate result | Exit | stderr |
|---|---|---|
| all hooks `allow` | **0** | (nothing, or `WARN …` lines) |
| any hook `allow_with_warning` | **0** | `WARN [<file>] <warning>` |
| any hook `deny` | **2** | `BLOCKED by agent-tools <event> hook(s):` + `  • <file>: <reason>` per deny |

Both Claude Code and Codex treat a **non-zero exit (2)** from a PreToolUse hook as *block the tool call* and surface the stderr text to the agent. On PostToolUse, exit 2 reports the deny after the fact — and because the drift hook has already reverted the unauthorized bytes to their pre-call state (see below), the block is backed by an actual undo.

**Default-deny.** A hook module that fails to import, lacks a default export, returns a bad shape, or throws is counted as a `deny` (see `invokeHookWithModule` / `importHookModule`). A broken engine fails closed, not open.

**Discovery.** `discoverHookFiles()` reads `agent-tools/hooks/<event_type>/`, keeps `*.ts` that are not `*.test.ts`/`*.d.ts`, sorts them, imports all in parallel, and runs each through the `Hook` contract. Add a guard by dropping a `.ts` file in that directory — no wiring change. An empty/missing directory → exit 0 (allow).

---

## Claude Code wiring — `.claude/settings.json`

Claude's hook model: a `matcher` is a `|`-joined list of tool names; when a matched tool is about to run (`PreToolUse`) or has just run (`PostToolUse`), Claude runs every `command` in that block with the event payload on stdin. Exit 2 + stderr blocks.

The installed `hooks` block declares, in `PreToolUse`:
1. **Layer A**, matcher `Edit|Write|NotebookEdit`, running `node "${CLAUDE_PROJECT_DIR}/.claude/hooks/protect-locked-files.js"`.
2. **Layer B / file edits**, matcher `Edit|Write|MultiEdit|NotebookEdit|apply_patch`, running `bun "${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts" pre_tool_use`.
3. **Layer B / Bash**, matcher `Bash`, running the same dispatcher `pre_tool_use`.

And in `PostToolUse`, the file-edit and Bash matchers each run the dispatcher `post_tool_use`. (Layer A has no PostToolUse — it is a pre-block only.)

`${CLAUDE_PROJECT_DIR}` is shell-expanded by Claude before invocation, so the command resolves regardless of the parent shell's cwd drift. This placeholder-prefixed form is the **canonical** command; a bare `bun agent-tools/lib/dispatcher.ts <event>` is the **legacy** form still accepted by the verifier.

### The literal `hooks` block

This is the exact nesting the installer produces and the verifier asserts —
`hooks → PreToolUse|PostToolUse → [ { matcher, hooks: [ { type: "command", command } ] } ]`.
`hooks:setup` writes the two Layer-B matcher blocks under each event; you
hand-author the single Layer-A block (the first `PreToolUse` entry) and commit it.
Merge this into your existing `.claude/settings.json` (alongside `permissions`,
`sandbox`, etc. — the installer preserves those):

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Edit|Write|NotebookEdit",
        "hooks": [
          {
            "type": "command",
            "command": "node \"${CLAUDE_PROJECT_DIR}/.claude/hooks/protect-locked-files.js\""
          }
        ]
      },
      {
        "matcher": "Edit|Write|MultiEdit|NotebookEdit|apply_patch",
        "hooks": [
          {
            "type": "command",
            "command": "bun \"${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts\" pre_tool_use"
          }
        ]
      },
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "bun \"${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts\" pre_tool_use"
          }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "Edit|Write|MultiEdit|NotebookEdit|apply_patch",
        "hooks": [
          {
            "type": "command",
            "command": "bun \"${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts\" post_tool_use"
          }
        ]
      },
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "bun \"${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts\" post_tool_use"
          }
        ]
      }
    ]
  }
}
```

The first `PreToolUse` block (matcher `Edit|Write|NotebookEdit`, running
`.claude/hooks/protect-locked-files.js`) is **Layer A** — hand-authored, since
`hooks:setup` preserves but never creates it. The remaining four blocks are
**Layer B**, written by the installer. Note Layer A's matcher is the narrower
`Edit|Write|NotebookEdit` (it fires only on file-edit tools, not `Bash`), while
Layer B's file-edit matcher is the full `Edit|Write|MultiEdit|NotebookEdit|apply_patch`.

---

## Codex CLI wiring — `.codex/hooks.json` + `.codex/config.toml`

Codex splits its wiring across **two** files, which is the first Claude↔Codex divergence.

**`.codex/hooks.json`** declares **Layer B** (the dispatcher) with the same shape as Claude's `hooks` block: `PreToolUse` and `PostToolUse`, each with a file-edit matcher (`Edit|Write|MultiEdit|NotebookEdit|apply_patch`) and a `Bash` matcher, all running `bun agent-tools/lib/dispatcher.ts <event>`. This file is generated by the installer.

**`.codex/config.toml`** declares **Layer A** (the Python locked-files guard) and, critically, flips the feature flag that makes any Codex hooks fire at all:

```toml
[features]
codex_hooks = true   # required for [hooks] to take effect (Codex CLI v0.129+)

[[hooks.PreToolUse]]
matcher = "apply_patch|Edit|Write|NotebookEdit"
[[hooks.PreToolUse.hooks]]
type = "command"
command = "/usr/bin/python3 .codex/hooks/protect-locked-files.py"
timeout = 5
statusMessage = "Checking locked-files policy"
```

Divergences from Claude in this file: the `[features] codex_hooks = true` gate has no Claude equivalent; TOML `[[hooks.PreToolUse]]` array-of-tables syntax instead of JSON; per-hook `timeout` and `statusMessage` fields; and an absolute interpreter path (`/usr/bin/python3`) rather than a `node`/`bun` launcher.

---

## Layer A — the standalone locked-files guards

Both Layer-A guards enforce the same intent (block edits to a small hardcoded list of build-infra files) but their protocols and matching differ, so they must be maintained as siblings.

### Claude — `.claude/hooks/protect-locked-files.js`

Node script. Reads stdin (`readFileSync(0)`), `JSON.parse`s it, and inspects `tool_name`. It acts only on `Edit`/`Write`/`NotebookEdit`; anything else exits 0. It reads `tool_input.file_path` and matches against a hardcoded `LOCKED_FILES` array using three tests: exact equality, `${cwd}/${locked}`, or `filePath.endsWith("/" + locked)`. A hit writes a `BLOCKED: …` message to stderr and **exits 2**. No hit → exit 0. Empty stdin or non-JSON → exit 0 (allow).

Its `LOCKED_FILES` list is the build-coupling infra: `turbo.json`, `scripts/validate-turbo-lint-coupling.js`, `.elves/rules/55-build-infrastructure.md`, `.githooks/pre-commit`, `CODEOWNERS`, `.claude/settings.json`, and `.claude/hooks/protect-locked-files.js` itself.

### Codex — `.codex/hooks/protect-locked-files.py`

Python script. Same intent, four protocol/behavior divergences from the JS version:

1. **Deny protocol.** Instead of exit-2, it prints a structured JSON object to **stdout** and exits 0:
   ```json
   {"hookSpecificOutput":{"hookEventName":"PreToolUse",
     "permissionDecision":"deny","permissionDecisionReason":"<reason>"}}
   ```
   (exit-2 + stderr also works as a fallback, but the JSON form is what it emits.)
2. **Event gate.** It first checks `payload.hook_event_name == "PreToolUse"` and exits 0 otherwise.
3. **Tool set.** It acts on `apply_patch` plus the `Edit`/`Write`/`NotebookEdit` aliases — because Codex normalizes edits to `apply_patch`, whose target paths are embedded inside `tool_input.command` (the multi-line patch string), not in a `file_path` field.
4. **Matching.** It builds a `haystack` of `command + "\n" + file_path` and does a plain substring test (`if locked in haystack`) rather than the JS guard's path-segment equality.

Its `LOCKED_FILES` list is a **superset** of the JS list — it adds the Codex-side files (`.codex/config.toml`, `.codex/hooks/protect-locked-files.py`) plus `AGENTS.md` and `Dockerfile`. When you add a file to one guard's list, add it to the other; the two lists are maintained in parallel and are intentionally not identical.

---

## What the dispatcher's guards do on each event (wiring-relevant summary)

You wire the dispatcher once; these guards are the `.ts` files it discovers. Relevant here only so you know what exit 2 means on each event:

- **PreToolUse** (`agent-tools/hooks/pre_tool_use/`): `protected-files.ts` denies writes to protected paths (unless a bypass token covers them) and, as a side effect, captures the **pre-call content snapshot** for this `tool_use_id`. `invocation-block.ts` blocks disallowed invocation shapes. A deny → exit 2 → the runtime blocks the tool.
- **PostToolUse** (`agent-tools/hooks/post_tool_use/protected-files-drift.ts`): re-hashes protected paths, diffs against the pre-call snapshot for this `tool_use_id`, and on **unauthorized drift reverts every drifted path to its captured pre-call bytes** — rewriting changed/removed files from the base64 snapshot and deleting files the call created (`restoreDrift`) — then denies (exit 2). The hook writes via `fs` directly, so its own restore is not re-intercepted. Authorized commands or a token covering all drifted paths → `allow_with_warning`.

### The drift snapshot store is itself wired as protected

The per-call snapshot lives **in-repo** under `.protected-snapshots/` (not `os.tmpdir()`), storing base64 **content** keyed by `tool_use_id` (see `agent-tools/lib/baseline.ts`). That directory is one of the two **Tier-1 never-bypass** globs in `agent-tools/lib/protection-config.ts`:

```ts
export const TIER1_NEVER_BYPASS_GLOBS = [
  ".elves/bypass-tokens/**",
  ".protected-snapshots/**",
];
```

Tier-1 paths are denied outright even with a valid bypass token — if the AI could write the snapshot store, a poisoned snapshot would make the revert restore attacker bytes. The engine's own files are covered by the Tier-2 `HARDCODED_PROTECTED_GLOBS`, which are **directory globs** (`agent-tools/hooks/**`, `agent-tools/lib/**`, `agent-tools/config/**`, `agent-tools/scripts/**`, `agent-tools/security/**`, plus the signed-exemption eslint files) so no single engine file can slip outside protection.

---

## Installer — `agent-tools/scripts/hooks-setup.ts`

Run via `bun run hooks:setup`. Idempotent. It performs, in order (`runHooksSetup`):

1. Read `agent-tools/config/protected-paths.json` (the dynamic glob list).
2. **Merge** dispatcher entries into `.claude/settings.json` via `mergeClaudeSettings` → `upsertMatcherBlocks`. Existing hook entries (including Layer A) are **preserved** — a matcher block is reused if present and the command is appended only if absent. This is why the installer never clobbers the hand-written Layer-A entry.
3. **Write/replace** `.codex/hooks.json` with the equivalent shape via `buildCodexShape` (full overwrite, not merge).
4. Compute the initial baseline with `hashTree(protectedGlobs)` and write `agent-tools/config/protected-baseline.json`.

Constants that define the wiring contract: `FILE_EDIT_MATCHER = "Edit|Write|MultiEdit|NotebookEdit|apply_patch"`, `BASH_MATCHER = "Bash"`, and the canonical `${CLAUDE_PROJECT_DIR}`-prefixed `CLAUDE_HOOK_COMMAND_PRE/POST`. Re-running when everything is present returns `alreadyInstalled: true` and a no-op note. The setup writer emits only the **canonical** command form; the legacy bare form is accepted at verify time but never written.

Note the installer manages **Layer B in both runtimes** and the baseline. It does **not** write the Layer-A guards or `.codex/config.toml` — those are hand-authored artifacts you commit into the repo.

---

## Verifier — `agent-tools/scripts/hooks-verify.ts`

Run via `bun run hooks:verify`, and wired into `bun run check` (root `package.json`: `"check": "bun run hooks:verify && turbo check && …"`) so CI fails fast if the wiring is gone. Two tiers (`runHooksVerify`):

**Tier 1 — HARD-FAIL, exit 1:**
- `.claude/settings.json` missing, unreadable, malformed, or lacking dispatcher entries for **both** `PreToolUse` and `PostToolUse` across **both** the file-edit and `Bash` matchers (`claudeHasOurHooks`).
- `.codex/hooks.json` missing or lacking the same (`codexHasOurHooks`).
- **`.codex/config.toml` missing, or not containing `[features] codex_hooks = true`, or missing the Layer-A `[[hooks.PreToolUse]]` registration, or the Layer-A guard scripts (`.claude/hooks/protect-locked-files.js`, `.codex/hooks/protect-locked-files.py`) absent.** `codex_hooks = true` is the master switch that makes **any** Codex hook fire — if an in-session agent (or a bad merge) flips it to `false` or deletes the `[features]` block, Codex enforcement goes fully dark while `hooks.json` still parses. The verifier must parse `config.toml` and require the switch and the Layer-A block, or a green `hooks:verify` is not evidence that Codex protection is live. **Do not delegate this to CODEOWNERS** — CODEOWNERS is PR-review only and does nothing against an in-session `sed -i` / redirect to `config.toml`.
- Accepted command forms: canonical `bun "${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts" <event>` **or** legacy `bun agent-tools/lib/dispatcher.ts <event>`.

**Tier 2 — WARN only, exit 0:** any file under `agent-tools/hooks/**`, `agent-tools/lib/**`, or `agent-tools/scripts/**` whose sha256 differs from `protected-baseline.json` (`diffBaseline` filtered to those three prefixes). Hook self-drift is observability-only at this stage; a missing baseline also warns (and points at `hooks:setup`). To ratchet WARN → hard-fail, change the constants in this file — no restructure needed. Refresh an intentional baseline change with `bun run hooks:baseline`.

Beyond Layer B, the verifier must also hard-fail on the `.codex/config.toml` checks above (the `codex_hooks = true` master switch and the Layer-A registration). The Tier-2 baseline drift check remains WARN-only, but the master switch and Layer-A wiring are Tier-1: `bun run check` must not go green while Codex enforcement is one flipped boolean away from dark. The guards' own self-locking (each guard's list includes the config that could disable it) and CODEOWNERS are additional layers, not a substitute for the verifier asserting the switch is on.

---

## Porting checklist

To wire this into a new repo:

1. Author `.claude/hooks/protect-locked-files.js` and `.codex/hooks/protect-locked-files.py` (Layer A) with the repo's build-infra file lists; each list must include the files that could disable that guard.
2. Author `.codex/config.toml` with `[features] codex_hooks = true` and the Python-guard hook block.
3. Populate `agent-tools/config/protected-paths.json` and `authorized-commands.json`.
4. Run `bun run hooks:setup` to merge Layer B into `.claude/settings.json`, write `.codex/hooks.json`, and generate the baseline.
5. Wire `bun run hooks:verify` into `bun run check`.
6. Add `.protected-snapshots/` to `.gitignore` (it is an in-repo, Tier-1-protected scratch store).
