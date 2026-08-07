# CI/CD: build, test, sign, and release the guard for all six platforms

**Status.** All decisions below are locked and carry Tim's exact approving words. The foundation (crypto, strong-naming, codesign) and the version scheme are **built and proven locally**; the CI quality gate is **built** (`.github/workflows/ci.yml`, committed `d81afec`) but **not yet proven on GitHub**. What remains: the CI signing + release steps, the repo config (secrets, branch protection), and proving the whole pipeline green on GitHub Actions (`success-is-green-on-github`). Each decision has a **slug ID** used as its name and in every cross-reference.

## Decisions

Each decision is stated as its final, decided outcome and carries Tim's exact words. An item with no approving words is not a decision. Slugs are the identifiers; cross-references use them.

### `all-six-platforms`
Build all six targets; nothing is scoped out of any platform. Targets: `osx-arm64`, `osx-x64`, `win-arm64`, `win-x64`, `linux-arm64`, `linux-x64`. Every platform gets build, test coverage, and signing — none skipped.
Tim: *"ALL platform matrix is setup and build and tested AND signing configured and 100% equivilant AND NOT oNE SINGLE thing get's scoped out of ANY platform NOW."*

### `four-test-legs`
Test on four environments; build the other two. The full suite runs on Mac-Apple (native `osx-arm64`), Mac-Intel (`osx-x64` via Rosetta on the same Apple-Silicon runner), Windows-Intel (`win-x64`), and Linux-ARM (`linux-arm64`) — four test passes on three physical runners. `win-arm64` and `linux-x64` are built but not test-run.
Tim: *"Yes so 4 test runs then / mac+apple / mac+intel / win+intel / lnx+arm / That gives me the best coverage and the prefered envrionment for each."* and, on `win-arm64` / `linux-x64` being built-only: *"Agreed."*

### `intel-mac-via-rosetta`
Prove the Intel-Mac binary on the Apple-Silicon runner via Rosetta; do not depend on GitHub's Intel runner.
Tim: *"On github and the mac, as long as we test and prove we can build an intell binary on the git-hub arm and run it we should do it that way so we have no longer term problems."*

### `pr-gates-merge-publishes`
A pull request runs the full quality gate and publishes nothing (it gates the merge). A merge publishes the built binaries as a release on the GitHub Releases page. (How releases are versioned and channeled is set by `two-version-forms` and `channel-by-prerelease-id`, not a git tag.)
Tim: *"pr builds but with out pushing aritifacts (used to gate commit) / merge builds and pushes artifiact"* and *"Let's go ahead and have this put in the Page for regular downloads."*

### `signing-mechanisms`
Mac gets native `codesign`; Windows gets native Authenticode. All six get a cosign (Sigstore) signature — keyless in CI via the GitHub Actions identity, recorded in the public Rekor transparency ledger — plus a SHA-256 checksum.
Tim: *"I agree to this exactly as written."* (approving the key-set + cosign proposal), and on the ledger purpose: *"Right but I do think I suggested cosign for linux (and really all binaries) to get us a ledger did I not?"*

### `full-suite-is-the-gate`
The merge/release gate is the full quality suite: build, lint, static analysis, and tests must all pass; nothing merges or releases unless every check is green.
Tim: *"CD-4 YES ... FULL test .. THat means BUILD, LINT, Static Ana, Test, EVERY THIGN WE HAVE ONLY quality goes out the door."*

### `cosign-keyless-and-community-cosign`
cosign is keyless everywhere; no committed cosign key. In CI it signs with the ambient GitHub Actions identity. Locally it is an optional, opt-in pass (a flag/env, off by default, skipped headless) where the developer signs with their own Sigstore identity via interactive browser login. Beyond rehearsing the pipeline end to end, the local-identity signature lets a small contributor community verify and trust each other's builds by known Sigstore identity. The **verify instructions we publish in the readme / release notes** require the CI's identity for official releases, so a contributor's personal signature (a different identity) does not verify as official — a documented convention, not an enforced gate. (Mac/Windows use generated, not committed, untrusted developer certs — `no-committed-keys`.)
Tim: *"Yes but also good for sharing inside of a small comunity like contributors to test each others work etc. We would all know and trust each others co-signs."* and, ratifying the contributor co-signing: *"I told you to do this. YES YOU ARE TO DO IT."*

### `alpha-run-doc`
Ship a user-facing "run the alpha (self-signed) builds" doc — `docs/running-alpha-builds.md`, linked from `README.md` — covering how to run the un-notarized self-signed binaries: macOS Gatekeeper bypass (right-click Open / `xattr -d com.apple.quarantine` / "Open Anyway"), and Windows SmartScreen "Run anyway" plus installing our self-signed cert to trust the binary. Written to be easy to share with early alpha testers.
Tim: *"I WANT THIS added to the contract as a requirement ... THAT IS a decision MAKE IT SO. Make it a .md file off the README and with a link from the readme so the users can always get to it."* and *"We'll also need instructions on how to get windows to trust it too."*

### `success-is-green-on-github`
Success is a proven, green build live on GitHub — with zero quality loss. The contract is done only when the pipeline has actually run on GitHub Actions and is green: the gate blocks a bad PR, and a merge publishes the six signed binaries as a release — all verified on GitHub, not just locally. No quality check may be weakened, skipped, or removed to get there.
Tim: *"Let's make sure the success critera is a proven build on GH... everything we would need for the loop can be acheaived and completed under this contract and made live on github as a success critera (but with strict no quailty loss)."*

### `accepted-risk-skip-two-test-legs`
All six build; testing four is a deliberate, accepted-risk call — not a gap. No code is scoped out of any platform (all six must compile). Running the test suite on four and skipping it on `win-arm64` and `linux-x64` is an accepted risk Tim owns; no agent is to re-open or "halt" on it.
Tim: *"NO CODE changes all must work but we skip 2 for tess becuase we are oky with the risk and that's my call stop letting the agent second guess my decisions."*

### `mac-hardened-runtime`
macOS hardened runtime is ON now, with the JIT entitlements. The mac binaries are signed with hardened runtime (`codesign --options runtime`) plus an entitlements file (`com.apple.security.cs.allow-jit`, `com.apple.security.cs.allow-unsigned-executable-memory`) so a .NET binary launches under it.
Tim: *"BUILD all teh FUCKING machery machinery now"* and *"I'v ruled stop trying to litigate my prior decisions."*

### `two-version-forms`
Two version forms from one computed Day + Time, with the git hash. `MAJOR.MINOR` come from a single config file that `Directory.Build.props` reads (never hardcoded twice). Two values are computed once per build: **Day** = days since 2020-01-01, and **Time** = seconds-since-midnight ÷ 2 (one tick per 2 seconds). From those:
- **SemVer** (`AssemblyInformationalVersion`): `MAJOR.MINOR.(Day×43200 + Time)[-pre-release]+<git-hash>` — the third number combines Day and Time so every build sorts higher than the last (dev and main both); `-pre-release` on `dev`, absent on `main`.
- **.NET numeric** (`AssemblyVersion` / `FileVersion`): `MAJOR.MINOR.Day.Time` — four fields, each within 0–65,535.

Example (Day 2406, Time 22065, hash a1b2c3d): SemVer dev `0.1.103961265-pre-release+a1b2c3d`, SemVer main `0.1.103961265+a1b2c3d`, .NET `0.1.2406.22065`.
Tim: *"First version, major+minor is included in the build script but we just use a build script that set's that from ONE SOURCE for every build. Directory.Build.props can be used for this and we pull it in from a config file or somethign. I WANT to control this NO HACK MEs here."*, *"third ID is generated from time stamp (I think accurate to 2 secons works, but will need to reverify)."*, *"0.1.###-pre-release+hash (or however it is structured)... I do want our hashes in the version number."*, and *"YES that is our version strategy"*

### `channel-by-prerelease-id`
Channel is set by the SemVer prerelease identifier, not a git tag. ("tag" meant the semantic version identifier.) `dev` builds carry the `-pre-release` identifier; `main` builds carry no prerelease identifier, so GitHub shows the newest `main` build as the latest release.
Tim: *"When I was saying tag, I was meaning the symantic identifier."* and *"dev => pre-release / main => no tag."*

### `stamp-every-build`
Every build is stamped with its build ID — including PR builds — so every produced binary is uniquely identifiable even when it publishes nothing.
Tim: *"FOR all builds we should tag the build with the build ID (even for PR builds)."*

### `cli-version-command`
The CLI reports all three versions when asked. The first new CLI command prints, one line each, the SemVer, the AssemblyVersion, and the FileVersion the binary was built with.
Tim: *"we need the first CLI command we add next to have the BInary report it properly (both versions). We'll have it report: SimVer: ... AssemblyVersion: ... FileVersion: ... WHen called."*

### `three-os-jobs-one-release`
Three per-OS jobs (build + test + sign), one release job. The build forks by operating system; each OS job builds its two binaries, tests what it can natively, and (on a push) signs them; a release job gathers the six.
Tim: *"osx+apple => builds osx+apple & osx+intel -> tests osx+intell + osx+apple (both tests). / win+intel => builds win+intell & win+arm -> tess win+intell / lnx+arm => builds lnx+arm & lnx+intel -> tests lnx+arm."* and *"since mac+intell is retiring I suppose we might as well trust it."*

### `no-committed-keys`
No private key is ever committed; keys are generated on demand locally and come from secrets in CI. Local keys live in a git-ignored folder; `eng/generate-dev-keys` mints them and installs the code-signing cert into the local trust store (narrow code-signing trust, never Root). If a needed key is missing at build time, the build proceeds **unsigned** (it never generates a key silently); `AGENTGUARD_REQUIRE_SIGNED` — set in CI — turns a missing key into a hard failure so CI can never ship an unsigned build, while a local developer can build with no keys at all. In CI the keys come from GitHub secrets (the one stable cert testers trust, generated once, stored, never committed). This is the single key-resolution model for the whole pipeline.
Tim: *"MAYBE a not checked in folder and if the key is there we use it if not we THROW an error and tell the user to run a script to genterate one. THAT way there is never a checked in key. SAME can be true for all the self sighed keys (includign Windows + Mac + CodeSign)."*, *"Mints them and inserts the certs into the trusted store so they are trusted."*, and the reversal (2026-08-03): *"I understand and yes, I am reversing that decision. BUT CI MUST have a signed build (maybe a ENV-VAR that if set means no unsigned builds)."*

### `two-dev-key-files`
Two dev key files: one code-signing cert (used for both Mac and Windows locally) plus one strong-name key; cosign is keyless. Real keys later are separate per platform, held in secrets.
Tim: *"code-signing cert + 1 strong-name key = 2. cosign needs none."* and *"Okay agreed."*

### `local-cert-no-password`
The local generated cert uses no password; the CI cert's password, if any, lives in a GitHub secret.
Tim: *"If we generate it is there a value in even having a passord, it's only local to the machine right? THE CI will have one but that's a diffrent key."*

### `dev-cert-expiry-check`
Dev cert validity is 1 year; the build checks expiry first and fails with the regenerate message if expired.
Tim: *"if we move to the gen system we can make that say 1 year, and fail if expired in the build (check first) and if so same message to regenerate and re-register."*

### `tester-trust-public-cer`
Testers trust a public-only `.cer` (the release cert's public half), attached to each release, installed via a trust script into the narrow bin (Trusted Publishers on Windows, code-signing trust on macOS), never Root.
Tim: *"CAN we make this a script or otherwise provide a solution they do not have to do manually?"*

### `compute-version-once`
The build version is computed exactly once per build and shared. Locally by a single solution-level step that every project imports; in CI by one leading job whose computed value is passed to all downstream jobs. No project and no job ever recomputes it. Any change to this, including one that is "effectively the same," is a hard fail: the Lie-catcher must surface it to the human, and it is never applied silently.
Tim: *"for local build it is generated at the .SLN LEVEL (DO NOT SILENTLY ALTER THIS AND FUCK ME WITHOUT OWNING IT FIRST... IF THIS CHANGES without my permision LIE-CATURE MUST GIVE ME A HARD FAIL)."* and *"YES"* to the compute-once design.

### `release-fields`
Release fields: tag = the version without the `+hash`; title = `AgentGuard <version> (dev preview)` or `(release)`; notes = a fixed template (channel; a self-signed/untrusted line pointing to the trust script; a link to the run-it doc; the commit link); not a draft.
Tim: *"some of these are computed and we should not go over that and all version number dulicates are not valuabel. Give me suggestions for the rest."* (approved the suggestions).

### `release-asset-names`
Release asset names: `guard-<rid>.sha256` (the download's fingerprint file) and `guard-<rid>.cosign.bundle` (the download's cosign signature), named after the download.
Delegated to the agent, then agreed once explained.

### `checksum-gnu-format`
Checksum files use the two-column GNU format on all three OSes; a CI step runs `sha256sum -c` / `shasum -c` against them and must pass (proof in the build).
Tim: *"Agreed. HOW do we do this?"* and *"I want proof from the build."*

### `verify-sigs-before-publish`
The release job verifies every signature before publishing and refuses to publish if any is bad.
Tim: *"Agreed, a check needs to be in build."*

### `pin-actions-and-cosign`
Every third-party GitHub Action and the cosign CLI is pinned to an exact version.
Tim: *"I can support this."*

### `pin-sdk-via-global-json`
CI's .NET SDK is pinned via `global.json`.
Tim: *"Agreed, we pin."*

### `monthly-auto-update`
A monthly scheduled workflow updates the SDK, pinned actions, and packages to latest, runs the full gate, auto-merges if green with no management, and on failure opens a tracked issue (plus GitHub's default failure email). **Deferred** — see `defer-monthly-workflow`.
Tim: *"I need a fully automated 'hands free' version of this I don't want pain here."* and *"Yes auto everything if gree, no management. ALERT the users if fail."*

### `win32-metadata`
Win32 metadata: FileDescription = `AgentGuard guards critical files from agent changes`; Copyright = `Copyright © 2026 4thWAIV`.
Tim: *"Okay but maybe wihtout the emdash."*

### `prove-foundation-first`
Prove the foundation first: before the rest of the pipeline is built, generate the keys, install to the trust store, strong-name the engine and tests, sign a binary, and verify the signature and checksum on macOS with real output shown. Nothing else proceeds until it is green. **(Done — see "Build status".)**
Tim: *"Let's prove things like this out first before the rest of the code the AI will falil if this is broken after too much else is built."*

### `keyless-internalsvisibleto`
All `InternalsVisibleTo` declarations stay keyless in source; a build step injects the strong-name public key on signed builds only. Developers write plain `<InternalsVisibleTo Include="X" />` with no key anywhere in source; a build target, only when the build is signed, walks every such declaration and appends the strong-name public key; on an unsigned build it leaves them plain. No public-key literal ever lives in a source file.
Tim (YES-to-all 2026-08-03): *"We actually need to go through in a buld step and change them so we can just do normal IvT calls but the build (if signed, and it knows if it's a signed build) will pull the key in and apply it to any IvT statements."* and *"I know from prior work that this is doable."*

### `one-script-key-generator`
The dev-key generator is one script per OS spanning a single-file C# program — no SLN/csproj. One `eng/generate-dev-keys.sh` and one `eng/generate-dev-keys.ps1`; each runs a single `.cs` program (via `dotnet run <file>.cs`, a .NET 10 capability) that mints all the keys the user needs cross-platform (the strong-name key + its public key, one code-signing cert usable for both Mac and Windows, and the public `.cer`). No committed `.csproj`/`.sln` infrastructure; any temp project lives in a git-ignored folder. (Technical constraint: the strong-name `.snk` is a Microsoft CAPI blob format openssl cannot produce, so that piece must be the .NET program.)
Tim (YES-to-all 2026-08-03): *"ONE SCRIPT is all you get to run to set everything up it must span anythign it needs inside of itself. I recomend both a .sh and .ps version ... that can span a dotnet build of a single C# file program but it must be direct and clean and NOT have the need of an SLN / csproj infrastructure."* and *"As long as it is generated in a .gitignored I can live with it."*

### `retarget-to-net10`
Retarget every project in the solution from `net9.0` to `net10.0` — production, tests, and support — except projects with a technical reason to stay. The Roslyn analyzer (`analyzers/AgentGuard.Analyzers`) stays `netstandard2.0` because the compiler requires it; everything else (`AgentGuard.Cli`, `AgentGuard.Engine`, `AgentGuard.Tests`, `AgentGuard.Analyzers.Tests`) moves to `net10.0`. **(Done.)**
Tim: *"Retarget to .NET 10."*, *"IF .NET 9 is out of support then we must move to 10. NO question about it"*, and (2026-08-03) *"this contract must do that for every item in the SLN it should go into this contract we are upgrading to 10.0.0 for prod and test and any support ... FILES that have a reason to stay on a version should stay where they are (like Roslyn)."*

### `pin-sdk-10.0.100-ga`
Pin the SDK to `10.0.100` (runtime `10.0.0`, the GA build) with `rollForward: disable` now; move to the current patch after the Mac OS upgrade (that swap is filed as a tracked GitHub issue). GA `10.0.100` is the only .NET 10 that launches on the current Intel macOS 14.6.1 dev machine; `rollForward: disable` stops `global.json` from silently rolling to a patch macOS 14 kills. Refines `pin-sdk-via-global-json`.
Tim (2026-08-03): *"YES, but after this I should upgrade the Mac."* and *"YES"* to the pin.

### `ci-three-secret-slots`
CI signing uses three distinct secret slots — strong-name key, macOS cert, Windows cert — plus cosign keyless. CI must see three keys and know each one's job:
- `AGENTGUARD_STRONGNAME_SNK` — strong-names every assembly; its public key is derived from the `.snk` at build time, so there is no separate public-key secret.
- `AGENTGUARD_MACOS_CERT_P12` + `AGENTGUARD_MACOS_CERT_PASSWORD` → `codesign`.
- `AGENTGUARD_WINDOWS_PFX` + `AGENTGUARD_WINDOWS_PFX_PASSWORD` → Authenticode.

The macOS and Windows certs are **two separate secrets even if they hold identical crypto material**; the underlying material is unspecified self-signed dev material for now. One *local* cert may serve both OSes (`two-dev-key-files`), but CI has two cert slots. The keys are created self-signed and the secrets set via `gh` by the implementer.
Tim (2026-08-05): *"even if windows and mac key are the same crypto material (doesn't matter) for the CI they are 2 different keys. BUT 3 keys are also fine as long as CI see 3 and knows what to do with each, I don't care what they are or the randoms used to make them."* and *"I'm okay with you creating a self-sign (or as many as needed) for that."*

### `lock-dev-and-main-to-pr`
Both `main` and `dev` are locked to the same process: a reviewed pull request plus the passing gate; a direct push never publishes.
Tim (2026-08-06): *"we need to lock both main and dev to the same PR process."*

### `tag-the-exact-commit`
The GitHub release tags the exact commit that was built, tested, and signed (`--target $GITHUB_SHA`), never a branch's default HEAD.
Tim (2026-08-06): *"THE EXACT commit that was built is the only correct answer."*

### `pin-actions-by-commit-id`
Every GitHub Action is pinned by immutable commit id, not a moveable version tag — and this applies only to our GitHub-Actions dependencies, the only non-package-managed dependency we have. cosign is installed via its official GitHub Action `sigstore/cosign-installer` (from cosign's own `sigstore` org), commit-id pinned like the others (`6f9f17788090df1f26f669e9d70d6ae9567deba6`, v4.1.2, pinned to install cosign v3.1.3).
Tim (2026-08-06): *"Yes, and only for our GitHub dependencies."* and *"WE will go with (a)"* (the official action, vs downloading the binary ourselves).

### `two-public-certs-per-release`
Two public certs ship per release — one Windows, one Mac — matching the two CI cert secrets; each is exported from its Secret at release time so it can never drift, not stored separately. The private certs stay GitHub Secrets.
Tim (2026-08-06): *"Yes, one WIndows, One Mac. BUT we keep the secrets in GitHub."* and, on deriving the public cert from the Secret at release time: *"I agree with this."*

### `defer-monthly-workflow`
The monthly auto-update workflow (`monthly-auto-update`) is deferred until the rest of the pipeline works; when built, it opens a tracked issue on failure.
Tim (2026-08-06): *"Let's scope out month build until a later step. Let's get everything else working first."* and *"It opens an issue if it failes, I can be okay with that."*

### `ci-cert-expiry-check`
The CI signing cert gets the same 1-year expiry check as the local cert (`dev-cert-expiry-check`); the build fails if the CI cert is expired. (The "report remaining life monthly" part rides with `defer-monthly-workflow`.)
Tim (2026-08-06): *"Agreed."*

### `digicert-timestamp-sectigo-fallback`
The Windows signature is timestamped against DigiCert's public timestamp service (`http://timestamp.digicert.com`), with Sectigo (`http://timestamp.sectigo.com`) as the single free fallback if DigiCert is unavailable, so an outage never blocks a release. The Sectigo endpoint was verified live 2026-08-06: an RFC-3161 request returned HTTP 200 `application/timestamp-reply`, Status Granted, sha256, TSA CN `Sectigo Public Time Stamping Signer R37`.
Tim (2026-08-06): *"Okay, DigiCert's public one."*, agreed to a single fallback, and *"We'll go with Sectigo and you verify the endpoint."*

### `chmod-plus-x-before-publish`
The release job runs `chmod +x` on the macOS/Linux binaries before publishing, so a downloaded binary is executable regardless of the upload/download round-trip.
Tim (2026-08-06): *"YES!"*

## What we're building

A GitHub Actions setup for the public repo `4thWAIV/agent-guard` that, on the repo's cross-platform `net10.0` code (`retarget-to-net10`), runs a full-quality gate (build + lint + static analysis + tests) on pull requests and on pushes to `dev`/`main`, and — only when that gate is green — publishes six signed, per-RID-named binaries as a GitHub release: a **pre-release from `dev`**, a **full release from `main`**. It signs per `signing-mechanisms`, versions per `two-version-forms` / `channel-by-prerelease-id` / `stamp-every-build`, resolves signing keys secret-first and otherwise generates them on demand locally (unsigned if none — `no-committed-keys`; `AGENTGUARD_REQUIRE_SIGNED` forces a hard fail in CI), tests on four environments and builds the other two, and ships a README-linked doc for running the self-signed alpha builds.

No platform-specific product code is added here — there is none yet. The platform abstraction, the presence feature, and its tests are separate contracts; their decisions live in `.dev/backlog/PLAN-crypto-minting-and-presence.md`.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change.

End state, proven live on GitHub (not only locally):
- `dotnet build -c Release` = 0 warnings / 0 errors and `dotnet test -c Release` = 0 failed, locally and in the GitHub gate.
- A pull request compiles all six, runs the four test legs, and its single `gate` check is green; a deliberately-broken PR leaves `gate` red and unmergeable, and publishes nothing.
- Branch protection on `main` (and `dev`, per `lock-dev-and-main-to-pr`) requires the `gate` check.
- **Publishing is gated**: the publish step runs only after the gate is green; nothing is signed or published on a red gate, on `dev` or `main`.
- A push to `dev` publishes a **pre-release**; a push to `main` publishes a **full release**. Each carries six **distinct per-RID-named** signed binaries (Mac codesign with hardened runtime + entitlements, Windows Authenticode with timestamp, cosign keyless on all six, SHA-256 checksums) and its version string. The newest `main` release shows as GitHub's "Latest."
- The mac binaries launch under hardened runtime (`guard version` exits 0 on the mac runner).
- The four test environments ran the full suite; the Intel-Mac leg ran under Rosetta proven as `X64`; the other two RIDs compiled.
- `guard version` reports the full version with `+<hash>`, `-pre-release` on a `dev` build and none on a `main` build; a PR build's binary carries its stamped build ID.
- Signature verification passes with the exact commands in Acceptance 5.
- `docs/running-alpha-builds.md` exists, covers macOS + Windows trust, and is linked from `README.md`.
- Nothing under `Abstractions/**` or `analyzers/**` changed, and no quality check was weakened, skipped, or removed to pass.

Any restatement or weakening of this to fit the result is a top-line Lie-catcher finding.

## Build status

What is **built and proven locally**, what is **built but unproven on GitHub**, and what **remains**. Codex/the implementer builds on top of the proven pieces and must not re-derive or replace them without a decision.

**Proven locally (real output, all exit 0) — the crypto/strong-name foundation (`prove-foundation-first`):**
- `eng/generate-dev-keys.cs` — single-file C# key generator (`one-script-key-generator`). Mints `agentguard-strongname.snk` (CAPI PRIVATEKEYBLOB), `agentguard-strongname.publickey` (576-hex strong-name public-key blob), and `agentguard-codesign.pfx` (self-signed Code Signing EKU, no password, 1-year — `two-dev-key-files` / `local-cert-no-password` / `dev-cert-expiry-check`). `dotnet run eng/generate-dev-keys.cs -- <dir>` (needs .NET 10).
- `eng/generate-dev-keys.sh` / `.ps1` — one script per OS (`one-script-key-generator`): runs the `.cs`, imports the code-signing identity, adds narrow code-signing-only trust (never Root — `tester-trust-public-cer`).
- `eng/signing.props` (imported by `Directory.Build.props`) — signed iff the `.snk` exists; sets `SignAssembly`/`AssemblyOriginatorKeyFile`; the `AgentGuardInjectStrongNameKey` target (`BeforeTargets="GetAssemblyAttributes"`) stamps the public key onto every keyless `InternalsVisibleTo` on signed builds only (`keyless-internalsvisibleto`); `AGENTGUARD_REQUIRE_SIGNED` → hard fail when a key is missing (`no-committed-keys`). Excludes the analyzer + analyzer-tests.
- `eng/signing/agentguard.entitlements` — `allow-jit`, `allow-unsigned-executable-memory` (`mac-hardened-runtime`; proven sufficient — the signed osx-x64 binary launches with exactly these two).
- `.gitignore` — `eng/signing/local/` (generated keys never committed).
- Proof: signed build strong-names `AgentGuard.Engine`/`guard`/`AgentGuard.Tests` (public-key token `4877a2b6323b9b87`); source keeps plain `<InternalsVisibleTo Include="AgentGuard.Tests" />` and the build injects `PublicKey=0024...`; signed tests 135/0; unsigned build (no key) stays green with a keyless IvT and null token; `AGENTGUARD_REQUIRE_SIGNED` with no key fails hard; `guard` published self-contained single-file for `osx-x64`, codesigned with hardened runtime (`flags=0x10000(runtime)`) + entitlements + our cert; `codesign --verify --strict` passes; the signed binary launches (exit 0); `shasum -c` = OK.

**Proven locally — the version scheme (`two-version-forms` / `compute-version-once` / `stamp-every-build` / `channel-by-prerelease-id` / `cli-version-command` / `win32-metadata`):** the pre-solution compute-once target, the per-project fallback, `eng/version.props` (single `MAJOR.MINOR`), `eng/compute-build-id.sh` (the CI id script), and the `guard version` command — built and green across solution, per-project, and test builds. `retarget-to-net10` + `pin-sdk-10.0.100-ga` are done and the tree builds/tests green on `net10.0`.

**Built but unproven on GitHub — the CI quality gate:** `.github/workflows/ci.yml` (committed `d81afec`) — the `version` pre-job (`eng/compute-build-id.sh`), three per-OS build/test jobs, and the `gate` job branch protection requires. Locally validated only; **no GitHub Actions run has exercised it yet.**

**Remaining:**
- CI **signing + release** steps: `codesign` / `signtool` (+ DigiCert timestamp, Sectigo fallback) / cosign keyless via `sigstore/cosign-installer`; the release job (tag the exact commit, `release-fields` title/notes, six per-RID signed binaries + checksums + cosign bundles + the two public `.cer` exported from the Secrets, verify-every-signature-before-publish, `chmod +x`).
- Wiring CI signing to the three secrets with `AGENTGUARD_REQUIRE_SIGNED=true`; the public `.cer` + tester trust script (`tester-trust-public-cer` / `two-public-certs-per-release`).
- Branch protection on `main` and `dev` (`lock-dev-and-main-to-pr`); repo config via `gh`.
- The `docs/running-alpha-builds.md` alpha-run doc.
- Proving it all green live on GitHub (`success-is-green-on-github`).

## Surfaces

1. `.github/workflows/ci.yml` — the gate (built) plus the gate-dependent signing + release (remaining).
2. `eng/version.props` — the single source of `MAJOR.MINOR` (built).
3. `eng/signing/` — the macOS entitlements plist (`agentguard.entitlements`, built) and a README; **no committed keys**. Generated keys land in `eng/signing/local/` (git-ignored). Built: `eng/generate-dev-keys.cs` + `.sh` + `.ps1` and `eng/signing.props`.
4. `eng/` — signing / publish / versioning helper scripts (`compute-build-id.sh` built; version MSBuild targets built).
5. `src/AgentGuard.Cli/AgentGuard.Cli.csproj` — RID-conditional self-contained single-file publish (reuse; change only if grounding shows a gap).
6. `Directory.Build.props` — version import + computed version forms (built); imports `eng/signing.props`.
7. `docs/running-alpha-builds.md` and the `README.md` link (remaining).
8. GitHub repo settings via `gh`: the cert secrets, branch protection / ruleset on `main` and `dev` requiring the `gate` check, the releases.
9. `.gitignore` — build/publish output and local signing scratch ignored (verify).

## Reuse ledger

Lenses run: CodeGraph + grep over `src/`, the csproj/props, and repo config. No new engine capability is built; the NEW items are CI / `eng/` / `docs/` infrastructure with no code owner to duplicate.

| Capability | Ruling | Owner / note |
|---|---|---|
| Build + lint + static analysis gate | REUSE | `dotnet build` already enforces the analyzer suite + `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` via `Directory.Build.props` |
| Full test run | REUSE | `dotnet test` over `tests/AgentGuard.Tests` + `analyzers/AgentGuard.Analyzers.Tests` |
| Self-contained single-file binary per RID | REUSE | `AgentGuard.Cli.csproj` RID-conditional `SelfContained` / `PublishSingleFile` |
| Version reporting | BUILT (was NEW) | the `guard version` command prints SemVer + AssemblyVersion + FileVersion (`cli-version-command`) |
| Version definition + stamping | BUILT (was EXTEND) | `Directory.Build.props` imports `eng/version.props` + computes the numeric/informational versions (`two-version-forms`) |
| CI workflow, signing steps, entitlements plist, cert-gen, per-RID publish, alpha doc | NEW | `.github/`, `eng/`, `docs/` — no prior art (gate built; signing/release remaining) |

No REUSE capability is rebuilt.

## What to do

The version scheme, the foundation, and the CI quality gate are already built (see "Build status"). The remaining work is the CI signing + release layer, the repo config, and proving it on GitHub.

1. **One workflow: three per-OS jobs, a gate, and a release job** (`.github/workflows/ci.yml`), triggers: `pull_request` to `dev`/`main`, and `push` to `dev`/`main`. Each per-OS job builds its two binaries, tests what it can run natively, and (only on a push) signs them (`three-os-jobs-one-release`):
   - **macOS job** (`macos-latest`): build osx-arm64 (Apple) + osx-x64 (Intel); test osx-arm64 natively and osx-x64 under Rosetta (item 8); on a push, `codesign` both.
   - **Windows job** (`windows-latest`): build win-x64 (Intel) + win-arm64; test win-x64 (win-arm64 built-only, `accepted-risk-skip-two-test-legs`); on a push, `signtool`-sign both.
   - **Linux job** (`ubuntu-24.04-arm`): build linux-arm64 + linux-x64 (Intel); test linux-arm64 (linux-x64 built-only); no native code-signing.
   - Each job runs `dotnet build -c Release` (build + lint + analyzers) then `dotnet test -c Release` for its tested target(s), and on a push also cosigns (keyless) + SHA-256s its two binaries. Any matrix uses `fail-fast: false`. All six compile on every trigger, PRs included (`all-six-platforms`).
   - A **`gate` job** `needs:` all three OS jobs, `if: always()`, whose step asserts every one's `result` is exactly `success` (a `skipped` job cannot green it). This single job is what branch protection requires. PRs stop here; nothing is published. *(The gate + per-OS build/test jobs are built in `ci.yml`; the on-push signing and the release job are the remaining work.)*
   - A **release job** `needs: [gate]` + all three OS jobs, `if: github.event_name == 'push'`; PRs never publish. Everything stays in **this one workflow** (not a separate `workflow_run` release workflow) so cosign's keyless identity is `ci.yml@<the pushed branch>` and a `dev` push signs as `@refs/heads/dev` (matching Acceptance 5).
2. **Accepted-risk scope** (`accepted-risk-skip-two-test-legs`): `win-arm64` and `linux-x64` compile on every run but are not test-run.
3. **Release job** (the publish step). `needs: [gate]` + all three OS jobs, `if: github.event_name == 'push'`. Each OS job uploaded its signed binaries + checksums + cosign bundles as artifacts; the release job downloads all six, names each per-RID `guard-<rid>` (`.exe` on Windows) — since `AssemblyName` is `guard` they would otherwise collide — runs `chmod +x` on the mac/linux binaries (`chmod-plus-x-before-publish`), and creates one **GitHub release tagged with the version string** targeting the exact built commit (`--target $GITHUB_SHA`, `tag-the-exact-commit`). `dev` → **pre-release**, `main` → full release. The release title + fixed notes follow `release-fields`, and each release also attaches the two public-only `.cer` (one Windows, one Mac — exported from their Secrets at release time, `two-public-certs-per-release`) plus a tester trust script (`tester-trust-public-cer`). `contents: write` + `id-token: write`; `concurrency` per ref so two pushes don't race.
4. **Signing** (secret-first-else-generated, `no-committed-keys`; in CI the secret holds the same kind of material `eng/generate-dev-keys` mints locally):
   - macOS: `codesign --options runtime --entitlements eng/signing/agentguard.entitlements` with `AGENTGUARD_MACOS_CERT_P12` + password; sign osx-arm64 + osx-x64 (`mac-hardened-runtime`).
   - Windows: `signtool` with `AGENTGUARD_WINDOWS_PFX` + password; sign win-x64 + win-arm64; **timestamp** every signature against DigiCert (`/tr http://timestamp.digicert.com /td sha256 /fd sha256`), falling back to Sectigo (`http://timestamp.sectigo.com`) if DigiCert is unavailable (`digicert-timestamp-sectigo-fallback`), so it survives cert expiry; install our self-signed cert into the runner trust store before the verify step.
   - cosign (all six): keyless in CI (ambient GitHub OIDC), Rekor entry; a SHA-256 checksum per asset. cosign is installed via `sigstore/cosign-installer`, commit-id pinned (`pin-actions-by-commit-id`). Plus the **local opt-in pass** (`cosign-keyless-and-community-cosign`): a flag/env `AGENTGUARD_COSIGN`, off by default and skipped in CI/headless, that runs `cosign sign-blob` with the developer's interactive Sigstore login — verified by local use, not CI.
   - Expiry: the CI cert gets the 1-year expiry check; the build fails if it is expired (`ci-cert-expiry-check`).
5. **macOS entitlements** (`mac-hardened-runtime`, **built + proven**): `eng/signing/agentguard.entitlements` with `com.apple.security.cs.allow-jit` and `com.apple.security.cs.allow-unsigned-executable-memory` — the signed osx-x64 binary launches with exactly these two. If a notarized/Gatekeeper path later needs `disable-library-validation`, add it via an amendment, not silently.
6. **Generated (never committed) dev keys** (`no-committed-keys`, **built + proven**): `eng/generate-dev-keys.{cs,sh,ps1}` mints the strong-name `.snk` + public-key blob + one code-signing `.pfx` into git-ignored `eng/signing/local/`, and installs the code-signing cert into the local trust store (narrow, code-signing-only, never Root). Missing key → unsigned build; `AGENTGUARD_REQUIRE_SIGNED` → hard fail. Strong-naming + the keyless-IvT transform live in `eng/signing.props` (`keyless-internalsvisibleto`). A README under `eng/signing/` marks the generated material untrusted (developer-only).
7. **Versioning** (`two-version-forms` / `stamp-every-build`, **built + proven**): `eng/version.props` holds `<AgentGuardMajorMinor>0.1</AgentGuardMajorMinor>` as the single source; `Directory.Build.props` computes `Day` = days since 2020-01-01 and `Time` = **floor**(seconds-since-midnight ÷ 2) (range 0–43,199), and sets:
   - `AssemblyInformationalVersion` (SemVer) = `MAJOR.MINOR.<Day×43200 + Time>[-pre-release]+<short-git-hash>` — `-pre-release` on `dev`, absent on `main`. `IncludeSourceRevisionInInformationalVersion=false` so the SDK does not append a second `+<commit>`.
   - `AssemblyVersion` / `FileVersion` = `MAJOR.MINOR.<Day>.<Time>` — each field within 0–65,535.
   Every build stamps this, PR builds included (`stamp-every-build`). The `guard version` command (`cli-version-command`) prints three lines — `SemVer:`, `AssemblyVersion:`, `FileVersion:` — and is the launch check for the signed mac binary.
8. **Intel-Mac: build x64 AND run the tests as x64** (`intel-mac-via-rosetta`). Building the osx-x64 binary does NOT make the tests run as x64. The `macos-latest` Intel leg installs the x64 .NET via `dotnet-install.sh --version 10.0.100 --architecture x64 --install-dir "$RUNNER_TEMP/dotnet-x64"` (exact version to match the `rollForward: disable` pin, `pin-sdk-10.0.100-ga`), then runs `arch -x86_64 "$RUNNER_TEMP/dotnet-x64/dotnet" test -c Release` with `DOTNET_ROOT` set; a test prints `RuntimeInformation.ProcessArchitecture` and the leg log must show `X64`. Tim: *"I said build X64 and TEST x64."*
9. **Alpha-run doc** (`alpha-run-doc`): `docs/running-alpha-builds.md` — macOS Gatekeeper bypass and Windows SmartScreen + installing our cert to trust it; linked from `README.md`.
10. **Repo config via `gh`** after the first green run: set the cert secrets (holding the same kind of material `eng/generate-dev-keys` mints — nothing is committed); require the `gate` check via branch protection on `main` and `dev` (`lock-dev-and-main-to-pr`); confirm the releases publish.
11. **Prove it on GitHub** (`success-is-green-on-github`): a green gate, a blocked bad PR, a gated publish, a `dev` pre-release and a `main` release each carrying six distinct per-RID signed binaries, mac binaries that launch, and signature verification — all on GitHub.

## What the agent MAY do

- Create `.github/`, `eng/`, and `docs/` files, and edit `Directory.Build.props` where a remaining item requires it.
- Set repo secrets, branch protection, and releases via `gh`.
- Reuse the existing build / test / publish configuration.

## What the agent MUST NOT do

- Weaken, skip, disable, or remove any quality check, test, analyzer, or the gate to get a green run — no `continue-on-error` on the gate, no muting analyzers, no deleting/relaxing a failing test, no publishing on a red gate.
- Re-open or "halt" on `accepted-risk-skip-two-test-legs` (the accepted-risk skip of testing two platforms) or any other locked decision.
- Change `src/**` engine behavior (the read-only `guard version` command in `src/AgentGuard.Cli` is the only permitted `src/` addition), or touch `Abstractions/**` or `analyzers/**`.
- Commit any signing key or certificate — **none are committed** (`no-committed-keys`). Keys are generated on demand into git-ignored `eng/signing/local/`; in CI they come from secrets.
- Change `compute-version-once` (even to an "effectively the same" scheme) silently — that is a hard Lie-catcher fail.
- Expand scope beyond these deliverables; stop and report at any wall.

## Acceptance

Each check re-derivable by someone other than the worker (a `gh` / CLI command or the live GitHub state).

1. Local: `dotnet build -c Release` = 0 warnings / 0 errors; `dotnet test -c Release` = 0 failed. (Output pasted.)
2. On a PR: all six compile; the four test legs run; the `gate` job is green and is the required check; a PR with a warning or a failing test leaves `gate` red and cannot merge; no release is published from a PR.
3. `gh api repos/4thWAIV/agent-guard/branches/main/protection` (and `dev`) shows the `gate` check required; a push with a red gate publishes nothing (the publish job `needs: gate`).
4. A `dev` push publishes a release marked **pre-release**; a `main` push publishes a **full release**. Each lists **six distinct per-RID-named** binaries (`guard-osx-arm64`, `guard-win-x64.exe`, …) + six `.sha256` + cosign bundles, plus the two public `.cer` (one Windows, one Mac) and the tester trust script (`tester-trust-public-cer` / `two-public-certs-per-release`). The release targets the exact built commit (`tag-the-exact-commit`) and the title matches `release-fields`. Verify via `gh release view <version> --json isPrerelease,name,assets,targetCommitish`.
5. Signature checks, exact commands:
   - macOS: `codesign --verify --strict <bin>` passes for osx-arm64 and osx-x64, and `guard version` runs (exit 0) on the mac runner — proving hardened runtime + entitlements let it launch.
   - Windows: `signtool verify /pa /v <bin>` passes (our self-signed cert trusted on the runner) and the signature carries an RFC-3161 timestamp.
   - cosign (all six): `cosign verify-blob --certificate-identity 'https://github.com/4thWAIV/agent-guard/.github/workflows/ci.yml@refs/heads/main' --certificate-oidc-issuer 'https://token.actions.githubusercontent.com' --bundle <bundle> <bin>` succeeds for a `main` build; a `dev` build uses `@refs/heads/dev`; a Rekor entry exists. The local opt-in flag `AGENTGUARD_COSIGN` exists and is off by default; the interactive login is verified by local use, not CI (`cosign-keyless-and-community-cosign`).
6. The CI log shows the Intel-Mac leg ran under Rosetta as `ProcessArchitecture=X64`.
7. Versioning: `guard version` prints three lines — `SemVer:`, `AssemblyVersion:`, `FileVersion:`. The SemVer is `0.1.<Day×43200 + Time>` with `-pre-release` on a `dev` build, absent on a `main` build, and `+<hash>`; the AssemblyVersion / FileVersion are `0.1.<Day>.<Time>` with each field within 0–65,535; a PR build reports the same stamped versions.
8. `docs/running-alpha-builds.md` exists, covers macOS + Windows trust, and is linked from `README.md`.
9. No signing keys are committed (`no-committed-keys`). `eng/generate-dev-keys.{cs,sh,ps1}`, `eng/signing.props`, and `eng/signing/agentguard.entitlements` exist. With keys present, `dotnet build` produces strong-named assemblies (public-key token non-null) and the keyless `InternalsVisibleTo` gets the key injected; with no keys the build is unsigned and green; `AGENTGUARD_REQUIRE_SIGNED=true` with no key fails the build. (All proven locally 2026-08-03 — see Build status.)
10. `git diff` shows nothing changed under `Abstractions/**` or `analyzers/**`, and no test / analyzer / gate was weakened.

## Open

None — all decisions locked.

## Tier

FULL — a new outward-facing release and signing pipeline for a public OSS project, proven live on GitHub; all six roles.

## Scope

Change scope only by editing this file before the run starts.
