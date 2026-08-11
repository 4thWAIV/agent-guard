# File Manifest — What to Copy, Author, Generate, and Drop

You are duplicating the agent-governance system into a new repository. This file is
the packing list. It sorts every artifact into one of four dispositions:

- **COPY VERBATIM** — the generically-reusable engine. Byte-for-byte copy; do not
  edit the logic. This is the machinery that makes "the AI cannot self-authorize"
  true, and it operates only on file paths + shell strings, so it is repo-agnostic.
- **AUTHOR PER-REPO** — thin config and wiring that encodes *your* repo's layout,
  secrets reference, and agent-runtime registration. Small, hand-written.
- **GENERATE** — produced once by a command, never hand-typed: the signing keypair
  and the drift baseline.
- **DROP / REPLACE** — 4thWAIV-specific content with zero portability. Delete it or
  swap in your own equivalent.

All paths are relative to repo root. The engine runs `.ts` directly under `bun`
(no build step), so "copy" means copy the `.ts` files as-is.

---

## COPY VERBATIM — the reusable engine

Copy the entire `agent-tools/` source tree (lib, hooks, scripts, security TS) plus
the signed-exemption *library* inside the ESLint plugin. Nothing in this column
encodes 4thWAIV's file layout — the layout lives in the JSON configs you author.

| Path | Role | Notes |
|------|------|-------|
| `agent-tools/lib/dispatcher.ts` | Single hook entry point; normalizes host payload, fans out to `hooks/<event>/*.ts`, any `deny` → exit 2 + stderr, default-deny on any hook failure | Copy as-is |
| `agent-tools/lib/types.ts` | `Hook`/`HookInput`/`HookContext`/`HostPayload`/`HookOutput` + token/capability contracts; snake→camel normalization | Copy as-is |
| `agent-tools/lib/protection-config.ts` | **The single source of truth for the protected-path policy.** Owns both glob tiers, the config loader + process cache, `effectiveProtectedGlobs`, and the shared match helpers. **Both hooks import from here — there is exactly one copy, no duplication.** | Copy as-is, then edit only the two in-source glob arrays per your repo (see AUTHOR row below) |
| `agent-tools/lib/paths.ts` | `isProtectedPath`, `normalizeToProjectRelative` (rejects `..`-escape), shell-quote `extractWriteTargets` (default-deny on tokenizer failure) | Copy as-is |
| `agent-tools/lib/baseline.ts` | `hashTree`/`captureTree`/`diffBaseline`; the **per-call snapshot store** — writes base64 file **content** (not hashes) under the in-repo dir `.protected-snapshots/<sha256(projectDir).slice>/` keyed by `tool_use_id`; `readPerCallContent` returns the base64 map the drift hook restores from | Copy as-is |
| `agent-tools/lib/workspace-discovery.ts` | Reads `package.json#workspaces`, emits protected globs per workspace; fails soft to `[]` | Copy as-is |
| `agent-tools/lib/bypass.ts` | **Runtime verify library** (public-key only): `canonicalizePayload`, `verifyTokenBlob`, `loadActiveTokens`, `bypassAllows` (guard routing + JSONL audit). Reads no private key | Copy as-is |
| `agent-tools/hooks/pre_tool_use/protected-files.ts` | Guard `protected_paths:no_write`; 4-source glob union; Bash write-target extraction; authorized-prefix + bypass overrides; writes the per-call content snapshot on allow | Copy as-is |
| `agent-tools/hooks/pre_tool_use/invocation-block.ts` | Guard `invocation_block:bash`; regex denylist matched against the **shell-normalized** command (tokenize + strip env-assignments + recurse into `bash -c`/`eval`), sharing the `paths.ts` normalizer | Copy as-is **only if the copy is already shell-aware**; a raw-string-matching version must be rebuilt to normalize first (see `invocation-block.md §3`) or the mint verbs are reachable via `bash -c "…"` wrappers |
| `agent-tools/hooks/post_tool_use/protected-files-drift.ts` | Guard `protected_paths:no_write` (the shared `PROTECT_GUARD`, same guard the pre-hook consults); per-call diff; one-token-covers-all-drift bypass; **on unauthorized drift REVERTS every changed/removed path to its captured pre-call bytes and deletes any file the call created** (not detect-only); always deletes the snapshot | Copy as-is |
| `agent-tools/security/bypass.ts` | **Human-only mint CLI** (mint/extend/add/revoke re-sign via `op read` private key; list/show/verify are public-key only) | Copy as-is; edit only its `op://` reference constant |
| `agent-tools/security/bypass-cli-args.ts` | Flag parsing for `--reason`/`--duration`/`--capability` with prompt fallback | Copy as-is |
| `agent-tools/security/bypass-display.ts` | Non-throwing JSON readers for list/show | Copy as-is |
| `agent-tools/scripts/hooks-setup.ts` | Idempotent installer: merges `.claude/settings.json` + writes `.codex/hooks.json` + computes baseline | Copy as-is |
| `agent-tools/scripts/hooks-verify.ts` | Tier-1 hard-fail if wiring missing/weakened; Tier-2 warn on hook-script drift | Copy as-is |
| `agent-tools/scripts/hooks-baseline.ts` | Regenerates `protected-baseline.json` from the JSON globs | Copy as-is |
| `agent-tools/scripts/scripts-modify.ts` | The only authorized editor of `package.json#scripts`; refreshes baseline after write | Copy as-is. If reconstructing: read `package.json`, mutate **only** the `scripts` key from CLI args (`--action=set\|remove\|rename --name=… --command=…`), atomically write it back (temp file + `rename`), then call `runHooksBaseline` to refresh `protected-baseline.json` — the same re-baseline pattern `hooks-baseline.ts` uses, so the drift hook doesn't flag the legitimate write. It refuses any mutation outside the `scripts` key |
| `agent-tools/tsconfig.json` | `allowImportingTsExtensions` + `noEmit` — the run-`.ts`-directly enabler | Copy; keep `extends: "../tsconfig.json"` satisfied by a root base |
| `agent-tools/package.json` | Manifest: no build step, deps `picomatch` + `shell-quote`, no `bin` | Copy; rename the `name` field |
| `agent-tools/**/*.test.ts` | The behavioral test suite for every engine file | Copy — it is your conformance harness on the new repo |
| **Signed-exemption library** (only if you keep a lint gate): `eslint-plugin-fourth-waiv/lib/{anchor-parser,exemption-signer,exemption-verifier,registry-loader,registry-schema,known-rules}.ts`, `cli/{lint-exempt,verify-config,id-minter}.ts` + `cli/verbs/*`, `rules/require-signed-exemption/index.js` | The Ed25519 gate machinery: parse `fw-exempt` tags, verify records against the same public key, mint 6-char Crockford ids, bind `(file, rule, anchor, count)` | Copy the **gate + signer + verifier + registry**; edit only the `op://` reference in `exemption-signer.ts`. See DROP row for the rule *catalog* |

**The two in-source glob arrays inside `protection-config.ts`** (copied with the
file, then tuned per repo):

- `TIER1_NEVER_BYPASS_GLOBS` — **two entries**, denied even with a valid bypass
  token: `.elves/bypass-tokens/**` (the token store — writing it would let the AI
  forge bypasses) and `.protected-snapshots/**` (the drift hook's restore source —
  writing it would let a poisoned snapshot restore attacker bytes). Keep both; only
  repoint the paths if your token/snapshot dirs differ.
- `HARDCODED_PROTECTED_GLOBS` — **directory globs** covering the whole engine so no
  single file slips outside protection: `agent-tools/hooks/**`, `agent-tools/lib/**`,
  `agent-tools/config/**`, `agent-tools/scripts/**`, `agent-tools/security/**`, the
  runtime-registration globs `.claude/**` and `.codex/**` (the hook configs, Layer-A
  guards, and the `codex_hooks = true` master switch — hardcoded so a Bash write
  cannot disable enforcement, since Layer A fires only on `Edit|Write|NotebookEdit`),
  plus the five signed-exemption lib files. Keep the `agent-tools/**` and
  `.claude/**` / `.codex/**` directory globs verbatim (they self-protect the engine,
  its wiring, the public key, and the dispatcher); repoint the runtime globs if you
  drive different agent runtimes, and drop the `eslint-plugin-fourth-waiv/*` entries
  if you are not carrying the lint gate.

---

## AUTHOR PER-REPO — config + wiring you write by hand

These encode *your* repo's file layout, secrets manager reference, and which agent
runtimes you drive. Small, hand-written, repo-specific.

| Path | What you author | Notes |
|------|-----------------|-------|
| `agent-tools/config/protected-paths.json` | The human-editable everyday protected-glob list (`{ "globs": [...] }`) — your repo's sensitive files (build config, CI, data-layer files, etc.) | This is the layer meant to change per repo. It is unioned *on top of* the two in-source tiers |
| `agent-tools/config/authorized-commands.json` | Bash prefix whitelist (`{ "prefixes": [...] }`) — the package-manager mutators and setup scripts your agent legitimately runs against protected paths | Consumed ONLY by the write guard, never by invocation-block |
| `agent-tools/config/invocation-blocked.json` | Regex denylist — at minimum the `bypass mint/extend/add/revoke` verbs, the `op read …<YourKeyRef>…`, the lint mint verbs, and any human-only scripts | Update the `op read` pattern to match *your* 1Password item name |
| `op://` reference constant in `security/bypass.ts` (and `exemption-signer.ts` if kept) | Point both signers at your vault/item: `op://<Vault>/<Item>/notesPlain` | Must match the item you create under GENERATE |
| `.claude/settings.json` | Claude hook registration (Layer-B dispatcher blocks are written by `hooks:setup`; add the Layer-A block by hand) | Commit it |
| `.claude/hooks/protect-locked-files.js` | Claude Layer-A standalone guard — hand-authored JS with a hardcoded `LOCKED_FILES` list for your repo; `exit(2)` to block. `hooks:setup` preserves but does not create it | Belt-and-suspenders first guard |
| `.codex/config.toml` | Codex feature flag `[features] codex_hooks = true` + Layer-A Python hook registration | Only if you drive Codex |
| `.codex/hooks/protect-locked-files.py` | Codex Layer-A guard; its own `LOCKED_FILES` list (keep in sync with the JS list by hand — no auto-sync exists) | Only if you drive Codex |
| Root `package.json` script aliases | `hooks:setup`, `hooks:verify`, `hooks:baseline`, `scripts:modify`, `bypass` (and `lint:exempt` if kept); make `check` run `hooks:verify` first; declare a `workspaces` array so workspace-discovery is meaningful | See the shipped aliases at root `package.json:25-29,79` |
| `CODEOWNERS`, `.githooks/pre-commit` (if you keep the lint half) | Lock the enforcement files to a human owner; local coupling check | Backed by branch-protection to have teeth |
| Deploy gate (Dockerfile / CI) | `RUN bun run hooks:setup` then `RUN bun run hooks:verify` before build | So CI cannot ship with disabled protection |

---

## GENERATE — produced by a command, never hand-typed

| Artifact | How | Disposition |
|----------|-----|-------------|
| `agent-tools/security/bypass-public-key.pem` + the private key | **Follow the `openssl` procedure in `bootstrap.md` Step 1** — `openssl genpkey -algorithm ed25519` then `openssl pkey -pubout`. Those two commands are the canonical path and produce exactly the PEMs the verifier expects: `genpkey` emits the **PKCS#8** private key (paste into a 1Password Secure Note `notesPlain`, never on disk) and `pkey -pubout` emits the **SPKI** public key (commit it — it is the trust anchor). The `generateKeyPairSync("ed25519")` node API with `publicKeyEncoding: { type: "spki" }` / `privateKeyEncoding: { type: "pkcs8" }` is an equivalent alternative that yields interchangeable PEMs, but you do **not** need to write a script for it — bootstrap's openssl commands suffice | **Generate fresh — never reuse 4thWAIV's key.** There is no repo script for this by design; it is a manual human act outside the agent's reach |
| `agent-tools/config/protected-baseline.json` | `bun run hooks:baseline` (also runs inside `hooks:setup`) — a committed `{version, computedAt, files{path:sha256}}` manifest over the JSON globs, used by setup/verify for out-of-band tamper checks | Generate on the target repo; commit it. Distinct from the runtime per-call snapshot |
| `.elves/bypass-tokens/*.token`, `.elves/bypass-audit.log` | Created at runtime by the human mint CLI and the runtime verifier | Do not copy 4thWAIV's; they are grant state |
| `.protected-snapshots/**` | Created at runtime by the pre-hook, deleted by the post-hook | Never commit; add to `.gitignore`. Keep it Tier-1 write-blocked |

---

## DROP / REPLACE — 4thWAIV-specific, no portability

| Path | Why it is 4thWAIV-only | Action |
|------|------------------------|--------|
| `eslint-plugin-fourth-waiv/rules/*` **except** `require-signed-exemption/` | The ~53 domain rules (native-HTML lockdown, doc-typed read/write, SecuredField, BlockNote, RHF/Recharts/Radix import locks) are React/TS product content with zero applicability elsewhere | Drop the catalog; keep only the gate rule. Author a fresh protected-rule catalog your codebase actually needs |
| `eslint-plugin-fourth-waiv/lib/doc-type-detection.*` | 4thWAIV doc-typing heuristic used by domain rules | Drop unless you keep the domain rules |
| Root `build:schema` step (`package.json:30,35`) and the `postinstall` playwright step | 4thWAIV app build, unrelated to governance | Remove from the copied root `package.json` |
| `.claude/config` values naming `4thWAIV_Engineering` / `ProtectedFilesBypassSigningKey` / `@tistocks` | Vault, item, and owner identifiers | Replace with yours |
| `agent-tools/UAT.md`, `docs/DEV-SETUP.md`, `.elves/rules/55-build-infrastructure.md` | 4thWAIV-specific narrative docs (stale file counts, repo-specific error catalog) | Drop or rewrite for your repo |
| The whole `eslint-plugin-fourth-waiv/` tree, `turbo.json` lint-coupling, `scripts/validate-turbo-lint-coupling.js` | Presupposes a turbo monorepo | Drop entirely if you do not want the lint half; the protected-files + bypass core stands alone without it |

---

## Minimal viable copy (protected-files + bypass core, no lint gate)

If you want only the "AI cannot write locked paths / cannot self-authorize" core and
skip lint gating entirely, the copy set collapses to: the whole `agent-tools/lib/**`,
`agent-tools/hooks/**`, `agent-tools/scripts/**`, `agent-tools/security/*.ts`,
`agent-tools/tsconfig.json`, `agent-tools/package.json` (COPY); the three
`agent-tools/config/*.json` + the Claude/Codex Layer-A guards + root script aliases +
deploy gate (AUTHOR); the keypair + baseline (GENERATE). Drop the entire
`eslint-plugin-fourth-waiv/` tree and remove the `eslint-plugin-fourth-waiv/*` entries
from `HARDCODED_PROTECTED_GLOBS`.
