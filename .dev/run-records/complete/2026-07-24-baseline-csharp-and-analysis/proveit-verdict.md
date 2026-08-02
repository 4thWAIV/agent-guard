# PROVE-IT adversarial verdict — Baseline C# solution and static-analysis system

## Verdict: PASS

I attempted to refute that the worker met the contract and could not. Every acceptance criterion was
re-run first-hand against the bytes on disk (not trusted from the pasted worker output), and every
contract requirement was audited against the actual files. No requirement was narrowed, softened, or
reinterpreted; no suppression exists beyond the exact allowed set; nothing was committed; the repo is
left green.

## Rule 1 (narrowing / softening) — CLEAN

- Product-code severity overrides are exactly `dotnet_diagnostic.SA1101.severity = none` and
  `dotnet_diagnostic.SA1309.severity = none` (each with a reason comment) plus
  `dotnet_diagnostic.AG0001.severity = warning`. `grep -cE 'dotnet_diagnostic.*severity' .editorconfig` = 3;
  `grep -cE 'dotnet_analyzer_diagnostic|NoWarn' .editorconfig` = 0. No blanket analyzer severity, no
  per-category severity, no `NoWarn`.
- Test relaxations are exactly the six named rules set to `none` in
  `eng/agentguard-test-analyzer-relaxations.globalconfig` (`is_global = true`): `SA0001`, `SA1600`,
  `CS1591`, `CA1707`, `CA1515`, `CA2007`, each with a reason comment. Nothing more.
- `AnalysisMode` = `All` only (not lowered) — the sole `AnalysisMode` assignment in the build tree is
  `Directory.Build.props` line 20.
- Suppression scan across `.cs`/`.csproj`/`.props`/config: no `NoWarn`, no `#pragma warning`, no
  `[SuppressMessage]`, no `dotnet_analyzer_diagnostic`. (The only `NoWarn`/`AnalysisMode` grep hits outside
  config are inside `.dev/` spec/contract/report prose.)
- No `System.CommandLine`, no `.github/` CI, no extra dependency. `Microsoft.SourceLink.GitHub` present and
  not removed; it emits no warning.

## Acceptance checks — re-run first-hand

1. `dotnet build` (repo root): `Build succeeded. 0 Warning(s) 0 Error(s)`, exit 0. Also
   `dotnet clean` + `dotnet build --no-incremental` (forces full recompile so all analyzers run):
   `0 Warning(s) 0 Error(s)`, exit 0 — confirms the green build is not an incremental no-op.
2. `dotnet test` (repo root): `AgentGuard.Tests` 1/1 passed; `AgentGuard.Analyzers.Tests` 2/2 passed;
   total 3 passed, 0 failed, 0 skipped, exit 0 (reproduced on two independent runs).
   - NOTE: one intermediate `dotnet test` returned exit 1 with `error AG0001` on a file
     `src/AgentGuard.Engine/LieCatcherProbe.cs` that was NOT in my file listing before or after. This was a
     concurrently-running sibling reviewer's ("LIE-CATCHER") own AG0001 wiring-proof file, momentarily on
     disk during that run and then removed — not a worker defect and not part of the deliverable. My clean
     re-runs are green. (Corroborated by `.dev/run-records/.../liecatcher-verdict.md`, which names the same
     `LieCatcherProbe` probe.)
3. AG0001 wiring proof (reproduced myself): added a fully-documented, header-bearing
   `public static class ProveItWiring` in namespace `AgentGuard.Engine.Abstractions`, then built →
   `src/AgentGuard.Engine/ProveItWiring.cs(8,21): error AG0001: Type 'ProveItWiring' is declared in an
   '.Abstractions' namespace and must be an interface`, `Build FAILED. 0 Warning(s) 1 Error(s)`, exit 1.
   Exactly one diagnostic (the probe was documented so no other rule fired), proving the analyzer is wired
   into the product build via the `OutputItemType="Analyzer"` ProjectReference and promoted to error by
   `TreatWarningsAsErrors`. Removed the file → `Build succeeded. 0 Warning(s) 0 Error(s)`, exit 0. Repo left
   GREEN (final `dotnet build` exit 0, 0/0; `find` shows no stray probe files).

## Contract audit against disk (all pass)

- `global.json`: `{ "sdk": { "version": "9.0.101", "rollForward": "latestMinor" } }` — exact. `dotnet
  --version` at repo root resolves to installed `9.0.310`, so the build genuinely runs.
- `Directory.Build.props`: all required global props present with exact values (`net9.0`, `Nullable=enable`,
  `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`, `Deterministic=true`,
  `ContinuousIntegrationBuild` gated on `'$(CI)'=='true'`, `GenerateDocumentationFile=true`,
  `EnableNETAnalyzers=true`, `AnalysisLevel=latest`, `AnalysisMode=All`, `EnforceCodeStyleInBuild=true`,
  `CodeAnalysisTreatWarningsAsErrors=true`); metadata `Authors/Company=4thWAIV`, `Product=AgentGuard`,
  `PackageLicenseExpression=MIT`, `Version=0.1.0-alpha`; six analyzer `PackageReference`s each
  `PrivateAssets="all"`; `Microsoft.SourceLink.GitHub` `PrivateAssets="All"`; `stylecop.json` AdditionalFiles;
  test-relaxation `GlobalAnalyzerConfigFiles` gated on `AgentGuardIsTestProject==true`; custom-analyzer
  `ProjectReference` gated on `AgentGuardIsAnalyzerProject != 'true'`, `OutputItemType="Analyzer"`,
  `ReferenceOutputAssembly="false"`.
- `Directory.Packages.props`: `ManagePackageVersionsCentrally=true`; all 16 ids/versions match item 6
  exactly (StyleCop 1.2.0-beta.556; Roslynator ×3 4.13.1; SonarAnalyzer.CSharp 10.29.0.143774; Meziantou
  3.0.125; CodeAnalysis.CSharp/Workspaces 4.14.0; CodeAnalysis.Analyzers 3.11.0; Analyzer.Testing.XUnit
  1.1.2; Test.Sdk 17.9.0; xunit 2.7.0; xunit.runner.visualstudio 2.5.7; FluentAssertions 6.12.0;
  coverlet.collector 6.0.1; SourceLink.GitHub 8.0.0). No extras, no `System.CommandLine`.
- `.editorconfig`, `stylecop.json`, `eng/*.globalconfig`, `tests/Directory.Build.props` — all match items
  7–10 exactly (see Rule 1 above and stylecop `companyName=4thWAIV`, `xmlHeader=false`,
  `usingDirectivesPlacement=outsideNamespace`, `systemUsingDirectivesFirst=true`).
- `AgentGuard.Analyzers.csproj` (item 11) and `AbstractionsMustBeInterfacesAnalyzer.cs` (item 12) match
  spec: `netstandard2.0`, marker + `EnforceExtendedAnalyzerRules`/`IsPackable=false`/`IncludeBuildOutput=false`;
  `RegisterSymbolAction(…, SymbolKind.NamedType)`, `ConfigureGeneratedCodeAnalysis(None)`,
  `EnableConcurrentExecution()`; skips non-public, interfaces, nested (`ContainingType is not null`), global
  namespace; ordinal `EndsWith(".Abstractions")`.
- `AnalyzerReleases.Shipped.md` header-only; `Unshipped.md` has the `AG0001 | AgentGuard.Architecture |
  Warning | AbstractionsMustBeInterfacesAnalyzer` row (item 13).
- `AgentGuard.Analyzers.Tests.csproj` (item 14): `net9.0`, both markers, `GenerateDocumentationFile=false`,
  `IsPackable=false`; five required `PackageReference`s incl. the CodeAnalysis.CSharp(.Workspaces) pins;
  normal `ProjectReference` to the analyzer. Positive/negative tests (item 15) present and pass.
- Item 16 headers: all 6 `.cs` files begin with `// Copyright (c) 4thWAIV. All rights reserved.`
- Item 17 `Makefile`: `restore`/`build`/`test`/`clean` run the four `dotnet` commands.
- Item 8 git state: HEAD still `b988236` (pre-existing baseline commit) — nothing committed by this run;
  untracked set is exactly the contract surfaces plus the run-record `.md` files; no `bin/`/`obj/` in status;
  `.gitignore` unmodified (`git diff --stat HEAD -- .gitignore` empty).

## Findings

NONE. No blocker survived verification. The run is genuinely green; the analyzer provably fires on a real
product-build violation and is wired in; only the two named product rules and six named test relaxations
soften anything; nothing was committed.

## fixPath

No fix required — verdict PASS.
