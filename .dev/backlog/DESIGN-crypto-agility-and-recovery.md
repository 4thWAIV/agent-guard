# Design pass — crypto agility, key custody, and recovery

Status: DRAFT for review, hardened against one adversarial-auditor round (nine confirmed defects folded in).
Makes the signature scheme a runtime-tagged, compile-time-extensible choice; adopts the hardware-stored key
model; introduces a Sealed signer roster; and adds a presence-proven reset/doctor escape hatch. Consolidates the
scope this conversation added. Nothing here is committed; the affected v1 code is uncommitted, so these are
refactors, not migrations.

## Goal
The signature scheme must be a runtime choice, not a compile-time assumption, so a hardware-native P-256 key
drops in later with no rework while we ship Ed25519 (store-in-hardware) now. SOLID/DRY test: adding a scheme is
one class plus one registry entry in the guard's own source. Separately, the human gets an escape hatch to reset
a corrupted key/config/store that the agent structurally cannot abuse.

## The spine: the trust anchor is Sealed and code-owned
Every hole the audit found traces to one root error — putting trust-defining state where a grant or the agent
could reach it. The rule that fixes all of them: **the trust anchor is Sealed (never grant-unlockable) or
compiled into the guard binary (never data at all).** The trust anchor is:
- the **signer roster** (accepted `{alg, signerKeyId, publicKey}` entries),
- the **accepted-algorithm set**,
- the **custody/keystore selection**, and
- the **recovery baseline**.

`RuleOrigin.Sealed` is never unlockable (`Rule.GrantUnlockable => Origin != Sealed`), so a `Scope.All` grant
cannot touch any of it. The only writer is a presence-gated human CLI action (`install`/`reset`/roster-edit),
which runs as the human's un-hooked terminal command, not an agent tool call. This is what makes "the agent
never authors the roster" true instead of aspirational.

## Three fused concerns, split into seams
Today `GrantStore` + `GrantTokenCodec` fuse what a token *means*, how it is *cryptographically bound* (Ed25519
hardcoded), and where the *key lives* (one committed public-key file). Separate them:
1. **Semantics** — the claim a token makes (path grant, capability grant, suppression approval, expectation).
2. **Cryptographic binding** — the signature scheme (Ed25519 now, P-256 later).
3. **Key custody** — where the private key lives and how a signature is authorized (software file, Keychain, TPM).

## Seams

### 1. Algorithm-tagged token envelope
Every token carries `schemaVersion`, `alg`, `signerKeyId`, a typed `claim` (with a type discriminator),
`expiresAt`, and `signature`. **The canonical signed bytes cover every field except `signature`** — explicitly
`schemaVersion`, `alg`, `signerKeyId`, the claim **type discriminator**, `expiresAt`, and the full claim body —
and the deserializer selects the claim class *only* from the signed discriminator. This blocks scheme downgrade,
key re-attribution, version downgrade, and claim type-confusion (re-tagging a `PathGrant` as a broader
`CapabilityGrant`). The current `GrantTokenPayload` becomes the `PathGrant` claim; nothing in the envelope is
Ed25519-specific. (Fixes audit #8.)

### 2. ISignatureAlgorithm (the crypto seam)
`AlgorithmId Id`; `bool Verify(payload, signature, publicKey)`; `byte[] Sign(payload, keyHandle)` on signing
installs only. `Ed25519Algorithm` wraps BouncyCastle and **owns the 32-byte public-key length rule** (that
constant moves here, off `GuardEngine`/`GrantStore`). `EcdsaP256Algorithm` is the future drop-in.

### 3. ISignatureAlgorithmRegistry — a closed, code-owned allowlist
The registry is a **closed allowlist compiled into the guard**; there is no `none`/test scheme reachable in a
release build, and no runtime or config path can register one. "One class + one registry entry" is **compile-time**
extensibility by the guard's authors, not a runtime/config extension point — the earlier draft over-sold that.
The accepted-algorithm set is **Sealed** and defaults to the single minting alg; an unknown or unaccepted `alg`
denies (fail closed). (Fixes audit #3.)

### 4. IKeyStore (the custody seam) — custody is not agent-selectable and is positively proven
`EnsureKey(alg)`; `ReleaseForSigning()` (presence-gated); `PublicKey()`; `Reset()`. Implementations:
`KeychainKeyStore` (macOS biometric ACL), `TpmKeyStore` (auth policy), and `FileKeyStore` (software, **headless
CI only, verify-only — a file-keystore install can never mint authority that governs a developer's floor**).
The keystore kind is **not an agent-writable config choice** (it is Sealed, §the spine). On a developer
workstation the guard **refuses to mint or reset unless a hardware-presence key store is demonstrably in force**
— proven by checking the active signer's key is a biometric-ACL Keychain/TPM item at mint/reset time, never
merely asserted. The one real algorithm/custody coupling: a *generate-in-hardware* store forces P-256, so config
validation rejects an impossible pairing (`keychain-native` + `ed25519`) with a clear fail-closed error.
(Fixes audit #1.)

### 5. ISignerRoster (the trust anchor) — Sealed
The set of accepted `{alg, signerKeyId, publicKey}` entries, **Sealed**, never grant-unlockable. Verification
requires the token's `signerKeyId` to be rostered AND the signature to verify under that entry's algorithm.
Supports many algorithms and signers at once. Replaces the single `.agentguard/grant-public-key` file. Adding a
signer is a presence-gated human action, structurally out of the agent's reach — not a System write a broad
grant could authorize. (Fixes audit #2.)

### 6. Project-level configuration — split by trust
`.agentguard/config.json` holds only **non-trust** settings the agent may propose (enabled providers, project
protected-paths). The **trust-defining** settings — minting algorithm, keystore kind, accepted-alg set, roster
pointer — are Sealed and writable only by the presence-gated CLI. "Changeable and project-configurable" holds
for the crypto choice, but the change is a human, presence-gated act, not an agent edit; verification always
follows each token's own signed `alg`, constrained to the Sealed accepted set. Unparseable/invalid config denies.

### 7. Capability and version claims are NEW claim-typed code, not CoverageChecker reuse
Honest correction: the frozen `Grant(Id, Scope, IReadOnlyList<IPathMatcher> CoveredPaths, ExpiresAt)` and
`IGrantStore` are **path-only and frozen**, and `CoverageChecker` matches on `Scope==All` + path matchers. They
cannot carry a `capability` or `versionConstraint`. So:
- The **File Guard** (frozen) continues to consume only **path grants**, via the existing `Grant`/`CoverageChecker`
  — unchanged.
- **Capability, suppression-approval, and expectation** claims are a **new claim-typed abstraction with its own
  coverage predicate**, consumed by the new oracle/tooling layer — *not* a generalization of `CoverageChecker`,
  and *not* flowed through the frozen `Grant`. Making the File Guard understand capability grants would require a
  frozen change; we do not do that now, so capability tokens govern the oracle/lint-gate layer and path tokens
  govern the File Guard. `versionConstraint` is checked against the current ruleset via the existing
  `RulesetFingerprint`. (Fixes audit #4.)

## The verify oracle is the read side of the new claim layer
`guard verify <expectation>` answers whether a valid, roster-verified token carries a claim that covers the
expectation, using the new claim-coverage predicate (§7). A workflow (Lie-catcher, lint gate, CI) trusts the
answer because the guard checked a signature the agent cannot forge — **but only if the workflow invokes the
real guard binary** (see the integrity requirement below), not a PATH-shadowed stub. This ends the
"post-approve my own approvals" noise: the human signs one token (Touch ID), and every tool asks the guard
whether that signed approval exists.

## Expiry is a convenience check, not a security pillar
The runtime clock (`TimeProvider.System`) is agent-influenceable (`faketime`, clock skew). So the real control
is **presence-at-mint**: the presence-gated minter caps `expiresAt` to a short bound at mint time. The runtime
expiry check stays as defense-in-depth, but no security decision hinges solely on the wall clock. (Fixes #5.)

## Reset escape hatch — presence-proven, baseline un-stageable
Safety does NOT come from "only humans have terminals" — the agent can run `guard reset` via its Bash tool. It
comes from:
- **Presence gate (primary):** reset and mint both require a physically present human (Touch ID / TPM), proven
  as in §4. The agent can invoke the command but cannot satisfy the gate.
- **Un-stageable baseline:** `reset`'s "known-good baseline" **ships inside the tool binary (or is Sealed)**,
  never a repo file the agent can pre-stage; `reset` re-derives the roster only from a presence-gated fresh
  keygen or a human-confirmed source. Otherwise the agent pre-writes a baseline containing its own key and waits
  for the human to run the gated reset. (Fixes audit #6.)
- **Hook block (secondary):** the PreToolUse hook denies an agent-issued `guard reset`/trust-config write.

`guard reset [--scope=key|config|snapshots|all]` regenerates the keypair, restores the in-binary baseline,
clears the snapshot store, and re-wires the hook — idempotent, logged, presence-gated. `guard doctor` is
read-only diagnosis with a `--fix` that calls the same recovery primitives (one shared core, DRY). Every
corruption case fails closed until the human resets; a silent auto-recover would itself be a bypass.

## Protecting the guard's own code and binary — by role, with an integrity check
The earlier "strip the source paths" rethink was half-wrong: do not *remove* the protection, *relocate* it.
- **In agent-guard's own repo** (it guards its own development), the analyzers + engine + CLI source stay
  System-protected **by a role default** ("this IS the guard's repo," detected, not a path shipped to
  consumers). This is the "disable the analysis" cheat we exist to stop, so it must be on by default.
  Consequence: developing the guard requires presence-gated grants, so **minting must land before the guard
  governs its own repo** — otherwise the self-lock returns.
- **In a consuming project**, the guard is an installed binary **outside `projectRoot`**, which no path rule can
  cover. Protect it by **integrity, not path**: the hook and the oracle invoke an **absolute, integrity-checked
  binary path** (never PATH resolution), and the guard **refuses to act if its own resolved image is
  agent-writable**. This closes both the unprotected-runtime gap and the PATH-shadow-the-oracle gap.
  (Fixes audit #7 and #9.)

## What changes in existing (uncommitted v1) code
- `Ed25519PublicKeyLength = 32` moves from `GuardEngine`/`GrantStore` into `Ed25519Algorithm`.
- `GrantStore` stops being Ed25519-specific; it consumes `ISignatureAlgorithmRegistry` + `ISignerRoster`.
- `GrantTokenPayload` becomes the `PathGrant` claim inside the new envelope; canonical bytes cover all fields.
- The single committed public-key file becomes the Sealed roster.
No back-compat cost — the affected code is uncommitted — but these are part of this pass.

## Accumulated scope (everything this conversation added), in build order
1. **Remove the hardcoded shipped-code System paths** (`src/AgentGuard.Engine`, `src/AgentGuard.Cli`,
   `analyzers`) from the built-in floor — DONE first, because it makes the floor general and stops the guard
   locking its own authors out. Interim state: the guard's own source is unprotected in its own repo; the
   role-based self-protection that replaces it lands in #4, after minting exists to unlock it.
2. **Trust-anchor + custody + presence-gated minting core** (this design pass) — gates the rest.
3. **Token system:** the envelope + typed claim classes, the verify oracle, the new claim-coverage predicate.
4. **Rules relocation:** role-based default re-protecting the guard's own repo (needs minting); installed-binary
   integrity check; Sealed trust anchor. Sealed stores unchanged.
5. **Distribution + tooling:** ship as a dotnet tool; `install` (presence-gated on a workstation); `doctor`;
   `reset`.
6. **Turn it on in agent-guard's own repo** — only after the above.
7. **Roadmap (flagged, not now):** TypeScript + Rust providers; the build scanner for `.csproj`/`.sln`/
   `Directory.Packages.props`; "post panic" (separate subject, filed).
