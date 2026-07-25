# SOLID/DRY adversarial verdict — baseline C# solution and analysis system

**Verdict: PASS**

Static review only (no build run, no code changes), per role. The structure is clean, matches the
contract, and centralizes every shared concern. The AG0001 analyzer is a real functioning
`DiagnosticAnalyzer` with genuine logic and two real verifier tests — not a scaffold.

## Files inspected
- Contract: `.dev/run-records/2026-07-24-baseline-csharp-and-analysis/contract.md`
- Worker report: `.dev/run-records/2026-07-24-baseline-csharp-and-analysis/worker-report.md`
- Source spec: `.dev/SPEC-csharp-analysis-system.md`
- Root config: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`,
  `stylecop.json`, `eng/agentguard-test-analyzer-relaxations.globalconfig`, `tests/Directory.Build.props`,
  `AgentGuard.sln`, `Makefile`
- Engine: `src/AgentGuard.Engine/AgentGuard.Engine.csproj`, `src/AgentGuard.Engine/EngineInfo.cs`
- Cli: `src/AgentGuard.Cli/AgentGuard.Cli.csproj`, `src/AgentGuard.Cli/Program.cs`
- Tests: `tests/AgentGuard.Tests/AgentGuard.Tests.csproj`, `tests/AgentGuard.Tests/EngineInfoTests.cs`
- Analyzer: `analyzers/AgentGuard.Analyzers/AgentGuard.Analyzers.csproj`,
  `AbstractionsMustBeInterfacesAnalyzer.cs`, `AnalyzerReleases.Shipped.md`, `AnalyzerReleases.Unshipped.md`
- Analyzer tests: `analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj`,
  `AbstractionsMustBeInterfacesAnalyzerPositiveTests.cs`, `AbstractionsMustBeInterfacesAnalyzerNegativeTests.cs`

## Structure (per contract) — clean
- Five projects present in `AgentGuard.sln`: Engine, Cli, Tests, Analyzers, Analyzers.Tests.
- Layout exact: `src/AgentGuard.Engine` (class lib), `src/AgentGuard.Cli` (`OutputType=Exe`,
  `AssemblyName=guard`, references Engine only — no `System.CommandLine`), `tests/AgentGuard.Tests` (xUnit),
  `analyzers/AgentGuard.Analyzers` (`netstandard2.0`), `analyzers/AgentGuard.Analyzers.Tests` (`net9.0`).
- Marker properties correct: `AgentGuardIsTestProject=true` on both test projects;
  `AgentGuardIsAnalyzerProject=true` on the analyzer project and (necessarily) on the analyzer-test project so
  the self-reference wiring is correctly excluded from both.

## DRY — no hardcoding, no duplication introduced by the worker
- **Central Package Management is real.** `grep 'Version='` across every csproj and `Directory.Build.props`
  returns zero hits; all versions live solely in `Directory.Packages.props` as `PackageVersion` entries.
  No inline version anywhere.
- **Global properties set once.** No csproj re-declares `Nullable`, `TreatWarningsAsErrors`,
  `ImplicitUsings`, `AnalysisMode`, `LangVersion`, or `Deterministic`. They exist only in the root
  `Directory.Build.props` and are inherited. `TargetFramework` is overridden in exactly two csprojs, both
  legitimately required (`netstandard2.0` for the analyzer host; `net9.0` for the analyzer-test project).
- **Analyzer wired once, centrally.** `OutputItemType="Analyzer"` appears exactly once in code
  (`Directory.Build.props`) via a `ProjectReference` with `ReferenceOutputAssembly="false"`, gated on
  `'$(AgentGuardIsAnalyzerProject)' != 'true'`. It is NOT wired per product project. The other `OutputItemType`
  matches are documentation (SPEC / contract / report), not code.
- **Severity concerns are cleanly split.** `.editorconfig` owns product-code severity (only `SA1101=none`,
  `SA1309=none`, `AG0001=warning`; no blanket `dotnet_analyzer_diagnostic.severity`, no `NoWarn`); the
  `eng/*.globalconfig` owns the six test-only relaxations gated on the test marker. No overlap between the two.
- **Each config file is single source of truth for its concern**: `global.json` (SDK pin),
  `Directory.Packages.props` (versions), `Directory.Build.props` (global props + analyzer suite + SourceLink +
  stylecop AdditionalFiles + globalconfig gate + analyzer wiring), `stylecop.json` (companyName/ordering),
  `.editorconfig` (format/style/product severity), the globalconfig (test relaxations).

## AG0001 analyzer — real, functioning, not a scaffold
- `[DiagnosticAnalyzer(LanguageNames.CSharp)] : DiagnosticAnalyzer`, `DiagnosticId="AG0001"`, category
  `AgentGuard.Architecture`, `defaultSeverity=Warning`, `isEnabledByDefault:true`.
- `Initialize` calls `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`,
  `EnableConcurrentExecution()`, and `RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType)` — exactly
  as specified.
- Genuine symbol-level logic: skips non-public types, interfaces, nested types (`ContainingType is not null`),
  and the global namespace; ordinal `EndsWith(".Abstractions")` on the containing namespace; reports at
  `symbol.Locations[0]` with the type name. This is behavior, not a stub.
- Release-tracking files correct: `Shipped.md` header-only; `Unshipped.md` declares the `AG0001` row.
- Two real verifier tests via `CSharpAnalyzerVerifier<AbstractionsMustBeInterfacesAnalyzer, DefaultVerifier>`:
  positive asserts exactly one `{|AG0001:Widget|}` on a public class in `Sample.Abstractions`; negative asserts
  zero diagnostics for an interface in `*.Abstractions` plus a class in `Sample.Implementation`. Worker report
  further shows the end-to-end wiring proof (real violation → AG0001 build error → removal → green).

## Findings (observations only — none blocking, none warrant a change)
1. `AgentGuard.Analyzers.Tests.csproj` restates `TargetFramework=net9.0`, duplicating the root
   `Directory.Build.props` default. It is contract-mandated (item 14) and defensible as explicit clarity
   sitting beside a `netstandard2.0` sibling. Not a fault.
2. `GenerateDocumentationFile=false` and `IsPackable=false` appear both in `tests/Directory.Build.props` and
   inline in the analyzer / analyzer-test csprojs. This is because those projects live under `analyzers/`, not
   `tests/`, and therefore cannot inherit `tests/Directory.Build.props`; every occurrence is contract-mandated
   (items 10, 11, 14). A marker-gated setting (`AgentGuardIsTestProject`) in the root props would be strictly
   more DRY, but adopting it would deviate from the contract's prescribed folder-based `tests/Directory.Build.props`
   design. This is a contract design choice faithfully executed, not worker-introduced duplication.

## fixPath (SOLID above DRY)
No change required. The two observations above are the contract's own topology tension (folder-based test
props for `tests/` vs marker-based governance everywhere else), not a worker DRY sin. Were the team to ever
tighten it, the SOLID-respecting move is to keep a single authority for the "test project" concern — i.e.
gate `GenerateDocumentationFile=false` / `IsPackable=false` on `AgentGuardIsTestProject` in the root
`Directory.Build.props` so the rule lives in one place regardless of folder — but that deviates from the
current contract and must not be done in this run. Leave as-is.
