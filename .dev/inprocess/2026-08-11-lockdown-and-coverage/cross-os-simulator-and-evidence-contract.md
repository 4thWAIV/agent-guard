# Cross-OS test simulator + CI/release evidence

The copy-on-write test simulator in `AgentGuard.TestHelpers` passes on macOS but fails the Windows and Linux CI legs of PR #35. This contract makes the simulator correct on all three operating systems, fixes the Linux per-OS coverage shortfall, and makes every build keep its own coverage/test/analyzer evidence. It is the follow-on to `test-system-contract.md` in this same folder.

## Decisions

Each decision below is a design choice this work rests on, with Tim's approving words quoted next to it. Where a line says "proposed", it is not yet decided — approving this contract is the sign-off.

### `fake-reads-real-home-and-temp`
The test simulator is copy-on-write over a real read-only base and never writes to real disk, so the fake environment's home directory and temp root are read from the real operating system through the owned `IEnvironment` — `GetHomeDirectory()` and `GetTempDirectory()` — instead of the hardcoded POSIX strings `"/agentguard-fake-home"` and `"/agentguard-fake-temp"`. A real home and temp are fully-qualified on every OS, which is what stops the Windows failure (below).
Tim: *"As we are copy on write, why don't we ask the BCL to give us the real root (home) and real temp directory via the BCL commands for that?"* / *"this is something that is safe for the Fake to pass through to the real, so we should not be hard coding bad ones."*

### `tests-orient-off-the-abstraction`
A test never hardcodes a filesystem root. It asks the abstraction (`IEnvironment`) what home and temp are and builds from there. The one test that hardcodes a root today, `ProcessArchitectureTests`, is moved to this pattern.
Tim: *"Move the test to use the system to orrient itself. THE tests should be asking the abstraction what is home and what is temp."*

### `case-mode-seeded-from-real-host`
The simulator's case-sensitivity mode is seeded from the real host using the case-sensitivity detection already built (`IPlatformFileSystem.IsCaseSensitive`, which on macOS reads the native `pathconf` query and elsewhere falls back to the shared probe), instead of the hardcoded `caseSensitive: true` at the overlay construction site.
Tim: *"Why are we not using the sensitive/insenstive test we put together to do this?"*

### `audit-the-posix-literals`
The remaining POSIX-shaped string literals in the tests (the stale-path literal in `SetupHarness`, and the `/repo`-style literals in `SnapshotSerializerTests`, `ShellProfileTests`, `ClaudeCodeHostAdapterTests`, `AcceptanceConfigProtectionTests`) are audited as part of this work: each is confirmed to be inert data (a JSON value or an expected string), not a filesystem root fed to a path API, and is left unchanged unless the audit shows otherwise.
Tim: *"Okay, let's evaluate that as part of the plan."*

### `temp-root-owner-exemption`
There is an analyzer, the temp-root back-door rule (`AGS5443`, `TempRootBackDoorAnalyzer`), added earlier in this same run, that turns every call to `IEnvironment.GetTempDirectory()` into a hard build error — its purpose is to stop code from reaching the shared OS temp root as a way around the guard, and it was written assuming there would be no legitimate caller. The decision `fake-reads-real-home-and-temp` above creates exactly one legitimate caller: the builder reading the real temp root once to seed the in-memory fake. The rule is changed to allow that call inside `SystemServicesBuilder` and nowhere else — an owner exemption, the same shape already used to license the temp and random primitives to their single owner — so the tree carries no suppression for it.
Tim (approving option A, "Exempt the one owner: teach AGS5443 that GetTempDirectory() is allowed inside SystemServicesBuilder and nowhere else"): *"Yes we can do that"*.

### `ci-uploads-build-evidence`
Every build uploads its evidence — the coverage report, the test results, and the analyzer/build diagnostics — so coverage is visible on every build and we have the per-line data to diagnose a leg we cannot run locally.
Tim: *"We should have the CI upload the coverage reports as part of all binaries. This will allow us to show coverage of our builds as evidence and provide the data we need to know what to fix when we can't work on every OS."* / *"we should also upload our tests and ana results as well into some directory of the artifact so that we know what happened in the build."*

### `evidence-on-pr-builds`
The evidence uploads run on pull-request builds too, not only on pushes — a PR is exactly where we need to see the failing legs' data. (Binaries stay push-only, unchanged; evidence carries no secrets and is not a runnable build, so it is a different risk class.)
Tim: *"THAT does not mean we can't upload the evidence for PR builds"*.

### `pr-build-version-channel`
A pull-request build is stamped with a `pr-<number>` version channel, not the `pre-release` channel a PR into `dev` gets today — a PR build is not a pre-release, and this label makes each build's evidence identifiable.
Tim: *"I like this one [pr-<number>] it helps us know what was what and provides clarity, besides it's just a label at this point anyway as we are not dropoing binaries."*

### `evidence-to-releases-and-releases-as-zips`
On a release (a push to `dev`/`main`), the evidence is also published to the GitHub Release as a permanent asset, not only as the run-scoped artifact that expires. The release's assets are organized as zip files, so the release carries a directory structure inside its archives instead of a flat list of loose files.
Tim: *"we can use this for our data, for every build (CI and release included) but we should upload our evidence to our releases and organize our releases as zip files."*

### `linux-native-case-query-is-per-os`
The rule for the per-OS projects is that any source that can be shared must be shared, so it is never duplicated; Linux-only code is allowed and always has been; and the only thing forbidden is Linux code that duplicates macOS code. Today the macOS-only native case-sensitivity query — the `OperatingSystem.IsMacOS()` branch in `PosixFileSystem.IsCaseSensitive` and the `pathconf` P/Invoke it calls — sits in a source file that is link-shared into both per-OS projects, so it compiles into the Linux build where it is permanently dead. The OS-uniform part of `IsCaseSensitive` stays shared. The macOS native query moves into a macOS-only file. Linux gets its own small file that runs no native query and defers to the shared probe. That Linux file is genuinely Linux-specific behavior, not a copy of the macOS code, so it is not a duplication. The exact lines that move are finalized against the real Linux coverage report the evidence strand uploads, not against inference.

The split is done with a `partial class`. `PosixFileSystem` becomes a partial class spread across a shared file (link-shared into both projects, holding every shared member plus a bodyless declaration of the native-case-query method), a macOS-only file (the `pathconf` body, its name constant, and the P/Invoke), and a Linux-only file (defer to the probe). The seam is a partial method that returns a value, so C# requires each per-OS project to supply a body or fail to build.
Tim: *"Only files that can be shared must be shared. NOTHING ever precluded linux only code. JUST NOT linux code that is a DRY violation of mac code."* / *"Okay I approve the partial class solution and the file naming convention."*

### `os-specific-file-naming-convention`
A source file that belongs to one operating system only is marked by a filename suffix naming that OS — `*.MacOS.cs`, `*.Linux.cs`, `*.Windows.cs` — while a file meant to be shared carries no such suffix. This is the marker the fourth guardrail rule keys off, so the rule matches on the convention rather than naming any specific file, and every future OS-specific file is covered automatically by following the convention.
Tim: *"Okay I approve the partial class solution and the file naming convention."*

### `guardrail-rules`
This work adds the analyzer rules and structural tests listed in "Rules to add" below, each of which makes one of these regressions a build failure instead of a silent cross-OS break. The first rule was narrowed to fire only on a literal at a fake-root seed site, so it cannot mistake a data string for a path.
Tim (on the narrowed first rule, the other three, and the contract as a whole): *"Okay, coninue."*

**Waiver (Tim, 2026-08-23) — the two seed-site rules (AG0035/AG0036) ship at the `NewOverlay`-call + `FakeEnvironment.Create` scope; two REFUTE-panel SOLID findings are waived:** (1) the coverage gap — a future direct `new InMemoryFileSystemStore(literal)` in some other method of `SystemServicesBuilder`, bypassing `NewOverlay`, would be uncaught (hypothetical: needs a second store construction bypassing the one factory, and cross-OS CI breaks on a bad root regardless); (2) the seed sites are matched by method name (`NewOverlay`/`Create`), so a rename would silently disable the rule. Both are accepted as disproportionate polish on a five-argument-slot belt that CI backstops. The DRY findings from that panel were fixed, not waived. Tim: *"Fix the DRY violations and you can pass on the rest."*

### `release-zip-layout`
The binaries are published as one zip per operating-system and architecture (`agentguard-<rid>.zip`, holding the binary, its signature, and its checksum). The evidence is published as one `evidence.zip`, organized inside with one subdirectory per operating-system and platform build, each subdirectory holding that leg's coverage, test, and analyzer results. Zipping happens after signing, so the signatures travel inside the archives and the existing sign-then-verify order is unchanged.
Tim: *"I agree, but the evidence.zip must be organized with subdirectories per OS/platform build."*

## The standard

When this ships, the whole solution builds `dotnet build -c Release` at 0 warnings / 0 errors under every analyzer rule with no new suppression, and `dotnet test` reports 0 failed on macOS, Linux, and Windows in CI — the same three-OS bar `test-system-contract.md` set and PR #35 is currently short of on two legs. Coverage stays at or above the hard 75% floor on the counted set and on each per-OS implementation on its own leg. Every CI and release build uploads its coverage, test, and analyzer evidence, and every release publishes that evidence and its binaries as zip assets.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change. Any restatement or weakening of it to fit the result is a top-line Lie-catcher finding. Nothing is "in" until it is in the code, green, and proven.

This run's expected end state: PR #35's CI is green on all three OS legs and the gate; the Windows `Init`/`Install`/`IntegrityCheck`/`Doctor` tests that threw `ArgumentException: Basepath argument is not fully qualified` now pass; the Linux per-OS implementation is at or above 75% coverage confirmed against the real uploaded Linux report (not inferred from macOS); the simulator hardcodes no filesystem root and no case-sensitivity literal; each CI and release build (including a failed leg) has downloadable coverage/test/analyzer evidence; releases publish evidence plus zip-packaged binaries; and the added guardrail rules ship red-then-clean.

## Surfaces

- **`tests/AgentGuard.TestHelpers/SystemServicesBuilder.cs`** — the single real-host read (today `ReadRealHostArchitecture`/`RealHostArchitecture`) widens to also read and cache the real home, temp root, and host case-sensitivity; the `DefaultFakeHome`/`DefaultFakeTempRoot` constants are deleted; `Build()` feeds the real home and temp into `FakeEnvironment.Create`; `NewOverlay` gains a case-sensitivity argument fed from the real host (`Fake()`) or the injected base (`SimulateFileSystem()`). This class is also the sole exempted caller of `IEnvironment.GetTempDirectory()`.
- **`tests/AgentGuard.TestHelpers/InMemoryFileSystemStore.cs`** — the overlay's `caseSensitive` construction parameter is now sourced from the real host, not a literal.
- **`tests/AgentGuard.Tests/ProcessArchitectureTests.cs`** — the hardcoded `"/agentguard-fake-home"` becomes a read of the real home through the builder's environment.
- **`tests/AgentGuard.Cli.Tests/CliCommandSuccessPathTests.cs`** and **`SettingsProbe`** (promoted from `AgentGuard.Tests` to `tests/Shared/`) — the init-wiring test reads the guard's wired hook command from the parsed settings through the shared `SettingsProbe.GuardGroup`/`CommandOf`, instead of a raw-text substring match on the serialized JSON, which failed on Windows where the path's backslashes are JSON-escaped. Tim approved parsing-and-comparing the property AND reusing the existing helper rather than hand-walking the JSON. Tim: *"Yes, you need to serialize and compare the expected property properly"* / *"REUSE THIS so we can remove the bad DRY violaiton. I approved you parsing and using the object but not you doing it manually and not using the proper class."*
- **`src/AgentGuard.CrossPlatform.MacOS/Posix/PosixFileSystem.cs`** and **`PosixNativeMethods.cs`** — become `partial`; the macOS-only native case query and its P/Invoke move to a new macOS-only file; a new Linux-only file supplies the deferring body. (These files are link-shared into `AgentGuard.CrossPlatform.Linux`.)
- **New files** `src/AgentGuard.CrossPlatform.MacOS/Posix/PosixFileSystem.MacOS.cs` and `src/AgentGuard.CrossPlatform.Linux/Posix/PosixFileSystem.Linux.cs`.
- **`analyzers/AgentGuard.Analyzers/TempRootBackDoorAnalyzer.cs`** (AGS5443) — gains the `SystemServicesBuilder` owner exemption; its analyzer tests extend to cover it.
- **`.github/workflows/ci.yml`** — the four `dotnet test` invocations gain a `trx` logger; the three `dotnet build` invocations gain a binlog; each OS job gains one evidence upload (coverage + test results + binlog), on push and pull-request, `if: always()`, `if-no-files-found: warn`; the version-channel computation gains the `pr-<number>` case; the release job packages binaries and evidence as zips and attaches the evidence zip to the release.
- **`eng/compute-build-id.sh`** — the `pr-<number>` channel.
- **`tests/AgentGuard.Tests/VersionStampTests.cs`** — the SemVer-scheme test (`SemVerMatchesTheScheme`) accepts the new `-pr-<number>` prerelease label alongside `-pre-release`; a PR build stamps that label, so without this the version-scheme test fails on every OS leg (caught by the evidence-first CI run 32600708327).
- **New analyzer files and analyzer tests** for the guardrail rules; **`AnalyzerReleases.Unshipped.md`**; new structural tests over the Linux `.csproj` and over `ci.yml`.
- **Coverage pipeline is reused unchanged** — `eng/coverage-gate.sh`, `.runsettings`, `.config/dotnet-tools.json` are not touched; in particular no `--results-directory` is added to `dotnet test`, because that would collapse the gate's per-project report grouping and mis-measure coverage.

## Reuse ledger

From the GROUND read of the live code and the DESIGN pass (2026-08-22).

| Capability | Ruling | Owner / note |
|---|---|---|
| read real host values into the fake | **reuse/extend** | `SystemServicesBuilder.ReadRealHostArchitecture` already opens the one `SystemServices.Create()` door and caches only values; widen that one record/reader to add home, temp, case-sensitivity — no second door |
| fully-qualified OS home/temp | **reuse** | `IEnvironment.GetHomeDirectory()`/`GetTempDirectory()` already wrap the BCL; call them, do not reimplement |
| host case-sensitivity detection | **reuse** | `IPlatformFileSystem.IsCaseSensitive` (native `pathconf` on macOS, shared probe elsewhere) already exists; seed the overlay from it |
| owner exemption on a raw-primitive rule | **reuse pattern** | the temp/random primitives already carry per-owner exemptions in their analyzers; apply the same shape to `AGS5443` for `SystemServicesBuilder` |
| per-OS-authored divergent primitive | **reuse pattern** | `ag0101-one-owner-per-os` already puts OS-divergent primitives in the one per-OS class; the partial-method split applies it one level down inside the POSIX family |
| CI artifact upload | **reuse** | `actions/upload-artifact` at the already-pinned commit is used six times; add evidence uploads with the same action, no new shell wrapper |
| test-results / binlog capture | **new** | no `trx` logger or binlog is configured today; add the standard `--logger trx` and `-bl` switches (built-in, no new tooling) |
| coverage cobertura | **reuse** | already emitted per test project under `TestResults`; glob it, do not change the gate |
| release zip packaging + release-asset attach | **new** | the release currently publishes flat signed binaries; add a post-signing zip step and attach the evidence zip |
| structural regression tests (csproj / workflow YAML) | **reuse pattern** | `PosixSourceIsLinkSharedTests` already parses a csproj as a structural test; the new link-share and workflow-lint tests follow the same shape |

## What to do

The evidence strand ships FIRST, so its first re-run uploads the real Linux coverage report, and the Linux structural fix is finalized against that report rather than against the macOS-based inference.

**Strand 3 — build evidence (first).**
1. Add `--logger "trx;LogFileName=<leg>.trx"` (no `--results-directory`) to each `dotnet test` invocation — the two macOS runs (native and Rosetta), Windows, and Linux — with a distinct name per invocation so the two macOS runs do not collide.
2. Add `mkdir -p evidence` and `-bl:evidence/<leg>-build.binlog` to each job's `dotnet build` step (MSBuild writes the binlog even when the build fails).
3. Add one `actions/upload-artifact` step per OS job (same pinned commit as the existing uploads), named `evidence-<os>`, globbing the binlog, the cobertura reports under `**/TestResults/**`, and the `.trx` files; `if: always()`, no `github.event_name == 'push'` gate (so it runs on PR and push, pass and fail), `if-no-files-found: warn`. Place it after the coverage-gate step and before the push-gated signing block.
4. Add the `pr-<number>` channel to `eng/compute-build-id.sh` and wire the `version` job to pass it on a `pull_request` event.
5. On the release job (push only): package each binary as `agentguard-<rid>.zip` after signing (binary + signature + checksum), package the evidence as `evidence.zip` with one subdirectory per OS/platform build inside it, and attach the evidence zip to the published release alongside the binary zips (`release-zip-layout`).
6. Land this strand and get one CI run — even with Windows/Linux still red — to upload the real Linux `coverage.cobertura.xml` and the Windows `.trx`.

**Strand 1 — simulator Windows/Linux correctness (`SystemServicesBuilder.cs`).**
7. Widen the existing real-host read into one cached record holding process architecture, OS architecture, home, temp root, and host case-sensitivity, read through the one `SystemServices.Create()` door. Add the `AGS5443` owner exemption for this `GetTempDirectory()` call (`temp-root-owner-exemption`).
8. Delete `DefaultFakeHome` and `DefaultFakeTempRoot`; feed the real home and temp into `Build()`'s `FakeEnvironment.Create`; add a `caseSensitive` argument to `NewOverlay`, sourced from the cached host value in `Fake()` and from `_real.Platform.FileSystem.IsCaseSensitive(realHome)` in `SimulateFileSystem()`.
9. Move `ProcessArchitectureTests`' hardcoded home to read the real home through the builder's environment (`tests-orient-off-the-abstraction`).
10. Run the audit (`audit-the-posix-literals`): confirm each remaining POSIX literal is inert data; change nothing unless one is a real filesystem key. Grep the corpus for any test that asserts the exact retired strings as an expected value (the fake home/temp now resolve to a host-dependent real path).

**Strand 2 — Linux structural coverage (after the real Linux report lands).**
11. CONFIRMED against the real Linux cobertura (CI run 32600708327, 2026-08-22, `evidence-linux` artifact): the Linux-only dead lines are exactly the macOS-only native case query and nothing else. In `PosixFileSystem.IsCaseSensitive`, the three lines of the `OperatingSystem.IsMacOS()` branch body (the `pathconf` call, the `native >= 0` check, the `return native == 1`); line 146, the `ProbeCaseSensitive` fallback, is covered on Linux. In `PosixNativeMethods`, the twelve lines of the `PathConf` `[LibraryImport]` generated marshalling. Linux is 50/77 = 64.94%; removing those 15 dead lines leaves 50/62 ≈ 80.6%, clear of the 75% floor, and no extra Linux test is needed (the remaining uncovered lines — the separator read, the rename-failure path, the Windows-guard throws, the best-effort catch blocks — are uncovered on macOS too, which still clears 75%).
12. Make `PosixFileSystem` and `PosixNativeMethods` `partial`; the shared file declares a bodyless partial method for the native case query and calls it unconditionally from `IsCaseSensitive` (no `OperatingSystem.IsMacOS()` left in shared source); a new macOS-only file implements it with the moved `pathconf` call, the `PosixCaseSensitiveName` const, and the moved `PathConf` P/Invoke; a new Linux-only file implements it as "no native query, defer to the probe".
13. Prove the existing link-share test, the interop-only analyzer, and the one-owner-per-OS analyzer stay green unchanged; confirm the real Linux report now clears 75% and that macOS did not drop below 75%.

## Rules to add (the `rule-phase-ruleset`)

This section is the ruleset the RULE-PHASE agent implements. Each new rule is red against a current violation where noted, cleaned to green by the strands, and wired into the normal build or CI. It is followed by one change to an existing analyzer.

1. **No literal fake-root at the seed** — a Roslyn analyzer, build error when a string literal is passed as the `home`, `currentDirectory`, or `tempDirectory` argument of `FakeEnvironment.Create`, or as the `tempRoot` argument of the `NewOverlay(...)` seed funnel in `SystemServicesBuilder` — the single store-construction site `AG0027` pins, through which `Fake()` and `SimulateFileSystem()` seed the overlay. The check targets the `NewOverlay` call, NOT the `InMemoryFileSystemStore` constructor: the constructor is only ever reached through `NewOverlay`, which forwards its own parameter, so the constructor call never carries a literal and a check there could never fire; a re-hardcode would be a literal at the `NewOverlay` call, which is what this rule catches. Those are the only places a fake root is seeded, and each must take a real-host or abstraction-derived value, never a literal. The rule fires only on a literal at those specific arguments, so it never inspects an arbitrary string and cannot mistake data (a `/repo`-style JSON value, a route, a regex) for a path — no false positives by construction. Stops a re-hardcoded fake root re-breaking Windows (or, while fixing Windows, a `C:\` literal re-breaking macOS/Linux) — a break invisible on the dev machine. (An earlier paired text-scan test for the exact retired tokens was dropped as over-engineered — Tim: *"it's a just a test and probably over engineered anyway"* — because it over-fired on comments and documentation and duplicated what this analyzer already catches semantically.)
2. **No literal case-mode seed** — a Roslyn analyzer, build error on a `true`/`false` literal passed to the `caseSensitive` argument of the `NewOverlay(...)` seed funnel in `SystemServicesBuilder` (the same single seed site as rule 1, for the same reason — the store constructor is always parameter-fed through `NewOverlay`, so the check must be at the `NewOverlay` call), excluding the deliberate `SetCaseSensitive`/`SimulateCaseSensitivity` test entry points. Forces the case mode to be host-derived.
3. **No single-OS branch inside a per-OS implementation library** — extend the existing analyzer that bans OS branching outside the cross-platform libraries (`AG0009`) with an inverted registration that bans `OperatingSystem.IsMacOS()`/`IsLinux()`/`IsFreeBSD()` and the like inside the `.MacOS`/`.Linux`/`.Windows` libraries (the cross-POSIX `!IsWindows()` family gate stays allowed). Stops a future single-OS branch in a link-shared file re-creating permanently-dead, uncoverable code.
4. **No OS-specific file is compiled into another OS's build** — a structural test that reads each per-OS project and fails if any file whose name carries an OS suffix (`*.MacOS.cs`, `*.Linux.cs`, `*.Windows.cs`, per `os-specific-file-naming-convention`) is compiled into, or link-shared into, a different OS's project. It matches on the suffix, never a specific filename, so every OS-specific file added later is covered without touching the rule. Stops an OS-specific file (such as the macOS-only native query) being re-linked into another OS's build, which would put dead, uncoverable code there and drop its coverage below the floor.
5. **Evidence uploads cannot regress** — a structural check over `ci.yml` asserting every evidence-upload step uses `if: always()`, is never push-gated, runs on all three OS jobs with the complete evidence set, and never globs `dist/**` or signing material. Stops the uploads reverting to push-only or leaking release binaries/secrets into a PR-visible artifact.

**Change to an existing analyzer (not a new red-forcing rule):** the temp-root back-door analyzer (`AGS5443`, `TempRootBackDoorAnalyzer`) gains the owner exemption approved in `temp-root-owner-exemption`. A call to `IEnvironment.GetTempDirectory()` is allowed inside `SystemServicesBuilder` and stays a build error everywhere else. This is an approved relaxation for one owner, so it does not leave standing red; its covering analyzer test extends to prove both the exemption and the still-firing ban elsewhere.

## What the agent MAY do

- Widen the existing real-host read, add the members and files named above, and add the `AGS5443` owner exemption for `SystemServicesBuilder`.
- Add the five rules and their tests; assign ids against `AnalyzerReleases.Unshipped.md`.
- Add the CI evidence steps, the `pr-<number>` channel, and the release zip/attach steps.

## What the agent MUST NOT do

- Weaken, suppress, exempt, or narrow any analyzer rule to force green, beyond the one approved `AGS5443` owner exemption; a raw call is fixed by routing it through the interface.
- Add `--results-directory` to `dotnet test`, add `[ExcludeFromCodeCoverage]`, lower the 75% floor, or exclude a source file to move the coverage number.
- Hardcode any filesystem root or case-sensitivity literal in the test helpers or tests.
- Finalize Strand 2's line-by-line extraction from inference — confirm it against the real uploaded Linux report first.
- Push-gate the evidence uploads, or let a failed leg upload nothing.
- Weaken, skip, delete, or re-point a test to make it pass; commit or push without approval; or expand scope. On any wall the plan does not cover, STOP and report.

## Acceptance

1. CI on PR #35 is green on macOS, Linux, and Windows and the gate; locally `dotnet build -c Release` = 0/0 with no new suppression beyond the approved `AGS5443` exemption, and `dotnet test -c Release` = 0 failed.
2. The Windows `Init`/`Install`/`IntegrityCheck`/`Doctor` tests that threw `Basepath argument is not fully qualified` pass; the fix is a real-host home/temp, proven by the Windows leg.
3. `SystemServicesBuilder` has no `DefaultFakeHome`/`DefaultFakeTempRoot`; the fake home, current directory, and temp root resolve from the real `IEnvironment` (AG0035 fires red-then-clean against the current hardcoded seeds).
4. The overlay's case mode is host-seeded, not a literal; `ProcessArchitectureTests` reads the real home through the abstraction; the POSIX-literal audit is recorded with its per-site finding.
5. `AGS5443` allows `GetTempDirectory()` inside `SystemServicesBuilder` and is still a build error everywhere else (red-then-clean probe); no `[SuppressMessage]` for it exists in the tree.
6. The Linux per-OS implementation is at or above 75% coverage, read from the real uploaded Linux `coverage.cobertura.xml`, not inferred; macOS stays at or above 75%; the link-share test, the interop-only analyzer, and the one-owner-per-OS analyzer are green unchanged.
7. Every CI and release build, including a failed leg, produces a downloadable `evidence-<os>` artifact carrying the cobertura report, the `.trx` results, and the binlog; a PR build is stamped `pr-<number>`, not `pre-release`.
8. A release publishes the evidence as a permanent release asset and packages binaries and evidence as zips; the evidence zip is organized with one subdirectory per OS/platform build; signatures verify from inside the archives. **Proven on merge, not on a PR:** the release job runs only on a push to `dev`/`main`, so a pull request skips it entirely; this criterion is verified by the first merge of this branch to `dev`, which is downstream of this contract by definition, not a gap in it. Tim: *"GETTIGN the CI/CD merge results was never part of this objective, it couldn't be, it can only be determined when we get this clean enough to merge."*
9. The five guardrail rules each fire red on a planted (or current) violation and are clean after the strands (red-then-clean probes); the two structural tests fail on a planted regression and pass as shipped.

## POSIX-literal audit record (satisfies `audit-the-posix-literals` / Acceptance #4)

Each POSIX-shaped string literal the audit covers was checked and found to be inert data — a value serialized or compared as text, never fed to a path API as a filesystem root — so none is a cross-OS hazard and none is flagged by `AG0035`:

- `tests/AgentGuard.Tests/SetupHarness.cs` — the stale-path literal `/old/relocated/.agentguard/bin/guard` is a value passed to `string.Replace` inside settings-JSON text, never a path API. Inert.
- `tests/AgentGuard.Tests/SnapshotSerializerTests.cs` — the `/repo/*` strings are `SnapshotEntry` record field values round-tripped through the snapshot serializer. Inert.
- `tests/AgentGuard.Tests/ShellProfileTests.cs` — `/home/tester`, `/bin/zsh`, `/usr/bin/bash`, `/opt/homebrew/bin/fish` are inputs and expected outputs of the pure `ShellProfile.Resolve` function (POSIX shell semantics by nature), never a real filesystem read. Inert.
- `tests/AgentGuard.Tests/ClaudeCodeHostAdapterTests.cs` — `/repo` and `/repo/a.cs` are JSON payload values and the expected parse results. Inert.
- `tests/AgentGuard.Tests/AcceptanceConfigProtectionTests.cs` — `/old/stale/guard` and `/tmp/evil/guard` are guard-path values written into and asserted against settings JSON. Inert.

No test asserts the retired `/agentguard-fake-home` / `/agentguard-fake-temp` strings, so none needed changing.

## Level

L1 — this mutates shipped production code (the per-OS split), the test infrastructure, the analyzer rules, and the CI/release pipeline, with cross-OS blast radius that only CI can prove. Full workflow, full adversary panel.

## Scope

Change scope only by editing this file before the run starts.
