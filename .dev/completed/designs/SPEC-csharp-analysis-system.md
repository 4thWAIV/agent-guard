# Source Spec — the full C# static-analysis system (from proteus), for replication in agent-guard

## Provenance and verification
Every value in this spec was read first-hand from the live proteus bytes on branch `dev` at HEAD
`e47ebeab2c0dcd61df4b0de1ea72d93ea341bfe0` (clean working tree, so committed bytes equal the working tree).
The config files, all package versions, the severity overrides, the SDK pin, and the entire custom-analyzer
project (analyzer csproj, analyzer-test csproj, both `AnalyzerReleases` files, the analyzer implementation,
and the analyzer test) were each opened and confirmed. This is a design/source spec; a contract is written
from it separately.

Facts to correct common assumptions: **SonarAnalyzer IS present.** There is no `.ruleset` file, no
`Directory.Build.targets`, no `nuget.config`, no `.github/` CI, no `NoWarn`, and no per-rule
`WarningsAsErrors` list anywhere. Warnings-as-errors is blanket via `TreatWarningsAsErrors=true`.

## Goal
Reproduce this exact analysis rigor inside agent-guard so the guard governs itself under the same golden
build. Target solution `AgentGuard.sln` with `AgentGuard.Engine` (class library), `AgentGuard.Cli` (console
app that becomes the `guard` binary), and `AgentGuard.Tests`; plus, to preserve the full custom-rule
mechanism, `AgentGuard.Analyzers` and `AgentGuard.Analyzers.Tests`.

---

## A. Inventory (files that make up the system)
Repo-relative to the proteus root.

- `Directory.Build.props` — the heart: global TFM/nullable/warnings-as-errors, the built-in-analyzer knobs,
  the six-analyzer NuGet suite, the `stylecop.json` AdditionalFiles, the test-relaxation globalconfig gate,
  and the custom-analyzer `ProjectReference` wiring.
- `Directory.Packages.props` — Central Package Management on; the single source of every version.
- `.editorconfig` (root, the only one) — formatting, code-style preferences, the `PRO0001` severity, and the
  two StyleCop deny-list entries.
- `stylecop.json` (root) — company name, no XML header, using-directive placement and ordering.
- `eng/test-analyzer-relaxations.globalconfig` — `is_global = true`; relaxes six rules for test projects;
  wired once, gated on `ProteusIsTestProject=true`.
- `global.json` — pins the .NET SDK to `9.0.101`, `rollForward: latestMinor`.
- The custom analyzer project: `analyzers/Nuevco.Proteus.Analyzers/` with its csproj,
  `AbstractionsMustBeInterfacesAnalyzer.cs`, `AnalyzerReleases.Shipped.md`, `AnalyzerReleases.Unshipped.md`.
- The analyzer test project: `analyzers/Nuevco.Proteus.Analyzers.Tests/` with its csproj and
  `Pro0001AnalyzerTests.cs`.
- `Makefile` — `restore` / `build` / `test` / `clean` entry points.

---

## B. Global MSBuild properties (exact, from root `Directory.Build.props`)
Set once and inherited by every project:

```
TargetFramework                   = net9.0
Nullable                          = enable
ImplicitUsings                    = enable
TreatWarningsAsErrors             = true
LangVersion                       = latest
Deterministic                     = true
ContinuousIntegrationBuild        = true    (only when env CI == 'true')
GenerateDocumentationFile         = true
EnableNETAnalyzers                = true
AnalysisLevel                     = latest
AnalysisMode                      = All
EnforceCodeStyleInBuild           = true
CodeAnalysisTreatWarningsAsErrors = true
```

Also set (non-analysis): `Authors=Nuevco`, `Company=Nuevco`, `Product=Proteus`,
`PackageLicenseExpression=MIT`, `Version=0.1.0-alpha`, and a global
`PackageReference Microsoft.SourceLink.GitHub PrivateAssets="All"`.

Not set anywhere: `WarningsAsErrors`, `WarningsNotAsErrors`, `NoWarn`, `WarningLevel`, `CodeAnalysisRuleSet`,
`RunAnalyzers`, `ReportAnalyzer`.

---

## C. Analyzer stack (packages, exact versions, families)
Central Package Management is on (`ManagePackageVersionsCentrally=true`), so csproj `PackageReference`s carry
no version. The six best-practice analyzers are referenced once in `Directory.Build.props`, each with
`PrivateAssets="all"` (analyzer-only, no runtime flow):

| Package | Version | Family |
|---|---|---|
| `StyleCop.Analyzers` | `1.2.0-beta.556` | `SA####` / `SX####` |
| `Roslynator.Analyzers` | `4.13.1` | `RCS1###` |
| `Roslynator.Formatting.Analyzers` | `4.13.1` | `RCS0###` |
| `Roslynator.CodeAnalysis.Analyzers` | `4.13.1` | `RCS9###` |
| `SonarAnalyzer.CSharp` | `10.29.0.143774` | `S####` |
| `Meziantou.Analyzer` | `3.0.125` | `MA####` |

Plus the built-in **Microsoft.CodeAnalysis.NetAnalyzers** (`CA####`) and IDE code-style analyzers (`IDE####`),
enabled by `EnableNETAnalyzers=true` + `AnalysisLevel=latest` + `AnalysisMode=All` +
`EnforceCodeStyleInBuild=true` (they ship with the SDK, not a package).

Roslyn analyzer-authoring and testing packages (pinned to the SDK's coherent set) in `Directory.Packages.props`:

| Package | Version | Used by |
|---|---|---|
| `Microsoft.CodeAnalysis.CSharp` | `4.14.0` | analyzer project + analyzer-test project |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | `4.14.0` | analyzer-test project (pins harness transitive Roslyn) |
| `Microsoft.CodeAnalysis.Analyzers` | `3.11.0` | analyzer project (brings the `RS####` release-tracking rules) |
| `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit` | `1.1.2` | analyzer-test project (verifier harness) |

Test stack in `Directory.Packages.props`: `Microsoft.NET.Test.Sdk 17.9.0`, `xunit 2.7.0`,
`xunit.runner.visualstudio 2.5.7`, `FluentAssertions 6.12.0`, `coverlet.collector 6.0.1`. Other:
`System.CommandLine 2.0.0-beta4.22272.1`, `Microsoft.SourceLink.GitHub 8.0.0`.

These six analyzer packages plus the built-in .NET analyzers are the entire stack; there is nothing else to
find. Confirmed absent (optional additions if we ever want them, not part of proteus): PublicApiAnalyzers,
Microsoft.VisualStudio.Threading.Analyzers, PolySharp, AsyncFixer, a standalone xunit.analyzers reference
(xUnit rules ride in transitively with `xunit`), and any security scanner such as SecurityCodeScan.

---

## D. Severity policy (complete)
The model is: **every analyzer runs at its own author-declared default severity, and
`TreatWarningsAsErrors=true` promotes every warning to a build error.** There is no blanket
`dotnet_analyzer_diagnostic.severity` and no per-category override. Deviations are only explicit per-rule
overrides, each with a written reason. The stated philosophy is "every other analyzer finding is fixed in
code," not silenced.

Product-code overrides in root `.editorconfig`:
- `dotnet_diagnostic.PRO0001.severity = warning` — the custom rule (then promoted to error).
- `dotnet_diagnostic.SA1101.severity = none` — SA1101 (prefix `this.`) contradicts
  `dotnet_style_qualification_for_* = false`.
- `dotnet_diagnostic.SA1309.severity = none` — SA1309 forbids the leading underscore, contradicting the
  chosen `_camelCase` private-field convention.
- SA1200 is deliberately NOT disabled; it is reconciled via `stylecop.json`
  `usingDirectivesPlacement=outsideNamespace` and passes.

Test-only overrides in `eng/test-analyzer-relaxations.globalconfig` (`is_global = true`; applies only where
`ProteusIsTestProject=true`), all `none`, each with a reason:
- `SA0001` — doc analysis off (test projects set `GenerateDocumentationFile=false`).
- `SA1600` — test elements need no XML docs.
- `CS1591` — missing XML comment; test types are not public API.
- `CA1707` — underscores in identifiers; xUnit `Method_State_Expected` naming.
- `CA1515` — "types can be internal"; xUnit v2 discovers only public classes.
- `CA2007` — ConfigureAwait not meaningful in xUnit; xUnit1030 forbids `ConfigureAwait(false)`.

Code-style (IDE) preferences in `.editorconfig` are mostly `:suggestion`, so they do not fail the build:
file-scoped namespaces, `var` everywhere, expression-bodied members, pattern matching, brace/newline rules,
indentation, spacing. Base `[*]`: 4-space indent, LF, UTF-8, trim trailing whitespace, final newline.
`csproj/props/targets/sln`, `json`, `yml/yaml` = 2-space; `md` = no trailing-whitespace trim; `*.rs` present
at 4-space.

Net effect: promotion to error is blanket; the only softenings are the two product deny-list rules and the
six test relaxations.

---

## E. StyleCop config (`stylecop.json`)
```json
{
  "settings": {
    "documentationRules": { "companyName": "Nuevco", "xmlHeader": false },
    "orderingRules": { "usingDirectivesPlacement": "outsideNamespace", "systemUsingDirectivesFirst": true }
  }
}
```
- `companyName` drives the required file-header text `// Copyright (c) Nuevco. All rights reserved.`
  (SA1636). The header text must match `companyName` exactly or SA1636 errors.
- `xmlHeader: false` — plain `//` headers, not XML.
- `usingDirectivesPlacement: outsideNamespace` reconciles SA1200 with file-scoped namespaces.
- `systemUsingDirectivesFirst: true` matches `.editorconfig`'s `dotnet_sort_system_directives_first`.
- Because `GenerateDocumentationFile=true` on product projects, the SA16xx documentation family is active
  there: every public type and member must be documented.

---

## F. The custom-analyzer mechanism (the full infrastructure)

### F.1 The reusable pattern — copy this, rename tokens
**Analyzer project csproj** (`netstandard2.0` is mandatory — a Roslyn analyzer loads into the compiler host):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <RootNamespace>…Analyzers</RootNamespace>
    <AssemblyName>…Analyzers</AssemblyName>
    <AgentGuardIsAnalyzerProject>true</AgentGuardIsAnalyzerProject>   <!-- renamed flag -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsPackable>false</IsPackable>
    <IncludeBuildOutput>false</IncludeBuildOutput>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp"    PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <AdditionalFiles Include="AnalyzerReleases.Shipped.md" />
    <AdditionalFiles Include="AnalyzerReleases.Unshipped.md" />
  </ItemGroup>
</Project>
```
`EnforceExtendedAnalyzerRules=true` turns on the `RS####` authoring rules; the two `AnalyzerReleases.*.md`
files satisfy RS2008 (a new rule must be declared).

**Consuming-project wiring** (once, in `Directory.Build.props`, so every product project gets it; guarded so
the analyzer and its test project do not reference themselves):
```xml
<ItemGroup Condition="'$(AgentGuardIsAnalyzerProject)' != 'true'">
  <ProjectReference Include="$(MSBuildThisFileDirectory)analyzers/AgentGuard.Analyzers/AgentGuard.Analyzers.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```
This references the analyzer AS an analyzer (`OutputItemType="Analyzer"` + `ReferenceOutputAssembly="false"`),
a `ProjectReference`, not a NuGet package.

**Analyzer test project** references the analyzer as a plain `ProjectReference` and pulls in the verifier
harness. It must pin `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.CSharp.Workspaces` to
`4.14.0` to stop `…Analyzer.Testing.XUnit 1.1.2` from resolving `Microsoft.CodeAnalysis 1.0.1` (net461 assets
→ NU1701 → build error under TreatWarningsAsErrors). The verifier alias and assertion pattern:
```csharp
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    TAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
// await VerifyCS.VerifyAnalyzerAsync(source);  with {|RULEID:span|} markup asserting exactly the diagnostics
```
`AnalyzerReleases.Shipped.md` is a header only; `AnalyzerReleases.Unshipped.md` declares each new rule as a
row `Rule ID | Category | Severity | Notes`.

**The analyzer class shape** (a `[DiagnosticAnalyzer(LanguageNames.CSharp)] : DiagnosticAnalyzer` with a
`public const string DiagnosticId`, a static `DiagnosticDescriptor Rule`, `SupportedDiagnostics`, and an
`Initialize` that calls `ConfigureGeneratedCodeAnalysis(None)`, `EnableConcurrentExecution()`, and a
`Register…Action`) is the template for any new rule.

**Process to author a new rule:** add a `DiagnosticAnalyzer` class with a new id and prefix; declare it as a
row in `AnalyzerReleases.Unshipped.md`; add a verifier test asserting it fires and produces no other
diagnostics; set its severity in `.editorconfig` if it should differ from the class default.

### F.2 Proteus-specific content (do NOT copy the rule body)
Proteus contains exactly one custom `DiagnosticAnalyzer` and zero `CodeFixProvider` classes, and there is no
written new-rule process anywhere — the authoring steps above are reconstructed from the code and the
`AnalyzerReleases.Unshipped.md` table, which is the only "New Rules" artifact. The one shipped rule is
`PRO0001` (`AbstractionsMustBeInterfacesAnalyzer`), category
`Nuevco.Proteus.Architecture`, default `Warning`: every public type declared directly in a namespace ending
in `.Abstractions` must be an interface. It uses `RegisterSymbolAction(…, SymbolKind.NamedType)`. For
agent-guard this rule body is optional — ship the analyzer project with the full mechanism and either zero
rules initially or an agent-guard-appropriate rule under a new prefix (for example `AGD0001`).

---

## G. Golden build
- Build: `dotnet build` from the repo root (selects the `.sln`). Because `TreatWarningsAsErrors=true` and
  `CodeAnalysisTreatWarningsAsErrors=true` are global, this is already a warnings-as-errors build; no flag is
  needed. The `Makefile` `build` target is `dotnet restore` then `dotnet build`.
- Test: `dotnet test` (Makefile `test` depends on `build`), running the xUnit suites.
- Green means zero warnings and zero errors and all tests pass.
- SDK pinned by `global.json` to `9.0.101`, `rollForward: latestMinor`.
- No in-repo CI; the contract lives in MSBuild properties + the Makefile. `CI=true` flips
  `ContinuousIntegrationBuild=true`.

---

## H. Reusable vs proteus-specific
Copy verbatim (rename only tokens): the `Directory.Build.props` structure (all §B props, the six-analyzer
ItemGroup with `PrivateAssets="all"`, the `stylecop.json` AdditionalFiles line, the test-globalconfig gate,
the guarded analyzer `ProjectReference`); the entire `Directory.Packages.props` (every version); the
`.editorconfig` formatting and style block, the SA1101/SA1309 deny-list with reasons; the `stylecop.json`
structure; the whole `eng/test-analyzer-relaxations.globalconfig`; `global.json`; the analyzer project csproj
shape and the analyzer-test csproj shape including the `4.14.0` pins; the `tests/Directory.Build.props`
re-import pattern; the `Makefile`.

Proteus-specific (adapt or omit): the names `ProteusIs*`, `Nuevco.Proteus.*`, `Product=Proteus`,
`Authors/Company=Nuevco`, `Version`, the `analyzers/Nuevco.Proteus.Analyzers/…` path; the `PRO0001` line in
`.editorconfig`; the `stylecop.json` `companyName` and the file-header copyright text; the
`AbstractionsMustBeInterfacesAnalyzer.cs` body and `Pro0001AnalyzerTests.cs`.

---

## I. Replication plan for agent-guard
Create, at the agent-guard repo root:
1. `global.json` — `{ "sdk": { "version": "9.0.101", "rollForward": "latestMinor" } }`. Confirm a 9.0.1xx SDK
   is installed before the first build.
2. `Directory.Packages.props` — CPM on; pin every version from §C exactly.
3. `Directory.Build.props` — all §B global props verbatim; metadata adapted to agent-guard; the SourceLink
   reference; the six-analyzer ItemGroup; the `stylecop.json` AdditionalFiles; the `GlobalAnalyzerConfigFiles`
   gate renamed to `AgentGuardIsTestProject`; the guarded analyzer `ProjectReference` renamed to
   `AgentGuardIsAnalyzerProject` and pointed at `analyzers/AgentGuard.Analyzers/AgentGuard.Analyzers.csproj`.
4. `.editorconfig` — the formatting and style block verbatim; the SA1101/SA1309 deny-list with reasons;
   replace the `PRO0001` line with the chosen agent-guard rule id or drop it.
5. `stylecop.json` — the structure; set `companyName`; keep `xmlHeader:false`,
   `usingDirectivesPlacement:outsideNamespace`, `systemUsingDirectivesFirst:true`.
6. `eng/test-analyzer-relaxations.globalconfig` — verbatim (all six relaxations, `is_global = true`).
7. `tests/Directory.Build.props` — the re-import + `GenerateDocumentationFile=false` + `IsPackable=false`
   pattern (adjust the `GetPathOfFileAbove` depth to the layout).
8. Projects:
   - `AgentGuard.Engine.csproj` (class library) — `AssemblyName`/`RootNamespace`/`PackageId` only.
   - `AgentGuard.Cli.csproj` — `OutputType=Exe`, `AssemblyName` (the `guard` binary), reference Engine + (if
     used) `System.CommandLine`.
   - `AgentGuard.Tests.csproj` — `AgentGuardIsTestProject=true`; test SDK + xunit + xunit.runner.visualstudio
     + FluentAssertions + coverlet; reference Engine.
   - `AgentGuard.Analyzers.csproj` — the §F.1 shape (`netstandard2.0`, `AgentGuardIsAnalyzerProject=true`, the
     two Roslyn packages `PrivateAssets="all"`, the two `AnalyzerReleases.*.md`).
   - `AgentGuard.Analyzers.Tests.csproj` — `net9.0`, both flags, `GenerateDocumentationFile=false`; test SDK +
     xunit + `…Analyzer.Testing.XUnit` + the `Microsoft.CodeAnalysis.CSharp(.Workspaces)` `4.14.0` pins;
     reference the analyzer.
9. `AnalyzerReleases.Shipped.md` (header only) and `AnalyzerReleases.Unshipped.md` (declare any rule shipped).
10. `Makefile` — the four targets.

---

## J. Risks and gotchas
1. The first warnings-as-errors build on real code will surface a large wall of findings (four analyzer
   suites at `AnalysisMode=All` + required XML docs on every public member, all as errors). Proteus's stance
   is fix-in-code, not silence. Budget for it, or stage `AnalysisMode` down then ratchet up.
2. Because `GenerateDocumentationFile=true` and SA16xx is not suppressed for product code, every public type
   and member in Engine and Cli needs XML docs and the exact `// Copyright (c) <Company>. All rights
   reserved.` header; the header must match `stylecop.json` `companyName` or SA1636 errors.
3. The analyzer project must target `netstandard2.0` and set `EnforceExtendedAnalyzerRules=true`; targeting
   `net9.0` breaks analyzer loading.
4. The Roslyn testing harness (`…Analyzer.Testing.XUnit 1.1.2`) drags in `Microsoft.CodeAnalysis 1.0.1`
   (net461 → NU1701 → error) unless the analyzer-test project pins `Microsoft.CodeAnalysis.CSharp` and
   `.Workspaces` to `4.14.0`.
5. Version pinning is load-bearing (pre-release versions like `StyleCop.Analyzers 1.2.0-beta.556`,
   `SonarAnalyzer.CSharp 10.29.0.143774`); a newer analyzer can turn green red. Pin exactly; upgrade one at a
   time.
6. `AnalysisLevel=latest` couples the CA/IDE rule set to the installed SDK; `global.json` pins `9.0.101` with
   `rollForward: latestMinor`, but a newer 9.0.1xx band could enable more CA rules. Tighten `rollForward` for
   fully reproducible builds.
7. With CPM on, csproj `PackageReference`s must carry no `Version`.
8. Test relaxations are gated on the `…IsTestProject=true` flag, not the folder; a test project missing the
   flag fails on SA1600/CA1707/CA2007/etc.
9. CA1515 (`AnalysisMode=All`) wants test classes internal, but xUnit v2 discovers only public classes; the
   `CA1515=none` test relaxation is essential.
10. There is no in-repo CI; enforcement is MSBuild + Makefile only.
11. `Deterministic=true` + `Microsoft.SourceLink.GitHub` expect a git repo with a GitHub remote; without the
    expected remote SourceLink can warn, and warnings are errors. agent-guard's remote is
    `4thWAIV/agent-guard` — verify SourceLink resolves cleanly, or scope/remove it until the remote is set.

---

## K. Decisions the contract must settle (not decided here)
- Company/header identity for agent-guard: the `stylecop.json` `companyName` and the `.cs` file-header text.
- Whether agent-guard ships a custom analyzer rule now (a new prefix such as `AGD0001`) or ships the analyzer
  project with the full mechanism and zero rules initially.
- Whether to include a CI job that runs `dotnet build` + `dotnet test`.
- Whether `SourceLink` stays enabled given the current remote state.
