# Coverage sub-task (A): Windows + Linux presence pointed-integration tests over the 75% floor

## Decisions

- **subtask-a-two-tests** — Add two pointed-integration tests that drive the real non-interactive native ops (mirroring the shipped macOS `MacOsObjCRuntimeSpecTests`) so the per-OS Windows and Linux presence assemblies clear the hard 75% coverage floor with no coverage waiver. Tim: "Yes, let's take both OSes through the process." and (sequencing) "I like it let's do that. Determin in DESIGN if it will work, get a lock on the spec and then do Window and Linux togheter in IMPLEMNT."
- **linux-off-bus-seam** — In `TmdsPolkitAuthority`, extract the request-build → `DBusConnection.System` → serialize preamble that `CheckAuthorizationAsync` inlines into one private helper both call, and add ONE internal static off-bus entry point over it that accepts and returns no `Tmds.DBus.Protocol` type. (The produced `MessageBuffer` is not `IDisposable` in the pinned Tmds.DBus.Protocol 0.94.0 — verified by reflection against the package — so the entry point builds and discards it; there is nothing to dispose. The original wording said "disposes the produced `MessageBuffer` itself"; corrected here to the verified fact.) `CreateCheckAuthorizationMessage` stays private. No change to `DbusOnlyInPolkitAuthorityAnalyzer` (AG0110) and no test-assembly exemption. Approved as part of "Yes, let's take both OSes through the process."
- **windows-credential-roundtrip-seam** — In `CredentialPromptNativeOps` only, add one `CredPackAuthenticationBufferW` P/Invoke and one internal static `TryPackCredential(userName, password, out buffer, out size)`, allocating the output with `Marshal.AllocCoTaskMem` so cleanup reuses the existing `FreePromptBuffer`. `TryPackCredential` is NOT added to `ICredentialPromptNativeOps` and `FakeCredentialPromptNativeOps` is untouched, so the production orchestrator can never reach it. Approved as part of "Yes, let's take both OSes through the process."
- **guardrail-1-nativespec-convention-test** — Add a build-failing convention test that requires every per-OS native-ops owner to have its own `[Trait("Category","NativeSpec")]` spec test, so a native-ops class sitting at 0% can never again hide under an assembly total that still clears 75%. Owner set: `IObjCRuntime`, `ICredentialPromptNativeOps`, `IPolkitAuthority`; `IWindowsHelloNativeOps` is exempt (interactive-only); the flow ports `ILocalAuthentication` / `IWindowsUserPresence` are excluded (fake-tested, not spec-tested). Tim: "FOR rales #1–#3, I approve your pick."
- **guardrail-2-cut** — Do NOT widen `MacOsNativeBoolMustBeI1Analyzer` (AG0103) to the Windows assembly. Windows already marshals `bool` as the 4-byte `BOOL` by default and the new + existing Windows bool P/Invokes already carry `[MarshalAs(UnmanagedType.Bool)]`, so the rule would be RED against nothing. Tim: "FOR rales #1–#3, I approve your pick."
- **guardrail-3-cut** — Do NOT add an analyzer fencing `TryPackCredential` off from production. `LogonUser` validates against real OS accounts so a packed fake is rejected, and an orchestrator edit could bypass presence with a one-line `return Verified` regardless, so the rule guards no incremental path. Tim: "FOR rales #1–#3, I approve your pick."
- **guardrail-4-deferred-to-49** — The reflection-into-Tmds-internals ban (proposed AG0117) is deferred out of this sub-task and recorded on issue #49, where the read-path work makes the dodge reachable. Tim: "You can defer this only if you update #49 NOW to reflect this beign included on it." (Recorded on #49 before this contract was written.)
- **guardrail-1-owner-marker-option-a** — Guardrail 1 identifies its required owners via a production marker attribute (`RequiresNativeSpecTest`) on the owner interfaces that the convention test reflects for; the five presence analyzers keep reading their own `PresenceContracts` list for now. Making that marker the single source for the analyzers too is Option B, tracked on issue #50. Tim: "I can accept Option A if we add a clean up to come back and fix it and do Option B."
- **polkit-subject-fixture-dedup** — The representative unix-process subject (pid 12345, start-time 5,000,000,000, uid 1000) shared by `TmdsPolkitAuthorityTests`, `TmdsPolkitAuthoritySerializationSpecTests`, and `LinuxPresenceCheckTests` is owned once by a new `PolkitSubjectFixtures`; all three route to it, overriding this contract's earlier "leave `TmdsPolkitAuthorityTests.cs` untouched" instruction. Tim: "CLEAN IT ... I HATE DRY VIOLATIONS WITH A PASSION THAT BURNS STOP DOING IT AND STOP ASKING TO UNDO IT!"

## The standard

Non-negotiable: no `[ExcludeFromCodeCoverage]` anywhere in source, and no test weakened, skipped, or made OS-conditional to reach green. Both new spec tests drive the REAL native ops. The per-OS Windows and Linux presence assemblies each reach ≥75% line coverage on their own CI leg with no waiver, exactly as macOS already does at 88.37%.

## Success definition

All criteria below met AND no errors introduced in the system as a result of the change. Specific expected end state: the Windows and Linux per-OS presence assemblies clear the 75% floor with no coverage waiver; guardrail 1 is shipped and demonstrably fails when a required spec test is missing; guardrail 4 is recorded on #49; macOS coverage is unchanged; the whole solution builds 0/0 and every test passes on each leg.

## Surfaces (where the measured value lives)

- Per-OS coverage numbers: `eng/coverage-gate.sh . AgentGuard.CrossPlatform.Linux` (Linux leg, locally runnable) and `eng/coverage-gate.sh . AgentGuard.CrossPlatform.Windows` (Windows CI leg only). macOS `AgentGuard.CrossPlatform.MacOS` must stay ≥75% (currently 88.37%).
- Production seams: `src/AgentGuard.CrossPlatform.Linux/Presence/TmdsPolkitAuthority.cs`, `src/AgentGuard.CrossPlatform.Windows/Presence/CredentialPromptNativeOps.cs`.
- New test files: `tests/AgentGuard.CrossPlatform.Tests/Presence/Linux/TmdsPolkitAuthoritySerializationSpecTests.cs`, `tests/AgentGuard.CrossPlatform.Tests/Presence/Windows/WindowsCredentialPromptNativeOpsSpecTests.cs`, and the guardrail-1 convention test file in `tests/AgentGuard.CrossPlatform.Tests/`.

## Reuse ledger

- **Build a credential buffer without a dialog** — `new`: call the OS's own `CredPackAuthenticationBufferW` (credui.dll), placed behind the existing `CredentialPromptNativeOps` owner. Grep-confirmed the function appears nowhere in the repo today; it is the OS-level counterpart to the already-wrapped `CredUnPackAuthenticationBuffer`.
- **Off-bus D-Bus serialization** — `reuse`: the existing private `CreateCheckAuthorizationMessage` / `WriteSubject` / `WriteSubjectEntry` / `WriteDetails` in `TmdsPolkitAuthority`, reached through one new internal composition method; no new serialization logic.
- **Per-OS test gating** — `reuse`: the existing RID-gated `Choose`/`When` block in `AgentGuard.CrossPlatform.Tests.csproj` (compiles `Presence/Linux/**` and `Presence/Windows/**` only on their legs) and the existing `InternalsVisibleTo` grants from both per-OS libs to the test assembly. No csproj or visibility change.
- **Native-ops owner identification (guardrail 1)** — `reuse`: the owner set must come from a single shared source, not a fresh hand-typed list that can drift from the analyzers' own owner definition (`PresenceContracts`). If a runtime convention test cannot read the compile-time definition directly, the single source is a marker the analyzer and the test both read; RULE-PHASE resolves the mechanism and escalates if it forces a design choice.
- **Spec-test pattern** — `reuse`: `MacOsObjCRuntimeSpecTests` as the exact template (construct the concrete internal owner via `InternalsVisibleTo`, `[Trait("Category","NativeSpec")]`, un-gated, no native-smoke opt-in).

## What to do

1. **Linux seam + test.** Extract `TmdsPolkitAuthority`'s inline preamble into one private helper; add the internal static off-bus entry point (no Tmds type in/out; the `MessageBuffer` it produces is not `IDisposable` in Tmds 0.94.0, so it is built and discarded). Add `Presence/Linux/TmdsPolkitAuthoritySerializationSpecTests.cs`, `[Trait("Category","NativeSpec")]`, calling the entry point off-bus and asserting it does not throw. `TmdsPolkitAuthorityTests.cs`'s request-shape assertions stay as they are, but its representative-subject constants were routed to the shared `PolkitSubjectFixtures` owner per the `polkit-subject-fixture-dedup` decision (overriding the earlier "leave untouched").
2. **Windows seam + test.** Add the `CredPackAuthenticationBufferW` P/Invoke (reuse `CredUiLibrary`, `[return: MarshalAs(UnmanagedType.Bool)]`, `StringMarshalling.Utf16`) and the internal static `TryPackCredential` (straight-line, `Marshal.AllocCoTaskMem` sized on the existing `CredentialFieldChars`) — both inside `CredentialPromptNativeOps`, not on the interface. Add `Presence/Windows/WindowsCredentialPromptNativeOpsSpecTests.cs`, `[Trait("Category","NativeSpec")]`, constructing the real `CredentialPromptNativeOps` and driving `TryPackCredential` → `Unpack` (assert `Success`) → `Logon` with a made-up account name (assert `false`) → `CloseToken` → `FreeFields` → `FreePromptBuffer`.
3. **Guardrail 1 (RULE-PHASE).** Add the OS-agnostic reflection-based convention test that, for the per-OS assembly on the current leg, fails when a required native-ops owner lacks its `NativeSpec` spec test — owner set and exemption per `guardrail-1-nativespec-convention-test`, owner identification per the reuse ledger. It goes RED on the Linux and Windows legs until step 1 and 2 add their spec tests (green on macOS, whose spec test already exists).

## What the agent MAY do

- Add the one internal Linux entry point and its private preamble helper; add the one Windows P/Invoke and `TryPackCredential`.
- Construct the concrete internal owner types directly in the new tests via the existing `InternalsVisibleTo`.

## What the agent MUST NOT do

- Add `TryPackCredential` (or any test-only member) to `ICredentialPromptNativeOps`, or touch `FakeCredentialPromptNativeOps`.
- Add a test-assembly exemption to `DbusOnlyInPolkitAuthorityAnalyzer` (AG0110), or widen `CreateCheckAuthorizationMessage`'s accessibility.
- Add any `[ExcludeFromCodeCoverage]`, or weaken / skip / OS-condition any test.
- Build guardrails 2, 3, or 4 (cut / cut / deferred to #49).
- Change any analyzer other than what guardrail 1 requires; commit or push.

## Acceptance

1. `dotnet build -c Release` is 0 warnings / 0 errors on the whole solution with the two seams, the two spec tests, and the guardrail-1 convention test present. Paste output with exit code.
2. Linux: `eng/coverage-gate.sh . AgentGuard.CrossPlatform.Linux` prints `PASS ... >= 75%` on a clean linux-x64 test run (locally runnable — the serialization is off-bus). Paste the number with exit code.
3. Windows: the win-x64 leg builds 0/0 and the new spec test compiles; the ≥75% number is proven on the `windows-latest` CI leg (`eng/coverage-gate.sh . AgentGuard.CrossPlatform.Windows`), stated explicitly as CI-proven since it cannot run on this host.
4. macOS: `eng/coverage-gate.sh . AgentGuard.CrossPlatform.MacOS` still ≥75% (no regression from 88.37%). Paste with exit code.
5. Guardrail 1 actually guards: with a required spec test temporarily removed, the convention test FAILS RED; restored, it passes. Paste both.
6. `grep -rn --include='*.cs' ExcludeFromCodeCoverage src` returns nothing added by this change; no test carries `Skip`, `[Trait]`-based OS skip, or `Assert.True(true)`.
7. Guardrail 4 is recorded on issue #49 (done before this contract).
8. Left-uncovered is documented, not silently dropped: Linux `ReadAuthorizationResult` and the live-bus call (→ #49); Windows `Prompt` and all of `WindowsHelloNativeOps` (CI smoke). State each with its owner.

## Left-uncovered accounting (acceptance #8)

- **Linux `TmdsPolkitAuthority.ReadAuthorizationResult` (reply parsing) and the live-bus call inside `CheckAuthorizationAsync`** — need a real system bus, out of scope here, owned by issue #49 (the real-D-Bus stub CI test).
- **Windows `CredentialPromptNativeOps.Prompt` (the secure-desktop dialog) and all of `WindowsHelloNativeOps`** — interactive, owned by the CI native smoke `tests/AgentGuard.CrossPlatform.Tests/Presence/PresenceNativeSmokeTests.cs` on the Windows CI leg.

## Tier

L1 — new production seams plus a new guardrail, cross-OS blast radius; the full pipeline and the five-adversary REFUTE run.

## Scope

Change scope only by editing this file before the run starts.
