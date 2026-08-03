# The Policy Module — Single Source of Truth

Build this module **first**. Every hook in the governance system imports it. It owns the entire protected-path policy — the two protection tiers, the effective-globs union, the config loader with its process cache, the parser, and the small matching helpers the hooks share. There is exactly one copy of this policy; hooks declare none of it themselves.

Target file: `agent-tools/lib/protection-config.ts`. It depends on two sibling helper files, `agent-tools/lib/paths.ts` (path matching + shell-aware write-target extraction) and `agent-tools/lib/workspace-discovery.ts` (auto-protecting each workspace's config), both described below.

---

## 1. What this module decides

Given a candidate write target (a file path, or a Bash command that will write files) and an optional bypass token, the policy answers two questions:

1. **Is the target protected?** — membership in the union of all glob tiers.
2. **Is it protected at Tier 1 (never bypassable)?** — a stricter subset that no bypass token can unlock.

The hooks combine those answers into allow / block / revert decisions. This module supplies only the data and the pure predicates; it performs no I/O beyond loading config, and it never blocks anything itself.

---

## 2. The two glob tiers

The policy is deliberately layered so that removing a file from the on-disk JSON config can never fully unprotect the enforcement engine, and so that the most dangerous paths cannot be unlocked even by a legitimately-minted bypass token.

### Tier 1 — never bypassable

`TIER1_NEVER_BYPASS_GLOBS` is a two-entry list of directory globs. Membership here means the write is denied outright, *before* any bypass token is even evaluated.

```ts
export const TIER1_NEVER_BYPASS_GLOBS: readonly string[] = [
  ".elves/bypass-tokens/**",
  ".protected-snapshots/**",
];
```

- `.elves/bypass-tokens/**` — the bypass-token directory. Minting a token reaches a human's private key (via 1Password); if an AI tool call could write this directory it could forge its own bypasses. So the token store itself is unwritable by any tool call.
- `.protected-snapshots/**` — the in-repo directory where the drift hook stores each call's pre-state snapshot (base64 file content) and restores from it. If an AI could write here, it could poison a snapshot so the "revert" restores the attacker's bytes. Only the hook process writes this dir, via `fs` directly, which no tool-call gate intercepts.

Both entries are `**` directory globs — everything under those roots is Tier 1.

### Tier 2 — hardcoded but bypassable

`HARDCODED_PROTECTED_GLOBS` protects the whole enforcement engine regardless of what `protected-paths.json` contains (so deleting a JSON entry can't unprotect the engine), but a valid bypass token *does* authorize a write here. It is built from **directory globs**, not a cherry-picked file list, so no single file inside these trees can slip outside protection:

```ts
export const HARDCODED_PROTECTED_GLOBS: readonly string[] = [
  "agent-tools/hooks/**",
  "agent-tools/lib/**",
  "agent-tools/config/**",
  "agent-tools/scripts/**",
  "agent-tools/security/**",
  ".claude/settings.json",
  ".claude/settings.local.json",
  ".claude/hooks/**",
  ".codex/hooks.json",
  ".codex/config.toml",
  ".codex/hooks/**",
  "eslint-plugin-fourth-waiv/rules/require-signed-exemption/index.js",
  "eslint-plugin-fourth-waiv/lib/anchor-parser.ts",
  "eslint-plugin-fourth-waiv/lib/exemption-signer.ts",
  "eslint-plugin-fourth-waiv/lib/exemption-verifier.ts",
  "eslint-plugin-fourth-waiv/lib/registry-loader.ts",
];
```

The five `agent-tools/*/**` directory globs cover the hooks, this library, the JSON config, the scripts, and the security assets — the entire governance surface. The six `.claude`/`.codex` entries cover the **runtime registration**: `.claude/settings.json` (+ `.local`), `.claude/hooks/**`, `.codex/hooks.json`, `.codex/config.toml` (which holds the `codex_hooks = true` master switch), and `.codex/hooks/**` (the Layer-A guards). They are scoped to exactly these enforcement files, **not** blanket `.claude/**` / `.codex/**`, so the rest of those trees (skills, commands, agent state) stays writable without a token. These files are what make enforcement fire at all, so by the rule stated below they belong in Tier 2. They must live **here in source**, not only in the shrinkable `protected-paths.json`, because the standalone Layer-A guard fires on `Edit|Write|NotebookEdit` — **not** `Bash` — so an agent could otherwise disable enforcement with one Bash `sed -i` / redirect into `.claude/settings.json` or `.codex/config.toml`. A valid bypass token still authorizes a legitimate wiring change; a config edit can never *remove* the protection. The individual `eslint-plugin-fourth-waiv/...` files are the signed-exemption enforcement chain (anchor parsing, signing, verification, registry loading) that the lint layer depends on.

**When porting to another repo:** replace these globs with the directory trees that hold *your* enforcement engine **and** the runtime-registration files for whichever agent runtimes you drive (the `.claude/`/`.codex/` analogs). The rule is: any file whose modification could disable enforcement belongs in Tier 2 as a directory glob; the two paths whose modification could forge trust (token store, snapshot store) belong in Tier 1.

### Dynamic tier — JSON + workspace-discovered

The third layer is data-driven and loaded at runtime (Section 4). It combines the globs from `protected-paths.json` with globs auto-discovered from the workspace layout. Unlike the two in-source tiers, this layer *can* shrink if someone edits the JSON — which is exactly why the load-bearing protection lives in the hardcoded tiers and this layer is "bonus."

---

## 3. The union order — `effectiveProtectedGlobs`

At decision time each hook asks for the full membership set via one function. The order is fixed and meaningful:

```ts
export function effectiveProtectedGlobs(
  cfg: ProtectionConfig,
): readonly string[] {
  return [
    ...TIER1_NEVER_BYPASS_GLOBS,
    ...HARDCODED_PROTECTED_GLOBS,
    ...cfg.protectedGlobs,
  ];
}
```

1. `TIER1_NEVER_BYPASS_GLOBS` first
2. `HARDCODED_PROTECTED_GLOBS` second
3. the cached dynamic globs (workspace-discovered ++ JSON-configured) last

This union is the **membership test only** — "is this path protected at all." It does not encode bypassability. Tier-1 enforcement (the "even a valid token can't unlock this" check) is a *separate* comparison each hook performs against `TIER1_NEVER_BYPASS_GLOBS` directly. Keeping the two lists in source and unioning them at decision time means a test that injects an empty dynamic cache still cannot disable the in-source protection — the hardcoded tiers are always prepended.

---

## 4. The config loader + process cache

```ts
export interface ProtectionConfig {
  protectedGlobs: readonly string[];
  authorizedPrefixes: readonly string[];
}

export async function loadConfig(
  configDir: string,
  projectDir: string,
): Promise<ProtectionConfig>;
```

`loadConfig` memoizes into a module-level `cachedConfig` for the lifetime of the dispatcher process (the hooks run inside a long-lived dispatcher, so the config is read from disk once). On a cache miss it:

1. Reads `protected-paths.json` and `authorized-commands.json` from `configDir` in parallel.
2. Parses each through `parseConfigArray` — `protected-paths.json` must expose a `globs` string array, `authorized-commands.json` a `prefixes` string array.
3. Calls `discoverWorkspacePaths(projectDir)` (Section 6) for the workspace-derived globs.
4. Stores `{ protectedGlobs: [...discovered, ...globs], authorizedPrefixes: prefixes }`.

Note the cache holds **only the dynamic portion**. The two in-source tiers are never stored here; they are prepended fresh on every `effectiveProtectedGlobs` call. That is what makes the empty-cache test injection safe.

`ProtectionConfig.authorizedPrefixes` carries the `authorized-commands.json` prefixes forward for the command-authorization guard; the matcher `commandStartsWith` (Section 5) consumes them.

### Parser contract

```ts
export function parseConfigArray(raw: string, key: string, filename: string): string[];
```

Parses `raw` as JSON, requires an object containing `key`, and requires that value to be an array of strings — throwing a precise `${filename}: ...` error otherwise. Strict typing (no `any`): the parsed value is `unknown`, narrowed with an `every((x): x is string => ...)` guard.

### Config-dir resolution

```ts
export function defaultConfigDir(): string; // agent-tools/lib → .. → agent-tools/config
```

Resolves the config directory relative to this module's own URL via `fileURLToPath(import.meta.url)`, so the hooks don't hardcode an absolute path.

### Test-only escape hatches

- `_resetConfigCacheForTests()` — nulls the cache.
- `_setConfigForTests(cfg)` — overrides the cache without disk I/O.

These exist so tests can control the dynamic layer. Because the in-source tiers bypass the cache entirely, no test can use them to disable Tier 1 or Tier 2.

---

## 5. The shared matchers

All three are pure functions the hooks call directly.

### `isProtectedPath` (lives in `paths.ts`)

```ts
export function isProtectedPath(
  filePath: string,
  projectDir: string,
  protectedGlobs: readonly string[],
): boolean;
```

Normalizes `filePath` to a project-relative POSIX path, then tests it against each glob with `picomatch(glob, { dot: true })`. The `{ dot: true }` is required so `**` globs match dot-directories like `.elves/`. Path normalization (`normalizeToProjectRelative`) accepts absolute, `./`-prefixed, or bare paths, strips trailing separators, and returns `null` for anything that escapes the project root (a leading `..` after `path.relative`) — an escape is treated as *no match*, never protected, so it can't be used to smuggle a path past the check.

### `argsCoversTarget` — bypass-scope matcher

```ts
export function argsCoversTarget(
  args: BypassArgs,       // "*" | Record<string, unknown>
  target: string,
  projectDir: string,
): boolean;
```

Decides whether a bypass token's `args` field authorizes writing a specific `target`. Both the PreToolUse block hook and the PostToolUse drift hook consult the single `protected_paths:no_write` guard (exported as `PROTECT_GUARD`), so one bypass capability authorizes both the write and the drift-revert; that shared guard uses this `args` vocabulary:

- `"*"` → covers any target that guard would block.
- `{ file: "<rel-path>" }` → exact project-relative path equality (target is normalized first).
- `{ glob: "<picomatch>" }` → target matches that single glob (delegates to `isProtectedPath` with a one-element list).

Anything else returns `false` — a malformed args object authorizes nothing.

### `commandStartsWith` — command-authorization matcher

```ts
export function commandStartsWith(command: string, prefix: string): boolean;
```

Token-wise prefix match: splits both strings on whitespace and returns true only when every prefix token equals the corresponding command token. Used against `authorizedPrefixes` to decide whether a Bash command is on the allow-list. Because it compares whole tokens (not a substring), `git commit` does not match a command that merely contains that text mid-stream.

---

## 6. Workspace auto-discovery (`workspace-discovery.ts`)

```ts
export async function discoverWorkspacePaths(projectDir: string): Promise<readonly string[]>;
```

Reads `<projectDir>/package.json`, parses its `workspaces` array, and for each workspace `ws` emits five protected globs:

```
<ws>/eslint.config.js
<ws>/eslint.config.mjs
<ws>/eslint.config.cjs
<ws>/eslint.config.ts
<ws>/package.json
```

This closes the workspace-side backdoor: an unprotected per-workspace `eslint.config.*` could add `ignores:` entries to silence lint rules, and an unprotected workspace `package.json` could mutate scripts or dependencies. Auto-protecting these on every dispatcher cold-start means new workspaces get covered the next boot with no manual JSON edit.

Two design points to preserve when porting:

- **Graceful degradation.** Any read or parse failure, or a missing/non-array `workspaces` field, returns `[]` and never throws. It is bonus protection layered *on top of* the hardcoded tiers, not load-bearing — so it must fail open into the stronger tiers rather than crash the hook.
- **No existence check.** Globs are matched against candidate write paths, not against the filesystem, so a workspace whose directory doesn't exist yet is still emitted; the first attempt to create its `package.json` is intercepted.

---

## 7. `DRIFT_RELEVANT_TOOLS`

```ts
export const DRIFT_RELEVANT_TOOLS = new Set<string>([
  "Edit", "Write", "MultiEdit", "NotebookEdit", "apply_patch", "Bash",
]);
```

The set of tool names whose invocation could mutate a protected file. Both hooks gate on it: a tool call whose name is not in this set is irrelevant to protection and short-circuits to allow without any glob work. `Bash` is included because a shell command can write files indirectly — which is why `paths.ts` ships `extractWriteTargets` (Section 8) to turn a Bash command into its concrete write targets.

---

## 8. Turning a Bash command into write targets (`paths.ts`)

Because `Bash` is drift-relevant, the policy needs to know which files a shell command will write. `extractWriteTargets` does this shell-aware, never with regex shell parsing:

```ts
export function extractWriteTargets(
  bashCommand: string,
  projectDir: string,
): { ok: true; targets: string[] } | { ok: false; reason: string };
```

Contract highlights an implementer must reproduce:

- **Default-deny on ambiguity.** Before tokenizing, `validateQuoteBalance` runs a small state machine for balanced single/double quotes, because `shell-quote.parse()` silently truncates on an unterminated quote rather than throwing. Any imbalance, or a tokenizer exception, returns `{ ok: false }` so the caller blocks rather than guesses.
- **Tokenize, then walk.** The command is split into segments at pipeline/separator operators (`|`, `&&`, `;`, `(`, …), and each segment is analyzed against a per-command table — never a widening regex. New write shapes are added by extending the table.
- **Coverage the table encodes:** redirections (`>`, `>>`, `&>`, `1>`, `2>`, …); write-all-positional commands (`tee`, `rm`, `mkdir`, `touch`, `truncate`, …); last-positional-destination commands (`cp`, `mv`, `ln`, `install`); permission commands (`chmod`, `chown`, `chgrp`); in-place editors (`sed -i`, `awk -i inplace`); `dd of=`; `git config core.hooksPath` (mapped to a `.git/config` write) and `git update-ref` (mapped to `.git/HEAD`); inline-script bodies (`python -c`, `node -e`, `bun -e`, `perl -e`, …) whose quoted string literals are pulled out as candidate paths; leading `FOO=bar` env-assignments (stripped and re-parsed); and nested shell entry-points (`bash -c '...'`) which recurse.
- **Returned targets** are project-relative POSIX paths when they resolve inside `projectDir`, otherwise the raw token. The caller passes each through `isProtectedPath`. Over-matching is harmless — an extracted string that matches no protected glob simply allows.

---

## 9. Build + test order

1. Implement `paths.ts` first: `normalizeToProjectRelative`, `isProtectedPath`, then `extractWriteTargets` with its per-command table. Unit-test each command shape and every default-deny path (unbalanced quotes, tokenizer failure, `..` escape → null → no-match).
2. Implement `workspace-discovery.ts`: assert five globs per workspace, and that every failure mode returns `[]`.
3. Implement `protection-config.ts`: the two tier constants, `effectiveProtectedGlobs` union order, `loadConfig` caching (call twice, assert one disk read), `parseConfigArray` throw cases, and the three matchers. Use `_resetConfigCacheForTests` / `_setConfigForTests` to test that an empty dynamic cache still returns all Tier-1 and Tier-2 globs from `effectiveProtectedGlobs`.

Only after this module and its tests are green should the two hooks (PreToolUse block, PostToolUse revert) be built against it.
