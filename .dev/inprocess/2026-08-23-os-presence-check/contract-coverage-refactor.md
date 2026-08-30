# Presence coverage refactor — shrink the untested native surface behind a mockable native-ops seam

A phase-2 contract of the OS presence-check run-record. The presence feature is implemented and behaviorally green; this refactor exists solely to raise per-OS line coverage toward the 75% gate by restructuring the native code for testability, adding no `[ExcludeFromCodeCoverage]`.

## Decisions

Each carries Tim's verbatim approving words.

### `deep-native-ops-seam`
Adopt the DEEP seam on macOS and Windows: a new internal native-ops interface sits BELOW the existing port. The port implementer (`LocalAuthentication` / `WindowsUserPresence`) keeps implementing its port interface unchanged and becomes a pure DI orchestrator injected with the native-ops interface; all flow, mapping, task/GCHandle wiring, and cancel-invalidate logic moves into the orchestrator (fake-driven in tests); only the raw calls stay in the thin native-ops implementer. Shallow (reshape-the-port, keep P/Invokes in place) is rejected: it caps macOS ~50% and leaks a raw handle up to the check layer.
Tim: *"YES, go with deep."*

### `macos-one-windows-two-native-ops`
macOS gets ONE native-ops interface (the Objective-C runtime is a single subsystem). Windows gets TWO — one for Windows Hello (WinRT `UserConsentVerifier`), one for the credential prompt (`CredUIPromptForWindowsCredentials` + `LogonUser`) — because they are independent native subsystems sharing no handle.
Tim: *"2 (is that not what we also just did for Mac?)"* and *"Native is a place where one extra layer of abstraction is key to testing."* (Clarified in reply: macOS one, Windows two; Tim did not redirect.)

### `fence-relocation` (decision-gated — Tim's personal yes)
Relocate the presence native-interop exemption (shared `PresenceContracts.IsInNativePresencePortOwner`, backing AG0113 and AG0101's presence branch) OFF the flow port and ONTO the new native-ops owner: raw presence P/Invoke and `Marshal` are legal ONLY inside the native-ops implementer; the flow-port implementer is REMOVED from the exemption, so a raw call added back into the orchestrator is a build error. Land it RED-first — prove the relocated rule flags the CURRENT pre-refactor `LocalAuthentication`/`WindowsUserPresence` raw calls before any code moves.
Tim: *"Yes, we sould place the rule where it will do the best good... and enforce the new behavior. THE rules should match the new design."*

### `native-ops-guardrail-rules`
Add the guardrails that keep the thin native-ops core thin, so no testable logic hides in the one class we don't fake: no control flow in a native-ops implementer (try/catch exempt), no async/task-completion state in it, no fields at all (fully stateless; const exempt); plus extend the existing no-timeout (AG0107), single-implementer (AG0114), and native-callback-guard (AG0105) rules down to the new native-ops owner. The native-ops class stays behind its interface and DI-mockable; these rules constrain only its implementation contents.
Tim: *"Okay but are these still behind an interface and DI mockable?"* and *"Okay let's do it and grind this to completion."* (Confirmed in reply that the class stays interface-backed and DI-mockable; Tim proceeded.)

### `no-coverage-exemption-this-phase`
Add NO `[ExcludeFromCodeCoverage]` in this refactor. Minimize the untestable surface by restructuring only, then measure the real per-OS numbers; Tim decides any residual exclusion afterward against those measured numbers.
Tim: *"DO NOT excempt any code from coverage ONLY refactor and minimize coverage area... who knows we may get under 75% on mac..."* and *"THEN report back and we'll decide what to do next."*

### `housekeeping-fixes-ride-along`
The four confirmed REFUTE housekeeping findings are fixed in this phase's IMPLEMENT: the `SetupCommands.cs` "(ungated in this build)" stale doc, the twelve test files' "RED now" TDD comments, the `IPolkitAuthority`/`TmdsPolkitAuthority` cancellation-doc overclaim (correct to start-only semantics, residual risk tracked in #47), and the `install.sh` action-id third-spelling DRY violation (install to the source basename).
Tim: *"Let's get through all other issues. GRIND us down to just coverage alone as the ONLY remaning task."*

## The standard / what we're building

Each per-OS presence assembly's decision/mapping/flow logic lives in a fake-testable orchestrator (the port implementer), and its raw native calls live in a thin, stateless, logic-free native-ops implementer behind a new internal interface. A pointed-integration test drives the REAL non-interactive native ops (framework load, context create/release, the capability probe, `dlsym`, block build) so the raw core is covered without a prompt; only the interactive `evaluatePolicy`/Hello call and its callback stay behind the CI-only smoke. Linux is unchanged — its `Tmds` `MessageWriter`/`Reader` are `ref struct`s that cannot be faked, so its serialization stays as-is and any Linux exclusion is a separate later decision.

## Success definition

Tim's standing definition (ALL criteria met AND no errors in the system as a result of the change) plus this phase's specific end state: the whole solution builds 0/0 under the fence including the new and relocated analyzers; the full test suite is green; the macOS per-OS coverage gate (`eng/coverage-gate.sh . AgentGuard.CrossPlatform.MacOS`) is measured and its real number reported (target ≥75% with no `[ExcludeFromCodeCoverage]`); the Windows and Linux per-OS numbers are measured and reported; no `[ExcludeFromCodeCoverage]` attribute is added anywhere; and the relocated fence is proven RED-first against pre-refactor code. Where a measured number still falls short of 75%, that is surfaced to Tim with the real number for his exclusion decision — never self-waived.

## Surfaces (every store the same value/behavior lives)

- macOS: `LocalAuthentication.cs` (becomes orchestrator), a new `IObjCRuntime` + thin `ObjCRuntime` (names Tim may rename), `MacOsPresenceCheck.cs` (untouched — orchestration stays in `LocalAuthentication`), the macOS fakes/tests, the new macOS native-ops fake + pointed-integration test.
- Windows: `WindowsUserPresence.cs` (orchestrator), two new native-ops interfaces + thin impls, the Windows fakes/tests.
- Linux: unchanged.
- Analyzers: `PresenceContracts.cs` (the shared owner function + a new native-ops-owner list), `PresenceNativeInteropOwnerAnalyzer` (AG0113) + `OsDivergentFilesystemOnlyInCrossPlatformAnalyzer` (AG0101) exemption relocation, three new analyzers (no-logic, no-async-orchestration, no-state in native-ops), the AG0107/AG0114/AG0105 extensions, plus `AnalyzerReleases.Unshipped.md`.
- Coverage: `eng/coverage-gate.sh` (unchanged; the measurement oracle), the per-OS cobertura reports.

## Reuse ledger (GROUND prior-art, verified during DESIGN)

| Capability | Ruling | Owner / evidence |
|---|---|---|
| native-ops interface + thin impl (per OS) | new (precedented) | mirrors the established per-OS port pattern (`ILocalAuthentication`/`IWindowsUserPresence`/`IPolkitAuthority`); no prior native-ops layer exists |
| per-OS native-ops fake with per-method call counts | reuse | compose the existing `SingleCallRecorder<TArg,TResult>` (one per method), the technique already used by the presence fakes |
| single-implementer guard for the native-ops interfaces | reuse | register the existing `SecondImplementerGuard` algorithm against a new `PresenceContracts.AllNativeOpsOwners` list — no new algorithm |
| the owner check for the new/relocated rules | reuse/extract | `OwnerClass.IsOwner` + `PresenceContracts` owner identities; extract the native-ops-owner list into `PresenceContracts` |
| no-logic / no-async / no-state analyzers | new | no existing analyzer walks operations scoped to a native-ops owner; `no-state` reuses AG0112's `SymbolAction`-on-field shape unconditionally |
| callback-guard extension to the Windows COM handler | extract | reuse `NativeCallbackBodyMustBeGuardedAnalyzer`'s shared guarded-try/catch check for the new COM-visible `Invoke` shape |
| pointed-integration test on the real non-interactive native ops | new (precedented) | mirrors `PlatformFileSystemSpecTests` driving real `PosixNativeMethods` on disk |
| the pure mappers (`MapErrorCode`, Hello-result, cred-status, AsyncStatus) | new | extracted pure functions, table-tested like the existing `MacOsPresenceMapperTests` |

## What to do

1. RULE-PHASE: relocate the AG0113/AG0101 exemption to the native-ops owner (RED-first proof against current code), add the three new native-ops analyzers, extend AG0107/AG0114/AG0105 to the native-ops owner, add the `PresenceContracts.AllNativeOpsOwners` list. Independent panel refutes the rules.
2. ARCHITECTURE: declare the new native-ops interfaces (macOS one, Windows two) + thin impl skeletons (throwing) + the orchestrator signatures; compile 0/0 under the relocated fence.
3. TDD: RED unit tests for every extracted mapper and the orchestrator flow (fake-driven, all branches), plus the pointed-integration test skeleton for the real non-interactive native ops.
4. IMPLEMENT: move the logic into the orchestrators, fill the thin native-ops impls, turn all RED green, and apply the four housekeeping fixes.
5. Measure per-OS coverage; report the real numbers.

## What the agent MAY do

- Rename the native-ops interfaces/impls from the design placeholders.
- Restructure the presence fakes and the shared `PresenceCheckPortSpec` as needed for the multi-method native-ops fakes, keeping them OS-agnostic and shared.

## What the agent MUST NOT do

- Add any `[ExcludeFromCodeCoverage]` attribute anywhere.
- Change `MacOsPresenceCheck`'s public contract or its existing fake tests' assertions (the port interface `EvaluateAsync` shape and the check-layer stay stable; the orchestration lives in the port implementer).
- Weaken, skip, or delete a test; suppress an analyzer beyond the five already granted; branch on OS outside the `CrossPlatform.*` libraries.
- Cache a native handle or any field on a native-ops implementer; mint a timeout inside any presence or native-ops class.
- Commit or push. Stop and report at any wall.

## Acceptance (each re-derivable by an adversary)

1. `dotnet build -c Release` = 0/0 under the fence (new + relocated analyzers live); `dotnet test -c Release` = 0 failed. Output pasted with exit codes.
2. `grep -rn "ExcludeFromCodeCoverage" src` returns nothing added by this phase (no coverage exemption).
3. RED-first proof: a recorded run showing the relocated AG0113/AG0101 rule flags the pre-refactor `LocalAuthentication`/`WindowsUserPresence` raw calls (the fence catches a real violation before the code moves).
4. Each native-ops implementer contains no control flow (try/catch aside), no async/`TaskCompletionSource`, and no field — proven by the three new analyzers going green only because the impls comply, with each rule's RED fixture.
5. The orchestrators' flow + every extracted mapper are unit-tested by a fake native-ops across all branches (probe-fail on each error code, probe-pass then interactive success/fail, cancellation; Hello 7-way + credential fallback).
6. The pointed-integration test drives the real non-interactive native ops and is green locally and on the macOS CI leg (no prompt shown).
7. `eng/coverage-gate.sh . AgentGuard.CrossPlatform.MacOS` measured and its real number pasted; same for `.Windows` and `.Linux` on their legs. Any shortfall reported to Tim, not waived.
8. The four housekeeping fixes are present: no "(ungated in this build)" in `SetupCommands.cs`, no "RED now" in the test tree, the polkit cancellation doc states start-only semantics, `install.sh` installs to `agentguard-presence.policy`.

## Rules (RULE-PHASE) — the seven from DESIGN

Relocate AG0113 + AG0101 presence exemption to the native-ops owner (decision-gated); AG0106 no-control-flow-in-native-ops; AG0115 no-async-orchestration-in-native-ops; no-state-in-native-ops (stateless, supersedes widening AG0112); extend AG0107 (no-timeout) to the native-ops owner; extend AG0114 (single-implementer) to the native-ops interfaces; extend AG0105 (callback-guard) to the Windows COM completed-handler. Each carries the failure mode it stops (see the DESIGN record).

## Tier

L1 — touches the analyzer fence, adds interfaces, and moves native code; SOLID and correctness both in play. Full chain: RULE-PHASE → ARCHITECTURE → TDD → IMPLEMENT → REFUTE → GATE → REPORT.

## Scope

Change scope only by Tim editing this file before the run starts.
