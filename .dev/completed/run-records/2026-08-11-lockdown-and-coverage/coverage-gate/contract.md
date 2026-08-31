# Coverage gate — collect coverage by default, fail the build under 75%

Coverage is collected on every `dotnet test` with no extra flags, and CI fails the build when line coverage of the product code is below 75%.

## Decisions

### `threshold-75-hard-gate`
Line coverage of the product code must be at least 75%; below it fails the build, the same as a failed test.
Tim (issue #12 "Force TDD, make code coverage a zero-effort standard, and gate at ≥75%"): *"gate at ≥75%"*; on the hard gate: *"Agreed."*

### `zero-effort-collection`
Coverage collection is on by default — every `dotnet test` produces a report with no extra flags or arguments.
Tim (issue #12): *"make code coverage a zero-effort standard."*

### `covered-assemblies`
The 75% aggregate covers `AgentGuard.Engine`, `AgentGuard.Cli`, and `AgentGuard.CrossPlatform`, measured on every leg. The per-OS `AgentGuard.CrossPlatform.MacOS`, `AgentGuard.CrossPlatform.Linux`, and `AgentGuard.CrossPlatform.Windows` are each measured at 75% on their own OS leg. `AgentGuard.Analyzers` and the three test projects (`AgentGuard.Tests`, `AgentGuard.CrossPlatform.Tests`, `AgentGuard.Analyzers.Tests`) are out.
Tim: *"Agreed."*

### `test-first-lives-in-the-guide`
The test-first practice is not machine-enforced — a build cannot force the test to be written before the code. It lives in the proof area of the best-practices guide (`../DRAFT-best-practices-guide.md`, area 6). This coverage gate enforces the outcome, not the order.

## What we're building

`coverlet.collector` (already installed) collects a cobertura report on every `dotnet test`, turned on by default through a `.runsettings` the test run uses automatically. A CI step merges the reports and fails the build when the aggregate line coverage of the in-scope assemblies is below 75%; each per-OS assembly gates at 75% on its own OS leg. `AgentGuard.Analyzers` and the test projects are excluded from the number.

Non-negotiable: the gate measures reality. No source is excluded to lift the number, no threshold is lowered to pass, no test is weakened.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change.

- `dotnet test` locally produces a cobertura coverage report with no extra flags.
- CI fails when aggregate line coverage of `Engine` + `Cli` + `CrossPlatform` is below 75%, computed once (the Linux leg), and passes at or above.
- Each per-OS implementation gates at 75% on its own OS leg.
- `AgentGuard.Analyzers` and the three test projects are absent from the coverage number.
- `dotnet build -c Release` = 0/0 and `dotnet test -c Release` = 0 failed, locally and on all three OS in CI; the gate is red only when coverage is genuinely below 75%.

## Surfaces

- `.runsettings` (new) — enables XPlat Code Coverage in cobertura format, with the in-scope assemblies included and `AgentGuard.Analyzers` plus `*.Tests` excluded.
- `Directory.Build.props` (edited) — points `dotnet test` at the `.runsettings` by default (`RunSettingsFilePath`), so collection is zero-effort.
- `Directory.Packages.props` (edited) — pins the report-merge/threshold tool (ReportGenerator).
- `.github/workflows/ci.yml` (edited) — the aggregate coverage gate on the Linux leg, and the per-OS threshold on the macOS and Windows legs.
- `analyzers/AgentGuard.Analyzers.Tests` — stays without `coverlet.collector` (out of scope).

## Reuse ledger

| Capability | Ruling | Owner / note |
|---|---|---|
| Coverage collector | reuse | `coverlet.collector` 6.0.1 is already in `Directory.Packages.props` and referenced by `AgentGuard.Tests` and `AgentGuard.CrossPlatform.Tests`. |
| Report merge + threshold check | new | No coverage merge or threshold step exists; add ReportGenerator (or a small cobertura line-rate parse) to compute the aggregate and fail under 75%. |
| The CI gate | extend | The existing `gate` job / the per-OS test steps in `.github/workflows/ci.yml`. |

## What to do

1. Add a `.runsettings` that turns on XPlat Code Coverage (cobertura), includes `AgentGuard.Engine`/`AgentGuard.Cli`/`AgentGuard.CrossPlatform*`, and excludes `AgentGuard.Analyzers` and every `*.Tests` assembly.
2. Point `dotnet test` at it by default via `Directory.Build.props` (`RunSettingsFilePath`), so no flags are needed.
3. Write the tests to bring every in-scope assembly to at least 75%: a new `AgentGuard.Cli.Tests` project exercising `Program.cs` (the install, init, remove, doctor, and hook paths — nothing references the CLI today, so it is at zero), plus more coverage of `AgentGuard.CrossPlatform` and each per-OS implementation. Measure with the merged report and keep going until every in-scope assembly, and each per-OS implementation on its own leg, is at or above 75%.
4. Add the report merge + threshold check: the Linux leg gates the aggregate of `Engine` + `Cli` + `CrossPlatform`; the macOS and Windows legs each gate their own per-OS implementation.
5. Turn the gate hard — the build fails under 75% — only once the tests carry it there.
6. Prove: `dotnet test` emits a report locally with no flags; a deliberate drop below 75% fails the gate; at or above passes.

## What the agent MUST NOT do

- Lower the threshold, exclude a source file, or add a `[ExcludeFromCodeCoverage]` to lift the number instead of writing the missing tests.
- Weaken, skip, or delete a test.
- Commit or push. Stop and report on any wall.

## Acceptance

1. `dotnet test` locally produces a cobertura report with no extra flags (show the report path in the output).
2. A deliberate uncovered change on an in-scope assembly drops the number below 75% and fails the CI gate; reverting it passes.
3. `grep` of the coverage report shows `AgentGuard.Analyzers` and the `*.Tests` assemblies are absent from the counted set.
4. `dotnet build`/`test` green locally and on all three OS in CI; the gate reflects the real number.

## Scope — measured, ruled

Current line coverage on macOS (rough, per project, not merged): `AgentGuard.Engine` about 86%; `AgentGuard.CrossPlatform` about 44% to 63%; `AgentGuard.Cli` zero, because no test project references it and `Program.cs` is never exercised. Tim ruled the hard 75% stands — no ratchet, and the CLI stays in the gate. So this run writes the tests to reach 75%; it is a real test-writing effort, not a config change.
Tim: *"You ruled 75%, so I build to that unless you say otherwise. THEN WHY THE FUCK did you HALT ON A BULLSHIT QUESTION I HAVE ALREADY ANSWERD"* — i.e. build to 75%.

**Hook path — resolved (REFUTE fix round).** The `guard hook` command cannot be exercised through the in-process CLI: `InstallIntegrity.Check` derives the install root from `Environment.ProcessPath`, which in a `dotnet test` host is `dotnet`, not the installed guard binary, so an in-process hook denies before the pipeline runs. Its real composition (integrity gate plus pipeline) is already proven end to end at the engine level by `HookExecutionTests` (benign edit → exit 0; tampered install → exit 2), which `GuardHost` documents as the exact composition the CLI hook handler runs. CLI-level hook testing needs the injected filesystem/binary-path seam the clr-primitive-lockdown adds, so it lands there — no one-off publish-and-subprocess mechanism and no one-off production seam is added here. The CLI `install`, `init`, and `doctor` success paths ARE now exercised through the real CLI.

## Tier

FULL — a new CLI test project plus a coverage lift across CrossPlatform and the per-OS libraries, then the CI gate wiring.

## Open — PARKED with REFUTE round-2 findings (2026-08-11)

The fix round closed round 1's findings, but the re-refute surfaced three real ones. Parked on the `coverage-gate` branch (WIP, not merged, blocks nothing):

1. `eng/coverage-gate.sh` merges every `coverage.cobertura.xml` under the results directory with no staleness handling, so the ordinary local edit-test-edit-test loop merges a stale report with a fresh one and yields a nonsense number (measured: 5.29% instead of 86.6%) instead of failing loudly — it violates "the gate measures reality." CI is unaffected (each leg starts clean). Fix: scope or clean the reports to the current run, or hard-fail on more reports than test projects.
2. The CLI test harness isolates `HOME` only via the POSIX env var, but the production path (`SetupContext.ForCurrentProcess` → `Environment.GetFolderPath(UserProfile)`) ignores `HOME` on Windows, so the CLI tests would silently use the real user profile on the Windows CI leg. Fix: isolate the profile the way the production code resolves it, cross-OS.
3. `CliMachine.cs` duplicates `SetupHarness.cs`'s machine/project layout (six property bodies across two test projects) instead of extracting to the `tests/Shared/` owner this change already created.

Discarded as not caused by this change: a pre-existing local build flake (reproduced on the base tree) and a leftover probe file from an earlier review pass.

## Scope

Change scope only by editing this file before the run starts.
