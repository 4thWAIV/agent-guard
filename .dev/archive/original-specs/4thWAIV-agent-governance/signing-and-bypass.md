# The Ed25519 Signing / Bypass Subsystem

This subsystem lets a human grant a scoped, time-limited, cryptographically-signed
exception to the agent-governance guards. A guard (a hook) that would otherwise
block a call first asks: *is there an active token that authorizes exactly this
call?* Only the human holds the private key, so only the human can mint a token;
the agent can verify, list, and honor tokens but can never create one.

The design splits cleanly into three parts:

1. **A runtime verify library** (`agent-tools/lib/bypass.ts`) — public-key only.
   Imported by every guard. Loads active tokens, verifies signatures, checks
   expiry, routes capabilities by guard name, and writes the audit trail.
2. **A human-only mint CLI** (`agent-tools/security/bypass.ts` +
   `agent-tools/security/bypass-cli-args.ts`) — reads the private key via
   `op read` (1Password) exactly once per invocation to sign new tokens.
3. **A committed public key** (`agent-tools/security/bypass-public-key.pem`) —
   the anchor of trust, checked into the repo. It verifies signatures; it cannot
   produce them.

Crypto is `node:crypto` ed25519 (`sign`/`verify` with algorithm `null`). No
external crypto dependency.

---

## 1. Keypair generation — the human one-off

This is done **once, by a human**, before the system is usable. It is the only
step that mints the private key, and it must never be scripted into agent-run
tooling.

Generate an ed25519 keypair, commit the public half, and store the private half
in a secret manager (this project uses 1Password):

```bash
# Generate the private key (PKCS#8 PEM) and derive the public key.
openssl genpkey -algorithm ed25519 -out bypass-private-key.pem
openssl pkey -in bypass-private-key.pem -pubout -out bypass-public-key.pem

# Commit ONLY the public key.
cp bypass-public-key.pem agent-tools/security/bypass-public-key.pem

# Store the private key in the secret manager, then destroy the local copy.
op item create ... # store bypass-private-key.pem contents
rm bypass-private-key.pem
```

The committed public key looks like:

```
-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEA...
-----END PUBLIC KEY-----
```

**The private key must never touch the repo, the agent's environment, or disk
outside the secret manager.** The mint CLI reads it transiently and discards it
(see §7). An AI agent cannot perform this step and cannot mint tokens, because it
has no access to the private key.

The 1Password reference the CLI reads by default is defined in
`agent-tools/security/bypass.ts`:

```ts
const DEFAULT_OP_REFERENCE =
  "op://4thWAIV_Engineering/ProtectedFilesBypassSigningKey/notesPlain";
```

Point this at wherever the private key PEM lives in your secret manager.

---

## 2. Token format and the canonical payload

A token is a JSON file `<id>.token` under `.elves/bypass-tokens/`. Its shape
(from `agent-tools/lib/types.ts`):

```ts
interface Token {
  payload: TokenPayload;
  signature: string; // base64 ed25519 signature over the canonicalized payload
}

interface TokenPayload {
  id: string;          // randomUUID
  issuedAt: string;    // ISO 8601
  expiresAt: string;   // ISO 8601 — single expiry per token
  reason: string;      // human-supplied justification, surfaced in list/audit
  capabilities: BypassCapability[];
}

interface BypassCapability {
  guard: string;       // must equal the guard name a hook passes to bypassAllows
  args: BypassArgs;    // "*" wildcard sentinel, or a guard-specific JSON object
}

type BypassArgs = "*" | Record<string, unknown>;
```

`expiresAt` lives at the payload top level: one expiry per token. Different
durations require separate tokens.

On disk the file is pretty-printed JSON (`JSON.stringify(token, null, 2)`), e.g.:

```json
{
  "payload": {
    "id": "3f2c9a1e-...",
    "issuedAt": "2026-07-23T18:00:00.000Z",
    "expiresAt": "2026-07-23T20:00:00.000Z",
    "reason": "hotfix to protected config for incident #412",
    "capabilities": [
      { "guard": "protected_paths:no_write", "args": { "file": "config/app.json" } }
    ]
  },
  "signature": "base64..."
}
```

### The canonical payload (what gets signed and verified)

Signer and verifier must hash byte-identical bytes. The signed message is the
payload serialized with keys sorted alphabetically at every depth; array order is
preserved (it is semantic). This is `canonicalizePayload` in `lib/bypass.ts`:

```ts
export function canonicalizePayload(payload: TokenPayload): string {
  return JSON.stringify(sortKeys(payload));
}
```

`sortKeys` recurses: arrays map through unchanged in order; objects rebuild with
`Object.keys(rec).sort()`; primitives pass through. The signature is computed
over `Buffer.from(canonicalizePayload(payload), "utf8")`. The on-disk
pretty-printed JSON is **not** what gets signed — only the canonical form is.
This is what lets the verifier re-derive identical bytes regardless of key order
or whitespace in the stored file.

---

## 3. The runtime verify library (`lib/bypass.ts`)

Public-key only. The private key is never imported here.

### Verifying one token blob

`verifyTokenBlob(raw, publicKey, { now? })` returns a discriminated
`VerifyResult`:

```ts
type VerifyResult =
  | { status: "valid"; token: Token }
  | { status: "expired"; token: Token }
  | { status: "invalid-signature"; reason: string }
  | { status: "malformed"; reason: string };
```

The steps, in order:

1. `JSON.parse(raw)` → `malformed` on failure.
2. `parseTokenShape` validates structure: non-empty `signature` string; `payload`
   object with string `id` (non-empty), `issuedAt`, `expiresAt`, `reason`, and a
   `capabilities` array where each item has a non-empty `guard` and `args` that is
   either the string `"*"` or a non-array JSON object. Any violation → `malformed`
   with a precise reason.
3. Signature check: decode `signature` base64, rebuild the canonical payload
   bytes, and call
   `cryptoVerify(null, payloadBytes, publicKey, sigBytes)`. Any throw or a false
   result → `invalid-signature`.
4. Expiry: `Date.parse(expiresAt)`; unparseable → `malformed`; `expiresAt <= now`
   → `expired`; otherwise → `valid`.

Capability guard names are **not** policed here. A multi-guard token verifies fine
in any context; guard routing happens at match time (§4). `verifyTokenFile` wraps
this with a file read.

### Loading all active tokens

`loadActiveTokens(projectDir, opts)` reads every `*.token` under
`.elves/bypass-tokens/` (sorted), verifies each, and returns only the ones whose
`status === "valid"` (signature-valid AND not expired) as
`{ token, filePath }[]`. Malformed / expired / bad-signature files are silently
dropped — a failed load is not itself an audit event. A missing directory returns
`[]`.

Default paths (all overridable for tests):

```ts
defaultTokenDir(p)      → <p>/.elves/bypass-tokens
defaultPublicKeyPath(p) → <p>/agent-tools/security/bypass-public-key.pem
defaultAuditLogPath(p)  → <p>/.elves/bypass-audit.log
```

`loadPublicKey(path)` parses the PEM into a `KeyObject` via `createPublicKey`.

---

## 4. Capability / guard / args scoping — `bypassAllows`

This is the one function each guard calls before it returns `deny`. **The guard
owns the check, not a central dispatcher.**

```ts
const bp = await bypassAllows({
  guardName: GUARD_NAME,                          // this guard's identity
  argsCheck: (args) => argsCoversTarget(args, rel, projectDir), // this guard's vocabulary
  projectDir,
  toolName,
  target,                                          // optional, for audit
  command,                                         // optional, for audit
});
if (bp.matched) { /* allow */ } else { /* block */ }
```

Algorithm (in `bypassAllows`):

1. Load active tokens. If none, return `{ matched: false }`.
2. For each token, for each capability:
   - Skip capabilities whose `guard !== guardName` (guard-name routing).
   - Call the guard's `argsCheck(cap.args)`. A throw counts as no-match.
   - On the first match: write a `bypass_used` audit entry (unless `skipAudit`),
     then return `{ matched: true, tokenId, capability }`.
3. If nothing matched, return `{ matched: false }` and the guard proceeds to
   block.

**The library never inspects `args` content itself.** Each guard supplies its own
`argsCheck` predicate, so each guard defines its own args vocabulary. Two real
examples:

The path guards accept `"*"` (any target this guard would block), `{ file: "<rel
path>" }` (exact project-relative match), or `{ glob: "<glob>" }` (target matches
that glob) — `argsCoversTarget` in `agent-tools/lib/protection-config.ts`:

```ts
export function argsCoversTarget(args, target, projectDir) {
  if (args === "*") return true;
  const fileArg = args["file"];
  if (typeof fileArg === "string") { /* exact rel-path match */ }
  const globArg = args["glob"];
  if (typeof globArg === "string") return isProtectedPath(target, projectDir, [globArg]);
  return false;
}
```

The invocation-block guard accepts `"*"`, `{ regex: "<re>" }` (command matches the
regex), or `{ command: "<exact>" }` — `argsCoversCommand` in
`agent-tools/hooks/pre_tool_use/invocation-block.ts`. Guard names in play today:
`protected_paths:no_write` and `invocation_block:bash`. Both the PreToolUse block
hook (`protected-files.ts`) and the PostToolUse drift hook
(`protected-files-drift.ts`) consult the single `protected_paths:no_write` guard
(exported as `PROTECT_GUARD` from `agent-tools/lib/protection-config.ts`), so one
capability authorizes both the write and the drift-revert — a token can never be
enough for the write but short of the revert.

The wildcard `"*"` is the broadest grant a capability can carry: it matches any
call the named guard would block. Narrow to `{ file }` / `{ glob }` / `{ command }`
/ `{ regex }` to scope tightly.

---

## 5. Expiry / TTL

Duration is supplied at mint time as a human string and converted to
`expiresAt = issuedAt + durationMs`. `parseDuration` (in `security/bypass.ts`)
accepts `<n><unit>` where unit is `s | m | h | d`:

```
"30m" → 1_800_000 ms   "2h" → 7_200_000    "24h"    "7d" → 604_800_000
```

`n` must be a positive integer; anything else (bad unit, zero, non-numeric)
returns `null` and the mint is rejected with a usage error. Expiry is enforced on
every verify (`verifyTokenBlob`): a token past `expiresAt` returns `expired` and
is dropped by `loadActiveTokens`, so it silently stops granting bypass — no
revoke needed for the common case. `extend` re-signs with a fresh
`expiresAt = now + newDuration`.

---

## 6. The audit log

Append-only JSONL at `.elves/bypass-audit.log`, one entry per line, written by
`appendAuditEntry`. Each line is `{ at: <ISO>, ...entry }`, appended with
`O_APPEND` so concurrent writers (mint, bypass-used, revoke) don't interleave
bytes. The entry is a discriminated union (`AuditEntry`); every kind carries the
fields required for that kind:

```
granted           tokenId, reason, capabilitiesCount, expiresAt
extended          tokenId, newExpiresAt
capability_added  tokenId, guard
revoked           tokenId, reason?
bypass_used       tokenId, guard, toolName, target?, command?
expired           tokenId
```

`granted` / `extended` / `capability_added` / `revoked` are written by the mint
CLI. `bypass_used` is written by the library the moment a guard's block is
overridden by a token — this is the record that a protection was actually
bypassed at runtime, with the tool and target/command that triggered it. Audit
writes never block a bypass: if the append throws, the match still wins.

---

## 7. The human-only mint CLI

Single Bun binary, run via `bun run bypass <subcommand>` (mapped in
`package.json` to `bun agent-tools/security/bypass.ts`). Seven subcommands split
by whether they need the private key:

**Human-only (read the private key via `op read`; on the agent's
invocation-block list, matched shell-normalized so the wrapped forms
—`bash -c "…"`, `eval '…'`, leading `FOO=bar` env-assignments— are blocked too,
not just the bare command):**

- `mint` — create a new token.
- `extend <id>` — re-sign with a new expiry.
- `add <id>` — append a capability and re-sign.
- `revoke <id>` — delete the token file (writes a `revoked` audit entry).

**Agent-OK (public-key only, no private key):**

- `list` — one line per token: short id, verify status, cap count, expiry, reason.
- `show <id>` — full status + pretty-printed payload.
- `verify [<id>]` — verify one or all tokens; exit 0 if all valid, else 1.

`revoke` is grouped with the human-only set operationally, but note it does not
itself read the private key — it only unlinks a file. It is gated to the human by
policy, not by a crypto requirement.

### Minting

`handleMint`:

1. Parse flags via `parseMintFlags` (`bypass-cli-args.ts`). Recognized:
   `--reason <text>`, `--duration <30m|2h|...>`, `--capability <json>`
   (repeatable). `--flag=value` form is also accepted. Unknown flags are a hard
   error (exit 2) so a typo doesn't silently fall through to a prompt.
2. Any omitted flag falls back to an interactive prompt. `--capability` JSON shape
   is `{"guard":"<name>","args":"*"|{...}}`; `promptCapabilities` loops guard +
   args interactively. At least one capability and a non-empty reason are required.
3. Build the payload: `id = randomUUID()`, `issuedAt = now`,
   `expiresAt = now + parseDuration(duration)`, `reason`, `capabilities`.
4. `readPrivateKey(ctx)` → `signToken(payload, privateKey)` →
   `signature = cryptoSign(null, canonicalBytes, privateKey).toString("base64")`.
5. Write `<id>.token` to the token dir and append a `granted` audit entry.

`extend` and `add` load the existing token by id-prefix (`findTokenFileById`
matches exact id or prefix), rebuild the payload, re-sign, rewrite the file, and
append the matching audit entry. Because the payload changed, the old signature is
invalid — re-signing is mandatory, which is why `extend`/`add` are also
human-only.

### The private key path — what the AI cannot do

`readPrivateKey` (in `security/bypass.ts`) is the only place the private key is
touched:

```ts
const ref = opts.opReference ?? DEFAULT_OP_REFERENCE;
const child = spawn("op", ["read", ref], ...);   // 1Password CLI, once per call
// ...on nonzero exit: throw with guidance to sign in / check the ref
return createPrivateKey({ key: keyPem, format: "pem" });
```

The key is read via `op read` **exactly once per invocation and discarded**. The
CLI never writes it to disk, never exports it to an env var, never caches it past
process exit. Tests inject a key via `BYPASS_TEST_PRIVATE_KEY_PEM` /
`privateKeyPemOverride`; production always goes through `op read`.

**Minting, extending, and adding capabilities all require the private key, and
the private key is only reachable through the human's authenticated 1Password
session.** Two barriers keep that out of the agent's reach, and both are
load-bearing:

1. The **invocation-block guard** denies the mint verbs and the `op read` of the
   signing key — matched against the **shell-normalized** command
   (`invocation-block.md §3`), so `bash -c "…"` / `eval '…'` / `FOO=1 …`
   wrappers are blocked, not just the bare form. A raw-string denylist would let
   the agent read the key or mint a token through a `bash -c` wrapper.
2. The **1Password session** must require **per-invocation authentication**
   during an agent run. A cached or biometric "don't ask again" window turns an
   already-unlocked session into an ambient capability the agent can spend the
   instant any gap in the block appears.

So the accurate statement is not that the agent is *physically incapable* of
running these verbs; it is that a shell-aware denylist plus a per-invocation-auth
secret store together put minting out of reach. The agent can only run
`list` / `show` / `verify`, and can only *benefit* from a token a human already
minted. When the agent hits a block it cannot pass, the correct move is to ask the
human to mint a scoped token for the relevant guard — never to attempt the mint
itself.

### Exit-code contract

`RunResult.exitCode` is `0 | 1 | 2`:

- `0` — success (or, for `verify`, all tokens valid).
- `1` — token not found by id, or `verify` found at least one non-valid token.
- `2` — usage / flag-parse error, missing required input, unparseable duration,
  unknown subcommand.

The top-level `catch` also exits `2` on any thrown error (e.g. `op read` failure).

---

## 8. Testing the subsystem

- **Signature round-trip**: sign a payload with a test private key
  (`BYPASS_TEST_PRIVATE_KEY_PEM`), verify with the matching public key → `valid`.
  Tamper one payload byte → `invalid-signature`.
- **Canonicalization**: two payloads with the same content but different key order
  must produce the same signature and both verify.
- **Expiry**: pass `now` past `expiresAt` → `expired`; before → `valid`.
- **Shape**: feed malformed JSON, missing fields, `args` that is an array →
  `malformed` with the specific reason.
- **Guard routing**: a token with guard `A` must not satisfy a `bypassAllows` call
  with `guardName: "B"`, even when `argsCheck` would return true.
- **argsCheck ownership**: `"*"` matches; `{ file }` matches only the exact
  rel-path; a throwing predicate counts as no-match.
- **Audit**: a successful match appends exactly one `bypass_used` line; an audit
  write failure does not change the `{ matched: true }` result.
- **CLI**: inject streams + `privateKeyPemOverride`, assert exit codes for the
  contract in §7 and that `mint` writes both a token file and a `granted` audit
  line.

Reference tests: `agent-tools/security/bypass.test.ts`,
`agent-tools/security/bypass.flags.test.ts`,
`agent-tools/hooks/pre_tool_use/protected-files.test.ts`,
`agent-tools/hooks/pre_tool_use/invocation-block.test.ts`.
