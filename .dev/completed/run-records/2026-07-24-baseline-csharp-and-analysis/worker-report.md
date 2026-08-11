# Worker report — Baseline C# solution and static-analysis system

Role: IMPLEMENTER. Repo: `/Users/timothystockstill/code/macos/4thWAIV/agent-guard`, branch `baseline-setup`.
Contract: `.dev/run-records/2026-07-24-baseline-csharp-and-analysis/contract.md`.
Spec: `.dev/SPEC-csharp-analysis-system.md`.

Self-assessment: **GREEN** — `dotnet build` and `dotnet test` are zero-warning / zero-error and all
acceptance checks pass. Wall: **none**.

---

## STEP 0 — SDK check

```
$ dotnet --list-sdks
8.0.100 [/Users/timothystockstill/.dotnet/sdk]
9.0.310 [/Users/timothystockstill/.dotnet/sdk]
```

The literal "9.0.1xx" band is not installed, but the contract's `global.json` pins `9.0.101` with
`rollForward: latestMinor`, which is specifically designed to roll forward across feature bands. I verified
empirically that this exact pin resolves against the installed 9.0.310 before creating any repo files:

```
$ cd <scratchpad>/sdktest && printf '{ "sdk": { "version": "9.0.101", "rollForward": "latestMinor" } }' > global.json && dotnet --version
9.0.310
EXIT=0
```

Because the contract-specified pin resolves cleanly (exit 0) to the installed SDK, the build can run — this is
the functional requirement STEP 0 protects. Therefore this is NOT a missing-SDK wall, and I proceeded. No SDK
was installed.

---

## Acceptance 1 — `dotnet build` at repo root (0 Warning / 0 Error)

```
$ dotnet build AgentGuard.sln
  Determining projects to restore...
  All projects are up-to-date for restore.
  AgentGuard.Analyzers -> .../analyzers/AgentGuard.Analyzers/bin/Debug/netstandard2.0/AgentGuard.Analyzers.dll
  AgentGuard.Engine -> .../src/AgentGuard.Engine/bin/Debug/net9.0/AgentGuard.Engine.dll
  AgentGuard.Analyzers.Tests -> .../analyzers/AgentGuard.Analyzers.Tests/bin/Debug/net9.0/AgentGuard.Analyzers.Tests.dll
  AgentGuard.Cli -> .../src/AgentGuard.Cli/bin/Debug/net9.0/guard.dll
  AgentGuard.Tests -> .../tests/AgentGuard.Tests/bin/Debug/net9.0/AgentGuard.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:04.68
BUILD_EXIT=0
```

All five projects build under the full analysis stack (StyleCop + 3x Roslynator + SonarAnalyzer.CSharp +
Meziantou + built-in CA/IDE at `AnalysisMode=All`) with `TreatWarningsAsErrors=true` and
`CodeAnalysisTreatWarningsAsErrors=true`. Restore was clean earlier — no NU1701 (the `Microsoft.CodeAnalysis.CSharp`
+ `.Workspaces` 4.14.0 pins in the analyzer-test project neutralized the `Analyzer.Testing.XUnit 1.1.2`
transitive net461 drag). SourceLink emitted no warning.

## Acceptance 2 — `dotnet test` at repo root (all pass)

```
$ dotnet test AgentGuard.sln
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 44 ms - AgentGuard.Tests.dll (net9.0)
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 3 s - AgentGuard.Analyzers.Tests.dll (net9.0)
TEST_EXIT=0
```

Totals: 3 passed, 0 failed, 0 skipped (1 in `AgentGuard.Tests`, 2 in `AgentGuard.Analyzers.Tests`).

## Acceptance 3 — AG0001 analyzer wiring proof

Added `src/AgentGuard.Engine/WiringProof.cs` declaring `public static class WiringProof` in namespace
`AgentGuard.Engine.Abstractions` (fully documented + header, so AG0001 is the sole failure), then built:

```
$ dotnet build AgentGuard.sln
.../src/AgentGuard.Engine/WiringProof.cs(8,21): error AG0001: Type 'WiringProof' is declared in an '.Abstractions' namespace and must be an interface [.../AgentGuard.Engine.csproj]
Build FAILED.
    0 Warning(s)
    1 Error(s)
BUILD_EXIT=1
```

AG0001 fired as a build **error** (warning promoted by `TreatWarningsAsErrors`), proving the analyzer is wired
into the product build via the `OutputItemType="Analyzer"` ProjectReference in `Directory.Build.props`. Then
removed the file and rebuilt:

```
$ rm src/AgentGuard.Engine/WiringProof.cs && dotnet build AgentGuard.sln
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
```

Repo left GREEN.

## Acceptance 4 — the two analyzer tests

`analyzers/AgentGuard.Analyzers.Tests/` contains:
- `AbstractionsMustBeInterfacesAnalyzerPositiveTests.cs` — asserts exactly one `{|AG0001:Widget|}` on a public
  class in `Sample.Abstractions`.
- `AbstractionsMustBeInterfacesAnalyzerNegativeTests.cs` — asserts zero diagnostics for an interface in a
  `*.Abstractions` namespace plus a class in `Sample.Implementation`.

Both use `CSharpAnalyzerVerifier<AbstractionsMustBeInterfacesAnalyzer, DefaultVerifier>` and both pass (the
2/2 in the Acceptance 2 run).

## Acceptance 5 — `Directory.Packages.props` versions match the spec

```
$ grep -E 'PackageVersion Include' Directory.Packages.props
StyleCop.Analyzers                                  1.2.0-beta.556
Roslynator.Analyzers                                4.13.1
Roslynator.Formatting.Analyzers                     4.13.1
Roslynator.CodeAnalysis.Analyzers                   4.13.1
SonarAnalyzer.CSharp                                10.29.0.143774
Meziantou.Analyzer                                  3.0.125
Microsoft.CodeAnalysis.CSharp                       4.14.0
Microsoft.CodeAnalysis.CSharp.Workspaces            4.14.0
Microsoft.CodeAnalysis.Analyzers                    3.11.0
Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit 1.1.2
Microsoft.NET.Test.Sdk                              17.9.0
xunit                                               2.7.0
xunit.runner.visualstudio                           2.5.7
FluentAssertions                                    6.12.0
coverlet.collector                                  6.0.1
Microsoft.SourceLink.GitHub                         8.0.0
```

Every id/version matches contract item 6 exactly. `System.CommandLine` is intentionally absent (contract
forbids adding it).

## Acceptance 6 — severity policy

`.editorconfig` — exactly three `dotnet_diagnostic.*.severity` lines, no blanket
`dotnet_analyzer_diagnostic.severity`, no per-category severity, no `NoWarn`:

```
79: dotnet_diagnostic.SA1101.severity = none      (reason: contradicts dotnet_style_qualification_for_* = false)
82: dotnet_diagnostic.SA1309.severity = none      (reason: contradicts _camelCase private-field convention)
85: dotnet_diagnostic.AG0001.severity = warning   (promoted to error by TreatWarningsAsErrors)
$ grep -cE 'dotnet_diagnostic.*severity' .editorconfig
3
$ grep -cE 'dotnet_analyzer_diagnostic|NoWarn' .editorconfig
0
```

`eng/agentguard-test-analyzer-relaxations.globalconfig` — `is_global = true` and exactly the six named rules
to `none`, each with a reason comment: `SA0001`, `SA1600`, `CS1591`, `CA1707`, `CA1515`, `CA2007`.

## Acceptance 7 — file headers and companyName

Every `.cs` (excluding `bin/`/`obj/`) starts with `// Copyright (c) 4thWAIV. All rights reserved.`:

```
OK   analyzers/AgentGuard.Analyzers.Tests/AbstractionsMustBeInterfacesAnalyzerNegativeTests.cs
OK   analyzers/AgentGuard.Analyzers.Tests/AbstractionsMustBeInterfacesAnalyzerPositiveTests.cs
OK   analyzers/AgentGuard.Analyzers/AbstractionsMustBeInterfacesAnalyzer.cs
OK   src/AgentGuard.Cli/Program.cs
OK   src/AgentGuard.Engine/EngineInfo.cs
OK   tests/AgentGuard.Tests/EngineInfoTests.cs
MISSING_COUNT=0
```

`stylecop.json` `documentationRules.companyName` = `"4thWAIV"` (matches the header text, satisfying SA1636).

## Acceptance 8 — git status (no commit, no bin/obj)

```
$ git status --short
?? .editorconfig
?? AgentGuard.sln
?? Directory.Build.props
?? Directory.Packages.props
?? Makefile
?? analyzers/
?? eng/
?? global.json
?? src/
?? stylecop.json
?? tests/
$ git status --short | grep -E 'bin/|obj/'   ->   NONE
$ git diff --stat HEAD -- .gitignore          ->   (empty; .gitignore unmodified)
```

Nothing is committed by this run (all surfaces untracked). `bin/`/`obj/` do not appear (already gitignored).
`.gitignore` was not modified. HEAD is `b988236 Ignore .NET build artifacts...`, the orchestrator's
pre-existing baseline commit (not created by this run).

---

## Files created (surfaces)

Root config: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`,
`stylecop.json`, `eng/agentguard-test-analyzer-relaxations.globalconfig`, `tests/Directory.Build.props`,
`AgentGuard.sln`, `Makefile`.

Projects:
- `src/AgentGuard.Engine/AgentGuard.Engine.csproj`, `src/AgentGuard.Engine/EngineInfo.cs`
- `src/AgentGuard.Cli/AgentGuard.Cli.csproj`, `src/AgentGuard.Cli/Program.cs`
- `tests/AgentGuard.Tests/AgentGuard.Tests.csproj`, `tests/AgentGuard.Tests/EngineInfoTests.cs`
- `analyzers/AgentGuard.Analyzers/AgentGuard.Analyzers.csproj`,
  `analyzers/AgentGuard.Analyzers/AbstractionsMustBeInterfacesAnalyzer.cs`,
  `analyzers/AgentGuard.Analyzers/AnalyzerReleases.Shipped.md`,
  `analyzers/AgentGuard.Analyzers/AnalyzerReleases.Unshipped.md`
- `analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj`,
  `analyzers/AgentGuard.Analyzers.Tests/AbstractionsMustBeInterfacesAnalyzerPositiveTests.cs`,
  `analyzers/AgentGuard.Analyzers.Tests/AbstractionsMustBeInterfacesAnalyzerNegativeTests.cs`

Plus this report.

## Notes on faithfulness to the "fix code, do not suppress" mandate

The only softenings are the two product deny-list rules (SA1101, SA1309) and the six named test relaxations —
nothing more. No `NoWarn`, no lowered `AnalysisMode`, no blanket analyzer severity, no per-rule suppression was
added. Green was reached by writing minimal, documented, header-bearing code that satisfies the full stack on
the first build.
