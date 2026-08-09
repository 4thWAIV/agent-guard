# Cross-platform engine + the native-interop framework

**Status.** This contract makes the engine run on all three OS by replacing the one Unix-only native call with a governed native-interop framework, and it sets the standing rules for how native interop is done in this codebase. It is the **prerequisite that unblocks the CI/CD contract** (`.dev/inprocess/2026-08-03-ci-cd-build-sign-release/contract.md`): that contract's success — the pipeline green on **all three OS** on GitHub — cannot be met until this one lands, because the Windows test leg fails today (the engine P/Invokes `libc rename`, which does not exist on Windows). The CI/CD contract stays open; no platform is left behind.

Every decision below carries Tim's exact words. The three-method interface and the whole structure were **proven on a disposable macOS prototype** (`proto/platform-interop`): six OS-agnostic spec tests green, 0 warnings / 0 errors under the repo's full analyzer suite, reviewer PASS.

## Execution (two-phase, per Rule-Driven Development)

1. **Rule phase** — an adversary-backed analyzer workflow (DRY / SOLID / Lie-catcher) creates and wires the interop analyzer rule; it goes **RED** against the existing `NativeInterop.cs`. (Rule-generation authority; edits `analyzers/**`.)
2. **Implementation phase** — a fresh agent takes this same contract, is told the rule, builds the framework, and **cleans up `NativeInterop.cs`** (and anything else out of spec) until the build is green under the rule. It has no authority to weaken, suppress, or exempt the rule.

(The token/revocation machinery is a later phase; for now the two phases are two separate adversary-backed workflows.)

## Decisions

### `native-interop-needs-my-decision`
Adding any native interop (a `[LibraryImport]`/`[DllImport]` P/Invoke) is a decision that requires Tim's explicit yes. The existing `src/AgentGuard.Engine/Setup/NativeInterop.cs` (`libc rename`) was added in a prior contract **without** that decision and is therefore unauthorized; it is removed and replaced under this contract.
Tim: *"THERE never should have been a NativeInterop in my codebase without my decision."* and *"NATIVE INTEROP IS FUCKING ONE OF THEM! SO THIS was illegal code in my mind."*

### `interop-only-in-crossplatform-libs`
All native interop lives **only** inside the per-OS `AgentGuard.CrossPlatform.*` implementation libraries, behind the locked interfaces. No `[LibraryImport]`/`[DllImport]` anywhere else in the codebase.
Tim: *"no native interop is done outside of these DLLS."*

### `interop-interfaces-locked`
Interop interfaces are locked: no change to an existing interop interface, and no new interop interface, without Tim's permission. Tim sees and agrees to the structure of the interfaces **and** the resulting classes before they are built.
Tim: *"NO CHANGE TO AN INTEROP INTERFACE can be made without my permision and no new ones can be made without it. ALL INTEROP requires I get to see and agree to the structure of the interafces and the resulting clasees."*
**Signed off 2026-08-08:** Tim approved the interface structure (the `IPlatformServices` container plus the three-method `IPlatformFileSystem`) and the resulting per-OS classes before build. Tim: *"I sign off on the inerfaces."*

### `three-per-os-libs-shared-source`
Three native libraries — `AgentGuard.CrossPlatform.MacOS`, `AgentGuard.CrossPlatform.Linux`, `AgentGuard.CrossPlatform.Windows` — each implementing the same interface, each with a bootstrap that also implements the interface so they are polymorphically swappable. **KLAXON — strict lie-catcher alteration check:** where a single implementation satisfies both mac and linux (the POSIX/`libc` code), **one `.cs` file is authored once (in MacOS) and `<Compile Include=… Link=…>`-linked into the Linux project** across directories — NOT duplicated into two copies, NOT collapsed to two libraries, NOT a shared base-class shortcut. Windows authors its own.
Tim: *"3 Native libraries all implementing the same interface each having a core bootstrap function that also implements the same interface so that they could be polymorphicly replaced. I LIKE THIS BEST."* and *"let's make it 3 now. HOWEVER!!!! ... we should have the convention be to LINK import the file in the csproj file from mac to linux. SO IF a single implementation can satisfy both. ONE .cs file is created and it is linked into the project (across project directories) into the other."*

### `platform-create-container`
The factory is `Platform.Create()`, returning a **container** `IPlatformServices` with one property now (`FileSystem`) and room to grow (e.g. `Bio`) with no restructuring of how services are located. **KLAXON:** a container, not a bare `IPlatformFileSystem Create()`.
Tim: *"It should be Platform.Create() THAT returns a container object that has one property now (FileSystem) ... But will expand later. That way when we need to add Bio We are not restructuring how services are located."*

### `namespace-crossplatform`
The namespace is `AgentGuard.CrossPlatform`. This resolves the type-name-equals-namespace analyzer complaint naturally (the factory type `Platform` no longer matches the namespace tail), so **no analyzer suppression is needed** and the `Platform.Create()` call shape is kept. Assemblies match: `AgentGuard.CrossPlatform`, `.MacOS`, `.Linux`, `.Windows`.
Tim: *"Change the namespace to AgentGuard.CrossPlatform. Then the problem is resolved."*

### `complete-set-interface`
The interop interface is a **complete set** — it provides the capabilities to do the act plus its setup and teardown (CRUD) — and is allowed to grow; every addition is an interop-interface change needing Tim's sign-off (`interop-interfaces-locked`). Test *scaffolding* (temp dirs, marker files) is plain cross-platform `System.IO` and is deliberately NOT in the interface.
Tim: *"the interface and implementation may need to be a 'complete set' meaning the interop library provides all the capabilities we would need to do the act but also setup the action and tare down (what we would think of as CRUD in data world). FOR US that means this interface will probably need to grow."*

### `three-method-filesystem-interface` (proven)
`IPlatformFileSystem` is exactly three methods, proven complete by the prototype (no fourth primitive was needed to express the six spec tests):
- `string? ReadLinkTarget(string linkPath)` — raw target, or null if not a link.
- `void Repoint(string linkPath, string relativeTarget)` — atomically point at the target, create OR replace, **never momentarily absent**; also creates any missing parent directory (see `behavioral-uniformity-proven-by-spec`).
- `void Remove(string linkPath)` — remove the link only, never its target.
The generic idempotency check ("skip if already correct") stays engine-side, not in the interface.

### `behavioral-uniformity-proven-by-spec`
The three OS implementations behave **exactly** the same, with no deviation. Any behavior — e.g. `Repoint` creating a missing parent directory — is done on all three or on none; if it cannot be identical on all three, it is removed. Whatever the behavior is (or is not), it is tested and proven by the one OS-agnostic spec. (Parent-directory creation is a managed `Directory.CreateDirectory` call that behaves identically on all three, so it **stays on all three**, is written into the interface contract, and gets a spec test.)
Tim: *"WHATEVER keeps the 3 OS instances showing the same behavior exactly with no deviation. IF we parent create we parent create ON ALL 3 — IF we can not parent create on all 3 or IF WE DO NOT ... THEN it must go. WHATEVER we do (or don't do) it must be tested and proven as a spec."*

### `one-osagnostic-spec-test-project`
**One** test project (`AgentGuard.CrossPlatform.Tests`), OS-agnostic — the tests assert the spec and never branch on OS; the implementation under test is swapped by csproj (the same conditional/link mechanism the engine uses), so the same tests validate each platform's implementation in its own CI leg. The real engine and all real-project tests **mock** `IPlatformServices`/`IPlatformFileSystem` and never touch native. **KLAXON:** one shared project, not per-OS test projects, not OS-branching test code.
Tim: *"1 test project to test all 3 of them at the same time ... THE tests should not care about the OS and should run the same on all if the functionality is to spec. SO ... 1 test file that uses the same [poly-load] to bring in the instance, and has it's import swapped by csproj magic (same magic we use in engine)."*

### `keep-symlinks-audience-has-privilege`
The machine layout keeps its symlinks (the audience is developers — ~95% admin, ~99% Developer Mode — so Windows symlink creation is not a real barrier); the fix is to replace the Unix-only atomic swap with the cross-platform one behind the interface, and to fail with a clear "enable Developer Mode or run elevated" message for the rare user with neither. No junction/launcher redesign.
Tim (audience): *"WHO IS going to use this tool? ... just being admin (95% of my users will be) or having dev mode (99%) enough?"*

### `wire-engine-delete-nativeinterop`
`SymlinkOps` is rewired to consume `IPlatformFileSystem` (read via `ReadLinkTarget`, write via `Repoint`, remove via `Remove`); the idempotency compare stays in `SymlinkOps`; the create-temp-then-atomic-swap moves into each platform's `Repoint`. `src/AgentGuard.Engine/Setup/NativeInterop.cs` is **deleted**. (Consumers, per CodeGraph: `CreationHelper.PointCurrent`/`EnsureBinGuard`, `CurrentSymlinkCondition`, `BinSymlinkCondition`, `InstallIntegrity`; `NativeInterop.Rename` has exactly one caller.)

### `config-protection-crlf-fix`
The config-protection canonical drift check normalizes line endings (CRLF/LF) so drift is detected identically on Windows (the two non-symlink Windows test failures). This is the guard's own config-protection code from the config-protection contract.

### `interop-ca-rule-enforcement`
The custom analyzer rule that makes `[LibraryImport]`/`[DllImport]` a build error outside the `AgentGuard.CrossPlatform.*` libraries (enforcing `interop-only-in-crossplatform-libs`) is created and **wired in during this contract's rule phase**, where it goes **RED** against the existing `NativeInterop.cs`. That red is the forcing function: the implementation phase must build the framework and **clean up `NativeInterop.cs`** until the build is green under the rule — the AI does the cleanup, with no exemption or suppression. Governed by Rule-Driven Development and the cleanup law now in the working rules (`rails-read-me`).
Tim (RDD + cleanup law): *"THE rules will be created and WILL FAIL ... RED ... WIth the existing code that is out of spec. THEN the contract will be forced to create the new system ... AND CLEAN UP ... YOU MSUT DO THE CLEANUP the old crap that never should have existed. ANY TIME we add a rule the next contract must do any clean up."* Earlier: *"we need ana-rules to BLOCK you ... so that this is enforced and no native interop is done outside of these DLLS."*

### `rule-phase-ruleset` — the four guardrails (DESIGN output, signed off)
The rule phase creates FOUR guardrails, determined in DESIGN and signed off by Tim (*"I agree those are DAMN fine rules."*). Each is written FIRST — RED where it hits live code, preventive where it does not — and the implementation phase cleans up under them with no suppression.
1. **Interop boundary** (Roslyn analyzer). `[LibraryImport]`/`[DllImport]` is a build error outside `AgentGuard.CrossPlatform.*`. RED against `src/AgentGuard.Engine/Setup/NativeInterop.cs`; forces its deletion. (This is `interop-ca-rule-enforcement` above.)
2. **No OS branching in OS-agnostic code** (Roslyn analyzer). A platform `#if`, `OperatingSystem.IsX()`, or `RuntimeInformation.IsOSPlatform` is a build error outside `AgentGuard.CrossPlatform.*`. RED against `src/AgentGuard.Engine/Setup/CreationHelper.cs:46` (`OperatingSystem.IsWindows()`), which the implementation moves behind the interface. Folds in the anti-`#if` rule (was `PLAN-crypto-minting-and-presence` platform decision #2) and keeps the engine and the one spec-test project OS-agnostic.
3. **Factory returns the container** (Roslyn analyzer). The platform factory must return `IPlatformServices`, never a bare `IPlatformFileSystem`. Preventive (the factory does not exist yet); pins `platform-create-container` so a later edit cannot collapse it to a bare service.
4. **POSIX source is link-shared, not copied** (a test, not a C# analyzer). A test asserts the Linux project `<Compile Include=… Link=…>`s the single MacOS POSIX source. It only needs to check the link: a real second copy would be the same type and namespace compiled twice into one assembly (a duplicate-type build error), so the compiler already forbids the copy. Fences `three-per-os-libs-shared-source`.

## What we're building

A governed native-interop framework and the engine change that uses it, so the guard runs correctly on macOS, Linux, and Windows. The `AgentGuard.CrossPlatform` contract assembly holds pure interfaces + the `Platform.Create()` container factory; three per-OS libraries implement them (POSIX source authored once, link-shared mac→linux; Windows authors its own with `MoveFileEx`); one OS-agnostic spec project proves identical behavior across all three; the engine's `SymlinkOps` is rewired to the interface and `NativeInterop.cs` is deleted; the config-protection CRLF gap is fixed. This unblocks the CI/CD contract's Windows-Intel test leg.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change.

End state:
- `AgentGuard.CrossPlatform` (interfaces + `Platform.Create()` container, zero native code) and the three per-OS libraries build; the POSIX source is one authored file `<Compile Link>`-shared mac→linux (verifiable in the csproj + a single source file), never duplicated.
- The one `AgentGuard.CrossPlatform.Tests` project is OS-agnostic and its spec tests pass on **all three** OS in CI, proving identical behavior (including `Repoint`'s parent-directory creation and the two safety properties: `Remove` never deletes the target; a re-point leaves no stray temp).
- The engine consumes `IPlatformFileSystem`; `src/AgentGuard.Engine/Setup/NativeInterop.cs` no longer exists; no `[LibraryImport]`/`[DllImport]` exists outside `AgentGuard.CrossPlatform.*`.
- `dotnet build -c Release` = 0/0 and `dotnet test -c Release` = 0 failed, locally and in CI, **including the Windows leg** (the ~35 install/setup failures and the 2 config-protection CRLF failures are gone).
- The CI/CD contract's gate goes green on all three OS on GitHub (its success, now reachable).
- Nothing under `Abstractions/**` or `analyzers/**` changed.

Any restatement or weakening of this to fit the result is a top-line Lie-catcher finding.

## Surfaces

- **New:** `src/AgentGuard.CrossPlatform/` (interfaces `IPlatformServices`, `IPlatformFileSystem`; assembly-info); `src/AgentGuard.CrossPlatform.MacOS/` (the POSIX impl authored here); `src/AgentGuard.CrossPlatform.Linux/` (links the POSIX `.cs`); `src/AgentGuard.CrossPlatform.Windows/` (`MoveFileEx` impl); `tests/AgentGuard.CrossPlatform.Tests/` (the one OS-agnostic spec project). The prototype at `proto/platform-interop/` is the structural model.
- **Edited (engine, non-frozen):** `src/AgentGuard.Engine/Setup/SymlinkOps.cs` (rewired to `IPlatformFileSystem`); `CreationHelper.cs`, `CurrentSymlinkCondition.cs`, `BinSymlinkCondition.cs`, `InstallIntegrity.cs` (obtain/read via the interface); wherever the `IPlatformServices` is constructed and threaded (via `GuardEngine`/`SetupContext`); the config-protection canonicalizer (CRLF normalization).
- **Deleted:** `src/AgentGuard.Engine/Setup/NativeInterop.cs`.
- **Solution + CI:** add the new projects to the solution; the CI legs already build/test all six RIDs (the CI/CD contract) — no CI change needed here beyond the projects being in the build.
- **Frozen — untouched:** `Abstractions/**`, `analyzers/**`.

## Reuse ledger (from the prior-art-ledger run, 2026-08-08)

| Capability | Ruling | Owner / note |
|---|---|---|
| platform-interop-interfaces (`IPlatformServices`, `IPlatformFileSystem`) | new | No live impl; the logic exists in `SymlinkOps` but with no interface seam. |
| platform-container-factory (`Platform.Create()`) | new | The factory does not exist yet. |
| posix-symlink-filesystem | **reuse** | `src/AgentGuard.Engine/Setup/SymlinkOps.cs:23` (`EnsurePointsTo`/`ReadRawTarget`/`PointAtomically`/`DeleteIfExists`) + `AtomicFile.TemporarySiblingPath` + `NativeInterop.Rename`. The POSIX impl EXTRACTS this existing logic (matching `wire-engine-delete-nativeinterop`'s "the create-temp-then-atomic-swap moves into each platform's `Repoint`"), never re-authored fresh. |
| windows-symlink-filesystem | new | No Windows symlink impl exists (`MoveFileEx` path is new). |
| symlinkops-rewire-nativeinterop-removal | new | `SymlinkOps` still calls `NativeInterop` directly; the rewire + deletion is new work. |
| config-crlf-normalization | new | No CRLF/LF normalization exists in the canonical drift path (`RegionDiffer`/`JsonRegionAdapter`). |
| interop-osbranching-factory-analyzers (AG0008/AG0009/AG0010 + link-shared test) | reuse | Created in this contract's rule phase; they now exist. |

## What to do

1. Build `AgentGuard.CrossPlatform` (the two interfaces + `Platform.Create()` container; `namespace-crossplatform`), following the proven prototype.
2. Build the three per-OS libraries (`three-per-os-libs-shared-source`): MacOS authors the POSIX `PosixFileSystem` (`ReadLinkTarget`/`Remove` managed; `Repoint` = create temp link + `libc rename`, creating missing parents); Linux `<Compile Link>`s the identical POSIX source; Windows authors its own (`Repoint` = create temp link + `MoveFileEx REPLACE_EXISTING`; `Remove` uses the Windows call that removes a directory vs file symlink; parents created identically).
3. Build the one `AgentGuard.CrossPlatform.Tests` (`one-osagnostic-spec-test-project`): the prototype's six spec tests plus a parent-directory-creation test (`behavioral-uniformity-proven-by-spec`); tighten the no-stray-temp test to assert the directory contains exactly the expected entries; the impl is swapped per-OS by csproj.
4. Rewire the engine (`wire-engine-delete-nativeinterop`): thread an `IPlatformFileSystem` through `SymlinkOps` and its consumers; keep the idempotency compare engine-side; delete `NativeInterop.cs`; the real engine tests mock the interface.
5. Fix the config-protection CRLF canonicalization (`config-protection-crlf-fix`).
6. Add the "enable Developer Mode / run elevated" clear error for the rare no-privilege Windows case (`keep-symlinks-audience-has-privilege`), and a line in the alpha-run doc.
7. The interop CA-rule is created and wired in the **rule phase** (going red against `NativeInterop.cs`); the **implementation phase** cleans up `NativeInterop.cs` until green under it (see Execution). No exemption, no suppression.
8. Prove: `dotnet build`/`test` green locally; then the CI/CD pipeline green on all three OS on GitHub (the CI/CD contract's success).

## What the agent MUST NOT do

- Add any native interop outside `AgentGuard.CrossPlatform.*`, or change/add an interop interface, without Tim's sign-off.
- Let the three OS implementations diverge in behavior, or leave any behavior un-proven by the spec.
- Duplicate the POSIX source instead of link-sharing it mac→linux; collapse to two libraries; use a shared base class; or make `Platform.Create()` a bare service instead of the container. (KLAXON — strict lie-catcher checks.)
- Touch `Abstractions/**` or `analyzers/**`.
- Weaken, skip, or mock-away the Windows behavior to force green; the spec runs for real on each OS in CI.

## Acceptance

1. Local: `dotnet build -c Release` = 0/0; `dotnet test -c Release` = 0 failed.
2. The one `AgentGuard.CrossPlatform.Tests` project's spec tests pass on macOS, Linux, and Windows in CI (evidence: the three CI legs green).
3. `grep -r "LibraryImport\|DllImport"` finds P/Invoke ONLY under `src/AgentGuard.CrossPlatform.*`; `src/AgentGuard.Engine/Setup/NativeInterop.cs` does not exist.
4. The POSIX source is a single authored `.cs`, `<Compile Link>`-referenced by the Linux csproj (shown in the csproj); no duplicate copy exists.
5. `Platform.Create()` returns `IPlatformServices` with `FileSystem`; adding a property needs no caller change.
6. The Windows engine test failures are gone: the ~35 install/setup `DllNotFoundException`s and the 2 config-protection CRLF failures pass.
7. The CI/CD contract's gate is green on all three OS on GitHub.
8. `git diff` shows nothing under `Abstractions/**` or `analyzers/**`.

## Open

- Structural confirm: this is a **new contract** that gates the CI/CD contract's completion (vs. edited into the CI/CD contract). Proceeding this way per Tim's "extended contract to this one"; flag if you want it merged into the CI/CD file instead.

## Tier

FULL — new assemblies, a native-interop framework, engine rewiring, a deletion, and cross-OS behavior proven live; all roles.

## Scope

Change scope only by editing this file before the run starts.
