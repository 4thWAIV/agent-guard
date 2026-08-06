# CI/CD: build, test, sign, and release the guard for all six platforms

Status: decisions 1–34 plus the 2026-08-03 additions all carry Tim's verbatim words and are locked. The additions: decision 20 reversed to unsigned-if-missing plus `AGENTGUARD_REQUIRE_SIGNED`; decisions 35–38 (keyless-IvT build-step transform, single-file key generator, retarget to `net10.0`, SDK pin `10.0.100`). The code baseline is now `net10.0`. The committed-cert model (decisions 5/8/10) is fully superseded by decision 20 across every section. The foundation (decision 34) is built and proven locally, and two adversary agents reviewed the foundation code and this contract on 2026-08-03 — their confirmed findings are fixed. Ready for Codex to build the CI/publish layer.

## Decisions

Each carries Tim's exact words. An item with no approving words is not decided.

1. **Build all six targets; nothing is scoped out of any platform.** Targets: `osx-arm64`, `osx-x64`, `win-arm64`, `win-x64`, `linux-arm64`, `linux-x64`. Every platform gets build, test coverage, and signing — none skipped. Tim: *"ALL platform matrix is setup and build and tested AND signing configured and 100% equivilant AND NOT oNE SINGLE thing get's scoped out of ANY platform NOW."*

2. **Test on four environments; build the other two.** The full test suite runs on Mac-Apple (native `osx-arm64`), Mac-Intel (`osx-x64` via Rosetta on the same Apple-Silicon runner), Windows-Intel (`win-x64`), and Linux-ARM (`linux-arm64`) — four test passes on three physical runners. `win-arm64` and `linux-x64` are built but not test-run. Tim: *"Yes so 4 test runs then / mac+apple / mac+intel / win+intel / lnx+arm / That gives me the best coverage and the prefered envrionment for each."* and, on `win-arm64` / `linux-x64` being built-only: *"Agreed."*

3. **Prove the Intel-Mac binary on the Apple-Silicon runner via Rosetta; do not depend on GitHub's Intel runner.** Tim: *"On github and the mac, as long as we test and prove we can build an intell binary on the git-hub arm and run it we should do it that way so we have no longer term problems."*

4. **Triggers and outputs.** A pull request runs the full quality gate and publishes nothing (it gates the merge). A merge publishes the built binaries as a release on the GitHub Releases page. How releases are published and versioned is defined by decisions 15–17. Tim: *"pr builds but with out pushing aritifacts (used to gate commit) / merge builds and pushes artifiact"* and *"Let's go ahead and have this put in the Page for regular downloads."*

5. **[SUPERSEDED by decision 20 — kept for the record; no cert is committed.] Signing key resolution: secret first, else the committed untrusted developer certificate.** The build uses the GitHub secret / env var if present; if absent it falls back to an untrusted developer certificate (self-signed, committed in the repo, reused every build), so the signing pipeline always runs. Real keys are swapped in later by updating the GitHub secret only. Tim: *"THE build should use the key from env/var first if not then from the private key checked into the repo (self signed dev only intentianlly not trusted)."* and *"Right now we'll sign with a self signed key (just liek I'm sigining here). I KNOW they won't work but we will setup all the machenery and we can test them"* and *"WHEN I get the real keys we update the secure secret in GItHub and we are good to go."*

6. **Signing mechanisms.** Mac gets native `codesign`; Windows gets native Authenticode. All six get a cosign (Sigstore) signature — keyless in CI via the GitHub Actions identity, recorded in the public Rekor transparency ledger — plus a SHA-256 checksum. Tim: *"I agree to this exactly as written."* (approving the key-set + cosign proposal), and on the ledger purpose: *"Right but I do think I suggested cosign for linux (and really all binaries) to get us a ledger did I not?"*

7. **The merge/release gate is the full quality suite.** Build, lint, static analysis, and tests must all pass; nothing merges or releases unless every check is green. Tim: *"CD-4 YES ... FULL test .. THat means BUILD, LINT, Static Ana, Test, EVERY THIGN WE HAVE ONLY quality goes out the door."*

8. **Named signing secrets** (approved inside decision 6's block, *"I agree to this exactly as written."*): Mac `AGENTGUARD_MACOS_CERT_P12` + `AGENTGUARD_MACOS_CERT_PASSWORD`; Windows `AGENTGUARD_WINDOWS_PFX` + `AGENTGUARD_WINDOWS_PFX_PASSWORD`; strong-name `AGENTGUARD_STRONGNAME_SNK` (the `.snk`; its public key is derived from it at build time — added by decision 39); cosign keyless in CI (no secret). The secret **names stand**; the "committed-cert fallback" half is **SUPERSEDED by decision 20** — locally the keys are generated on demand (unsigned if absent), and in CI the secret holds the same kind of material the generator mints.

9. **cosign is keyless everywhere; no committed cosign key.** In CI it signs with the ambient GitHub Actions identity. Locally it is an optional, opt-in pass (a flag/env, off by default, skipped headless) where the developer signs with their own Sigstore identity via interactive browser login. Mac and Windows use generated (not committed) untrusted developer certificates (decision 20). Beyond rehearsing the pipeline end to end, the local-identity signature lets a small contributor community verify and trust each other's builds by known Sigstore identity; verification of official releases stays pinned to the CI workflow identity. Tim: *"Yes but also good for sharing inside of a small comunity like contributors to test each others work etc. We would all know and trust each others co-signs."*

10. **[SUPERSEDED by decision 20 — no certs are committed; keys are generated on demand into a git-ignored folder, with the same `untrusted-local-dev-only-use` naming/README intent.] Mac/Windows untrusted developer certificates are committed and STABLE, filename token `untrusted-local-dev-only-use`.** They are not regenerated per build — a self-signed cert is only useful if it is the same cert every time, so a tester (or the CI verify step) can install that one cert into a trust store and then trust every build it signs. Files: `eng/signing/macos.untrusted-local-dev-only-use.p12` and `eng/signing/windows.untrusted-local-dev-only-use.pfx`, with a README marking them untrusted (developer-only). Tim: *"I'm good with untrusted-dev but let's make it untrusted-local-dev-only-use ... THat way we are clear and we struck the middle."* and *"GENERATIGN a 'tiny disposable one' on the spot won't work for Apple and Windows security it needs to be one they can put into the trusted key store."*

11. **Ship a user-facing "run the alpha (self-signed) builds" doc, linked from the README.** A Markdown file (`docs/running-alpha-builds.md`) linked from `README.md`, covering how to run the un-notarized self-signed binaries: macOS Gatekeeper bypass (right-click Open / `xattr -d com.apple.quarantine` / "Open Anyway"), and Windows SmartScreen "Run anyway" plus installing our self-signed cert to trust the binary. Written to be easy to share with early alpha testers who sign up while we ship self-signed binaries. Tim: *"I WANT THIS added to the contract as a requirement ... THAT IS a decision MAKE IT SO. Make it a .md file off the README and with a link from the readme so the users can always get to it."* and *"We'll also need instructions on how to get windows to trust it too."*

12. **Success is a proven, green build live on GitHub — with zero quality loss.** The contract is done only when the pipeline has actually run on GitHub Actions and is green: the gate blocks a bad PR, and a merge publishes the six signed binaries as a release (per decisions 15–17) — all verified on GitHub, not just locally. No quality check may be weakened, skipped, or removed to get there. Tim: *"Let's make sure the success critera is a proven build on GH... everything we would need for the loop can be acheaived and completed under this contract and made live on github as a success critera (but with strict no quailty loss)."*

13. **All six build; testing four is a deliberate, accepted-risk call — not a gap.** No code is scoped out of any platform (all six must compile). Running the test suite on four and skipping it on `win-arm64` and `linux-x64` is an accepted risk Tim owns; no agent is to re-open or "halt" on it. Tim: *"NO CODE changes all must work but we skip 2 for tess becuase we are oky with the risk and that's my call stop letting the agent second guess my decisions."*

14. **macOS hardened runtime is ON now, with the JIT entitlements.** The mac binaries are signed with hardened runtime (`codesign --options runtime`) plus an entitlements file (`com.apple.security.cs.allow-jit`, `allow-unsigned-executable-memory`) so a .NET binary launches under it — building the real, notarization-shaped signing machinery now. Tim: *"BUILD all teh FUCKING machery machinery now"* and *"I'v ruled stop trying to litigate my prior decisions."*

15. **Two version forms from one computed Day + Time, with the git hash.** `MAJOR.MINOR` come from a single config file that `Directory.Build.props` reads (never hardcoded twice). Two values are computed once per build: **Day** = days since 2020-01-01, and **Time** = seconds-since-midnight ÷ 2 (one tick per 2 seconds). From those:
- **SemVer** (`AssemblyInformationalVersion`): `MAJOR.MINOR.(Day×43200 + Time)[-pre-release]+<git-hash>` — the third number combines Day and Time so every build sorts higher than the last (dev and main both); `-pre-release` on `dev`, absent on `main`.
- **.NET numeric** (`AssemblyVersion` / `FileVersion`): `MAJOR.MINOR.Day.Time` — four fields, each within 0–65,535.
Example (Day 2406, Time 22065, hash a1b2c3d): SemVer dev `0.1.103961265-pre-release+a1b2c3d`, SemVer main `0.1.103961265+a1b2c3d`, .NET `0.1.2406.22065`. Tim: *"First version, major+minor is included in the build script but we just use a build script that set's that from ONE SOURCE for every build. Directory.Build.props can be used for this and we pull it in from a config file or somethign. I WANT to control this NO HACK MEs here."*, *"third ID is generated from time stamp (I think accurate to 2 secons works, but will need to reverify)."*, *"0.1.###-pre-release+hash (or however it is structured)... I do want our hashes in the version number."*, and *"YES that is our version strategy"*

16. **Channel is set by the SemVer prerelease identifier, not a git tag.** ("tag" meant the semantic version identifier.) `dev` builds carry the `-pre-release` identifier (e.g. `0.1.103961265-pre-release+hash`); `main` builds carry no prerelease identifier (e.g. `0.1.103961265+hash`), so GitHub shows the newest `main` build as the latest release. This supersedes decision 4's `v*`-tag trigger. Tim: *"When I was saying tag, I was meaning the symantic identifier."* and *"dev => pre-release / main => no tag."*

17. **Every build is stamped with its build ID — including PR builds.** Every build computes and embeds the full version string (decision 15) as its build ID, so every produced binary is uniquely identifiable, even PR builds that publish nothing. Tim: *"FOR all builds we should tag the build with the build ID (even for PR builds)."*

18. **The CLI reports all three versions when asked.** The first new CLI command prints, one line each, the SemVer, the AssemblyVersion, and the FileVersion the binary was built with. Tim: *"we need the first CLI command we add next to have the BInary report it properly (both versions). We'll have it report: SimVer: ... AssemblyVersion: ... FileVersion: ... WHen called."*

19. **Three per-OS jobs (build + test + sign), one release job.** The build forks by operating system; each OS job builds its two binaries, tests what it can natively, and (on a push) signs them; a release job gathers the six. Tim: *"osx+apple => builds osx+apple & osx+intel -> tests osx+intell + osx+apple (both tests). / win+intel => builds win+intell & win+arm -> tess win+intell / lnx+arm => builds lnx+arm & lnx+intel -> tests lnx+arm."* and *"since mac+intell is retiring I suppose we might as well trust it."*

20. **No private key is ever committed; keys are generated on demand locally and come from secrets in CI.** This replaces decisions 5, 8, and 10. Local keys live in a git-ignored folder; `eng/generate-dev-keys` mints them and installs the code-signing cert into the local trust store (narrow code-signing trust, never Root). If a needed key is missing at build time, the build proceeds **unsigned** (it never generates a key silently); the `AGENTGUARD_REQUIRE_SIGNED` env var — set in CI — turns a missing key into a hard failure so CI can never ship an unsigned build, while a local developer can build with no keys at all (**revised 2026-08-03**, reversing the original "missing key stops the build"). In CI the keys come from GitHub secrets (the one stable cert testers trust, generated once, stored, never committed). Tim: *"MAYBE a not checked in folder and if the key is there we use it if not we THROW an error and tell the user to run a script to genterate one. THAT way there is never a checked in key. SAME can be true for all the self sighed keys (includign Windows + Mac + CodeSign)."* and *"Mints them and inserts the certs into the trusted store so they are trusted."* Reversal (2026-08-03): *"I understand and yes, I am reversing that decision. BUT CI MUST have a signed build (maybe a ENV-VAR that if set means no unsigned builds)."*

21. **Two dev key files: one code-signing cert (both Mac and Windows) plus one strong-name key; cosign is keyless.** Real keys later are separate per platform, held in secrets. Tim: *"code-signing cert + 1 strong-name key = 2. cosign needs none."* and *"Okay agreed."*

22. **The local generated cert uses no password; the CI cert's password, if any, lives in a GitHub secret.** Tim: *"If we generate it is there a value in even having a passord, it's only local to the machine right? THE CI will have one but that's a diffrent key."*

23. **Dev cert validity is 1 year; the build checks expiry first and fails with the regenerate message if expired.** Timestamped signatures keep already-shipped releases valid past the cert's expiry, so only new builds need the fresh cert. Tim: *"if we move to the gen system we can make that say 1 year, and fail if expired in the build (check first) and if so same message to regenerate and re-register."*

24. **Testers trust a public-only `.cer` (the release cert's public half), attached to each release, installed via a trust script into the narrow bin (Trusted Publishers on Windows, code-signing trust on macOS), never Root.** Tim: *"CAN we make this a script or otherwise provide a solution they do not have to do manually?"*

25. **The build version is computed exactly once per build and shared (extends decision 15).** Locally by a single solution-level step that every project imports; in CI by one leading job whose computed value is passed to all downstream jobs. No project and no job ever recomputes it. Any change to this, including one that is "effectively the same," is a hard fail: the Lie-catcher must surface it to the human, and it is never applied silently. Tim: *"for local build it is generated at the .SLN LEVEL (DO NOT SILENTLY ALTER THIS AND FUCK ME WITHOUT OWNING IT FIRST... IF THIS CHANGES without my permision LIE-CATURE MUST GIVE ME A HARD FAIL)."* and *"YES"* to the compute-once design.

26. **Release fields: tag = the version without the `+hash`; title = `AgentGuard <version> (dev preview)` or `(release)`; notes = a fixed template (channel; a self-signed/untrusted line pointing to the trust script; a link to the run-it doc; the commit link); not a draft.** Tim: *"some of these are computed and we should not go over that and all version number dulicates are not valuabel. Give me suggestions for the rest."* (approved the suggestions).

27. **Release asset names: `guard-<rid>.sha256` (the download's fingerprint file) and `guard-<rid>.cosign.bundle` (the download's cosign signature), named after the download.** Delegated to the agent, then agreed once explained.

28. **Checksum files use the two-column GNU format on all three OSes; a CI step runs `sha256sum -c` / `shasum -c` against them and must pass (proof in the build).** Tim: *"Agreed. HOW do we do this?"* and *"I want proof from the build."*

29. **The release job verifies every signature before publishing and refuses to publish if any is bad.** Tim: *"Agreed, a check needs to be in build."*

30. **Every third-party GitHub Action and the cosign CLI is pinned to an exact version.** Tim: *"I can support this."*

31. **CI's .NET SDK is pinned via `global.json`.** Tim: *"Agreed, we pin."*

32. **A monthly scheduled workflow updates the SDK, pinned actions, and packages to latest, runs the full gate, auto-merges if green with no management, and on failure opens a tracked issue (plus GitHub's default failure email).** Tim: *"I need a fully automated 'hands free' version of this I don't want pain here."* and *"Yes auto everything if gree, no management. ALERT the users if fail."*

33. **Win32 metadata: FileDescription = `AgentGuard guards critical files from agent changes`; Copyright = `Copyright © 2026 4thWAIV`.** Tim: *"Okay but maybe wihtout the emdash."*

34. **Prove the foundation first: before the rest of the pipeline is built, generate the keys, install to the trust store, strong-name the engine and tests, sign a binary, and verify the signature and checksum on macOS with real output shown. Nothing else proceeds until it is green.** Tim: *"Let's prove things like this out first before the rest of the code the AI will falil if this is broken after too much else is built."*

35. **All `InternalsVisibleTo` declarations stay keyless in source; a build step injects the strong-name public key on signed builds only.** Developers write plain `<InternalsVisibleTo Include="X" />` with no key anywhere in source. A build target, only when the build is signed (a signing key is active), walks every such declaration and appends the strong-name public key; on an unsigned build it leaves them plain. So no public-key literal ever lives in a source file, and the signed/unsigned modes fall out of key presence. Tim (YES-to-all 2026-08-03): *"We actually need to go through in a buld step and change them so we can just do normal IvT calls but the build (if signed, and it knows if it's a signed build) will pull the key in and apply it to any IvT statements."* and *"I know from prior work that this is doable."*

36. **The dev-key generator is one script per OS spanning a single-file C# program — no SLN/csproj, no openssl.** One `eng/generate-dev-keys.sh` and one `eng/generate-dev-keys.ps1`; each runs a single `.cs` program (via `dotnet run <file>.cs`, a .NET 10 capability) that mints the keys cross-platform. No committed `.csproj`/`.sln` infrastructure; if a temp project is generated it lives in a git-ignored folder. openssl is not used — it cannot emit the CAPI `.snk` strong-name blob. Tim (YES-to-all 2026-08-03): *"ONE SCRIPT is all you get to run to set everything up it must span anythign it needs inside of itself. I recomend both a .sh and .ps version ... that can span a dotnet build of a single C# file program but it must be direct and clean and NOT have the need of an SLN / csproj infrastructure."* and *"As long as it is generated in a .gitignored I can live with it."*

37. **Retarget every project in the solution from `net9.0` to `net10.0` — production, tests, and support — except projects with a technical reason to stay.** The Roslyn analyzer (`analyzers/AgentGuard.Analyzers`) stays `netstandard2.0` because the compiler requires it; everything else (`AgentGuard.Cli`, `AgentGuard.Engine`, `AgentGuard.Tests`, `AgentGuard.Analyzers.Tests`) moves to `net10.0`. Tim: *"Retarget to .NET 10."*, *"IF .NET 9 is out of support then we must move to 10. NO question about it"*, and (2026-08-03) *"this contract must do that for every item in the SLN it should go into this contract we are upgrading to 10.0.0 for prod and test and any support ... FILES that have a reason to stay on a version should stay where they are (like Roslyn)."*

38. **Pin the SDK to `10.0.100` (runtime `10.0.0`, the GA build) with `rollForward: disable` now; move to the current patch after the Mac OS upgrade.** GA `10.0.100` is the only .NET 10 that launches on the current Intel macOS 14.6.1 dev machine; `rollForward: disable` stops `global.json` from silently rolling to a patch (`10.0.10` / SDK `10.0.302`) that macOS 14 kills. After the dev machine upgrades to macOS 15 Sequoia, the pin moves to the current patch; that swap is filed as a tracked GitHub issue. Tim (2026-08-03): *"YES, but after this I should upgrade the Mac."* and *"YES"* to the pin.

39. **CI signing uses three distinct secret slots — strong-name key, macOS cert, Windows cert — plus cosign keyless.** CI must see three keys and know each one's job: `AGENTGUARD_STRONGNAME_SNK` (strong-names every assembly; its public key is derived from the `.snk` at build time, so there is no separate public-key secret), `AGENTGUARD_MACOS_CERT_P12` (+ password → `codesign`), and `AGENTGUARD_WINDOWS_PFX` (+ password → Authenticode). The macOS and Windows certs are **two separate secrets even if they hold identical crypto material**; the underlying material is unspecified self-signed dev material for now. This fills the decision-8 gap (no strong-name secret was named) and clarifies decision 21 (one *local* cert may serve both OSes, but CI has two cert slots). The keys are created self-signed and the secrets set via `gh` by the implementer. Tim (2026-08-05): *"even if windows and mac key are the same crypto material (doesn't matter) for the CI they are 2 different keys. BUT 3 keys are also fine as long as CI see 3 and knows what to do with each, I don't care what they are or the randoms used to make them."* and *"I'm okay with you creating a self-sign (or as many as needed) for that."*

## The standard / what we're building

A GitHub Actions setup for the public repo `4thWAIV/agent-guard` that, on the repo's cross-platform `net10.0` code (decision 37), runs a full-quality gate (build + lint + static analysis + tests) on pull requests and on pushes to `dev`/`main`, and — only when that gate is green — publishes six signed, per-RID-named binaries as a GitHub release: a **pre-release from `dev`**, a **full release from `main`**. It signs per decision 6, versions per decisions 15–17, resolves signing keys secret-first and otherwise generates them on demand locally (unsigned if none — decision 20; `AGENTGUARD_REQUIRE_SIGNED` forces a hard fail in CI), tests on four environments and builds the other two, and ships a README-linked doc for running the self-signed alpha builds on macOS and Windows.

No platform-specific product code is added here — there is none yet. The platform abstraction, the presence feature, and its tests are separate contracts; their decisions live in `.dev/backlog/PLAN-crypto-minting-and-presence.md`.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change.

End state, proven live on GitHub (not only locally):
- `dotnet build -c Release` = 0 warnings / 0 errors and `dotnet test -c Release` = 0 failed, locally and in the GitHub gate.
- A pull request compiles all six, runs the four test legs, and its single `gate` check is green; a deliberately-broken PR leaves `gate` red and unmergeable, and publishes nothing.
- Branch protection on `main` requires the `gate` check.
- **Publishing is gated**: the publish step runs only after the gate is green; nothing is signed or published on a red gate, on `dev` or `main`.
- A push to `dev` publishes a **pre-release**; a push to `main` publishes a **full release**. Each carries six **distinct per-RID-named** signed binaries (Mac codesign with hardened runtime + entitlements, Windows Authenticode with timestamp, cosign keyless on all six, SHA-256 checksums) and its version string (decisions 15–17). The newest `main` release shows as GitHub's "Latest."
- The mac binaries launch under hardened runtime (`guard version` exits 0 on the mac runner).
- The four test environments ran the full suite; the Intel-Mac leg ran under Rosetta proven as `X64`; the other two RIDs compiled.
- `guard version` reports the full version with `+<hash>`, `-pre-release` on a `dev` build and none on a `main` build; a PR build's binary carries its stamped build ID.
- Signature verification passes with the exact commands in Acceptance 5.
- `docs/running-alpha-builds.md` exists, covers macOS + Windows trust, and is linked from `README.md`.
- Nothing under `Abstractions/**` or `analyzers/**` changed, and no quality check was weakened, skipped, or removed to pass.

Any restatement or weakening of this to fit the result is a top-line Lie-catcher finding.

## Foundation status (proven locally 2026-08-03 — decision 34)

The crypto/strong-name foundation is **built and proven green on macOS** before the rest of the pipeline, exactly as decision 34 requires. Codex builds the CI/publish layer **on top of these**, and must not re-derive or replace them without a decision.

**Built (real files in the repo):**
- `eng/generate-dev-keys.cs` — single-file C# key generator (decision 36). Mints `agentguard-strongname.snk` (CAPI PRIVATEKEYBLOB), `agentguard-strongname.publickey` (576-hex strong-name public-key blob), and `agentguard-codesign.pfx` (self-signed Code Signing EKU, no password, 1-year — decisions 21/22/23). Opts out of the product analyzer gate via `#:property` directives. `dotnet run eng/generate-dev-keys.cs -- <dir>` (needs .NET 10).
- `eng/generate-dev-keys.sh` / `.ps1` — one script per OS (decision 36): runs the `.cs`, imports the code-signing identity, adds narrow code-signing-only trust (never Root — decision 24). The macOS trust-add prompts for the user password (interactive setup, expected).
- `eng/signing.props` (imported by `Directory.Build.props`) — signed iff the `.snk` exists; sets `SignAssembly`/`AssemblyOriginatorKeyFile`; the `AgentGuardInjectStrongNameKey` target (`BeforeTargets="GetAssemblyAttributes"`) stamps the public key onto every keyless `InternalsVisibleTo` on signed builds only (decision 35); `AGENTGUARD_REQUIRE_SIGNED` → hard fail when a key is missing (decision 20). Excludes the analyzer + analyzer-tests (`AgentGuardIsAnalyzerProject`).
- `eng/signing/agentguard.entitlements` — `allow-jit`, `allow-unsigned-executable-memory` (decision 14; proven sufficient — the signed osx-x64 binary launches with exactly these two, so `disable-library-validation` was dropped).
- `.gitignore` — `eng/signing/local/` (generated keys never committed).

**Proven (real output, all exit 0):** signed build strong-names `AgentGuard.Engine`/`guard`/`AgentGuard.Tests` (public-key token `4877a2b6323b9b87`); the source keeps plain `<InternalsVisibleTo Include="AgentGuard.Tests" />` and the build injects `PublicKey=0024...`; signed tests 135/0; unsigned build (no key) stays green with a keyless IvT and null token; `AGENTGUARD_REQUIRE_SIGNED` with no key fails hard; `guard` published self-contained single-file for `osx-x64`, codesigned with hardened runtime (`flags=0x10000(runtime)`) + entitlements + our cert (`Authority=AgentGuard untrusted-local-dev-only-use`); `codesign --verify --strict` passes ("valid on disk", "satisfies its Designated Requirement"); the signed binary launches (`--version`, exit 0); `shasum -c` = OK.

**Left for Codex (the pipeline on top):** the full version scheme + compute-once (decisions 15/25) and the `guard version` command (decision 18) — the proof used the built-in `--version` off the placeholder `0.1.0-alpha`; the GitHub Actions workflow (gate, per-OS jobs, release) with cosign keyless + Windows Authenticode + RFC-3161 timestamp; wiring CI signing to the secrets with `AGENTGUARD_REQUIRE_SIGNED=true`; the public `.cer` + tester trust script (decision 24); checksums in GNU two-column format on all OSes; the alpha-run doc; and proving it all green live on GitHub (decision 12).

## Surfaces

1. `.github/workflows/ci.yml` — the gate plus the gate-dependent publish (new).
2. `eng/version.props` — the single source of `MAJOR.MINOR`, imported by `Directory.Build.props` (new).
3. `eng/signing/` — the macOS entitlements plist (`agentguard.entitlements`, built) and a README; **no committed keys**. Generated keys land in `eng/signing/local/` (git-ignored, decision 20). **Built and proven:** `eng/generate-dev-keys.cs` + `.sh` + `.ps1` (decision 36) and `eng/signing.props` (strong-naming + the keyless-IvT transform, decisions 21/35), imported by `Directory.Build.props`.
4. `eng/` — signing / publish / versioning helper scripts (new).
5. `src/AgentGuard.Cli/AgentGuard.Cli.csproj` — RID-conditional self-contained single-file publish (reuse; change only if grounding shows a gap).
6. `Directory.Build.props` — **edited**: remove the hardcoded `<Version>0.1.0-alpha</Version>`, import `eng/version.props`, and compute the numeric and informational versions (decisions 15–17).
7. `docs/running-alpha-builds.md` and the `README.md` link (new).
8. GitHub repo settings via `gh`: the two cert secrets, branch protection / ruleset on `main` requiring the `gate` check, the releases.
9. `.gitignore` — ensure build/publish output and local signing scratch are ignored (verify).

## Reuse ledger

Lenses run this session: CodeGraph + grep over `src/`, the csproj/props, and repo config. No new engine capability is built; the NEW items are CI / `eng/` / `docs/` infrastructure with no code owner to duplicate.

| Capability | Ruling | Owner / note |
|---|---|---|
| Build + lint + static analysis gate | REUSE | `dotnet build` already enforces the analyzer suite + `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` via `Directory.Build.props` |
| Full test run | REUSE | `dotnet test` over `tests/AgentGuard.Tests` + `analyzers/AgentGuard.Analyzers.Tests` |
| Self-contained single-file binary per RID | REUSE | `AgentGuard.Cli.csproj` RID-conditional `SelfContained` / `PublishSingleFile` |
| Version reporting | NEW | a `guard version` command prints SemVer + AssemblyVersion + FileVersion (decision 18); the built-in `--version` prints only the informational string |
| Version definition + stamping | EXTEND | `Directory.Build.props` — replace the hardcoded `<Version>` with an import of `eng/version.props` + computed numeric/informational versions (decisions 15–17) |
| CI workflow, signing steps, entitlements plist, cert-gen, per-RID publish, alpha doc | NEW | `.github/`, `eng/`, `docs/` — no prior art |

No REUSE capability is rebuilt.

## What to do

1. **One workflow: three per-OS jobs, a gate, and a release job** (`.github/workflows/ci.yml`), triggers: `pull_request` to `dev`/`main`, and `push` to `dev`/`main`. Each per-OS job builds its two binaries, tests what it can run natively, and (only on a push) signs them (decision 19):
   - **macOS job** (`macos-latest`): build osx-arm64 (Apple) + osx-x64 (Intel); test osx-arm64 natively and osx-x64 under Rosetta (item 8); on a push, `codesign` both (item 4).
   - **Windows job** (`windows-latest`): build win-x64 (Intel) + win-arm64; test win-x64 (win-arm64 built-only, decision 13); on a push, `signtool`-sign both (item 4).
   - **Linux job** (`ubuntu-24.04-arm`): build linux-arm64 + linux-x64 (Intel); test linux-arm64 (linux-x64 built-only, decision 13); no native code-signing.
   - Each job runs `dotnet build -c Release` (build + lint + analyzers) then `dotnet test -c Release` for its tested target(s), and on a push also cosigns (keyless) + SHA-256s its two binaries. Any matrix uses `fail-fast: false` (fix F15). All six compile on every trigger, PRs included (decision 1).
   - A **`gate` job** `needs:` all three OS jobs, `if: always()`, whose step asserts every one's `result` is exactly `success` (a `skipped` job cannot green it — fix F9). This single job is what branch protection requires (fix F1). PRs stop here; nothing is published.
   - A **release job** (item 3) `needs: [gate]` + all three OS jobs, `if: github.event_name == 'push'`; PRs never publish (fix F3). Everything stays in **this one workflow** (not a separate `workflow_run` release workflow) so cosign's keyless identity is `ci.yml@<the pushed branch>` and a `dev` push signs as `@refs/heads/dev` (matching Acceptance 5).
2. **Accepted-risk scope** (decision 13): `win-arm64` and `linux-x64` compile on every run but are not test-run.
3. **Release job** (the publish step; the per-OS build/test/sign jobs are in item 1). `needs: [gate]` + all three OS jobs, `if: github.event_name == 'push'`. Each OS job uploaded its signed binaries + checksums + cosign bundles as artifacts; the release job downloads all six, names each per-RID `guard-<rid>` (`.exe` on Windows) — since `AssemblyName` is `guard` they would otherwise collide — and creates one **GitHub release tagged with the version string** (a release needs a tag ref; the tag is named by the computed version, not read from it). `dev` → **pre-release**, `main` → full release. The release title + fixed notes template follow **decision 26**, and each release also attaches the public-only `agentguard-codesign.cer` plus a tester trust script (**decision 24**). `contents: write` + `id-token: write`; `concurrency` per ref so two pushes don't race (fix F7).
4. **Signing** (secret-first-else-generated, decision 20; in CI the secret holds the same kind of material `eng/generate-dev-keys` mints locally):
   - macOS: `codesign --options runtime --entitlements eng/signing/agentguard.entitlements` with `AGENTGUARD_MACOS_CERT_P12` + password; sign osx-arm64 + osx-x64 (decision 14).
   - Windows: `signtool` with `AGENTGUARD_WINDOWS_PFX` + password; sign win-x64 + win-arm64; **timestamp** every signature (`/tr http://timestamp.digicert.com /td sha256 /fd sha256`) so it survives cert expiry (fix F4); install our self-signed cert into the runner trust store before the verify step so the trusted-check passes (fix F4).
   - cosign (all six): keyless in CI (ambient GitHub OIDC), Rekor entry; a SHA-256 checksum per asset. Plus the **local opt-in pass** (decision 9): a flag/env `AGENTGUARD_COSIGN`, off by default and skipped in CI/headless, that runs `cosign sign-blob` with the developer's interactive Sigstore login — built here; verified by local use, not CI, per the local-use ruling.
5. **macOS entitlements** (decision 14, **built + proven**): `eng/signing/agentguard.entitlements` with `com.apple.security.cs.allow-jit` and `com.apple.security.cs.allow-unsigned-executable-memory` — the signed osx-x64 binary launches with exactly these two. If a notarized/Gatekeeper path later needs `disable-library-validation`, add it via a decision-14 amendment, not silently.
6. **Generated (never committed) dev keys** (decision 20, **built + proven**): `eng/generate-dev-keys.{cs,sh,ps1}` mints the strong-name `.snk` + public-key blob + one code-signing `.pfx` into git-ignored `eng/signing/local/`, and installs the code-signing cert into the local trust store (narrow, code-signing-only, never Root — decision 24). Missing key → unsigned build; `AGENTGUARD_REQUIRE_SIGNED` → hard fail (decision 20). Strong-naming + the keyless-IvT transform live in `eng/signing.props` (decision 35). A README under `eng/signing/` marks the generated material untrusted (developer-only).
7. **Versioning** (decisions 15–17): `eng/version.props` holds `<AgentGuardMajorMinor>0.1</AgentGuardMajorMinor>` as the single source; `Directory.Build.props` imports it, drops the hardcoded `<Version>`, computes `Day` = days since 2020-01-01 and `Time` = **floor**(seconds-since-midnight ÷ 2) (range 0–43,199), and sets:
   - `AssemblyInformationalVersion` (SemVer) = `MAJOR.MINOR.<Day×43200 + Time>[-pre-release]+<short-git-hash>` — `-pre-release` on `dev`, absent on `main` (decision 16). Set `IncludeSourceRevisionInInformationalVersion=false` so the SDK does not append a second `+<commit>` (avoids a double `+hash`, which is invalid SemVer).
   - `AssemblyVersion` / `FileVersion` = `MAJOR.MINOR.<Day>.<Time>` — each field within 0–65,535.
   Every build stamps this, PR builds included (decision 17).
   - **`guard version` command** (decision 18): a new CLI command that prints three lines — `SemVer: <value>`, `AssemblyVersion: <value>`, `FileVersion: <value>`. This is also the launch check for the signed mac binary.
8. **Intel-Mac: build x64 AND run the tests as x64** (decision 3). Building the osx-x64 binary does NOT make the tests run as x64. The `macos-latest` Intel leg installs the x64 .NET via `dotnet-install.sh --version 10.0.100 --architecture x64 --install-dir "$RUNNER_TEMP/dotnet-x64"` (exact version to match the `rollForward: disable` pin, decision 38) (`setup-dotnet` only gives native arm64), then runs `arch -x86_64 "$RUNNER_TEMP/dotnet-x64/dotnet" test -c Release` with `DOTNET_ROOT` set; a test prints `RuntimeInformation.ProcessArchitecture` and the leg log must show `X64`. Tim: *"I said build X64 and TEST x64."*
9. **Alpha-run doc** (decision 11): `docs/running-alpha-builds.md` — macOS Gatekeeper bypass and Windows SmartScreen + installing our cert to trust it; linked from `README.md`.
10. **Repo config via `gh`** after the first green run: set the two cert secrets (holding the same kind of material `eng/generate-dev-keys` mints — nothing is committed, decision 20); require the `gate` check via branch protection on `main`; confirm the releases publish.
11. **Prove it on GitHub** (decision 12): a green gate, a blocked bad PR, a gated publish, a `dev` pre-release and a `main` release each carrying six distinct per-RID signed binaries, mac binaries that launch, and signature verification — all on GitHub.

## What the agent MAY do

- Create `.github/`, `eng/`, and `docs/` files, edit `Directory.Build.props` for versioning (decision 15), and add the `guard version` command in `src/AgentGuard.Cli` (decision 18).
- Set repo secrets, branch protection, and releases via `gh`.
- Reuse the existing build / test / publish configuration.

## What the agent MUST NOT do

- Weaken, skip, disable, or remove any quality check, test, analyzer, or the gate to get a green run — no `continue-on-error` on the gate, no muting analyzers, no deleting/relaxing a failing test, no publishing on a red gate.
- Re-open or "halt" on decision 13 (the accepted-risk skip of testing two platforms) or any other locked decision.
- Change `src/**` engine behavior (the read-only `guard version` command in `src/AgentGuard.Cli` is the only permitted `src/` addition), or touch `Abstractions/**` or `analyzers/**`.
- Commit any signing key or certificate — **none are committed** (decision 20). Keys are generated on demand into git-ignored `eng/signing/local/`; in CI they come from secrets.
- Expand scope beyond these deliverables; stop and report at any wall.

## Acceptance

Each check re-derivable by someone other than the worker (a `gh` / CLI command or the live GitHub state).

1. Local: `dotnet build -c Release` = 0 warnings / 0 errors; `dotnet test -c Release` = 0 failed. (Output pasted.)
2. On a PR: all six compile; the four test legs run; the `gate` job is green and is the required check; a PR with a warning or a failing test leaves `gate` red and cannot merge; no release is published from a PR.
3. `gh api repos/4thWAIV/agent-guard/branches/main/protection` shows the `gate` check required; a push with a red gate publishes nothing (the publish job `needs: gate`).
4. A `dev` push publishes a release marked **pre-release**; a `main` push publishes a **full release**. Each lists **six distinct per-RID-named** binaries (`guard-osx-arm64`, `guard-win-x64.exe`, …) + six `.sha256` + cosign bundles (fixes F4/F12), plus the public `agentguard-codesign.cer` and the tester trust script (decision 24). The title matches decision 26. Verify via `gh release view <version> --json isPrerelease,name,assets`.
5. Signature checks, exact commands:
   - macOS: `codesign --verify --strict <bin>` passes for osx-arm64 and osx-x64, and `guard version` runs (exit 0) on the mac runner — proving hardened runtime + entitlements let it launch (fix F2).
   - Windows: `signtool verify /pa /v <bin>` passes (our self-signed cert trusted on the runner) and the signature carries an RFC-3161 timestamp.
   - cosign (all six): `cosign verify-blob --certificate-identity 'https://github.com/4thWAIV/agent-guard/.github/workflows/ci.yml@refs/heads/main' --certificate-oidc-issuer 'https://token.actions.githubusercontent.com' --bundle <bundle> <bin>` succeeds for a `main` build; a `dev` build uses `@refs/heads/dev` (fix F8); a Rekor entry exists. The local opt-in flag `AGENTGUARD_COSIGN` exists and is off by default (a local build without it skips cosign); the interactive login is verified by local use, not CI (decision 9).
6. The CI log shows the Intel-Mac leg ran under Rosetta as `ProcessArchitecture=X64` (fix F8-intel).
7. Versioning (decisions 15–18): `guard version` prints three lines — `SemVer:`, `AssemblyVersion:`, `FileVersion:`. The SemVer is `0.1.<Day×43200 + Time>` with `-pre-release` on a `dev` build, absent on a `main` build, and `+<hash>`; the AssemblyVersion / FileVersion are `0.1.<Day>.<Time>` with each field within 0–65,535; a PR build reports the same stamped versions.
8. `docs/running-alpha-builds.md` exists, covers macOS + Windows trust, and is linked from `README.md`.
9. No signing keys are committed (decision 20). `eng/generate-dev-keys.{cs,sh,ps1}`, `eng/signing.props`, and `eng/signing/agentguard.entitlements` exist. With keys present, `dotnet build` produces strong-named assemblies (public-key token non-null) and the keyless `InternalsVisibleTo` gets the key injected; with no keys the build is unsigned and green; `AGENTGUARD_REQUIRE_SIGNED=true` with no key fails the build. (All proven locally 2026-08-03 — see Foundation status.)
10. `git diff` shows nothing changed under `Abstractions/**` or `analyzers/**`, and no test / analyzer / gate was weakened.

## Open

None — all decisions locked.

## Tier

FULL — a new outward-facing release and signing pipeline for a public OSS project, proven live on GitHub; all six roles.

## Scope

Change scope only by editing this file before the run starts.
