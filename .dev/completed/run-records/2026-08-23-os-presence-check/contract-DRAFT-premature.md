# OS presence check — gate install/init/remove on a physically-present human

## Decisions

Each carries Tim's verbatim approving words. An item without them is not a decision.

### `binding-flat-net10-pinvoke`
The presence check binds each OS's native API by P/Invoke inside the per-OS `AgentGuard.CrossPlatform.*` libraries, on flat `net10.0` (no `net10.0-macos`, no workload, no Xcode): macOS `objc_msgSend` → LocalAuthentication, Windows Hello via Win32, Linux `libpam`. One common interface, three per-OS implementations. Option B (`net10.0-macos` + first-party binding) is a proven fallback, not used. Both spike-proven on Intel Tahoe (`spike-objc-msgsend.md`, `spike-net10macos-b.md`).
Tim: *"we can define a common interface for them all to live behind. It doesn't matter to me how each one performs that task as long as it does."* and *"Okay we can continue with A and know that B is at least an option we can fall back to."*

### `presence-is-an-owned-service`
Presence is a first-class owned service interface — `IPresenceCheck` — exposed per-OS as a new property `IPlatformServices.Presence` (sibling to `FileSystem`), reached as `services.Platform.Presence` and threaded into `SetupContext` the same way `FileSystem` is. It is governed by the existing one-owner / container / builder analyzers with no rule edits. It has two operations: `Check` (evaluate presence — the approval gate consumes it) and a per-OS `Setup` (configure). The approval gate stops being a bare always-approve static and consumes the injected `IPresenceCheck`; `RequireApproval` gains the context/service, the three call sites already hold `context`, and the "drops in with no change to the callers" comment is removed as placeholder aspiration.
Tim: *"WHy doesn't Presense becomse a Service interface and have an owner class (IPresence or something)? We treat it like all our other abstractiosn?"* and *"It would be SystemServices.Platform.Presense right"*.

### `platform-contributed-cli-commands`
The CLI does not branch on OS. `IPlatformServices` (per-OS) contributes its CLI commands generically — each a `(name, description, handler)`; the CLI is a generic host that appends `services.Platform`'s contributions to its command set at startup (open/closed, AG0009-clean, no phantom no-op commands). Linux contributes `presence enable-fingerprint` and a matching disable; macOS and Windows contribute none.
Tim: *"A Platform service that 'specifies additional switches' via a config we setup. It's given the swtich and a method to call. The CLI calls this and if it returns new methods/switches it configures them. Only Linux would return it."*

### `linux-password-baseline-fingerprint-optin`
Linux presence works out of the box with no root: authenticate the current user against an existing system PAM service (password, plus opportunistic fingerprint where the distro already wired `fprintd`), best-effort → password → deny. Guaranteed fingerprint is an opt-in privileged step: `sudo guard presence enable-fingerprint` (final command name per `platform-contributed-cli-commands`) writes a root-owned `/etc/pam.d/agent-guard` stack (`auth sufficient pam_fprintd.so` → `auth required pam_unix.so` → deny); its existence is the switch, the guard owns/writes/removes it, and the main `guard install` stays user-space.
Tim: *"It kind of has to work out of the box on linux... We want it to give best effort and fall back to password validation."* and *"If we support password and an extra step to provide fingerprint, then that would be okay."*

### `macos-biometric-or-password`
macOS uses `LAPolicy.DeviceOwnerAuthentication` (=2): Touch ID OR the account password satisfies presence — works on Touch-ID-less Intel Macs and is symmetric with the Linux ladder. Accepted tradeoff: a known account password passes the gate.
Tim: *"3) biometric-or-password"*.

### `windows-win32-validated-in-ci`
Windows binds the **Win32** Hello API (a single-file CLI must not depend on package identity). It is built for real in IMPLEMENT and validated by the Windows CI leg — the runner builds it (surfacing any package-identity problem), a smoke test calls the availability query (returns unavailable on the Hello-less runner, auto-proving the unavailable→deny path), everything above the interface runs against the fake, and the interactive success path is a manual pre-release checklist line. WinRT `UserConsentVerifier` is used only if an early CI smoke-test first proves it loads from a single-file exe.
Tim: *"Is there a way we couold validate this as part of our normal development and run it through the CI?"* and *"Both good, write the contract"*.

### `prompt-timeout-60s-fail-closed`
The interactive prompt is bounded by one shared ~60-second wait across all three OS that denies on expiry (fail-closed), so a stalled/unanswerable prompt in a headless or detached session denies rather than hangs forever.
Tim: *"Prompt timeout: sure 60s sounds right."*

### `approval-decision-reason-code`
`ApprovalDecision` gains a reason enum — `Approved / Denied / Unavailable / Cancelled / TimedOut / Error` — and each reason surfaces as its own CLI line instead of one generic "X was not approved".
Tim: *"Okay yes, and those are good options to start, let's go with them."*

### `os-dialog-text-owned-resource`
The string shown in the OS's own presence dialog (macOS `evaluatePolicy` `localizedReason`, Windows Hello message) is one reviewed, action-specific English string per command (install/init/remove), held as a single owned resource, never an inline literal. Localization is deferred but the seam is kept.
Tim: *"OS dialog text: ... Okay"*.

### `per-os-presence-impls-not-shared`
The three presence implementations are authored separately (macOS `objc_msgSend`, Linux `libpam`, Windows Hello) — no macOS→Linux link-share like the filesystem, because the native APIs share nothing.
Tim: *"Agreed"* (to "three separately authored presence impls ... no macOS→Linux link-share").

### `entitlements-verify-amend-if-needed`
At IMPLEMENT, call `evaluatePolicy` on the REAL hardened-runtime CI-signed macOS binary (the spike only proved the ad-hoc-signed apphost). If it needs an entitlement, add it to `eng/signing/agentguard.entitlements` only via a decision-14 amendment (that file's no-silent-change rule), never silently. Most likely no new key is needed.
Tim: *"Both good, write the contract"* (confirming the verify-and-amend-only-if-needed plan explained to him).

### `coverage-exclusion-deferred`
Keep the uncoverable surface to the one `evaluatePolicy`-success line as a thin per-OS member (the AG0101 interim-waiver precedent). Measure the per-OS coverage once the code exists and decide the `[ExcludeFromCodeCoverage]` + AG0032 waiver (issue #37) THEN, against the real numbers — not now.
Tim: *"We'll make the final decision when we get to the coverage gate and see the numbers."*

## What we're building

The three mutating setup commands — `install`, `init`, `remove` — become gated on a physically-present human, per OS, replacing today's always-approve placeholder. A new owned service `IPresenceCheck` on `IPlatformServices.Presence` exposes `Check` (evaluate presence) and a per-OS `Setup`. Three per-OS implementations bind the native APIs by P/Invoke inside `AgentGuard.CrossPlatform.MacOS/.Linux/.Windows`: macOS LocalAuthentication (`objc_msgSend`, biometric-or-password), Windows Hello (Win32), Linux PAM (password baseline out of the box, fingerprint via an opt-in root step that writes `/etc/pam.d/agent-guard`). The approval gate consumes the injected service; every OS path is bounded by a 60s fail-closed timeout and returns a typed reason. The CLI gains a generic platform-contributed-command host so the Linux `enable-fingerprint`/`disable-fingerprint` commands appear only on Linux with no OS branch in the CLI. `doctor` stays ungated (self-heal). Everything above the native call is tested against a fake presence; the native calls are validated on their own OS CI leg (build + the unavailable→deny path); the interactive success path is a manual pre-release checklist item on macOS and Windows.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change.

End state:
- `install`, `init`, `remove` deny and return early (with a typed reason and its per-reason CLI line) when presence is not confirmed; `doctor` remains ungated.
- `IPresenceCheck` exists as an owned service on `IPlatformServices.Presence`, with a real per-OS implementation in each of the three `CrossPlatform.*` libraries and a fake in `SystemServicesBuilder`; the existing one-owner/container/builder analyzers pass with no analyzer edits beyond the RULE-PHASE rules below.
- macOS uses `LAPolicy.DeviceOwnerAuthentication`; Windows binds Win32 Hello; Linux authenticates via the reused system PAM service by default and via `/etc/pam.d/agent-guard` after the opt-in step.
- The interactive prompt is bounded by the shared 60s fail-closed timeout on all three OS.
- The Linux `enable-fingerprint`/`disable-fingerprint` commands appear on Linux only, contributed through `IPlatformServices` with no `OperatingSystem.Is*` / `#if` / `RuntimeInformation.IsOSPlatform` anywhere in the CLI or engine.
- `dotnet build -c Release` = 0 warnings / 0 errors and `dotnet test -c Release` = 0 failed, locally and on all three CI OS legs; each per-OS `CrossPlatform.<OS>` assembly and the aggregate meet the hard 75% coverage gate (the one `evaluatePolicy`-success line handled per `coverage-exclusion-deferred`).
- The Windows CI leg builds the Win32 Hello impl and its smoke test proves the availability query loads and the unavailable→deny path holds; the macOS leg proves the same for LocalAuthentication.
- Nothing under `Abstractions/**` is added or changed except the new `IPresenceCheck` and the `Presence` property on `IPlatformServices` (both interface additions, signed off here). No silent change to `eng/signing/agentguard.entitlements`.

Any restatement or weakening of this to fit the result is a top-line Lie-catcher finding.

## Surfaces

- `src/AgentGuard.Abstractions/Contracts/IPlatformServices.cs` — add the `Presence` property (+ the CLI-contribution surface).
- `src/AgentGuard.Abstractions/Contracts/IPresenceCheck.cs` — NEW interface (`Check`, `Setup`).
- `src/AgentGuard.Abstractions/Contracts/ISystemServices.cs` — unchanged (reaches presence via `Platform`).
- `src/AgentGuard.Engine/Setup/ApprovalGate.cs` — the gate consumes `IPresenceCheck`, applies the 60s timeout, returns a typed reason; no longer a bare always-approve static.
- `src/AgentGuard.Engine/Setup/ApprovalDecision.cs` — add the reason enum (`approval-decision-reason-code`).
- `src/AgentGuard.Engine/Setup/SetupContext.cs` — add the `Presence` property, sourced from `services.Platform.Presence` in `ForCurrentProcess`.
- `src/AgentGuard.Engine/Setup/SetupCommands.cs` — the three call sites (Install/Init/Remove) pass `context`; per-reason CLI messages; Doctor unchanged.
- `src/AgentGuard.CrossPlatform.MacOS/` — the macOS presence impl (`objc_msgSend` → LocalAuthentication) + its native-methods file; `PlatformServices.Create()` builds `Presence`.
- `src/AgentGuard.CrossPlatform.Windows/` — the Windows Hello impl (Win32) + native-methods; the Windows CLI contribution is empty.
- `src/AgentGuard.CrossPlatform.Linux/` — the Linux PAM impl (`libpam`), its `PosixFileSystem.Linux.cs`-style fragment authored separately (not link-shared from macOS), the `/etc/pam.d/agent-guard` template, and the Linux CLI contributions (enable/disable-fingerprint).
- `src/AgentGuard.CrossPlatform/PlatformImplementation.targets`, the per-OS `.csproj`s — unchanged selection mechanism; `AllowUnsafeBlocks` already set.
- `src/AgentGuard.Cli/Program.cs` — the generic platform-command host that appends `services.Platform`'s contributions.
- `tests/AgentGuard.TestHelpers/SystemServicesBuilder.cs` — `FakePlatformServices` grows `Presence` (a fake returning approved/denied on command) + the `With`/`Wrap` the builder-completeness analyzer requires.
- `tests/AgentGuard.Tests/` and `tests/AgentGuard.Cli.Tests/` — above-the-seam gating tests (install/init/remove approved vs each denial reason) against the fake.
- `tests/AgentGuard.CrossPlatform.Tests/` — the OS-agnostic presence spec (availability query, unavailable→deny) run per-OS on its own leg.
- `.github/workflows/ci.yml` — the per-OS smoke steps (macOS + Windows availability query / unavailable→deny).
- `eng/signing/agentguard.entitlements` — touched ONLY if `entitlements-verify-amend-if-needed` proves a key is needed, and then only via a decision-14 amendment.
- Frozen: `analyzers/**` (except the RULE-PHASE rules below, signed off), no other `Abstractions/**` change.

## Reuse ledger (from GROUND, `wf_95dc65f9-453`)

| Capability | Ruling | Owner / note |
|---|---|---|
| presence-interface | NEW | No presence/biometric abstraction exists; `IPlatformServices` doc earmarks a biometric surface. Add `IPresenceCheck` on `IPlatformServices.Presence`. |
| per-os-native-presence | NEW | No native presence interop exists. Mirror the existing P/Invoke pattern (`PosixNativeMethods`/`WindowsNativeMethods`): `internal static partial class <Os>NativeMethods`, `[LibraryImport]`, `SetLastError`, `Marshal.GetLastPInvokeError`. |
| presence-test-fake | REUSE | `tests/AgentGuard.TestHelpers/SystemServicesBuilder.cs` — extend the `FakeSystemServices`/`FakePlatformServices` pattern (private ctor + static `Create`, builder-composable). Do NOT invent a new fake shape. |
| coverage-exclusion-waiver | NEW (precedented) | Follow the AG0101 interim-waiver shape at `tests/AgentGuard.CrossPlatform.Tests/PlatformFileSystemSpecTests.cs:248-252`. Deferred to the gate (`coverage-exclusion-deferred`). |
| cli-command-host | NEW | The generic platform-contributed-command mechanism; no prior art. |

## What to do

1. Add `IPresenceCheck` (Abstractions) with `Check` (evaluate presence → a typed result feeding `ApprovalDecision`) and a per-OS `Setup`. Add `Presence` to `IPlatformServices` and the CLI-contribution surface. Add the reason enum to `ApprovalDecision`.
2. Rewire `ApprovalGate` to consume the injected `IPresenceCheck` off `SetupContext` (add `Presence` to `SetupContext.ForCurrentProcess` from `services.Platform.Presence`), apply the 60s fail-closed timeout, and return the typed reason. Change the three call sites to pass `context`; give each denial reason its own CLI line. Remove the placeholder always-approve body and comment. Doctor stays ungated.
3. macOS impl (`CrossPlatform.MacOS`): the `objc_msgSend` LocalAuthentication surface from `spike-objc-msgsend.md` (four typed P/Invoke declarations, `I1` BOOL return, framework `NativeLibrary.Load` before class lookup), `LAPolicy.DeviceOwnerAuthentication`, `localizedReason` from the owned resource. `Setup` is a no-op that reports "used automatically". `PlatformServices.Create()` builds `Presence`.
4. Windows impl (`CrossPlatform.Windows`): Win32 Hello, availability query + interactive verify, `Setup` no-op. Wire the Windows CI smoke step (availability query returns unavailable on the runner → deny).
5. Linux impl (`CrossPlatform.Linux`, authored separately per `per-os-presence-impls-not-shared`): `libpam` P/Invoke (`pam_start`/`pam_authenticate`/`pam_end`, a conversation callback), default against the reused system service; the `Setup`/`enable-fingerprint` handler writes the root-owned `/etc/pam.d/agent-guard` template; `disable-fingerprint` removes it; the presence check uses `agent-guard` when the file exists, else the reused service. Contribute the two Linux CLI commands.
6. CLI (`Program.cs`): the generic host that appends `services.Platform`'s contributed commands to the command set — no OS branch.
7. Tests: extend `SystemServicesBuilder` with a fake `Presence`; above-the-seam gating tests (each command approved vs each denial reason); the OS-agnostic presence spec (availability + unavailable→deny) run per-OS.
8. Entitlements: perform the `entitlements-verify-amend-if-needed` check on the CI-signed binary; amend only via a decision-14 amendment if required.
9. Prove: `dotnet build`/`test` green locally and on all three CI legs; the coverage gate green (handling the one uncoverable line per `coverage-exclusion-deferred`); the per-OS CI smoke steps pass.

## What the agent MAY do
- Add the two interface additions named above (`IPresenceCheck`, `IPlatformServices.Presence`) — signed off in the Decisions.
- Add per-OS P/Invoke inside the `CrossPlatform.*` libraries only.
- Extend `SystemServicesBuilder` with the presence fake and the required `With`/`Wrap`.

## What the agent MUST NOT do
- Branch on OS (`OperatingSystem.Is*`, `RuntimeInformation.IsOSPlatform`, platform `#if`) anywhere outside the `CrossPlatform.*` libraries (AG0008/AG0009).
- Add P/Invoke outside the four `CrossPlatform.*` assemblies.
- Change `eng/signing/agentguard.entitlements` silently (only via a decision-14 amendment).
- Weaken, skip, or delete a test; suppress an analyzer; or apply `[ExcludeFromCodeCoverage]` to reach 75% (the deferred coverage decision is made against real numbers with Tim, per `coverage-exclusion-deferred`).
- Change any decision above without Tim's words; make the main `guard install` require root; call `evaluatePolicy`/the interactive verify in an automated test.
- Touch `Abstractions/**` beyond the two signed-off additions, or `analyzers/**` beyond the signed-off RULE-PHASE rules.
- Commit or push. Stop and report at any wall.

## Acceptance

Each check re-derivable by someone other than the worker.

1. Local: `dotnet build -c Release` = 0/0; `dotnet test -c Release` = 0 failed. (Output pasted.)
2. Gating: an above-the-seam test proves each of install/init/remove denies-and-returns-early when the fake presence denies, and proceeds when it approves; doctor runs without a presence call. Each denial reason (`Denied/Unavailable/Cancelled/TimedOut/Error`) yields its own CLI line.
3. `grep -rn "OperatingSystem\.Is\|RuntimeInformation.IsOSPlatform\|#if " src/AgentGuard.Cli src/AgentGuard.Engine` finds no OS branch; the Linux `enable-fingerprint` command is present on the Linux build and absent on the macOS/Windows builds.
4. `grep -rn "LibraryImport\|DllImport"` finds the presence P/Invoke ONLY under `src/AgentGuard.CrossPlatform.*`.
5. CI: all three OS legs green; the macOS smoke step shows the LocalAuthentication availability query loads and the unavailable→deny path holds; the Windows smoke step shows the Win32 Hello availability query loads and the unavailable→deny path holds.
6. Coverage: each `CrossPlatform.<OS>` assembly and the aggregate meet the hard 75% gate; any `[ExcludeFromCodeCoverage]`+AG0032 waiver used is the one Tim signed off at the gate (`coverage-exclusion-deferred`), nothing more.
7. `git diff` shows `Abstractions/**` changed only by the two signed-off additions, `analyzers/**` only by the signed-off RULE-PHASE rules, and `eng/signing/agentguard.entitlements` unchanged unless a decision-14 amendment records a needed key.

## Rules to add (RULE-PHASE — determined here, each needs Tim's sign-off before it is written)

Proposed for the macOS `objc_msgSend` surface (the spike flagged both as cheap to encode); NOT yet signed off:
- **objc_msgSend one-declaration-per-signature**: reject reusing a single `objc_msgSend` P/Invoke declaration across differing argument/return types.
- **objc_msgSend BOOL returns as I1**: an `objc_msgSend` declaration returning a bool must marshal it as `UnmanagedType.I1`.
These are candidates; Tim signs off (or drops) each before RULE-PHASE writes it. The existing analyzers (AG0008/AG0009/AG0010/AG0019/AG0022/AG0029/AG0032) already govern the rest with no edits.

## Tier

L1 — a new cross-OS capability with native interop on all three platforms, a trust boundary (what counts as "present"), a new privileged Linux install path, and a change to the setup-command gate; all roles run. Tim set the level.

## Scope

Change scope only by editing this file before the run starts.
