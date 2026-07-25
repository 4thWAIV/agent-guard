# Stand up the AgentGuard C# solution and its full static-analysis system

## Standard
`dotnet build` and `dotnet test`, run from the repo root, complete with **zero warnings and zero errors**
under the full analysis stack with warnings treated as errors, and every test passes. The custom-analyzer
mechanism is proven to fire on a real violation and is wired into the product build. The end state is a
five-project `AgentGuard.sln` (`AgentGuard.Engine`, `AgentGuard.Cli`, `AgentGuard.Tests`,
`AgentGuard.Analyzers`, `AgentGuard.Analyzers.Tests`) governed by the exact analysis configuration below.

The authoritative source for every exact value is `./.dev/SPEC-csharp-analysis-system.md`; the values are
also inlined here so each is directly checkable.

## What to do

### Solution and projects
1. Create `AgentGuard.sln` with five projects in this layout: `src/AgentGuard.Engine/` (class library),
   `src/AgentGuard.Cli/` (console app, `OutputType=Exe`, assembly name `guard`),
   `tests/AgentGuard.Tests/` (xUnit), `analyzers/AgentGuard.Analyzers/` (the analyzer, `netstandard2.0`),
   `analyzers/AgentGuard.Analyzers.Tests/` (xUnit).
2. `AgentGuard.Engine` and `AgentGuard.Cli` contain minimal content sufficient to build (a documented
   placeholder type in Engine; a minimal `Program` in Cli). Do NOT add `System.CommandLine` or any product
   dependency in this unit.
3. `AgentGuard.Tests` references `AgentGuard.Engine` and sets `AgentGuardIsTestProject=true`.

### Root configuration files
4. `global.json` pins the SDK: `{ "sdk": { "version": "9.0.101", "rollForward": "latestMinor" } }`.
5. `Directory.Build.props` sets, for every project: `TargetFramework=net9.0`, `Nullable=enable`,
   `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`, `Deterministic=true`,
   `ContinuousIntegrationBuild` gated on `'$(CI)' == 'true'`, `GenerateDocumentationFile=true`,
   `EnableNETAnalyzers=true`, `AnalysisLevel=latest`, `AnalysisMode=All`, `EnforceCodeStyleInBuild=true`,
   `CodeAnalysisTreatWarningsAsErrors=true`; metadata `Authors=4thWAIV`, `Company=4thWAIV`,
   `Product=AgentGuard`, `PackageLicenseExpression=MIT`, `Version=0.1.0-alpha`. It declares the global
   analyzer `PackageReference` set (each `PrivateAssets="all"`): `StyleCop.Analyzers`, `Roslynator.Analyzers`,
   `Roslynator.Formatting.Analyzers`, `Roslynator.CodeAnalysis.Analyzers`, `SonarAnalyzer.CSharp`,
   `Meziantou.Analyzer`; the global `Microsoft.SourceLink.GitHub` reference (`PrivateAssets="All"`); the
   `stylecop.json` `AdditionalFiles` include; the test-relaxation `GlobalAnalyzerConfigFiles` include gated on
   `'$(AgentGuardIsTestProject)' == 'true'`; and the custom-analyzer `ProjectReference` gated on
   `'$(AgentGuardIsAnalyzerProject)' != 'true'`, pointing at
   `analyzers/AgentGuard.Analyzers/AgentGuard.Analyzers.csproj` with `OutputItemType="Analyzer"` and
   `ReferenceOutputAssembly="false"`.
6. `Directory.Packages.props` sets `ManagePackageVersionsCentrally=true` and pins these exact versions:
   - `StyleCop.Analyzers` `1.2.0-beta.556`
   - `Roslynator.Analyzers` `4.13.1`, `Roslynator.Formatting.Analyzers` `4.13.1`,
     `Roslynator.CodeAnalysis.Analyzers` `4.13.1`
   - `SonarAnalyzer.CSharp` `10.29.0.143774`
   - `Meziantou.Analyzer` `3.0.125`
   - `Microsoft.CodeAnalysis.CSharp` `4.14.0`, `Microsoft.CodeAnalysis.CSharp.Workspaces` `4.14.0`,
     `Microsoft.CodeAnalysis.Analyzers` `3.11.0`, `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit` `1.1.2`
   - `Microsoft.NET.Test.Sdk` `17.9.0`, `xunit` `2.7.0`, `xunit.runner.visualstudio` `2.5.7`,
     `FluentAssertions` `6.12.0`, `coverlet.collector` `6.0.1`
   - `Microsoft.SourceLink.GitHub` `8.0.0`
7. `.editorconfig` (`root = true`) carries the formatting and code-style block from the spec, and exactly two
   product-code severity overrides — `dotnet_diagnostic.SA1101.severity = none` and
   `dotnet_diagnostic.SA1309.severity = none`, each with its reason comment — plus
   `dotnet_diagnostic.AG0001.severity = warning`. It contains no blanket
   `dotnet_analyzer_diagnostic.severity`, no per-category severity, and no `NoWarn`.
8. `stylecop.json` sets `documentationRules.companyName = "4thWAIV"`, `documentationRules.xmlHeader = false`,
   `orderingRules.usingDirectivesPlacement = "outsideNamespace"`, `orderingRules.systemUsingDirectivesFirst =
   true`.
9. `eng/agentguard-test-analyzer-relaxations.globalconfig` sets `is_global = true` and exactly these six rules
   to `none`, each with its reason comment: `SA0001`, `SA1600`, `CS1591`, `CA1707`, `CA1515`, `CA2007`.
10. `tests/Directory.Build.props` imports the root props via `GetPathOfFileAbove` and sets
    `GenerateDocumentationFile=false` and `IsPackable=false`.

### The analyzer mechanism (must be functional, not scaffolded)
11. `AgentGuard.Analyzers.csproj`: `netstandard2.0`, `AgentGuardIsAnalyzerProject=true`,
    `EnforceExtendedAnalyzerRules=true`, `IsPackable=false`, `IncludeBuildOutput=false`; `PackageReference`
    to `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Analyzers` (each `PrivateAssets="all"`);
    `AdditionalFiles` for `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md`.
12. Author one `DiagnosticAnalyzer` named `AbstractionsMustBeInterfacesAnalyzer` with `DiagnosticId = "AG0001"`,
    category `"AgentGuard.Architecture"`, `defaultSeverity = DiagnosticSeverity.Warning`,
    `isEnabledByDefault: true`: every public type declared directly in a namespace whose name ends in
    `".Abstractions"` that is not an interface is reported. Use `RegisterSymbolAction(…, SymbolKind.NamedType)`,
    `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`, `EnableConcurrentExecution()`, and skip
    non-public types, interfaces, nested types, and the global namespace.
13. `AnalyzerReleases.Shipped.md` is the header comment only. `AnalyzerReleases.Unshipped.md` declares `AG0001`
    in a `### New Rules` table row: `AG0001 | AgentGuard.Architecture | Warning | AbstractionsMustBeInterfacesAnalyzer`.
14. `AgentGuard.Analyzers.Tests.csproj`: `net9.0`, `AgentGuardIsAnalyzerProject=true`,
    `AgentGuardIsTestProject=true`, `GenerateDocumentationFile=false`, `IsPackable=false`; `PackageReference`
    to `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`,
    `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit`, and — required to avoid NU1701 —
    `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.CSharp.Workspaces`; a normal `ProjectReference`
    to the analyzer project.
15. Write two analyzer tests using
    `CSharpAnalyzerVerifier<AbstractionsMustBeInterfacesAnalyzer, DefaultVerifier>`: a positive test asserting
    exactly one `{|AG0001:...|}` on a public class in a `*.Abstractions` namespace, and a negative test
    asserting zero diagnostics for an interface in a `*.Abstractions` namespace plus a class elsewhere.

### Headers, build entry points
16. Every `.cs` file begins with the header line `// Copyright (c) 4thWAIV. All rights reserved.`
17. Add a `Makefile` with `restore`, `build`, `test`, and `clean` targets running `dotnet restore`,
    `dotnet build`, `dotnet test`, and `dotnet clean`.
18. Reach a green build by fixing findings in the guard's own code (adding XML documentation and headers), not
    by suppressing rules.

## MAY
- Add XML documentation and the required header to the guard's own baseline code to reach green.
- Choose the minimal placeholder content of `AgentGuard.Engine` and `AgentGuard.Cli`, provided each builds
  and satisfies the analyzers.
- Adjust the `GetPathOfFileAbove` relative depth in `tests/Directory.Build.props` to the actual layout.

## MUST NOT
- Disable, downgrade, `NoWarn`, or per-rule-suppress any analyzer or rule beyond the exact set named here
  (`SA1101` and `SA1309` for product code; the six test relaxations). Do not lower `AnalysisMode`, do not add
  `NoWarn`, do not set a blanket analyzer severity. Reach green by fixing the code.
- Weaken, skip, `xfail`, comment out, or delete any test.
- Ship the analyzer as a scaffold: `AG0001` must actually fire on a real violation and be wired into the
  product build.
- Add `System.CommandLine`, a CI workflow, or any dependency not listed above.
- Remove or disable `Microsoft.SourceLink.GitHub`; if it emits a warning that fails the build, STOP and
  report rather than silently removing it.
- Commit, or change any file outside the surfaces listed below.
- Expand scope. Stop and report on any wall rather than deviating.

## Acceptance (paste command, full output, and exit code for each)
1. `dotnet build` at the repo root exits 0 and reports `0 Warning(s)` and `0 Error(s)`.
2. `dotnet test` at the repo root exits 0; paste the passed/failed/total counts.
3. Analyzer wiring proof: add a public class in a `*.Abstractions` namespace inside `AgentGuard.Engine`, run
   `dotnet build`, and show it fails with `AG0001` reported as an error; then remove that class and show
   `dotnet build` green again. Paste both builds with exit codes.
4. `AgentGuard.Analyzers.Tests` contains the positive `{|AG0001:...|}` test and the negative test, and both
   pass in the run 2 output.
5. `Directory.Packages.props` contains exactly the versions listed in item 6 above — an adversary greps each
   id and confirms the version string.
6. `.editorconfig` contains only `SA1101` and `SA1309` set to `none` for product code (plus the `AG0001`
   warning line), with no blanket `dotnet_analyzer_diagnostic.severity` and no `NoWarn`; and
   `eng/agentguard-test-analyzer-relaxations.globalconfig` sets exactly the six named rules to `none`.
7. Every `.cs` file's first line is `// Copyright (c) 4thWAIV. All rights reserved.`, and `stylecop.json`
   `companyName` is `"4thWAIV"` — an adversary greps for any `.cs` lacking the header.
8. `git status` shows no commit and only the surfaces listed below as new files.

## Tier
FULL — mutating; installs the project baseline and the self-governing analysis stack.

## Surfaces (every file this change creates)
`global.json`; `Directory.Build.props`; `Directory.Packages.props`; `.editorconfig`; `stylecop.json`;
`eng/agentguard-test-analyzer-relaxations.globalconfig`; `tests/Directory.Build.props`; `AgentGuard.sln`;
`src/AgentGuard.Engine/` (csproj + one documented placeholder `.cs`); `src/AgentGuard.Cli/` (csproj +
`Program.cs`); `tests/AgentGuard.Tests/` (csproj + at least one test `.cs`);
`analyzers/AgentGuard.Analyzers/` (csproj, `AbstractionsMustBeInterfacesAnalyzer.cs`,
`AnalyzerReleases.Shipped.md`, `AnalyzerReleases.Unshipped.md`); `analyzers/AgentGuard.Analyzers.Tests/`
(csproj + the two test `.cs` files); `Makefile`.

## Success definition
ALL acceptance criteria are met AND there are no errors in the system as a result of the change. This run's
expected end state: the five-project `AgentGuard.sln` builds and tests green under the full analysis stack
with warnings as errors; the `AG0001` analyzer is proven to fire on a real violation and is wired into the
product build; only `SA1101`, `SA1309`, and the six named test relaxations soften anything; and nothing is
committed.

## Scope
Change scope only by editing this file before the run starts.
