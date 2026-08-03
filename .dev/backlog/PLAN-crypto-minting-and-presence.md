# Plan: crypto-minting + presence (the gate + exception foundation)

The multi-contract breakdown of the "make a real, authorized change to a protected thing possible" work. Too big for one contract. Detailed (but **out of date**) sketch: `DESIGN-crypto-agility-and-recovery.md` in this folder — treat it as a sketch, ground fresh per contract, and run each contract through workflow-with-adversaries.

## Grounded starting state (verified 2026-08-02)
- The presence seam already exists: `ApprovalGate.RequireApproval` is a placeholder that `install`/`init`/`remove` call; it currently always approves. Its comment: "the real Touch ID check is a later build and drops in behind this call with no change to the callers."
- Grant path is verify-only (Ed25519, one committed public key). No minting, no keygen, no Touch ID / Keychain / dev-cert exist. `GrantStore` "never mints."
- The guard's own source is not in the protected floor (self-protection was deferred to the role-based approach, which needs minting).

## The sequence (dependency order; first = 1a)

**1a — Signable Apple binary + signing pipeline.** Retarget the pieces that need it to `net9.0-macos` (the first-party LocalAuthentication binding), and stand up code-signing: entitlements, `guard dev-cert` (local self-signed cert), the `codesign` step — so the guard produces a properly-targeted, code-signed macOS binary. *Independent deliverable: the signing infrastructure plus a verifiable signed binary.* Prerequisite for Touch ID (LocalAuthentication won't prompt from an unsigned binary). Also feeds Contract 6's integrity check (verify the guard's own code signature) and future notarization/distribution. **First contract.**

**1b — Touch ID presence check (GitHub issue #3).** The signed LocalAuthentication helper built on 1a; verify the helper's own code signature; wire behind `RequireApproval` so `install`/`init`/`remove` are gated. Needs 1a.

**2 — Crypto-agility refactor of the verify side.** Algorithm-tagged token envelope; `ISignatureAlgorithm` (Ed25519); closed compiled-in `ISignatureAlgorithmRegistry`; Sealed `ISignerRoster` replacing the single public-key file. Still verify-only, no behavior loss. Independent of 1a/1b; foundational.

**3 — Key custody + presence-gated minting.** `IKeyStore` (biometric-ACL Keychain; file-store verify-only for CI); config split by trust (Sealed trust settings vs agent-proposable); the `guard mint` command, presence-proven. Needs 1b + 2. **Milestone: real authorized exceptions become possible ("gates the rest").**

**4 — Claim layer + verify oracle.** Typed claim classes (capability / suppression / expectation) as a new claim-typed layer (not through the frozen `Grant`/`CoverageChecker`); the coverage predicate; `guard verify <expectation>`. Needs 2 + 3.

**5 — Reset / doctor recovery.** `guard reset` (presence-gated, un-stageable in-binary baseline) and `doctor --fix` sharing one recovery core; fail-closed until a human resets. Needs 1b + 2 + 3.

**6 — Self-protection + installed-binary integrity.** Role-based re-protection of the guard's own repo (unlocked only by minting); installed-binary integrity check (verify own code signature via 1a; no PATH-shadow; refuse if own image is agent-writable). Needs 1a + 3.

## Later / roadmap (not contracts yet)
Turn it on in this repo; TS/Rust providers; the build scanner (`.csproj`/`.sln`/`Directory.Packages.props`); dotnet-tool distribution + notarization.
