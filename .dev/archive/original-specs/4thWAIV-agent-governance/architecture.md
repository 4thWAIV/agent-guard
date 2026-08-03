# Architecture: one engine, two runtimes

This is the mental model for the whole governance system. Read it once and the
rest of the guide slots into place: every other file describes one box in the
diagram below.

The design in one sentence: **two AI runtimes (Claude Code and Codex CLI) both
shell out to the same TypeScript dispatcher on every tool call; the dispatcher
runs a set of hook modules; the hooks consult one shared policy module; a hook
that would block first asks a signed-token seam for permission; every grant is
appended to an audit log.**

There is exactly ONE decision engine. The two runtimes are just two ways of
invoking it. Do not build the policy twice.

---

## The layers, top to bottom

```
  ┌─ RUNTIME ──────────────────┐     ┌─ RUNTIME ──────────────────┐
  │        Claude Code         │     │         Codex CLI          │
  │  registers hooks in        │     │  registers hooks in        │
  │  .claude/settings.json     │     │  .codex/hooks.json         │
  └──────────────┬─────────────┘     └──────────────┬─────────────┘
                 │  on every Edit/Write/…/Bash tool call:            
                 │  spawn:  bun agent-tools/lib/dispatcher.ts <event>
                 │  stdin  = host-CLI JSON payload                   
                 │  argv[2]= pre_tool_use | post_tool_use            
                 └──────────────┬───────────────────┘
                                ▼
        ┌───────────────────────────────────────────────┐
        │  DISPATCHER   agent-tools/lib/dispatcher.ts     │  ← ONE engine
        │  • parse event arg + stdin → normalized input   │
        │  • discover *.ts under hooks/<event>/           │
        │  • import + run each hook, aggregate decisions  │
        │  • deny→exit 2+stderr · warn→exit 0 · allow→0   │
        └───────────────┬───────────────────────────────┘
              ┌─────────┼──────────────────────────┐
              ▼         ▼                          ▼
   pre_tool_use/    pre_tool_use/          post_tool_use/
   protected-files  invocation-block       protected-files-drift
   (no_write)       (bash denylist)        (no_write + REVERT)
              │         │                          │
              └────────┬┴──────────────────────────┘
                       ▼
        ┌───────────────────────────────────────────────┐
        │  SHARED POLICY  agent-tools/lib/protection-     │
        │  config.ts   — tiers, glob union, config loader │
        └───────────────┬───────────────────────────────┘
                        ▼  reads
        agent-tools/config/protected-paths.json      (dynamic globs)
        agent-tools/config/authorized-commands.json  (allowed prefixes)
        agent-tools/config/invocation-blocked.json   (regex denylist)
        + TIER1_NEVER_BYPASS_GLOBS  (in source)
        + HARDCODED_PROTECTED_GLOBS (in source)
        + workspace-discovery       (runtime)
                       │
                       ▼  a hook about to DENY asks first:
        ┌───────────────────────────────────────────────┐
        │  SIGNING SEAM  agent-tools/lib/bypass.ts        │
        │  bypassAllows() — load .elves/bypass-tokens/*,  │
        │  ed25519-verify vs agent-tools/security/        │
        │  bypass-public-key.pem, route by guard name     │
        └───────────────┬───────────────────────────────┘
                        ▼  on match, and on mint/extend/revoke:
              .elves/bypass-audit.log   (JSONL append-only)
```

---

## 1. Runtime wiring — two configs, one command

Both runtimes register the SAME command string against the SAME tool matchers.
The only differences are the config file location and how the project root is
spelled.

**Claude Code** — `.claude/settings.json`, `hooks` block. Registers the
dispatcher for both events, matching the file-write tools and `Bash`
separately, and passes the project root via the host variable:

```json
"command": "bun \"${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts\" pre_tool_use"
```

**Codex CLI** — `.codex/hooks.json`, same `PreToolUse` / `PostToolUse` shape,
same matchers, relative path (Codex runs from the project root):

```json
"command": "bun agent-tools/lib/dispatcher.ts pre_tool_use"
```

The matcher on both is `Edit|Write|MultiEdit|NotebookEdit|apply_patch` plus a
separate `Bash` entry, for both `PreToolUse` and `PostToolUse`. When adding a
new runtime, you write ONE more config file that spawns the same command — you
do not touch the engine.

> Belt-and-suspenders note: Claude Code's settings also register a tiny
> standalone `protect-locked-files.js` guard ahead of the dispatcher. It is an
> independent hardcoded-list backstop, not part of the engine. The real,
> extensible enforcement is the dispatcher and its hooks described here.

## 2. The dispatcher — parse, discover, run, aggregate

`agent-tools/lib/dispatcher.ts` is the single entry point. Its job is
mechanical and stateless:

1. **Event from argv.** `argv[2]` is `pre_tool_use` or `post_tool_use`.
   `VALID_EVENT_TYPES` maps it to the canonical `PreToolUse` / `PostToolUse`
   name. Unknown arg → exit 2.
2. **Payload from stdin.** `readHostPayload()` reads the host CLI's JSON on
   stdin into a `HostPayload`. Empty stdin (no tool actually invoked) → exit 0.
3. **Normalize.** The snake_case host payload becomes the camelCase hook
   contract — `HookInput` (`toolName`, `toolInput`, `toolUseId`) and
   `HookContext` (`projectDir`, `hookEventName`, `sessionId`, `cwd`). This
   normalization is why hooks never see host-CLI-specific field names, and why
   both runtimes can feed the same hooks.
4. **Discover hooks.** `discoverHookFiles()` lists `*.ts` under
   `agent-tools/hooks/<event_type>/`, excluding `*.test.ts` / `*.d.ts`, sorted.
   Dropping a new `.ts` file into that directory registers a new guard — no
   central list to edit. No files for the event → allow (exit 0).
5. **Run + aggregate.** Each module is dynamic-imported and its default export
   invoked as a `Hook`. Results aggregate: any `deny` → exit 2 with every
   reason on stderr; any `allow_with_warning` → warning on stderr, exit 0;
   otherwise exit 0.

**Default-deny is structural.** A hook that fails to import, has no default
export, returns a bad shape, or throws is converted to a `deny`
(`importHookModule` / `invokeHookWithModule`). A broken guard fails closed, not
open.

### The hook contract

Every hook is a default-exported async function:

```ts
type Hook = (input: HookInput, ctx: HookContext) => Promise<HookOutput>;

type HookOutput =
  | { decision: "allow" }
  | { decision: "deny"; reason: string }
  | { decision: "allow_with_warning"; warning: string };
```

That three-value contract IS the decision protocol. The dispatcher understands
nothing about protected paths, tokens, or drift — it only knows how to run
`Hook`s and combine their `HookOutput`s.

## 3. The shared policy module — one copy of the rules

`agent-tools/lib/protection-config.ts` is the single source of truth for *what
is protected*. Both the PreToolUse write-guard and the PostToolUse drift-guard
import it; neither declares any of the policy itself. It owns:

- **`TIER1_NEVER_BYPASS_GLOBS`** — two directory globs never writable by an AI
  tool call, even with a valid token: `.elves/bypass-tokens/**` (writing the
  token dir would let the AI forge its own bypasses) and
  `.protected-snapshots/**` (the drift snapshot store — a poisoned snapshot
  would corrupt the revert). Checked *before* bypass evaluation in both hooks.
- **`HARDCODED_PROTECTED_GLOBS`** — protected regardless of what
  `protected-paths.json` says (JSON-removal-resistant), but a valid token still
  authorizes the write. These are *directory* globs covering the whole
  enforcement engine — `agent-tools/hooks/**`, `agent-tools/lib/**`,
  `agent-tools/config/**`, `agent-tools/scripts/**`, `agent-tools/security/**`
  — **plus the runtime registration `.claude/**` and `.codex/**`** (the hook
  configs, the Layer-A guards, and the `codex_hooks = true` master switch that
  make enforcement fire at all) and the individual signed-exemption lint files.
  Keeping the wiring in source matters because the standalone Layer-A guard fires
  only on `Edit|Write|NotebookEdit`, so without a hardcoded glob a single Bash
  `sed -i` into `.claude/settings.json` could disable enforcement. Directory
  globs mean no single new file inside the engine or its wiring can slip outside
  protection.
- **`loadConfig()`** — reads `protected-paths.json` (`globs`) and
  `authorized-commands.json` (`prefixes`), unions in workspace-discovered
  paths, and memoizes for the process lifetime.
- **`effectiveProtectedGlobs(cfg)`** — the membership set evaluated at decision
  time: `TIER1 ++ HARDCODED ++ dynamic`. Because Tier-1 and hardcoded globs are
  concatenated here at call time (not stored in the cache), editing or emptying
  the JSON config can never disable them.
- Matching helpers (`commandStartsWith`, `argsCoversTarget`) and the shared set
  `DRIFT_RELEVANT_TOOLS` both hooks gate on.

## 4. The signing seam — a hook asks before it blocks

The bypass check is owned by the *hook*, not the dispatcher. Any hook about to
return `deny` first calls `bypassAllows()` from `agent-tools/lib/bypass.ts`,
supplying the shared guard name (`protected_paths:no_write`, exported as
`PROTECT_GUARD` from `agent-tools/lib/protection-config.ts`) and an args
predicate. Both the pre-hook write block and the post-hook drift+revert consult
this single guard, so one bypass capability authorizes the whole edit.

`bypassAllows()`:

- loads every `.elves/bypass-tokens/*.token`,
- ed25519-verifies each against the committed public key at
  `agent-tools/security/bypass-public-key.pem` (via `node:crypto.verify` — no
  external dependency), over a canonically-sorted JSON payload so signer and
  verifier hash identical bytes,
- drops expired/malformed tokens,
- walks capabilities whose `guard` matches, runs the hook's `argsCheck`, and on
  first match writes a `bypass_used` audit entry and returns
  `{ matched: true }`.

The private key never touches this library. Tokens are minted out-of-band by a
CLI that reads the key through 1Password (`op`), so the running AI can produce a
denial but never a grant. Tier-1 targets short-circuit *before* this seam is
consulted — no token is even considered.

## 5. Audit — every grant is a line

`appendAuditEntry()` writes JSONL to `.elves/bypass-audit.log`, one entry per
line, `O_APPEND` so concurrent writers don't interleave. The `AuditEntry` union
covers the token lifecycle: `granted`, `extended`, `capability_added`,
`revoked`, `bypass_used`, `expired`. Every bypass that actually authorized a
write leaves a `bypass_used` record naming the token id, guard, tool, and
target. The audit trail is the human-readable record of every time policy was
overridden.

---

## The two-pass data flow for a single file write

This is the end-to-end path the reader must reproduce. The write-guard and the
drift-guard are two passes around the same tool call, coordinated through an
in-repo snapshot.

**Pass 1 — PreToolUse (`hooks/pre_tool_use/protected-files.ts`):**

1. Resolve the target path from `toolInput` (or the target(s) of a `Bash`
   command). Unresolvable → default-deny.
2. If the target is in `TIER1_NEVER_BYPASS_GLOBS` → deny outright, no bypass.
3. If the target is in `effectiveProtectedGlobs` → call `bypassAllows`; on match
   allow-with-warning, otherwise deny.
4. **On any allowed outcome**, capture the pre-call CONTENT of the protected
   tree — project-relative path → base64 of the file's bytes — and write it via
   `writePerCallState()` into the in-repo, Tier-1-protected
   `.protected-snapshots/` directory, keyed by the host CLI's `tool_use_id`.
   (The store holds base64 *content*, not hashes, so the next pass can restore
   byte-for-byte.)

The host CLI then performs the tool call.

**Pass 2 — PostToolUse (`hooks/post_tool_use/protected-files-drift.ts`):**

1. Look up the snapshot for this `tool_use_id`. Missing / no id → allow (Pre is
   the primary defense; Post is the safety net for interception gaps).
2. Read the snapshot content into memory, then re-hash the protected tree now
   and diff against the snapshot. The snapshot file is always deleted on the way
   out.
3. No drift → allow. Drift from an authorized command → allow-with-warning.
4. Drift on a Tier-1 path → deny, never bypassable.
5. Otherwise ask whether ONE token covers *every* drifted path
   (`driftedTargetsAllCovered`). If so, allow-with-warning and audit once.
6. **Unauthorized drift → REVERT and deny.** `restoreDrift()` rewrites every
   changed/removed protected file back to its captured pre-call bytes and
   deletes any file the call created, writing through `fs` directly (not a tool
   call, so the restore is not itself intercepted). The AI cannot land an
   unapproved change even if the write slipped past PreToolUse.

The snapshot is per-call and keyed by `tool_use_id`, which is why only drift
*caused by this call* is judged — pre-existing drift on disk never blocks an
unrelated later call.

---

## What to keep invariant when porting

- One dispatcher command, spawned identically by every runtime config. New
  runtime = new config file, never a second engine.
- Hooks are discovered from a directory, not a registry. New guard = new file.
- The three-value `HookOutput` is the entire decision protocol.
- Policy lives in exactly one module (`protection-config.ts`); both hooks import
  it. Tier-1 and hardcoded globs are unioned in source at decision time so no
  config edit can disable them.
- The block-decision owner (the hook) consults the signing seam; the seam only
  ever verifies, never mints. Every grant is appended to the audit log.
