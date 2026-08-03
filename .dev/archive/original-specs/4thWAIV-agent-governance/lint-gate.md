# The Lint-Rule Enforcement + Signed-Exemption Gate

This gate answers one question at build time: *is this suppression of a lint
rule cryptographically authorized?* An agent can add `// eslint-disable-next-line`
to silence a rule the same way it edits any other line of code — so a disable
directive is worth exactly nothing as a control unless every disable is verified
against a signed record. This document tells you how to build that verification:
a custom ESLint rule that treats an unsigned disable of a protected rule as a
lint error, a signing/minting CLI that only a human can run, and a structural
check that stops the lint step from being silently skipped at build time.

The signing here is **the same crypto as the bypass subsystem** (see the bypass
gate doc): the same Ed25519 keypair, the same 1Password-held private key, the
same canonical-JSON-then-`sign(null, …)` payload shape, the same committed
public key at `agent-tools/security/bypass-public-key.pem`. Build one crypto
core and both gates use it.

---

## 1. What the gate is made of

Four cooperating pieces, all under `eslint-plugin-fourth-waiv/` plus one root
script:

| Piece | File | Role |
| --- | --- | --- |
| The enforcement rule | `rules/require-signed-exemption/index.js` | Fails lint on any unsigned disable of a protected rule. Runs on plain Node — no TS loader. |
| Verify-side crypto | `lib/exemption-verifier.ts` | `verifyRecord(record, publicKey)` — public-key-only signature check. |
| Sign-side crypto | `lib/exemption-signer.ts` | `signRecord(payload, privateKey)` + `readPrivateKeyViaOp()` — human-only, reads the key from 1Password. |
| Record schema | `lib/registry-schema.ts` | `ExemptionRecord`, `ExemptionTuple`, the discriminated-union shape signed on disk. |
| Mint CLI | `cli/lint-exempt.ts` + `cli/verbs/*` | `mint-current`, `extend`, `shrink`, `revoke`, `repair`, `verify-config`. |
| Build coupling check | `scripts/validate-turbo-lint-coupling.js` | Fails if `turbo.json` lets any package build without its lint. |

Signed records live in `lint-exemptions/*.json` (one file per mint batch).
The public key lives at `agent-tools/security/bypass-public-key.pem`. The
private key never touches disk — it is read from 1Password at sign time.

---

## 2. The signed record

The unit of authorization is an **exemption record**, defined in
`lib/registry-schema.ts` as `ExemptionRecord`. A record is signed once and
carries a base64 Ed25519 signature over the canonical JSON of every field
*except* `signature`.

```
ExemptionRecord {
  id            // 6-char Crockford base32, unique among records
  batch         // human label, usually the filename stem
  areaOfWork    // description
  addedBy       // git user.email of the minter (immutable)
  addedAt       // ISO-8601 mint time (immutable)
  commitAtMint  // git HEAD at mint (informational)
  reason        // non-empty justification
  reviewBy?     // optional YYYY-MM-DD revisit date (warned when past)
  editedBy?     // git email of last extend/shrink
  editedAt?     // ISO-8601 of last edit
  kind          // "tuples" | "infrastructureGlobs" (discriminator)
  exemptions?   // ExemptionTuple[]  — present iff kind == "tuples"
  infrastructureGlobs?  // string[]  — present iff kind == "infrastructureGlobs"
  signature     // base64 Ed25519 over canonical(record - signature)
}
```

The record is a **discriminated union**. A record is *either* a tuples record
(`exemptions[]` populated) *or* an infrastructure-globs record
(`infrastructureGlobs[]` populated) — never both, never neither. The loader
rejects a record where both are populated (polluted) or neither is (malformed):

```js
const hasTuples = Array.isArray(r.exemptions) && r.exemptions.length > 0;
const hasInfraGlobs =
  Array.isArray(r.infrastructureGlobs) && r.infrastructureGlobs.length > 0;
if (hasTuples === hasInfraGlobs) continue;               // both or neither → reject
if (hasInfraGlobs && r.kind !== "infrastructureGlobs") continue; // must declare kind
```

### The tuple — anchor bound to file + rule + count

Each `ExemptionTuple` is the fine-grained grant a line-level disable resolves
against:

```
ExemptionTuple {
  anchor  // 6-char Crockford id, unique WITHIN the record
  rule    // the ESLint rule id this tuple authorizes
  file    // repo-relative source path OR picomatch glob
  count   // scope marker + cap (see below)
}
```

The identity of a single exemption is the **`(recordId, anchor)` pair** —
anchors are unique within a record, not globally.

`count` is both a marker and a cap:

- `count >= 1` → **line-scope**. The anchor may appear in the bound `file` at
  most `count` times. Fewer occurrences is always fine (quality going up is
  always allowed); more is an over-count error.
- `count == 0` → **file-scope marker**. Authorizes a *config-level* silencing of
  the rule (severity below `error`, or the file sits in an `ignores:` block).
  The line-scope rule **skips count-0 tuples entirely** — they are consumed only
  by the static `verify-config` checker (§5).

---

## 3. The enforcement rule: `require-signed-exemption`

`rules/require-signed-exemption/index.js`. Note the extension: the rule is
authored in **`.js`, not `.ts`**, so ESLint loads it through plain Node with
zero loader configuration. It re-implements a thin slice of the `.ts` lib logic
(directive parsing, canonicalization, signature verify) inline so the plugin
runs on a clean Node + ESLint stack. The `.ts` libs remain the canonical source
for the CLI and tests.

### Configuration contract

```js
["error", {
  mode: "specific" | "all-configured-rules" | "every-directive",
  protectedRules: string[],
  infrastructureGlobs?: string[],   // documentation-only at the rule layer
}]
```

- **Default** `{ mode: "specific", protectedRules: [] }` — inert until rules opt
  in. This is deliberate: turning the rule on globally is a `protectedRules`
  edit, not a code change.
- `"specific"` — only directives targeting a rule listed in `protectedRules`
  need a signed exemption.
- `"all-configured-rules"` — every disable of any configured rule needs signing.
  ESLint does not expose "all configured rules" to a rule, so the caller passes
  that list as `protectedRules`.
- `"every-directive"` — every `eslint-disable*` needs signing.

A **bare directive** (no rule list — `// eslint-disable-next-line` with nothing
after it) implicitly disables *all* rules and therefore always requires a signed
tag under every mode.

In this repo the rule is wired in `eslint.config.js` as `mode: "specific"` with
a `protectedRules` array naming ~40 `fourth-waiv/*` architectural rules
(including `require-signed-exemption` itself).

### The `fw-exempt` directive tag

To claim authorization, a disable directive carries a description tag after the
`--` separator:

```ts
// eslint-disable-next-line fourth-waiv/no-secured-cast -- fw-exempt record=A1B2C3 anchor=D4E5F6
```

`parseDisableDirective(commentText)` splits the directive at `--`, extracts the
rule list, and — if the description contains the literal token `fw-exempt` —
pulls `record=<id>` and `anchor=<id1,id2,…>` (both validated against the 6-char
Crockford id regex `^[0-9A-HJKMNP-TV-Z]{6}$`).

### The decision algorithm

The rule runs on `Program:exit`, walks every comment, and for each disable
directive:

1. **Bare + no tag** → report `bareMissingExemption`. Done.
2. **Non-bare** → filter the rule list to only the rules that require signing
   under the active mode (`requiresSigning`). If none, pass silently.
3. **Protected rules but no tag** → report `missingExemption` per rule.
4. **Has a tag** → resolve it against the loaded registry via `resolveAndCheck`:
   - Record id not in registry → `missingRecord`.
   - Anchor id not authorized by that record → `missingAnchor`.
   - Anchor's tuple has `count == 0` → treated as *not authorized for line use*
     → `missingAnchor` (file-scope markers never license a line disable).
   - Tuple's `file` ≠ the current source file (project-relative) →
     `fileMismatch`. **An anchor copied to a different file does not validate.**
   - Every protected rule in the directive must be covered by at least one
     resolved anchor; an uncovered protected rule → `missingExemption`.
5. **Over-count second pass** — occurrences of each `(record, anchor)` across the
   file are tallied in the first pass; if the tally exceeds the tuple's signed
   `count`, report `overCount` once at the first occurrence.

Every message names the remediation verb, e.g. `overCount` says
`Use bun run lint:exempt extend <record> to increase the count.`

### Registry loading + signature verification (inside the rule)

The registry is a **module-level lazy singleton**, loaded once per ESLint
process and cached for its lifetime (reset in tests via `__resetRegistry()`).
`findProjectDir(filename)` walks up from the linted file (max 12 levels) looking
for a `lint-exemptions/` dir or the public key. `loadRegistry` then:

1. Reads `agent-tools/security/bypass-public-key.pem` into a public `KeyObject`.
2. Reads every `lint-exemptions/*.json` (skipping `schema.json`,
   `batch-schema.json`, `.gitkeep`), sorted for determinism.
3. For each record: validates id/signature shape, checks the discriminated-union
   invariant, then **verifies the signature** before admitting it. Only tuples
   records contribute per-anchor entries to the lookup map.

The signature check is the crux — an attacker who hand-writes a
`lint-exemptions/*.json` cannot forge a valid record without the private key:

```js
function verifyRecord(record, publicKey) {
  const sigBytes = Buffer.from(record.signature, "base64");
  const { signature: _ignored, ...payload } = record;
  const payloadBytes = Buffer.from(canonicalize(payload), "utf8");
  return cryptoVerify(null, payloadBytes, publicKey, sigBytes);  // Ed25519
}
```

`canonicalize` sorts object keys alphabetically at every depth (arrays keep
order) so signer and verifier hash byte-identical input. `cryptoVerify(null, …)`
is `node:crypto.verify` with a null algorithm — the Ed25519 convention.

---

## 4. The crypto core (shared with the bypass gate)

The verify and sign sides live in two lib modules and are byte-for-byte
compatible with each other and with the bypass token machinery.

### Verify side — `lib/exemption-verifier.ts`

Public-key-only. `verifyRecord(record, publicKey)` re-canonicalizes the payload
(record minus `signature`) and calls `cryptoVerify(null, payloadBytes,
publicKey, sigBytes)`. Returns `false` on any failure (bad base64, mismatch,
malformed key) — never throws into the caller. `loadPublicKey(path)` and
`defaultPublicKeyPath(projectDir)` resolve the committed PEM at
`agent-tools/security/bypass-public-key.pem`.

### Sign side — `lib/exemption-signer.ts` (human-only)

`signRecord(payload, privateKey)` canonicalizes the payload and returns the
record with a base64 signature attached:

```ts
export function signRecord(payload, privateKey) {
  const bytes = Buffer.from(canonicalizeRecord(payload), "utf8");
  const signature = cryptoSign(null, bytes, privateKey).toString("base64");
  return { ...payload, signature };
}
```

`canonicalizeRecord` is the same alphabetical-key-sort canonicalization as the
verifier and **matches `agent-tools/lib/bypass.ts#canonicalizePayload`
exactly** — that identity is what makes "same crypto as the bypass subsystem"
literally true.

`readPrivateKeyViaOp()` obtains the Ed25519 private key by spawning
`op read op://4thWAIV_Engineering/ProtectedFilesBypassSigningKey/notesPlain`,
parsing the PEM result into an in-memory `KeyObject`. The key **lives only in
memory for the calling process** — never written to disk, never exported to env,
never cached past exit. Tests inject a key via `LINT_EXEMPT_TEST_PRIVATE_KEY_PEM`
(or `privateKeyPemOverride`) instead of hitting 1Password.

The asymmetry is the whole security model: **verifying is agent-safe** (public
key, runs in every lint), **signing is human-only** (private key, gated behind
1Password and the invocation-block list). An agent can read and check
signatures all day; it cannot mint one.

---

## 4a. Two supporting files the mint CLI leans on

`file-manifest.md` lists `cli/id-minter.ts` and `lib/known-rules.ts` as COPY
VERBATIM, and the CLI below depends on their behavior. A docs-only builder
reconstructing the gate needs their contract, so it is spelled out here — do not
re-derive it from prose.

### `cli/id-minter.ts` — the 6-char Crockford id generator

Mints the `record` id and every `anchor` id. The alphabet is Crockford base32,
`0-9A-HJKMNP-TV-Z` (the `CROCKFORD_BASE32_ALPHABET` constant lives in
`lib/registry-schema.ts`) — I, L, O, U are excluded to avoid visual confusion
with 1, 0, etc. Ids are exactly 6 chars, matching the validation regex
`^[0-9A-HJKMNP-TV-Z]{6}$` (`32^6 ≈ 1.07B` distinct ids).

```ts
export function mintId(existingIds: Iterable<string>, options: MintIdOptions = {}): string {
  const rng = options.rng ?? defaultRng;          // crypto.randomBytes(4) → float in [0,1)
  const maxAttempts = options.maxAttempts ?? 1000;
  const taken = new Set(existingIds);             // snapshot so a Map's keys() is fine
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    let id = "";
    for (let i = 0; i < 6; i++) id += randomChar(rng);
    if (!taken.has(id)) return id;                // re-roll on collision
  }
  throw new Error("id-minter: failed to mint a non-colliding id …; registry may be saturated");
}

export function isValidIdFormat(id: string): boolean {
  return /^[0-9A-HJKMNP-TV-Z]{6}$/.test(id);
}
```

**Uniqueness contract.** `mintId` takes the set of ids already in play and
re-rolls on any collision (up to `maxAttempts`, default 1000, then throws). The
CLI passes it the union of **all existing record/anchor ids in the live registry
plus the ids already minted in the in-progress batch**, so a fresh id collides
with neither an on-disk record nor a sibling anchor being written in the same
run. `rng` is injectable for deterministic tests; production uses
`crypto.randomBytes(4)`.

### `lib/known-rules.ts` — the exemptable-rule manifest

An **explicit, hand-maintained** allowlist of ESLint rule ids the mint CLI will
sign exemptions for. It is deliberately *not* auto-derived from
`eslint.config.js` so that "is this rule exemptable" stays a reviewable PR diff.

```ts
export const KNOWN_RULES: readonly string[];        // frozen, deduped, ordered
export function isKnownRule(id: string): boolean;    // membership test
```

Contents are the rules active at close of the signed-exemption run, grouped for
readability — the type-checked `@typescript-eslint/*` family
(`no-explicit-any`, `no-unsafe-*`, `no-floating-promises`, `no-unused-vars`, …),
a few core rules (`no-restricted-globals`, `no-restricted-imports`,
`no-restricted-syntax`, `prefer-const`), and `fourth-waiv/require-signed-exemption`
itself. `mint-current` **refuses to sign a record for any rule id not in
`KNOWN_RULES`**, and `--filter=<rule>` is validated against it. Rules that must
**never** be exempted (a hard security invariant) are deliberately left OFF the
list. When you port the gate, replace the contents with your repo's own
exemptable-rule set; keep the "explicit, reviewable, not auto-derived" property.

> `getProtectedRules` (used by `mint-current` in §5) reads the **live** rule
> config — the `protectedRules` array wired into `eslint.config.js` — to know
> which violations need signing. `known-rules.ts` is the orthogonal gate on which
> rule ids are *eligible* to be signed at all. Both must admit a rule before an
> exemption for it can be minted.

---

## 5. The mint CLI

`cli/lint-exempt.ts`, run as `bun run lint:exempt <verb>`. The human-only verbs
(`mint-current`, `extend`, `shrink`, `revoke`, `repair`) call
`readPrivateKeyViaOp` and sit on the agent invocation-block list. The
agent-readable verb (`verify-config`) is public-key-only and runs inside
`bun run check` / `bun run build`.

### `mint-current` — the primary mint flow

`cli/verbs/mint-current.ts`. The workflow:

1. Run lint to collect current violations of `protectedRules` (pulled from the
   live rule config via `getProtectedRules`).
2. Group protected violations by `(file, rule)`, counting **distinct lines** the
   rule fires on (one directive per line covers all violations of that rule on
   that line, so the anchor appears once per line).
3. Mint a record id and one anchor per `(file, rule)` tuple, with
   `count = number of distinct lines`.
4. Insert `// eslint-disable-… -- fw-exempt record=… anchor=…` directives at
   every violation line (`upsertDisableDirectivesByLine`).
5. Read the private key from 1Password, sign the record with `kind: "tuples"`,
   write/append the batch file in `lint-exemptions/`, and `git add` both the
   batch file and every touched source file.

`--dry-run` reports what would be signed without reading the key or writing
anything. `--filter=<rule>` narrows the *signed* set to one protected rule.
Unprotected violations sharing a line ride along in the directive's rule-list
but never enter the signed record.

Sibling verbs: `extend <id>` raises a tuple's count, `shrink` lowers it,
`revoke` removes a record, `repair` re-derives counts from the current source,
`mint-config` mints the `count: 0` file-scope markers, and
`mint-infrastructure-globs` signs one blanket `infrastructureGlobs` record
covering every protected rule for a set of paths.

### `verify-config` — the static config-level checker

`cli/verify-config.ts`, agent-readable, part of `bun run check` and the first
step of `bun run build` (`"build": "bun run lint:exempt verify-config && turbo
build && …"`). The line-scope ESLint rule cannot see config-level silencing —
setting a protected rule to `off`, or dropping a file into an `ignores:` block,
suppresses it with no directive to catch. `verify-config` walks every
`eslint.config.*` block directly and emits a finding for three shapes:

- **Shape A (`rules-off`)** — a block sets a protected rule to `off`/`warn`.
- **Shape B (`ignored`)** — a top-level `{ ignores: [...] }` excludes files
  from linting entirely.
- **Shape C (`rule-absent`)** — a block applies a protected rule to `files` but
  not to its `ignores`.

Each finding's `(file, rule)` is matched against the signed `count: 0` tuples
via picomatch. An unsigned finding exits 1 and fails the build; a signed one
passes. The verb also emits (never fails on) stale-review warnings for any
record whose `reviewBy` date has passed.

---

## 6. The build ↔ lint coupling check

None of the above matters if the build can run without running lint. Turbo
caches tasks and follows a `dependsOn` graph; if a package's `#build` task does
not depend on its `#lint` task, `bun run build` can green-light code that never
saw the linter — and the signed-exemption gate silently evaporates.

`scripts/validate-turbo-lint-coupling.js` (run as
`bun run validate:turbo-lint-coupling`) enforces the coupling structurally. It
parses `turbo.json` and, for every task named `<pkg>#build`, asserts that its
`dependsOn` array contains **both** `<pkg>#lint` and `<pkg>#check`:

```js
for (const [taskName, taskDef] of Object.entries(tasks)) {
  if (!taskName.endsWith('#build')) continue;
  const pkg = taskName.slice(0, -('#build'.length));
  const deps = Array.isArray(taskDef.dependsOn) ? taskDef.dependsOn : [];
  if (!deps.includes(`${pkg}#lint`))  failures.push(/* missing lint  */);
  if (!deps.includes(`${pkg}#check`)) failures.push(/* missing check */);
}
```

Exit 0 when every `#build` couples its `#lint` and `#check`; exit 1 with a
per-task, actionable error otherwise. It is wired into the **Docker build step**
and the **pre-commit hook** (`.githooks/pre-commit`), so a decoupling edit fails
before it can merge or deploy. In this repo every package task honors it, e.g.:

```json
"@your-scope/app#build": {
  "dependsOn": ["@your-scope/shared#build", "@your-scope/app#lint", "@your-scope/app#check"]
}
```

The intended remediation when this fails is fixed by the script's own banner:
do **not** relax the script — restore the missing `dependsOn` entries in
`turbo.json`. Lint errors are build errors, by construction.

---

## 7. Rebuilding this in a new repo — checklist

1. **One crypto core.** Ed25519 keypair; commit the public key at a stable path
   (here `agent-tools/security/bypass-public-key.pem`); hold the private key in
   a secrets manager (1Password `op read`). Canonicalize payloads by
   alphabetical key sort at every depth; sign/verify with
   `crypto.sign(null, …)` / `crypto.verify(null, …)`. Share this exact
   canonicalization with your bypass gate so one key serves both.
2. **Record schema.** Discriminated union over `kind`; tuples bind
   `(anchor, rule, file, count)`; `(recordId, anchor)` is the identity;
   `count == 0` is the config-scope marker.
3. **Enforcement rule in plain JS.** No TS loader. Lazy singleton registry that
   *verifies every signature* before admitting a record. Parse the `fw-exempt`
   tag; enforce record-exists → anchor-authorized → file-match → rule-cover →
   count-cap, in that order, each with its own message.
4. **Human-only mint CLI.** Verbs that read the private key sit on the agent
   invocation-block list; the config verifier is public-key-only and runs in the
   build. Collect violations, group by `(file, rule)`, sign one record, insert
   tagged directives, commit both.
5. **Structural build coupling.** A script that fails the build if any
   `#build` task can run without its `#lint` — wired into the deploy gate and
   pre-commit. Without it, every other layer is optional.
