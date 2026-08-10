# Drift Detect & Revert — the PostToolUse safety net

This file tells you how to build the **PostToolUse drift hook**: the second layer
of protected-path enforcement. The PreToolUse hook is the primary gate — it
inspects a tool call's *arguments* and denies a write to a protected file before
it happens. But a PreToolUse gate can only see writes it recognizes. A shell tool
(`Bash`) can modify files through a command whose target the pre-gate never parsed
(redirection, `sed -i`, a script, a heredoc). The PostToolUse hook closes that
gap by comparing the **actual bytes on disk** before and after each tool call, and
**reverting** any unauthorized change the call made to a protected file.

Source of truth:
- `agent-tools/hooks/post_tool_use/protected-files-drift.ts` — the drift decision.
- `agent-tools/lib/baseline.ts` — the tree-hash / snapshot-store helpers.
- `agent-tools/lib/protection-config.ts` — the shared protected-path policy (globs,
  tiers, config loader, matching helpers). Both hooks import from here; there is no
  duplicated copy. See the protection-config topic for its full contract.

---

## The core idea: per-call, content-based baselining

Do **not** keep a single persistent baseline of protected files and diff every
tool call against it. That produces a false-positive avalanche: any pre-existing
drift on disk (a legitimate edit made outside the hook, an uncommitted change)
makes *every subsequent* tool call look like it drifted, and the gate jams shut.

Instead the two hooks cooperate **per call**, keyed by the host CLI's
`tool_use_id`:

1. **PreToolUse** — for any drift-relevant tool with a `toolUseId`, walk the
   protected tree and snapshot the **raw bytes** (base64) of every protected file,
   keyed by that `toolUseId`, into an in-repo store.
2. The tool call runs.
3. **PostToolUse** — for the same `toolUseId`, re-hash the protected tree, diff
   against hashes derived from the snapshot, and decide. Then delete the snapshot.

Because the comparison is scoped to one call's own before/after, the only drift it
can ever report is drift *this call caused*. Pre-existing disk state is invisible.

---

## What counts as "drift-relevant"

Both hooks gate on the same tool set, exported from `protection-config.ts`:

```ts
export const DRIFT_RELEVANT_TOOLS = new Set<string>([
  "Edit", "Write", "MultiEdit", "NotebookEdit", "apply_patch", "Bash",
]);
```

If `input.toolName` is not in this set, the hook returns `{ decision: "allow" }`
immediately — a read-only tool cannot drift a file.

---

## The snapshot store

Location and format live in `baseline.ts`. The store is an **in-repo** directory,
**not** `os.tmpdir()`:

```ts
export function perCallStateDir(projectDir: string): string {
  const projectKey = createHash("sha256")
    .update(path.resolve(projectDir)).digest("hex").slice(0, 16);
  return path.join(path.resolve(projectDir), ".protected-snapshots", projectKey);
}
```

- The store lives at `.protected-snapshots/<projectKey>/` under the repo root.
  Keeping it in-repo (rather than a temp dir) means the revert reads its restore
  bytes from a location that is itself protected.
- `.protected-snapshots/**` is a **Tier-1 never-bypass** protected glob (see
  `TIER1_NEVER_BYPASS_GLOBS` in `protection-config.ts`, whose two entries are
  `.elves/bypass-tokens/**` and `.protected-snapshots/**`). No AI tool call can
  write the snapshot store, so a poisoned snapshot can't be used to "restore" an
  attacker's bytes. The hook process writes it via `fs` directly, which no
  tool-call gate intercepts.
- `.protected-snapshots/` is gitignored.
- The store is **pruned from the protected-tree walk itself** — `PRUNE_DIRS` in
  `baseline.ts` includes `.protected-snapshots` (alongside `node_modules`, `dist`,
  `server-dist`, `.turbo`, `coverage`) so the snapshot store never snapshots
  itself.

> **Residual-risk note — the snapshot store's write protection is
> PreToolUse-only.** Because `.protected-snapshots/**` is pruned from the tree
> walk, a write *into* the store never appears in `drift.added/changed/removed`,
> so the PostToolUse drift check can never fire for it. Every other protected
> path has two layers (PreToolUse block + PostToolUse drift net); the snapshot
> store has one — the PreToolUse Tier-1 extraction in `protected-files.ts`. That
> is acceptable only because Tier-1 denies the store outright before any bypass,
> and `extractWriteTargets` default-denies on any tokenizer ambiguity. If a
> recognized-but-unlisted Bash write shape ever slipped a snapshot write past
> `extractWriteTargets`, the poisoned snapshot would be neither blocked nor
> reverted. If you want the second layer here too, have the PostToolUse hook
> independently `stat` the snapshot directory (outside the pruned walk) and
> deny+treat as Tier-1 any snapshot file for the in-flight `tool_use_id` that the
> hook process did not itself write. At minimum, know the asymmetry exists.

Each snapshot is one JSON file named `<toolUseId>.json`:

```ts
interface PerCallStateFile {
  writtenAt: number;                 // Date.now() at capture
  globs?: string[];                  // protected globs in force at capture time
  files: Record<string, string>;     // project-relative POSIX path → base64 bytes
}
```

The map stores **base64 CONTENT** of every protected file, not hashes. Content is
what a revert needs; hashes are derived from it on demand. `globs` records the
protected-glob set the snapshot was captured with, so PostToolUse diffs against
that pinned universe rather than the globs as they stand after the call. It is
optional: legacy snapshots taken before this field fall back to the current
globs.

---

## Capturing the snapshot (PreToolUse side)

`captureTree(globs, projectDir)` in `baseline.ts` walks the tree and records
base64 bytes:

```ts
export async function captureTree(globs, projectDir): Promise<Record<string,string>> {
  const matchers = globs.map((g) => picomatch(g, { dot: true }));
  const out: Record<string, string> = {};
  await walk(absRoot, absRoot, matchers, out, readBase64);   // readBase64 = buf.toString("base64")
  return sortedByKey(out);
}
```

It shares the `walk` traversal with `hashTree` — same globs
(`effectiveProtectedGlobs(cfg)`), same `dot: true` matching (protected paths live
under `.claude/`, `.codex/`, `.git/`, `.elves/`), same `PRUNE_DIRS`. The only
difference is the per-file read function: `readBase64` vs `hashFile`.

The PreToolUse hook writes it (best-effort — any failure just means PostToolUse
will find no pre-state and degrade to allow):

```ts
// hooks/pre_tool_use/protected-files.ts
if (DRIFT_RELEVANT_TOOLS.has(input.toolName)
    && typeof input.toolUseId === "string" && input.toolUseId !== "") {
  const globs = effectiveProtectedGlobs(cfg);
  const content = await captureTree(globs, ctx.projectDir);
  await writePerCallState(ctx.projectDir, input.toolUseId, content, globs);
}
```

`writePerCallState` records both the base64 content **and** the protected-glob
set the snapshot was captured with, then writes atomically (temp file +
`rename`) into `perCallStateDir`. Storing the globs alongside the content lets
PostToolUse pin its diff to the exact universe that was protected when the call
started (see below).

---

## Reading it back (PostToolUse side)

Three readers, all in `baseline.ts`, all keyed by `toolUseId`, all returning
`null` on missing / stale / corrupt:

- **`readPerCallContent(projectDir, toolUseId)`** → `Record<path, base64>` — the
  raw bytes, used to revert. Validates the JSON shape, checks `writtenAt` against
  a **1-hour TTL** (`PER_CALL_STATE_TTL_MS`); anything older is treated as missing
  (the process likely crashed between Pre and Post).
- **`readPerCallGlobs(projectDir, toolUseId)`** → `string[]` — the protected-glob
  set the snapshot was captured with, used to pin the diff. Returns `null` when
  missing / stale / corrupt **or** on a legacy snapshot that predates the `globs`
  field, in which case the PostToolUse hook falls back to the current
  `effectiveProtectedGlobs(cfg)`.
- **`readPerCallState(projectDir, toolUseId)`** → `Record<path, sha256hex>` — the
  hashes, used for the diff. It calls `readPerCallContent` and derives each hash
  from the stored bytes:

  ```ts
  out[rel] = createHash("sha256").update(Buffer.from(b64, "base64")).digest("hex");
  ```

  So there is exactly one stored artifact (content); hashes are a projection of it.
  This guarantees the diff and the revert can never disagree about what the
  pre-call state was.

---

## The decision algorithm (PostToolUse hook body)

`agent-tools/hooks/post_tool_use/protected-files-drift.ts`, the default-exported
`hook: Hook`:

1. **Not drift-relevant** → `allow`.
2. Fire `cleanupStalePerCallState(projectDir)` (fire-and-forget) to sweep snapshots
   left by any call that skipped PostToolUse.
3. **No `toolUseId`** on input → `allow`. Without a key there is no pre-state to
   look up; PreToolUse remains the primary defense.
4. `loadConfig(defaultConfigDir(), projectDir)`.
5. `preState = readPerCallState(...)`. **`null`** → `allow` (PreToolUse failed to
   capture, or a Pre/Post mismatch — don't second-guess).
6. `preContent = readPerCallContent(...)` and `preGlobs = readPerCallGlobs(...)` —
   grab the bytes **and** the capture-time globs into memory **now**, before the
   snapshot is deleted, so a revert can proceed and the diff can run over the
   pinned universe. Resolve the diff globs, falling back to the current config for
   legacy snapshots:

   ```ts
   const preGlobs = await readPerCallGlobs(ctx.projectDir, toolUseId);
   const diffGlobs = preGlobs ?? effectiveProtectedGlobs(cfg);   // legacy snapshots fall back
   ```

7. Re-hash the tree **over `diffGlobs`** and diff, always deleting the snapshot
   afterward:

   ```ts
   try {
     const current = await hashTree(diffGlobs, ctx.projectDir);
     drift = diffBaseline(current, preState);
   } finally {
     await deletePerCallState(ctx.projectDir, toolUseId);   // never reusable
   }
   ```

   `diffBaseline` (in `baseline.ts`) yields `added` / `changed` / `removed` /
   `unchanged` path lists. Diffing against `diffGlobs` — the set that was protected
   when the call started — means an edit that *expands* the protected globs mid-call
   cannot make a pre-existing file look "added" and get deleted; the new universe
   only takes effect on the next call's own snapshot.
8. **`added + changed + removed === 0`** → `allow` (this call touched nothing
   protected).
9. **Authorized command?** If the originating call was `Bash` and its `command`
   starts with one of `cfg.authorizedPrefixes`
   (`config/authorized-commands.json`, matched token-wise by `commandStartsWith`),
   return `allow_with_warning`. Authorized commands legitimately modify protected
   files (build steps, generators). **No revert.**
10. **Tier-1 hit?** Scan the drifted targets for any that match
    `TIER1_NEVER_BYPASS_GLOBS`. If found, no bypass can ever authorize it — skip
    the bypass check and go straight to deny+revert with a Tier-1-specific reason.
11. **Bypass covers all drift?** If no Tier-1 hit, ask whether a single
    `protected_paths:no_write` bypass token covers **every** drifted path (see
    below). If so, return `allow_with_warning`. **No revert.** This is the same
    guard the PreToolUse block hook consults, so one bypass capability authorizes
    both the write (pre) and the drift-revert (post) of a single edit.
12. **Otherwise — unauthorized drift.** Build a human-readable deny reason listing
    the tool, `toolUseId`, the `Bash` command if any, and the CHANGED / ADDED /
    REMOVED path lists. Then **revert**, and return `{ decision: "deny", reason }`.

---

## The revert (this is the teeth)

On unauthorized drift the hook does **not** merely report — it rolls the disk back
to the pre-call bytes so the AI cannot land an unapproved change:

```ts
if (preContent !== null) {
  await restoreDrift(drift, preContent, ctx.projectDir);
  summaryLines.push("The drifted file(s) have been reverted to their pre-call content.");
}
return { decision: "deny", reason: summaryLines.join("\n") };
```

`restoreDrift` is best-effort (a restore failure still denies):

```ts
async function restoreDrift(drift, preContent, projectDir) {
  const absRoot = path.resolve(projectDir);
  for (const rel of [...drift.changed, ...drift.removed]) {
    const b64 = preContent[rel];
    if (b64 === undefined) continue;                  // nothing captured to restore from
    const abs = path.join(absRoot, rel);
    await fs.mkdir(path.dirname(abs), { recursive: true });
    await fs.writeFile(abs, Buffer.from(b64, "base64"));   // rewrite pre-call bytes
  }
  for (const rel of drift.added) {
    await fs.rm(path.join(absRoot, rel), { force: true }); // delete files the call created
  }
}
```

- **changed** and **removed** files are rewritten to their captured pre-call bytes
  (a removed file is recreated; `mkdir -p` its parent first).
- **added** files (the call created them) are **deleted**.

The hook performs these writes with `fs` directly — not through a tool call — so
the restore is not itself intercepted by the protected-path gate. That is the only
reason a hook is allowed to write a protected path.

`authorized-command` drift and `bypass`-covered drift are **left untouched** — the
early returns at steps 9 and 11 happen before any revert.

---

## Bypass coverage for drift

A `protected_paths:no_write` bypass must cover **every** drifted path with a
**single** token, so a narrow `{ file: … }` bypass can't accidentally launder
unrelated collateral drift. `driftedTargetsAllCovered` resolves one bypass per
target and requires they all resolve to the same `tokenId`:

```ts
for (const t of targets) {
  const bp = await bypassAllows({ guardName: GUARD_NAME, argsCheck: (a)=>argsCoversTarget(a,t,projectDir),
                                  projectDir, toolName: input.toolName, target: t, skipAudit: true });
  if (!bp.matched) return { matched: false };
  if (firstTokenId === null) firstTokenId = bp.tokenId;
  else if (firstTokenId !== bp.tokenId) return { matched: false };
}
// one consolidated audit entry naming all targets
```

`GUARD_NAME` is bound to `PROTECT_GUARD` (`"protected_paths:no_write"`), imported
from `protection-config.ts` and re-exported as `guardName` for self-documentation.
Both this drift hook and the PreToolUse block hook consult that one guard, so a
human never grants two capabilities for one edit and a token can never be "enough
for the write but not the revert." Args vocabulary (`"*"` / `{file}` / `{glob}`)
is `argsCoversTarget` in `protection-config.ts`. Tier-1 paths are excluded from
this path entirely (step 10 short-circuits before it).

---

## The decision contract

Input: a `HookInput` (`toolName`, `toolInput`, `toolUseId`) plus a context
`{ projectDir }`. Output: a `HookOutput`, one of:

| decision | when | revert? |
|---|---|---|
| `allow` | not drift-relevant, no `toolUseId`, no pre-state, or zero drift | — |
| `allow_with_warning` | drift after an **authorized command**, or drift fully covered by one **bypass token** | no |
| `deny` (with `reason`) | **unauthorized** drift (incl. any Tier-1 drift) | **yes — files reverted** |

The dispatcher maps `deny` to the host CLI's block exit code; the `reason` string
is surfaced to the agent.

---

## Graceful degradation (never jam the agent)

Every "we can't be sure" branch degrades to **allow**, because PreToolUse is the
primary defense and a jammed PostToolUse would block all work:

- tool not drift-relevant → allow
- no `toolUseId` → allow (no key to look up pre-state)
- pre-state missing / stale (>1h) / corrupt → allow
- zero drift → allow

And every exit deletes the snapshot (`deletePerCallState` in the `finally`), so a
snapshot is never reusable across calls. `cleanupStalePerCallState` opportunistically
sweeps orphaned snapshots (TTL by mtime) so the store can't grow unbounded if the
host ever skips PostToolUse.

Performance budget: per-call under ~100ms (the tree walk is the cost; assert it in
tests).

---

## How to test it

Drive the hook directly with a fake `HookInput` and a temp `projectDir`:

- **Zero drift** — capture, no change, call hook → `allow`, snapshot deleted.
- **Unauthorized change** — capture, mutate a protected file on disk, call hook →
  `deny`, and assert the file's bytes are back to the pre-call content.
- **Unauthorized create** — capture, create a new protected file, call hook →
  `deny`, and assert the created file is gone.
- **Unauthorized delete** — capture, delete a protected file, call hook → `deny`,
  and assert the file is recreated with its pre-call bytes.
- **Authorized command** — `Bash` command matching an authorized prefix drifts a
  protected file → `allow_with_warning`, file **unchanged** (no revert).
- **Bypass-covered** — mint a `protected_paths:no_write` token covering all
  drifted paths → `allow_with_warning`, no revert.
- **Tier-1 drift** — drift `.protected-snapshots/**` or `.elves/bypass-tokens/**`
  → `deny` with the Tier-1 reason, even if a bypass token exists.
- **Degradation** — missing `toolUseId`, missing snapshot, stale snapshot (age >
  TTL) → `allow`.

Co-located tests already exist at
`agent-tools/hooks/post_tool_use/protected-files-drift.test.ts` and
`…/protected-files-drift.tier.test.ts` — follow their patterns.
