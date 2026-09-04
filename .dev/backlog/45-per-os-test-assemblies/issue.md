# 45 — Reorganize per-OS presence tests into per-OS test assemblies (retire the CrossPlatform.Tests glob-surgery)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/45

---

## What

Reorganize the **already-written, already-passing** per-OS presence tests out of the single `AgentGuard.CrossPlatform.Tests` project (where they are selected by `<Compile Remove>` / `<Choose>` glob-surgery on `AgentGuardPlatformRid`) into **per-OS test assemblies** — `AgentGuard.CrossPlatform.MacOS.Tests`, `.Linux.Tests`, `.Windows.Tests` — mirroring how the source is already split into `AgentGuard.CrossPlatform.MacOS` / `.Linux` / `.Windows`.

**This is a pure reorganization, NOT a coverage gap.** Every per-OS presence test already exists and runs correctly under the current glob (the per-OS mappers, the check-against-fake tests, the Linux subject-building, the `BuildCheckAuthorizationRequest` wire-builder test, the policy-integrity test). Nothing is untested. The only thing being fixed is the project organization.

## Why

The source is cleanly per-OS-assembly. The test project instead jams all three OSes' test folders into one project and picks one by conditional compilation — the exact anti-pattern the `OsSpecificFileNotCrossCompiledTests` guard ("rule 4 of the cross-OS ruleset") forbids for `src/`, except in the one place that guard does not reach (the test project). It works and it keeps coverage per-leg-clean, but it is inelegant and inconsistent with the code structure.

The current glob-surgery (`tests/AgentGuard.CrossPlatform.Tests/AgentGuard.CrossPlatform.Tests.csproj`):

```xml
<ItemGroup>
  <Compile Remove="Presence/MacOS/**/*.cs" />
  <Compile Remove="Presence/Linux/**/*.cs" />
  <Compile Remove="Presence/Windows/**/*.cs" />
</ItemGroup>
<Choose>
  <When Condition="...win...">   <Compile Include="Presence/Windows/**/*.cs" /> </When>
  <When Condition="...linux...">  <Compile Include="Presence/Linux/**/*.cs" />  </When>
  <When Condition="...osx...">    <Compile Include="Presence/MacOS/**/*.cs" />  </When>
</Choose>
<Import Project="../../src/AgentGuard.CrossPlatform/PlatformImplementation.targets" />
```

## Hard constraints (learned while designing this — do NOT re-derive)

- **Per-leg coverage scoping is mandatory, not optional.** Each per-OS implementation (`.MacOS`/`.Linux`/`.Windows`) is coverage-gated at ≥75% **only on its own OS leg** (`eng/coverage-gate.sh`; the `.runsettings` `<Include>` counted-set self-check asserts each per-OS assembly is gated on exactly one leg). The reorg must keep each OS's tests measured and gated on its own leg only — no cross-leg coverage pollution (do NOT make the per-OS test projects "always build and run everywhere"; that cross-measures a per-OS impl on a leg that is not supposed to gate it).
- **The Windows WinRT cross-compile constraint.** `AgentGuard.CrossPlatform.Windows` compiles on macOS/Linux today only because its `UserConsentVerifier` interop is still a throwing skeleton. Once the real WinRT interop lands, it may require a Windows-only target framework and stop compiling off-Windows — so `Windows.Tests` must be **conditionally not-built off-Windows**, not "built empty" (empty just relocates the glob smell into the project).
- **The native smoke stays shared.** `PresenceNativeSmokeTests` is OS-agnostic (it drives the host-selected real impl via `PlatformImplementation.targets`) and stays in `AgentGuard.CrossPlatform.Tests`. It does NOT move into the per-OS projects (a Linux smoke on a Mac would hit real polkit and fail).
- **`InternalsVisibleTo` moves.** Each per-OS src assembly currently grants IVT to `AgentGuard.CrossPlatform.Tests`; move that grant to the matching new per-OS test assembly.

## Acceptance

- Three per-OS test projects, each holding only that OS's presence tests; no `<Compile Remove>`/`<Choose>` glob-surgery in any test csproj.
- The coverage gate stays per-leg-clean and green on all three legs (each per-OS impl ≥75% on its own leg; the counted-set self-check passes).
- Whole solution builds 0/0 under the fence; every test still passes.

## Do it grounded

Before touching a csproj: read how the source's per-OS assemblies and the `PlatformImplementation.targets` RID selector interact with the per-leg coverage gate and the CI per-leg invocations (`.github/workflows/ci.yml`), and design the per-leg scoping deliberately (reference each impl directly; "not build" off-leg; handle Windows). This was deferred from the OS presence-check contract precisely so it could be done as its own grounded step rather than a mid-contract detour.
