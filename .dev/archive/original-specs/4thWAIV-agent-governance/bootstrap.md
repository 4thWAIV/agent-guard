# Bootstrap: Standing the Governance System Up in a Fresh Repo

This is the ordered setup. Run the steps in the sequence given — later steps
read artifacts earlier steps create. When you finish, an AI agent editing this
repo is blocked from writing any protected path unless it presents a signed
bypass token, and every protected write is snapshotted and reverted on
unauthorized drift.

The moving parts you assemble here:

1. An **Ed25519 signing keypair** — the private half lives only in a human
   secret store; the public half is committed to the repo.
2. Three **config files** under `agent-tools/config/` — `protected-paths.json`,
   `invocation-blocked.json`, `authorized-commands.json`.
3. The **hook wiring** — `hooks:setup` writes dispatcher entries into
   `.claude/settings.json` and `.codex/hooks.json`.
4. The **baseline** — `hooks:setup` (or `hooks:baseline`) records a sha256 of
   every protected file for drift detection.
5. **Verification** — `hooks:verify` confirms the wiring is present and the
   baseline is clean.

Assumptions: `bun` is the runtime, the enforcement code already lives under
`agent-tools/` (hooks, lib, scripts, security), and the root `package.json`
exposes the script aliases shown below.

---

## Root `package.json` script aliases

Every command in this guide goes through a root alias. Add these to the root
`package.json` `scripts` block so the setup commands resolve regardless of cwd:

```json
{
  "scripts": {
    "hooks:setup":    "bun agent-tools/scripts/hooks-setup.ts",
    "hooks:verify":   "bun agent-tools/scripts/hooks-verify.ts",
    "hooks:baseline": "bun agent-tools/scripts/hooks-baseline.ts",
    "scripts:modify": "bun agent-tools/scripts/scripts-modify.ts",
    "bypass":         "bun agent-tools/security/bypass.ts"
  }
}
```

`bun run hooks:setup`, `bun run hooks:verify`, `bun run hooks:baseline`, and
`bun run scripts:modify` must ALSO appear as authorized prefixes in
`authorized-commands.json` (Step 3) — otherwise the very hook they install would
block their own protected writes.

---

## Step 0 — Install prerequisites (the system will not run without these)

Do this **before** anything else. The enforcement code imports two runtime deps
and runs `.ts` directly under `bun`; skip these and the hooks throw at import (so
every tool call default-denies) or don't type-check.

1. **Runtime dependencies.** `isProtectedPath` needs `picomatch`;
   `extractWriteTargets` needs `shell-quote`. Install them into `agent-tools/`:

   ```bash
   cd agent-tools && bun add picomatch shell-quote && cd -
   # dev types too, if you type-check: bun add -d @types/picomatch @types/shell-quote @types/node
   ```

2. **`agent-tools/tsconfig.json`** — the run-`.ts`-directly enabler. Author it so
   `bun` executes the `.ts` files with no build step:

   ```json
   {
     "extends": "../tsconfig.json",
     "compilerOptions": {
       "allowImportingTsExtensions": true,
       "noEmit": true
     }
   }
   ```

   `allowImportingTsExtensions` is what lets the hooks import each other as
   `./paths.ts` (with the extension); `noEmit` is what makes it a type-check-only
   config. `extends: "../tsconfig.json"` must resolve to a root base config.

3. **Gitignore the snapshot store.** `.protected-snapshots/` is an in-repo,
   Tier-1-protected runtime scratch store created by the pre-hook and deleted by
   the post-hook — it must never be committed:

   ```bash
   echo ".protected-snapshots/" >> .gitignore
   ```

---

## Step 1 — Generate the Ed25519 keypair; commit the public half

The bypass system is asymmetric. The **private** key signs tokens and is used
ONLY by a human running `bun run bypass mint|extend|add|revoke`. The **public**
key verifies tokens and is the only half the repo (and any agent) ever sees.

`agent-tools/lib/bypass.ts` resolves the public key from a fixed path:

```
agent-tools/security/bypass-public-key.pem   (defaultPublicKeyPath)
```

Generate the pair and write the public half into that exact file:

```bash
# Private key — do NOT commit this; it goes to the secret store, then is deleted.
openssl genpkey -algorithm ed25519 -out /tmp/bypass-private-key.pem

# Public key — this IS committed.
openssl pkey -in /tmp/bypass-private-key.pem -pubout \
  -out agent-tools/security/bypass-public-key.pem
```

The committed public key is a 3-line PEM, e.g.:

```
-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEA...=
-----END PUBLIC KEY-----
```

**Store the private key in a human-only secret store.** `bun run bypass mint`
reads it at call time via the 1Password CLI and never writes it to disk, never
exports it to an env var, and discards it at process exit. The reference is
hardcoded in `agent-tools/security/bypass.ts` as `DEFAULT_OP_REFERENCE`:

```
op://4thWAIV_Engineering/ProtectedFilesBypassSigningKey/notesPlain
```

Paste the full private-key PEM into the `notesPlain` field of a 1Password item
at that vault/item path (or edit `DEFAULT_OP_REFERENCE` to your own vault/item),
then wipe the local copy:

```bash
op read "op://<vault>/<item>/notesPlain"   # confirm it round-trips
rm /tmp/bypass-private-key.pem
```

The mint path reads it like this (`readPrivateKey` in `bypass.ts`):

```ts
const child = spawn("op", ["read", ref], { stdio: ["ignore", "pipe", "pipe"] });
// ... createPrivateKey({ key: keyPem, format: "pem" })
```

Tests substitute the key via `BYPASS_TEST_PRIVATE_KEY_PEM`; production always
goes through `op read`.

---

## Step 2 — Author `protected-paths.json`

`agent-tools/config/protected-paths.json` holds a single `globs` array of
picomatch patterns. The PreToolUse hook blocks agent writes to anything
matching; the PostToolUse hook re-checks these after the write and reverts
drift. Both hooks load this file through the ONE shared module
`agent-tools/lib/protection-config.ts` (`loadConfig`); there is no second copy.

```json
{
  "$schema": "./protected-paths.schema.json",
  "globs": [
    "package.json",
    "turbo.json",
    "eslint.config.js",
    ".claude/settings.json",
    ".claude/settings.local.json",
    ".codex/hooks.json",
    ".codex/config.toml",
    ".git/hooks/**",
    ".git/config",
    "agent-tools/hooks/**",
    "agent-tools/lib/**",
    "agent-tools/scripts/**",
    "agent-tools/config/**",
    "agent-tools/security/**",
    ".elves/bypass-tokens/**"
  ]
}
```

Tune the list to your repo (add the lint-rule files, data-layer files, etc. you
want frozen). Two things to understand about how this interacts with the
in-source policy in `protection-config.ts`, so you author it correctly:

- **You do not need to — and cannot — rely on this JSON alone for the
  enforcement engine's own files.** `HARDCODED_PROTECTED_GLOBS` in
  `protection-config.ts` already protects the enforcement directories as
  DIRECTORY globs — `agent-tools/hooks/**`, `agent-tools/lib/**`,
  `agent-tools/config/**`, `agent-tools/scripts/**`, `agent-tools/security/**`.
  Those stay protected even if someone deletes them from this JSON
  (JSON-removal-resistant). Listing them here too is redundant but harmless.
- **`.elves/bypass-tokens/**` and `.protected-snapshots/**` are Tier-1** —
  `TIER1_NEVER_BYPASS_GLOBS` in `protection-config.ts` denies writes to them
  even with a valid bypass token, because writing the token dir would let an
  agent forge its own bypasses and writing the snapshot dir would let it poison
  the bytes the drift hook restores from. You never author `.protected-snapshots`
  anywhere — the hook process creates it directly via `fs`.

Do NOT delete `agent-tools/config/**` from this list: keeping the config
directory protected is what stops an agent from editing these very files to
weaken the policy.

**Never remove the runtime-wiring globs, and do not rely on this JSON to protect
them.** The files that register the engine with the runtimes —
`.claude/settings.json`, `.claude/hooks/**`, `.codex/hooks.json`,
`.codex/config.toml`, `.codex/hooks/**` — are what make enforcement fire at all.
If their protection lived *only* in this shrinkable JSON, an agent could
`sed -i` or redirect into `.claude/settings.json` (or flip `codex_hooks = false`
in `.codex/config.toml`) with a single Bash write and go dark — and Layer A
fires on `Edit|Write|NotebookEdit`, **not** `Bash`, so it would not catch that
write. Author them into the in-source `HARDCODED_PROTECTED_GLOBS`
(`policy-module.md §2` adds `.claude/**` and `.codex/**` for exactly this
reason) so the registration is protected in source and cannot be removed by
editing config. Listing them here too is belt-and-suspenders, not the load-bearing
layer.

---

## Step 3 — Author `authorized-commands.json`

`agent-tools/config/authorized-commands.json` is the Bash-command prefix
allowlist. The PreToolUse hook permits a write to a protected path when the
tool's `command` starts with one of these prefixes (prefix match is on the
parsed token stream, so extra args after the prefix still match). Loaded by
`loadConfig` as `authorizedPrefixes`.

```json
{
  "$schema": "./authorized-commands.schema.json",
  "prefixes": [
    "bun run hooks:setup",
    "bun run hooks:verify",
    "bun run hooks:baseline",
    "bun run scripts:modify",
    "bun run lint:exempt",
    "bun add",
    "bun remove",
    "bun install",
    "bun update",
    "npm install",
    "npm i",
    "npm uninstall",
    "yarn add",
    "yarn remove"
  ]
}
```

Rule of thumb: a command belongs here when it legitimately writes a protected
file as a side effect (dependency installers touch `package.json`; the hook
scripts touch config and the baseline). This file lives inside
`agent-tools/config/**`, so the agent cannot add itself to the allowlist.

---

## Step 4 — Author `invocation-blocked.json`

`agent-tools/config/invocation-blocked.json` denies Bash invocations by regex,
independent of any file write. It is what stops an agent from running the
human-only bypass-minting and exemption-signing subcommands. The PreToolUse
invocation-block hook (`agent-tools/hooks/pre_tool_use/invocation-block.ts`)
tests each `patterns` entry as a JavaScript regex against the command string.

```json
{
  "patterns": [
    "^bun run bypass (mint|extend|add|revoke)\\b",
    "^bun agent-tools/security/bypass\\.ts (mint|extend|add|revoke)\\b",
    "^op read .*ProtectedFilesBypassSigningKey.*"
  ]
}
```

Add a pattern for every human-only subcommand your repo exposes (exemption
minting, config signing, and the `.tmp/user_execute.sh` operator script are the
common additions). A bypass token carrying the guard `invocation_block:bash`
overrides these via the dispatcher, so a human can still authorize one when
needed.

---

## Step 5 — Run `hooks:setup` to wire the runtimes and write the first baseline

```bash
bun run hooks:setup
```

`agent-tools/scripts/hooks-setup.ts` (`runHooksSetup`) is idempotent and does
four things in order:

1. Reads `agent-tools/config/protected-paths.json`.
2. Merges dispatcher entries into `.claude/settings.json` under both
   `PreToolUse` and `PostToolUse`, for two matchers each —
   `Edit|Write|MultiEdit|NotebookEdit|apply_patch` and `Bash`. Existing hook
   entries are preserved; ours are upserted only if absent. The command it
   writes is cwd-resistant:

   ```
   bun "${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts" pre_tool_use
   bun "${CLAUDE_PROJECT_DIR}/agent-tools/lib/dispatcher.ts" post_tool_use
   ```

3. Writes `.codex/hooks.json` with the equivalent `PreToolUse`/`PostToolUse`
   shape (same matchers, same dispatcher commands) so Codex's `apply_patch`
   flows are gated too.
4. Calls `hashTree()` over the protected globs and writes
   `agent-tools/config/protected-baseline.json` (a project-relative-path →
   sha256 map). Setup prints the file count.

Re-running is safe: if both settings files already carry our hooks and the
baseline exists, it prints `nothing to do — already installed` and exits 0.

After this step the two runtime configs exist and the baseline reflects the
current bytes of every protected file. If you edit `protected-paths.json` later,
re-hash with:

```bash
bun run hooks:baseline
```

`hooks-baseline.ts` re-hashes the current `globs` and rewrites
`protected-baseline.json`, printing the new and previous file counts. Run it
after ANY authorized write to a protected file, or the drift hook will keep
reverting to stale bytes.

---

## Step 6 — Verify

```bash
bun run hooks:verify
```

`agent-tools/scripts/hooks-verify.ts` (`runHooksVerify`) is the acceptance gate.
It runs two tiers:

**Tier 1 — hard-fail (exit 1):**
- `.claude/settings.json` missing, malformed, or lacking dispatcher entries for
  BOTH `PreToolUse` and `PostToolUse` across BOTH matchers.
- `.codex/hooks.json` with the same requirement.
- `.codex/config.toml` missing, or without `[features] codex_hooks = true`, or
  without the Layer-A `[[hooks.PreToolUse]]` registration, or with either
  Layer-A guard script absent. `codex_hooks = true` is the master switch for
  every Codex hook — a green verify that skips this check is not proof Codex
  enforcement is live (see `hook-wiring.md` Verifier section).

Both the canonical `${CLAUDE_PROJECT_DIR}`-prefixed command form and the legacy
bare-relative form (`bun agent-tools/lib/dispatcher.ts <event>`) are accepted,
so a repo that hasn't re-run setup after the cwd-resistance upgrade still passes.

**Tier 2 — warn (stderr, exit 0):**
- Any file under `agent-tools/hooks/**`, `agent-tools/lib/**`, or
  `agent-tools/scripts/**` whose sha256 differs from the recorded baseline. This
  is observability-only for the enforcement engine's own source; it does not
  fail the command. A missing baseline is reported here as a WARN pointing you
  back at `hooks:setup`.

A clean run prints, on stdout:

```
hooks-verify: ok: .claude/settings.json has dispatcher hooks for PreToolUse + PostToolUse
hooks-verify: ok: .codex/hooks.json has dispatcher hooks for PreToolUse + PostToolUse
hooks-verify: ok: hook scripts match recorded baseline (no drift)
hooks-verify: completed in <n>ms (exit 0)
```

Wire `hooks:verify` into your CI/CD gate (e.g. a `check` task) so a repo that
ships with the hooks stripped out fails fast. Its performance budget is under
200ms.

---

## Ordered checklist

```
0a. cd agent-tools && bun add picomatch shell-quote           (runtime deps — hooks throw at import without them).
0b. Author agent-tools/tsconfig.json (extends ../tsconfig.json, allowImportingTsExtensions:true, noEmit:true).
0c. echo ".protected-snapshots/" >> .gitignore                (never commit the Tier-1 snapshot store).
1.  Add root package.json script aliases (hooks:setup/verify/baseline, scripts:modify, bypass).
2.  openssl genpkey ed25519 → private key.
3.  openssl pkey -pubout → agent-tools/security/bypass-public-key.pem  (commit).
4.  Put private-key PEM in 1Password notesPlain at DEFAULT_OP_REFERENCE; rm local copy.
5.  Author agent-tools/config/protected-paths.json      (globs).
6.  Author agent-tools/config/authorized-commands.json  (prefixes — include the hook scripts).
7.  Author agent-tools/config/invocation-blocked.json   (patterns — human-only subcommands).
8.  Author .claude/hooks/protect-locked-files.js, .codex/config.toml (codex_hooks=true + Layer-A), .codex/hooks/protect-locked-files.py.
9.  bun run hooks:setup     → wires .claude + .codex Layer-B, writes protected-baseline.json.
10. bun run hooks:verify    → expect exit 0, three "ok:" lines + the .codex/config.toml master-switch check.
```

Once verify is green, the guarantee holds: an agent tool call that targets a
protected path is blocked pre-write unless it carries a valid bypass token, and
the post-write drift hook restores any protected file (and deletes any protected
file it created) back to its pre-call bytes from the in-repo
`.protected-snapshots/` store.
