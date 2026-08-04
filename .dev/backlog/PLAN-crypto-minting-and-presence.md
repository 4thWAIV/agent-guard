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

## Locked decisions — 2026-08-03 (presence & platform structure)

Recorded verbatim, immediately, per the rule that a decision carries the human's exact words. These govern the presence contracts (1a/1b), not the CI/CD pipeline.

1. **One binary; platform-specific logic isolated behind one shared abstraction in a separate assembly, general across Windows and Linux.** Tim: *"One binary and if we really want to isolate we'll create a seperate assembly to hold all of the per platform logic. BEcAUSE we'll need the same capabilities behind the same abstraction for WIN and Linux ..."*

2. **Platform differences are absorbed by polymorphism and library centralization confined to a few classes — never scattered `#if`s. Add this to the platform-work instructions, and add a lint rule to enforce it.** Tim: *"THere are VERY GOOD structured techniques and good use of polimorphism and good libary centralization can minimize this impact to a couple of classes where we hold all the complexity... DO NOT make a mess of this it should be added to the instructions for this and then probably lint rules to keep the AI from shitting #ifs all throughout the codebase (and only doing 1/2 of them and leaving the others to rot)."* (The lint rule is its own contract — `analyzers/**` is frozen. File it as a GitHub issue.)

3. **All platforms or none: when Touch ID lands, presence must be solved for every platform in the same release.** Tim: *"ALL platform matrix is setup and build and tested AND signing configured and 100% equivilant AND NOT oNE SINGLE thing get's scoped out of ANY platform NOW. ... WHEN we add the touch ID ... WE"LL be forced to solve the problem for all platforms."*

4. **Presence testing: seam the real sensor call behind the interface, test everything above it in CI with a fake, auto-test the "biometrics unavailable → deny" path on the CI runner, and cover the real sensor path with a manual pre-release checklist.** Approved as written; his exact words on the exact text below: *"You can record I approve this decision exactly as written."* The approved text:
   > - The actual sensor call is one tiny piece behind the presence interface. Everything above it is tested in CI against a fake that returns "approved"/"denied" on command — that's ~95% of it, fully automated.
   > - The Mac CI runner has no fingerprint enrolled, so we automatically test the "biometrics unavailable → deny" path — proves we fail safe, for free.
   > - The real "human touches sensor → approved" path is a manual pre-release checklist step. It's the only part no robot can do, and the seam keeps it tiny on purpose.

5. **Research the best Linux presence experience before designing the Linux solution.** Tim: *"What is the experiance and what is the best epxeriance linxu can offer. WE must research this before we design the linux solution."*
