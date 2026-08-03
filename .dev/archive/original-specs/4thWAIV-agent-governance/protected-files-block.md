# The PreToolUse Block Hook

**Source:** `agent-tools/hooks/pre_tool_use/protected-files.ts`
**Depends on:** `agent-tools/lib/paths.ts`, `agent-tools/lib/protection-config.ts`, `agent-tools/lib/baseline.ts`, `agent-tools/lib/bypass.ts`, `agent-tools/lib/types.ts`

This is the *before-the-write* guard. It runs as a PreToolUse hook: the host CLI (Claude Code, Codex) hands it a tool call the agent is *about to make*, and the hook returns a decision that either lets the call proceed or blocks it before any bytes touch disk. Its one job is to stop an AI agent from writing to a protected file. As a side effect on any *allowed* write that could later drift a protected file, it snapshots the pre-call content so the PostToolUse drift hook can revert an unauthorized change byte-for-byte.

Build this hook to enforce a single rule: **a write to a protected path is denied unless a valid bypass token authorizes it, and some paths are denied even then.**

---

## The decision contract

The hook conforms to the shared `Hook` type (`agent-tools/lib/types.ts`):

```ts
type Hook = (input: HookInput, ctx: HookContext) => Promise<HookOutput>;
```

**Input** (`HookInput`): `toolName` (string), `toolInput` (`Record<string, unknown>` — raw, shape varies by tool), and optional `toolUseId` (correlates this call with its later PostToolUse).

**Context** (`HookContext`): `projectDir` (absolute repo root) is the only field this hook reads.

**Output** (`HookOutput`) is a three-way discriminated union — this is the exact contract:

```ts
type HookOutput =
  | { decision: "allow" }
  | { decision: "deny"; reason: string }
  | { decision: "allow_with_warning"; warning: string };
```

- `allow` — the tool call proceeds silently.
- `deny` — the tool call is blocked; `reason` is surfaced to the agent as stderr. The dispatcher short-circuits any `deny` to **host exit code 2**, which is how the host CLI knows to abort the tool call.
- `allow_with_warning` — the call proceeds (a bypass token authorized it) and `warning` is logged.

The default-deny bias is the whole point: when the hook cannot *prove* a call is safe, it denies.

---

## Top-level flow

The exported `hook` function does two things in order:

1. Load the protection config once, then call `decide(input, projectDir, cfg)` to get a `HookOutput`.
2. If the decision was **not** `deny` and the tool is drift-relevant, capture the pre-call content snapshot (see [Snapshot capture](#snapshot-capture-on-allowed-drift-relevant-calls)).
3. Return the decision.

```ts
const cfg = await loadConfig(defaultConfigDir(), ctx.projectDir);
const decision = await decide(input, ctx.projectDir, cfg);
// ...snapshot capture if decision !== "deny" and drift-relevant...
return decision;
```

`decide` routes by tool name into three branches:

- **File-edit tools** (`Edit`, `Write`, `MultiEdit`, `NotebookEdit`, `apply_patch`) → `checkFileEditTarget`.
- **`Bash`** → `checkBashCommand`.
- **Everything else** (`Read`, `Glob`, `Grep`, `WebFetch`, MCP tools, etc.) → `{ decision: "allow" }`. These tools do not write files, so they are never gated.

The file-edit tool set is a hardcoded `FILE_EDIT_TOOLS` `Set`. These are the tools that carry a `file_path` argument the hook can read directly. Bash is different — its write targets are hidden inside a shell command string and must be *extracted*.

---

## Branch 1 — file-edit tools

For `Edit`/`Write`/`MultiEdit`/`NotebookEdit`/`apply_patch`, the write target is a single path. `checkFileEditTarget`:

1. **Extract the path.** `extractFilePath` reads `toolInput.file_path`; if that is missing it falls back to `toolInput.path` (Codex `apply_patch` uses `path`). If neither is a non-empty string, the target cannot be verified → **default-deny**:

   ```ts
   if (filePath === null) {
     return { decision: "deny", reason: `${input.toolName}: missing/unrecognised file_path ... (default-deny per AD6).` };
   }
   ```

2. **Membership test.** If `isProtectedPath(filePath, projectDir, effectiveProtectedGlobs(cfg))` is false → `allow`. Nothing protected is being touched.

3. **Tier-1-before-bypass.** Normalize to a project-relative path, then check it against `TIER1_NEVER_BYPASS_GLOBS` **first**. A Tier-1 match denies outright — no bypass evaluation happens:

   ```ts
   if (isProtectedPath(rel, projectDir, TIER1_NEVER_BYPASS_GLOBS)) {
     return { decision: "deny", reason: tier1Reason(input.toolName, rel) };
   }
   ```

4. **Bypass evaluation.** Only if the path is protected but *not* Tier-1 does the hook consult `bypassAllows`. A matching token yields `allow_with_warning` (naming the 8-char token id prefix and the target). No token → `deny` with a path-specific remediation message.

---

## Branch 2 — Bash write-target extraction

Bash is the hard case: the target lives inside a command string. `checkBashCommand`:

1. **Extract the command** from `toolInput.command`. Missing/empty → **default-deny**.

2. **Authorized-prefix bypass FIRST.** Before any tokenizing, check the command against the config's `authorizedPrefixes` via `matchesAuthorizedPrefix` → `commandStartsWith` (token-wise leading-prefix match, not substring). A match means the command is a known-safe managed script (e.g. `bun add`, `bun run scripts:modify`) → `allow`. This ordering matters: authorized commands are the sanctioned way to mutate protected files, so they short-circuit before the (fallible) tokenizer runs.

3. **Tokenize and extract write targets** via `extractWriteTargets(command, projectDir)` (in `agent-tools/lib/paths.ts`). This returns a discriminated result:

   ```ts
   type ExtractWriteTargetsResult =
     | { ok: true; targets: string[] }
     | { ok: false; reason: string };
   ```

   **On `ok: false` → default-deny.** Tokenizer failure — an unterminated quote, a shape it cannot parse — is treated as *unsafe*, never waved through:

   ```ts
   if (!result.ok) {
     return { decision: "deny", reason: `Bash: ${result.reason}. ... (AD6 default-deny.)` };
   }
   ```

4. **Per-target decision.** Loop over `result.targets`. For each target that `isProtectedPath` matches:
   - **Tier-1 check first** → deny outright if it hits `TIER1_NEVER_BYPASS_GLOBS`.
   - Otherwise consult `bypassAllows` → `allow_with_warning` on a matching token, else `deny`.

   A non-protected target is skipped (`continue`). If no target is protected, the loop falls through to `allow`.

### How write targets are extracted

`extractWriteTargets` never uses regex to parse shell. It:

- **Pre-validates quote balance** with a hand-rolled state machine (`validateQuoteBalance`), because `shell-quote.parse()` silently truncates on an unterminated quote instead of throwing. An unbalanced quote → `{ ok: false }`.
- **Tokenizes** with `shell-quote`, splits into command segments at pipeline/separator operators (`| || && ; & ( )`), and walks each segment.
- **Per-command knowledge table.** Write targets are recognized per command, not by pattern-guessing:
  - Redirections (`>`, `>>`, `&>`, `>|`, `1>`, `2>`, `&>>`) → the following bareword.
  - `tee`, `rm`, `mkdir`, `rmdir`, `touch`, `mkfifo`, `mknod`, `truncate` → all positional args.
  - `cp`, `mv`, `ln`, `install` → last positional (destination).
  - `chmod`, `chown`, `chgrp`, `chflags` → positionals after the mode/owner.
  - `sed -i` / `awk -i inplace` → the file operands.
  - `dd of=…` → the `of=` target.
  - `python -c` / `node -e` / etc. → string literals pulled from the inline script body.
  - `git config core.hooksPath …` → treated as a write to `.git/config`; `git update-ref` → `.git/HEAD`.
  - `bash -c '…'` / `sh -c` / `zsh -c` → recursively re-parses the inner command.

  New write shapes are added by extending this table — never by widening a regex. Over-matching is harmless (a false target simply fails the protected-glob test); under-matching is the danger the default-deny and quote checks guard against.

Targets are returned as project-relative POSIX paths when they resolve inside `projectDir`.

---

## Path matching and normalization (`agent-tools/lib/paths.ts`)

`isProtectedPath(filePath, projectDir, globs)` is the membership test used everywhere:

- Normalizes the input to a project-relative POSIX path via `normalizeToProjectRelative`. Absolute, `./`-prefixed, and bare paths all normalize to the same relative form.
- A path that escapes the project root (`../…`, resolves outside `projectDir`) returns `null` from normalization and is treated as **no-match** — this hook only protects paths inside the repo.
- Matches the normalized path against each glob with `picomatch(glob, { dot: true })`. Dot-files are matched because protected paths live under `.claude/`, `.codex/`, `.git/`, `.elves/`.

---

## The two protection tiers (`agent-tools/lib/protection-config.ts`)

The protected-path policy lives in **one shared module** that both the block hook and the drift hook import. There is no duplicated copy.

**Tier 1 — `TIER1_NEVER_BYPASS_GLOBS`** — denied for AI tool calls regardless of any bypass token. Exactly two directory globs:

```ts
export const TIER1_NEVER_BYPASS_GLOBS = [
  ".elves/bypass-tokens/**",
  ".protected-snapshots/**",
];
```

These back the enforcement machinery itself: `.elves/bypass-tokens/**` is the token store (letting the AI write it would let it forge bypasses), and `.protected-snapshots/**` is the pre-call content snapshot store the drift hook restores from (letting the AI write it would poison a restore). Both are written by the hook process via `fs` directly, which no tool-call gate intercepts.

**Tier 2 — `HARDCODED_PROTECTED_GLOBS`** — protected even if `protected-paths.json` is emptied (JSON-removal-resistant), but a valid bypass token *does* authorize the write. These are **directory globs** covering the whole enforcement engine, not a cherry-picked file list:

```ts
export const HARDCODED_PROTECTED_GLOBS = [
  "agent-tools/hooks/**",
  "agent-tools/lib/**",
  "agent-tools/config/**",
  "agent-tools/scripts/**",
  "agent-tools/security/**",
  "eslint-plugin-fourth-waiv/rules/require-signed-exemption/index.js",
  "eslint-plugin-fourth-waiv/lib/anchor-parser.ts",
  "eslint-plugin-fourth-waiv/lib/exemption-signer.ts",
  "eslint-plugin-fourth-waiv/lib/exemption-verifier.ts",
  "eslint-plugin-fourth-waiv/lib/registry-loader.ts",
];
```

Using directory globs (`agent-tools/hooks/**`) rather than named files means no single new file inside the enforcement engine can slip outside protection.

**The effective set** used for membership testing is the union, built at decision time by `effectiveProtectedGlobs(cfg)`:

```ts
return [
  ...TIER1_NEVER_BYPASS_GLOBS,
  ...HARDCODED_PROTECTED_GLOBS,
  ...cfg.protectedGlobs,  // workspace-discovered + protected-paths.json
];
```

Unioning the in-source lists at decision time means a test-injected empty config can never disable Tier-1/Tier-2 protection. Tier-1 *enforcement* (deny-before-bypass) is a **separate check** in each hook — the union above is only the membership test for "is this path protected at all."

`loadConfig` reads `protected-paths.json` and `authorized-commands.json` from `agent-tools/config/`, merges in workspace-discovered paths, and memoizes for the process lifetime.

---

## Snapshot capture on allowed drift-relevant calls

After `decide` returns anything other than `deny`, and only when the tool is one of `DRIFT_RELEVANT_TOOLS` (`Edit`, `Write`, `MultiEdit`, `NotebookEdit`, `apply_patch`, `Bash`) and a non-empty `toolUseId` is present, the hook captures the pre-call state:

```ts
const content = await captureTree(effectiveProtectedGlobs(cfg), ctx.projectDir);
await writePerCallState(ctx.projectDir, input.toolUseId, content);
```

- `captureTree` (`agent-tools/lib/baseline.ts`) walks the repo and records **base64 CONTENT** (raw bytes) of every protected file — not hashes. Storing content is what lets the PostToolUse drift hook **revert** an unauthorized change to its exact pre-call bytes (and delete a file the call created), rather than merely detecting that drift happened.
- `writePerCallState` stores it keyed by `toolUseId` under an **in-repo** directory: `.protected-snapshots/<hash-of-projectDir>/<toolUseId>.json` (via `perCallStateDir`). This is a Tier-1 path — the AI can never write it — and it is *not* `os.tmpdir()`. The snapshot is scoped per-project and carries a `writtenAt` timestamp; the reader treats anything older than a 1-hour TTL as stale.
- **Capture is best-effort.** Any failure (no `toolUseId`, IO error) is swallowed in a `try/catch`. A missing snapshot just means the PostToolUse hook falls back to allowing — the design keeps PreToolUse, not PostToolUse, as the primary defense.

Per-call keying (each snapshot scoped to one `toolUseId`) is deliberate: it eliminates the persistent-baseline false-positive class where one legitimate earlier change would otherwise flag every later call.

---

## Deny reasons

Deny messages are actionable, telling the agent exactly how to proceed legitimately:

- **`tier1Reason`** — explains that `.elves/bypass-tokens/**` and `.protected-snapshots/**` are never writable by tool calls, and points to the only legitimate mint path (`op signin` → `bun run bypass mint …`).
- **`protectedReason`** → `remediationFor(target)` returns a path-specific remedy: `package.json` → use `bun add`/`bun remove`/`bun run scripts:modify`; `turbo.json` → ask the human owner; `lint-exemptions/…` → use `bun run lint:exempt`; `.claude/`/`.codex/` → managed by `bun run hooks:setup`; `agent-tools/` → self-modification needs a human review pass; `.git/` → repointing hooks would silently disable enforcement.

---

## How to test it

Drive `decide` (or the exported `hook`) directly with synthetic `HookInput` values and assert on the `HookOutput`. The contract makes these cases exhaustive:

- **Allow non-write tools:** `{ toolName: "Read", toolInput: {...} }` → `allow`.
- **Allow unprotected edit:** `Write` to `src/foo.ts` → `allow`.
- **Deny protected edit:** `Edit` to `turbo.json` (no token) → `deny`.
- **Tier-1 deny beats bypass:** `Write` to `.elves/bypass-tokens/x.json` *with* a valid wildcard token → still `deny` (assert the Tier-1 reason).
- **Bypass allows Tier-2:** `Edit` to `agent-tools/lib/foo.ts` with a matching token → `allow_with_warning`.
- **Default-deny on missing path:** `Edit` with no `file_path`/`path` → `deny`.
- **Bash authorized prefix:** `{ toolName: "Bash", command: "bun add left-pad" }` → `allow`.
- **Bash redirect to protected:** `command: "echo x > turbo.json"` → `deny`.
- **Default-deny on tokenizer failure:** `command: "echo \"unterminated > turbo.json"` (unbalanced quote) → `deny`.
- **Snapshot side effect:** an *allowed* drift-relevant call with a `toolUseId` writes `.protected-snapshots/<key>/<toolUseId>.json`; assert the file exists and holds base64 content of the protected files.

The whole hook is designed to run under a per-call budget of well under 50ms; assert timing if you want a regression guard.
