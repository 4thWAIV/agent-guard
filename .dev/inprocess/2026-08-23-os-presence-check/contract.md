# OS presence check — gate install/init/remove on a physically-present human

## Decisions

Each decision carries the human's verbatim approving words. An item without them is not a decision. Decisions settled before the two DESIGN runs are grouped first; the polkit rework (which replaced the original Linux design) and the sign-off on it follow. Full record: `decisions-settled.md` in this folder.

### Cross-platform shape (settled)

#### `binding-flat-net10-pinvoke`
macOS binds LocalAuthentication by `objc_msgSend` P/Invoke inside the per-OS `AgentGuard.CrossPlatform.*` libraries, on flat `net10.0`. Windows Hello has NO flat-Win32 entry point — it binds via the WinRT `UserConsentVerifier` interop (WinRT activation, needs an HWND; the interop method's documented minimum client is Windows 11/22000 even though Hello and the WinRT class are Windows 10+ — a Win10-vs-11 floor to verify at IMPLEMENT); the credential-prompt fallback (`CredUIPromptForWindowsCredentials`) and `LogonUser` are flat P/Invoke. Linux uses no P/Invoke (see the polkit decisions). Spike-proven for macOS (`spike-objc-msgsend.md`); the WinRT-from-single-file question is a Windows IMPLEMENT verification.
Tim: *"we can define a common interface for them all to live behind. It doesn't matter to me how each one performs that task as long as it does."*

#### `presence-is-an-owned-service`
Presence is a first-class owned service interface — `IPresenceCheck` — exposed per-OS as `IPlatformServices.Presence` (sibling to `FileSystem`), reached at the gate as `services.Platform.Presence` (Tim's words). It has one operation, `Check` (evaluate presence); there is no `Setup` on the interface. It is governed by the existing one-owner / container / builder analyzers with no rule edits. It is NOT stored as a separate `SetupContext` property — the reviews found that redundant with the root services the gate already holds.
Tim: *"WHy doesn't Presense becomse a Service interface and have an owner class?"* and *"It would be SystemServices.Platform.Presense right"*.

#### `presence-interface-check-only`
`IPresenceCheck` carries only `Check`. Fingerprint / setup is not an interface operation. This removes any `Setup`, `PresenceSetupRequest`, `PresenceSetupResult`, and `PresenceSetupAction`.
Tim: *"Yes drop it we moved it to the comand handler many turns back."* and *"Agreed we don't need the setup command now."*

#### `presence-async-gate-ripple`
The approval gate becomes asynchronous. `ApprovalGate.RequireApprovalAsync(ISystemServices services, string action)` returns `Task<ApprovalDecision>`; `SetupCommands.Install`/`Init`/`Remove` become `async Task<CommandOutcome>` and receive the root `ISystemServices`; `Program.cs` switches those three to the async command path the `hook` command already uses, with `RunCommandAsync` **replacing** the now-callerless `RunCommand`. `doctor` stays ungated.
Provenance: the mechanical consequence of the approved async presence check and the 60-second timeout (global rule: approve at the decision level, then execute).

#### `polkit-wire-test-seam` (option 2 of the TDD escalation)
The `AllowUserInteraction` flag and the 64-bit start-time wire encoding of the polkit `CheckAuthorization` call are not observable through `IPolkitAuthority` (the flag is not a parameter) and cannot be inspected without a live bus, so acceptance #4's wire clause was untestable — the TDD test author escalated rather than fake it. Resolution: `TmdsPolkitAuthority` gains a pure internal `BuildCheckAuthorizationRequest(PolkitSubject subject, string actionId, string message)` returning the NEW inspectable `PolkitCheckAuthorizationRequest` record (`SubjectKind`, the reused `PolkitSubject`, a per-key `SubjectVariantTypes` D-Bus wire-type map, `ActionId`, `Details`, `Flags`, `CancellationId`), so TDD asserts `Flags == 1` (AllowUserInteraction) and `SubjectVariantTypes["start-time"] == "t"` (64-bit) without opening a bus. `IPolkitAuthority` is unchanged; the record is BCL-only so the test never references `Tmds.DBus.Protocol`; both bodies throw (IMPLEMENT wires them). Recorded in Surfaces and the Reuse ledger (NEW). Rejected: extending `IPolkitAuthority` with a flag argument (pollutes the port, still misses the wire encoding); deferring the clause to IMPLEMENT (loses the test-first / separation-of-powers guarantee).
Tim: *"Okay do option 2."* and *"Okay record the ledger row"*.

#### `macos-biometric-or-password`
macOS uses `LAPolicy.DeviceOwnerAuthentication` (=2): Touch ID or the account password satisfies presence, via the OS's own out-of-band dialog. Accepted tradeoff: a known account password passes the gate.
Tim: *"3) biometric-or-password"*.

#### `windows-hello-or-password`
Windows presence is Hello-or-password, both out-of-band, mirroring macOS. The `IWindowsUserPresence` port tries Windows Hello via the WinRT `UserConsentVerifier` interop when a method is enrolled and it can bind; else it pops the **secure-desktop** credential prompt (`CredUIPromptForWindowsCredentials` with `CREDUIWIN_SECURE_PROMPT`, scoped to the current user) and validates what the user types against the current account with `LogonUser` (`LOGON32_LOGON_NETWORK`, domain `.`). The secure desktop is the same isolated surface as UAC/logon, so a user-mode caller can neither read nor inject the prompt. A truly passwordless account (Hello-only, some Entra-ID) has nothing to type → `Unavailable` (deny) — the one residual gap. Validated by the Windows CI leg (availability/unavailable→deny); the interactive success path is a manual pre-release checklist line. Rejected alternative: UAC elevation proves *admin* consent, not current-user presence.
Documented, not yet executed (verify at IMPLEMENT): that `CREDUIWIN_SECURE_PROMPT` engages for a non-elevated caller on a clean standard-user box; the `EnableSecureCredentialPrompting` policy (which can require Ctrl+Alt+Del to reach the dimmed prompt); that the WinRT `UserConsentVerifier` interop loads and activates from a single-file self-contained exe (the recorded Win32-vs-WinRT hedge); and the real Windows-version floor for the Hello interop (Microsoft's page lists Windows 11/22000, but Hello and the WinRT class are Windows 10+ — confirm whether it binds on Windows 10). Where Hello can't bind (version, no HWND, or single-file activation fails), the Hello leg is skipped and the credential prompt is the only path — so on those hosts a Hello-only/passwordless account denies.
Bound (applies to Hello too): at same-user / standard integrity the AI caller and the CLI share a trust level, so these prompts protect the input channel (the caller can't answer), not the CLI's own decision logic from same-user memory tampering — a separate problem (integrity levels / a broker), not solved by any prompt.
Tim: *"Add the secure-desktop credential fallback. Windows then mirrors macOS exactly ... Great, I approve of that design."* and *"YES"*.

#### `prompt-timeout-60s-fail-closed`
The interactive prompt is bounded by one shared 60-second wait across all three OS that denies on expiry (fail-closed).
Tim: *"Prompt timeout: sure 60s sounds right."*

#### `approval-decision-reason-code`
`ApprovalDecision` moves to `(string Action, ApprovalReason Reason, string Detail)` with `IsApproved` derived from the reason. `ApprovalReason` is one shared Abstractions enum — `Approved / Denied / Unavailable / Cancelled / TimedOut / Error` — surfaced as its own CLI line via the gate-populated `Detail` (one owner, no per-reason `switch` in the three commands). The boundary never emits `TimedOut` (gate-synthesized).
Tim: *"Okay yes, and those are good options to start, let's go with them."*

#### `os-dialog-text-owned-resource`
The prompt string is one reviewed, action-specific English string per command (install / init / remove), held as a single owned resource keyed by `SetupVerb`, never an inline literal. `SetupVerb` (constants `Install`/`Init`/`Remove`, mirroring `HookCommand.Verb`) is the single owner of the three verb strings, referenced from `Program.cs`'s registrations, the gate call sites, and the `SetupCommands` messages.
Tim: *"OS dialog text: ... Okay"*.

#### `per-os-presence-impls-not-shared`
The per-OS presence implementations are authored separately (macOS `objc_msgSend`, Windows Hello, Linux polkit); no link-share of the implementation body.
Tim: *"Agreed"*.

#### `per-os-native-behind-a-port`
Each per-OS presence implementation reaches its native library only through an internal port, one implementer each: `ILocalAuthentication` (macOS `objc_msgSend`/LocalAuthentication), `IWindowsUserPresence` (Windows Hello or the secure-desktop credential prompt), `IPolkitAuthority` (Linux Tmds D-Bus). The `IPresenceCheck` implementation calls the port, gets a plain native-result record, and maps it to `ApprovalReason` via a pure function — no implementation touches a native library directly. The native-call ownership (a family-scoped scheme — a narrow AG0101 edit plus the paired AG0113 rule; see `presence-native-owner-rule`) and the single-implementer guard follow each port. Uniform across the three OS.
Tim: *"ILocalAuthentication, IWindowsUserPresence => YES and we should encourage more of this pattern of consistency. I WILL ALMOST NEVER reject an extra abstraction, it's the designs that cut that are the hard ones."*

#### `entitlements-verify-amend-if-needed`
At IMPLEMENT, call `evaluatePolicy` on the real hardened-runtime CI-signed macOS binary; if it needs an entitlement, add it to `eng/signing/agentguard.entitlements` via a decision-14 amendment, never silently.
Tim: *"Both good, write the contract"*.

#### `coverage-exclusion-deferred`
Keep the uncoverable surface to the one interactive-success line per OS; decide the `[ExcludeFromCodeCoverage]` + AG0032 waiver (issue #37) at the coverage gate, against real numbers.
Tim: *"We'll make the final decision when we get to the coverage gate and see the numbers."*

### Linux via polkit (the rework)

The original Linux design read the password from the CLI's own stdin, which is in-band — the caller (possibly the AI) controls stdin — so it was not a presence proof. Linux presence now goes through polkit's out-of-band agent, matching the macOS/Windows OS dialogs. Full grounding: `research-polkit.md`.

#### `linux-polkit-out-of-band`
Linux presence is a single polkit `CheckAuthorization` call. polkit's session authentication agent shows the dialog and runs PAM in its own process; where the distro wired fingerprint into `/etc/pam.d/polkit-1`, that dialog already offers biometric-or-password. polkit's desktop agent is a vetted attention front-end (not affected by CVE-2024-37408, unlike a hand-rolled `pam_fprintd` front-end), so polkit is primary and there is NO separate hand-rolled fingerprint path. This drops the entire direct-libpam design (PAM conversation callback, `NativeMemory`, `/etc/pam.d/agent-guard`, the fingerprint commands).
Tim: *"Yes, polkit so we have a similar structure to the other OSs and we make better use of trusted system services."*

#### `linux-policy-root-install-fail-closed`
A custom `auth_self` polkit action is installed as a root-owned `.policy` file under `/usr/share/polkit-1/actions/`, one-time. Interim install path: an `install.sh` shipped with the release `sudo install`s the `.policy` (and the binary); the guard binary never writes it. A real distribution system (npm, a Claude plugin, or an OS package) that installs the policy is deferred to **issue #44**. Linux presence is **fail-closed until a user runs the installer** — with no policy installed, the (undeclared) action faults → deny, so there is no separate installed-detection step. Accepted: Linux ships fail-closed-until-installed.
Tim: *"Yes, for linux due to technical requirements, this is okay."* and *"Yes, we can drop an install.sh file with the package until we can get this into a distribution system which would probably be npm or a claude plugin or other distribution method."*

#### `tmds-dbus-protocol-pinned`
The polkit call is D-Bus, made via the pinned managed NuGet `Tmds.DBus.Protocol` **version `0.94.0`** (low-level, NOT the reflection-based high-level `Tmds.DBus`). Pure-managed, single-file/AOT clean. It is a `0.x`/pre-1.0 line, accepted on the record because no 1.0 exists; the risk is bounded — we bundle a pinned version behind the single `IPolkitAuthority` owner and bump only deliberately (it is not a system dependency, so the user's machine never sees a version conflict). Recorded as a reuse-a-primitive-behind-an-owner decision.
Tim: *"Yes, a trusted NuGet package is prefered here. We'll just pin it."* and, on pinning `0.94.0` and accepting the pre-1.0 line, *"YES"*.

#### `presence-subject-triple`
The calling process is named to polkit with the classic unix-process triple — process id, start time, user id — all read from `/proc/self/stat` and `/proc/self/status` through the owned `IFileReader`, no native call on Linux. Required for the polkit on RHEL 8 / older Ubuntu LTS, which reject a pidfd subject. Preferring pidfd where supported is deferred to issue #40.
Tim: *"Triple for now but file an issue to improve this to support both and via a process to provide the modern solution where it is supported."*

#### `polkit-action-id`
The action id is `com.4thwaiv.agentguard.presence`, one action for all three verbs (differentiated by the per-call `polkit.message`), a C# const referenced by both the D-Bus adapter and the policy-integrity test. Confirm at IMPLEMENT that polkit accepts the digit-leading `4thwaiv` segment; if not, spell it `com.fourthwaiv.agentguard.presence`.
Tim: *"It should be `com.4thwaiv.agentguard.presence` I think we will release this under 4thwaiv."* and *"Okay we'll spell it out — only if we have to."*

#### `drop-cli-contribution-framework`
The generic platform-contributed-CLI-commands mechanism (`IPlatformServices.CliCommands`, `IPlatformCliCommands`, `IPlatformCliCommand`, the AG0034 wrapper) and the fingerprint enable/disable commands are removed. Post-rework no OS contributes any CLI command; the policy file ships with packaging.
Tim: *"Agreed we don't need the setup command now."*

### Security invariants (settled, all three OS)

#### `fresh-interaction-no-cached-yes`
Every presence check forces a live human interaction and never returns a cached or retained "yes." Linux: the action is `auth_self` on all three scopes, no `_keep`, no `yes`; `AllowUserInteraction` always passed; reject any result authorized without a challenge (a `polkit.temporary_authorization_id` present → treat as non-fresh → deny). macOS: a fresh `LAContext` per check. Windows: a fresh Hello request per check.
The proactive revoke-before-check is NOT in this design: it needs a session subject we don't build, and is redundant given `auth_self`-no-keep plus the reject backstop. Deferred as a contingent improvement to **issue #43** if real Linux results show unexpected caching.
**Documented, not yet executed:** the polkit runtime behaviors this rests on — `auth_self`-no-keep mints no cached authorization and re-prompts every call; a retained grant surfaces `polkit.temporary_authorization_id` — come from the freedesktop docs, not a real call (the whole Linux path first runs in IMPLEMENT/CI). The fake-based tests prove our mapping, not polkit's behavior; the fail-closed floor is proven on the headless CI runner; "re-prompts every time / no retained grant" is a manual pre-release checklist line, like the interactive-success path.
Tim: *"Agreed."* and *"we should document it as a potential improvement depending on Linux results."*

#### `no-self-answerable-agent`
The guard uses only an authentication agent already registered by the trusted login session; it never creates, spawns, or registers an agent (no `pkttyagent`, no internal-agent flag). No pre-existing agent → deny. Accepted residual risk: a hostile pre-registered agent in an already-compromised session, above this tool's lazy-AI bar. Enforced by an analyzer rule.
Tim: *"Yes, add the rule and send it through DESIGN."*

#### `presence-native-owner-rule` (APPROVED — Tim's personal yes; corrected 2026-08-24 after RULE-PHASE)
Presence native calls are family-scoped, realized by a **narrow edit to AG0101 plus a paired rule (AG0113)**. AG0101's P/Invoke and `Marshal` branches today ban ANY P/Invoke / ANY `Marshal` outside the filesystem owner (unconditional — NOT filesystem-member-specific). Those two branches are narrowly edited to ALSO exempt the presence-port owners — `ILocalAuthentication` (in `CrossPlatform.MacOS`), `IWindowsUserPresence` (in `CrossPlatform.Windows`); AG0101's `File`/`Directory`-divergent-member ban stays filesystem-owner-only, so there is no cross-family over-grant. AG0113 confines the presence ports' native interop to those owners. The two rules SHARE one P/Invoke/`Marshal` detector (no divergent copy). AG0101's File/Directory owner identity (`ContractInterfaces.PlatformFileSystem`, shared with AG0020) is NOT mutated — the presence exemption is a separate check — so AG0020 is unaffected. Linux is native-call-free (no owner needed).
Correction: the earlier "AG0101 stays `{IPlatformFileSystem}`, unchanged" framing was wrong — AG0101's P/Invoke/`Marshal` catch is unconditional, so leaving it unchanged left the presence ports permanently RED (a presence port is not the filesystem owner). The RULE-PHASE rule-refute (SOLID + DRY) caught it; editing AG0101 is required. Deferred cleanup: fold AG0101 + AG0113 into one owner→primitive-family map (issue #39).
Tim: *"I approve"* (the narrow family-scoped AG0101 edit).

### Granted rule suppressions (settled 2026-08-27)
Five `[SuppressMessage]` attributes in the presence code are approved exceptions to the fence — each a place where a contract rule or an unavoidable native-frame constraint mandates a construct a general analyzer forbids. This is the cleanup law's "explicit waiver from the user." Any suppression in the presence code that is NOT one of these five is unapproved and fails REFUTE. No signature yet; each is signed later when the flag-unsigned mechanism lands (tracking issue #46).

1. `LinuxPresenceCheck.Check` — `CA1031`. A fail-closed presence boundary must turn any fault into `ApprovalReason.Error` and never surface `Approved`, so it catches broadly; the same pattern already approved in `Pipeline.cs` and `Program.cs`.
2. `LocalAuthentication.EvaluateReplyCallback` — `CA1031`. A method invoked across the native Objective-C block frame must catch every managed exception (AG0105 mandates the catch-all that never rethrows); `CA1031` forbids exactly that.
3. `LocalAuthentication.ReplyInvokePointer` — `S6640`. AG0104 mandates the callback be reached by a function pointer `&Method`, legal only in `unsafe`; the only alternative is the marshalled delegate AG0104 exists to forbid.
4. `WindowsUserPresence.TryVerifyWithHelloAsync` — `CA1031`. The broad catch is the "Hello degrades to the password prompt" behavior — any Hello binding failure falls back rather than denying the user.
5. `WindowsUserPresence.AsyncCompletedHandler.Invoke` — `CA1031`. A COM completed handler invoked across the native frame must catch everything; it already completes the awaiting task with a non-verified outcome.

Not granted, corrected instead: `EvaluateReplyCallback`'s `RCS1075` (empty catch). The callback is rewritten to complete the awaiting task with a fail-closed result inside the catch, which removes the empty-catch suppression and denies immediately on a callback fault instead of hanging until the gate's 60-second timeout.

Tim: *"Record these exceptions and authorized."*

### Native smoke — CI-only, near-zero bound, port honors cancellation (settled 2026-08-28)
The `PresenceNativeSmokeTests` native smoke drives the REAL presence impl, so it shows the OS auth prompt. Two settled changes keep it off developer machines and safe on CI. (1) It is removed from compilation unless the dedicated `AgentGuardRunNativeSmoke` flag is set (a csproj build directive, not a code-level `Skip`, so AG0026 stays clean); `ci.yml` sets it on the three OS test jobs and nothing else does — NOT the generic `CI` env var, which agent/automation contexts set too and which therefore popped the dialog during REFUTE on the interactive machine. So the smoke runs on the CI legs and is absent everywhere else. Tim: *"IT HAS TO GO THE OTHER WAY!!!!!... tests opted out locally and only run on the CI when the flag is SET!"* and *"Now we need to not showing up on interactive builds."* (2) When it runs, it bounds the real `Check` with a near-zero timeout through the injected clock (1 second now, ~500 ms later), and the macOS port now HONORS the cancellation token — on cancel it messages `[LAContext invalidate]`, which cancels the pending evaluation and dismisses the dialog; LA fires the reply with a cancel error that maps to non-approved. Honoring a passed token is not minting a timeout (AG0107 clean), and it also makes the gate's real 60-second timeout dismiss the prompt in production. Windows Hello honor-cancellation is deferred to the Windows CI leg (issue #47). Tim: *"THE real smoke should set a near 0 time out"*, *"1 second is fine we can reduce to 500ms later."*

## What we're building

The three mutating setup commands — `install`, `init`, `remove` — become gated on a physically-present human, per OS, replacing today's always-approve placeholder; `doctor` stays ungated. Presence is proven only through an out-of-band channel the calling process cannot answer: macOS LocalAuthentication, Windows Hello, and on Linux a polkit `CheckAuthorization` whose prompt is shown by the session's own authentication agent.

`IPresenceCheck` sits on `IPlatformServices.Presence` with one operation, `Task<PresenceResult> Check(PresenceRequest request, ISystemServices services, CancellationToken ct)`. `PresenceResult` carries an `ApprovalReason` and a detail string; `ApprovalReason` is one shared Abstractions enum, and the boundary never emits `TimedOut`.

The gate is asynchronous. `ApprovalGate.RequireApprovalAsync` reaches `services.Platform.Presence` and `services.Clock` through the root, resolves the reviewed dialog text from its owned resource keyed by `SetupVerb`, and races the check against a clock-driven 60-second delay; expiry denies with `TimedOut`. `ApprovalDecision` stays in the Engine (moving it to Abstractions would make the presence interface return an Engine type, a backward dependency).

All three OS share one shape: the `IPresenceCheck` implementation calls an internal per-OS native port, gets a plain result record, and maps it to `ApprovalReason` through a pure function. On Linux, `LinuxPresenceCheck` talks to polkit through `IPolkitAuthority` (sole implementer `TmdsPolkitAuthority`, the only class touching `Tmds.DBus.Protocol`). On macOS and Windows the presence impl talks to `ILocalAuthentication` / `IWindowsUserPresence` (sole implementer each, the only class touching the native API), creating a fresh native auth context per call. Every check forces a fresh human interaction and rejects any cached authorization.

The new public surface is the minimum: `IPresenceCheck`, `ApprovalReason`, `PresenceRequest`, `PresenceResult`, and the `Presence` property. No username lookup, no console password read, no CLI-contribution interfaces.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change.

End state:
- `install`, `init`, `remove` deny and return early, each reason on its own CLI line, whenever presence is not confirmed; `doctor` makes no presence call.
- `IPresenceCheck` exists on `IPlatformServices.Presence` (Check-only), with a real per-OS implementation in each `CrossPlatform.*` library and a fake in `SystemServicesBuilder`; the container / one-owner / builder analyzers pass with no edits beyond the signed-off rules.
- macOS uses `LAPolicy.DeviceOwnerAuthentication` with a fresh `LAContext` per check; Windows tries Windows Hello (WinRT `UserConsentVerifier` interop), else the secure-desktop credential prompt validated with `LogonUser`, fresh per check; Linux uses one polkit `CheckAuthorization` with the triple subject, `auth_self`, `AllowUserInteraction`, and rejection of any cached authorization.
- The 60-second fail-closed timeout holds on all three OS, proven by advancing a fake clock with no real wait.
- No `OperatingSystem.Is*` / `#if` / `RuntimeInformation.IsOSPlatform` in the CLI or Engine.
- `dotnet build -c Release` = 0/0 and `dotnet test -c Release` = 0 failed, locally and on all three CI legs; each `CrossPlatform.<OS>` assembly and the aggregate meet the hard 75% gate, the one interactive-success line per OS handled per `coverage-exclusion-deferred`.
- Each OS CI leg's native smoke proves the real implementation returns a non-approved reason on the headless runner (the Linux ubuntu leg has no session agent, so `CheckAuthorization` returns a non-approved reason — directly, or via `Error` on a bus-less runner).
- Nothing under `Abstractions/**` changes except the additions in Surfaces. No silent change to `eng/signing/agentguard.entitlements`. `Tmds.DBus.Protocol` is added only to central pinning and the Linux csproj.

Any restatement or weakening of this to fit the result is a top-line Lie-catcher finding.

## Surfaces

New Abstractions files:
- `src/AgentGuard.Abstractions/Contracts/IPresenceCheck.cs` — NEW (`Check` only).
- `src/AgentGuard.Abstractions/ApprovalReason.cs` — NEW enum.
- `src/AgentGuard.Abstractions/Contracts/PresenceRequest.cs`, `PresenceResult.cs` — NEW records.

Changed Abstractions:
- `src/AgentGuard.Abstractions/Contracts/IPlatformServices.cs` — add the `Presence` property (only).
- `IEnvironment.cs` / `IConsole.cs` / `ISystemServices.cs` — UNCHANGED (no `GetUserName`, no `ReadPassword`; presence reached via `Platform`, clock via `Clock`).

Engine:
- `ApprovalGate.cs` — async `RequireApprovalAsync(ISystemServices services, string action)`.
- `ApprovalDecision.cs` — `(string Action, ApprovalReason Reason, string Detail)`, `IsApproved` derived.
- `SetupContext.cs` — UNCHANGED for presence (gate reaches presence and clock through the root).
- `SetupCommands.cs` — `Install`/`Init`/`Remove` async, take the root `ISystemServices`, reference `SetupVerb`; `Doctor` unchanged.
- `PresenceDialogText.cs` (or the owned-resource location) + `SetupVerb` — the reviewed per-verb strings and the single verb owner.

macOS (`src/AgentGuard.CrossPlatform.MacOS/`): `MacOsPresenceCheck : IPresenceCheck` (maps to `ApprovalReason`); the internal `ILocalAuthentication` port + its sole implementer (the `objc_msgSend` → LocalAuthentication class, fresh `LAContext` per call — the presence-native-rule owner) + its native-methods file; `CreatePresence()` fragment.

Windows (`src/AgentGuard.CrossPlatform.Windows/`): `WindowsPresenceCheck : IPresenceCheck`; the internal `IWindowsUserPresence` port + its sole implementer (Windows Hello via the WinRT `UserConsentVerifier` interop, else the secure-desktop credential prompt validated with `LogonUser`, fresh per call — the presence-native-rule owner) + the WinRT interop for Hello plus flat `[LibraryImport]` for `CredUIPromptForWindowsCredentials`/`LogonUser`.

Linux (`src/AgentGuard.CrossPlatform.Linux/`): `LinuxPresenceCheck.cs` (decision logic, subject construction from `/proc/self` via `IFileReader`, `PolkitDecision` mapping); an internal `IPolkitAuthority` port; `TmdsPolkitAuthority.cs` (sole `Tmds.DBus.Protocol` user); `CreatePresence()` fragment. No P/Invoke, no `/etc/pam.d` writing. `TmdsPolkitAuthority` also carries a pure internal `BuildCheckAuthorizationRequest(PolkitSubject, actionId, message)` returning the NEW inspectable `PolkitCheckAuthorizationRequest.cs` record — the option-2 test seam (see the escalation ruling): it carries `Flags` (AllowUserInteraction=1) and a per-key `SubjectVariantTypes` map of D-Bus wire-type codes so the wire encoding — notably the 64-bit start-time (`"t"`, not 32-bit `"u"`) — is unit-testable without opening a bus. BCL-only types, so the test never references `Tmds.DBus.Protocol`.

Policy file: `eng/polkit/agentguard-presence.policy` — NEW shipped repo file (the `auth_self` action XML). Plus `eng/polkit/install.sh` — NEW, shipped with the Linux release to `sudo install` the policy (and binary) into place (interim, per issue #44). Its **filename does not embed the action id** — polkit matches by the `<action id>` inside, so the id lives in exactly two places (the C# const and the XML `id`), both pinned by the integrity test. Installed root-owned by packaging, never by the guard.

Shared / CLI / deps:
- `Directory.Packages.props` — add `Tmds.DBus.Protocol` to the "Runtime dependencies" pin group (roll-forward disabled).
- `AgentGuard.CrossPlatform.Linux.csproj` — the only `<PackageReference>` to `Tmds.DBus.Protocol` (versionless; owners live at their lowest consumer).
- `src/AgentGuard.Cli/Program.cs` — `RunCommandAsync` replaces `RunCommand`; `install`/`init`/`remove` reference `SetupVerb` and pass the root `services`. No CLI-contribution host.

Tests:
- `tests/AgentGuard.TestHelpers/SystemServicesBuilder.cs` — `FakePlatformServices` grows a `Presence` slot (default `Approved`), with the `With`/`Wrap` the builder-completeness analyzer requires; new `FakePresenceCheck`; a `FakePolkitAuthority` for the Linux unit tests.
- `tests/AgentGuard.Tests/` — gating tests (each reason; the fake-clock timeout test).
- `tests/AgentGuard.CrossPlatform.Tests/` (and, if it can't host Linux-only internals, a Linux-leg test project — confirm reuse first): the `PolkitDecision` table, `LinuxPresenceCheck` against `FakePolkitAuthority`, subject construction from fabricated `/proc/self` via `FakeFileSystem`, the policy-integrity test, the per-OS native smokes, a RED fixture per new analyzer.
- `.github/workflows/ci.yml` — the per-OS native smoke steps; confirm the ubuntu runner has dbus/polkitd or that a missing system bus surfaces as `Error`, not a crash.

Frozen: `analyzers/**` except the signed-off rules; no other `Abstractions/**` change.

## Reuse ledger

| Capability | Ruling | Owner / note |
|---|---|---|
| presence-interface | NEW | No presence abstraction exists. Add `IPresenceCheck` on `IPlatformServices.Presence`. |
| macos-native-presence | NEW | `objc_msgSend` → LocalAuthentication behind the internal `ILocalAuthentication` port; mirror the `PosixNativeMethods` P/Invoke pattern. |
| windows-native-presence | NEW | Hello via the WinRT `UserConsentVerifier` interop (WinRT activation, NOT the flat `WindowsNativeMethods` pattern) + the secure-desktop credential prompt (`CredUIPromptForWindowsCredentials`) + `LogonUser` (these two mirror `WindowsNativeMethods`), behind the internal `IWindowsUserPresence` port. |
| linux-polkit-dbus | REUSE (behind a new owner) | Reuse the `Tmds.DBus.Protocol` primitive; do NOT reimplement D-Bus. Owner = `TmdsPolkitAuthority` in `CrossPlatform.Linux`. Pinned in `Directory.Packages.props`. |
| process-identity-triple | REUSE | pid/start-time/uid from `/proc/self` through the already-owned `IFileReader`; NOT `Environment.ProcessId` (AG0011-owned to Boundaries → RED in `CrossPlatform.Linux`). No new owner member. |
| presence-test-fake | REUSE | Extend the `SystemServicesBuilder` `FakePlatformServices` pattern; do not invent a new fake shape. |
| coverage-exclusion-waiver | NEW (precedented) | AG0101 interim-waiver shape at `PlatformFileSystemSpecTests.cs:248-252`; deferred to the gate. |
| polkit-request-builder (option 2) | NEW | `TmdsPolkitAuthority.BuildCheckAuthorizationRequest` + the `PolkitCheckAuthorizationRequest` record — a pure, inspectable pre-wire assembly of the CheckAuthorization arguments, split from the send so a unit test can assert `AllowUserInteraction=1` and the 64-bit start-time variant without opening a bus. Every discovery lens (grep across src/analyzers/tests, CodeGraph, lore) came back empty — no prior art. Added on Tim's option-2 ruling of the acceptance-#4 wire-test escalation. |

### Analyzer-side reuse ledger (RULE-PHASE)

Reuse rulings for the new analyzer rules and their shared helpers, so the detection logic is owned once and not reinvented per rule.

| Capability | Ruling | Owner / note |
|---|---|---|
| P/Invoke + `Marshal` recognition | NEW (consolidates) | `NativeInteropUse` — one detector, shared by AG0101 and AG0113. |
| timeout-member recognition (`Task.Delay`/`WaitAsync`/CTS-timeout/`CancelAfter`/`CreateTimer`) | NEW (consolidates) | `TimeoutMembers` — shared by AG0038 and AG0107. |
| attribute-name resolver (`SimpleName`/`FullName`/`"Attribute"`) | EXTRACT | `AttributeSyntaxName` — extracted from AG0008 + AG0105; both route to it. |
| presence identities + `IsPresenceCheckInvocation` + `IsInNativePresencePortOwner` | NEW | `PresenceContracts` — shared by AG0107/0108/0109/0113/0114. |
| interface-membership invocation ("is this a call to member M on a type implementing interface I") | REUSE (centralized) | `OwnerClass.IsInterfaceMemberInvocation` — composes `OwnerClass.Implements` (which walks `AllInterfaces`) + `WellKnownType.IsAnyOf`; `PresenceContracts.IsPresenceCheckInvocation` routes through it so a `Check` reached through a concrete implementer reference (`LinuxPresenceCheck`/`MacOsPresenceCheck`/`WindowsPresenceCheck`/`FakePresenceCheck`) is caught the same as one through the `IPresenceCheck`-typed reference. The interface-membership check itself stays owned by `OwnerClass.Implements`/`WellKnownType` — no rule re-derives it. |
| owned-source-vs-literal/const/field detection | NEW (reusable) | `OwnedSourceArgument` — the AG0109 boilerplate; future literal-vs-owned rules reuse it (Tim: "this should be boilerplate"). |
| namespace-tree match | EXTRACT/REUSE | `WellKnownType.IsInNamespaceTree` — the existing type-identity home; AG0110 routes to it. |
| the presence guardrail diagnostics (AG0038, AG0102–AG0114) | NEW | Each a new analyzer + tests; no prior art (presence is a new capability). |
| test fixtures (`IPresenceCheck`/`PresenceRequest` stub) | REUSE | `SharedAnalyzerSources.cs` — one canonical stub, all presence-rule tests reference it. |
| `Compilation` → per-OS-implementation-assembly gate adapter (pull `AssemblyName`, call `IsPerOsImplementationAssembly`) | EXTRACT | `CrossPlatformBoundary.IsAnyPerOsImplementationLibrary` — the one cached `Func<Compilation,bool>` wrapper, next to the string check it wraps, mirroring `OwnerClass.InAssembly`. Consumers: AG0101 (`OsDivergentFilesystemOnlyInCrossPlatformAnalyzer.IsFilesystemOwnerClass`, formerly its own `InPlatformLibrary` lambda) and AG0113 (`PresenceNativeInteropOwnerAnalyzer`, formerly via `PresenceContracts.InPerOsImplementation`); both re-wrapping fields removed, both now reference the owner directly. `PathPurityAnalyzer.InSeparatorOwnerAssembly` stays a distinct broader gate (per-OS OR `TestHelpers`) that calls the owner method directly — not a copy. |

### Test-side reuse ledger (TDD)

Reuse rulings for the shared presence test-infrastructure the TDD stage extracted, so each dedup owner is on the contract's record, not only in code. (The builder-level `IPresenceCheck` fake stays covered by the `presence-test-fake` row above; these are the lower-level per-OS-port primitives it does not reach.)

| Capability | Ruling | Owner / note |
|---|---|---|
| per-OS native-port call recorder | EXTRACT | `SingleCallRecorder<TArg,TResult>` — the shared record-call-count-and-last-argument recorder the three port fakes (`FakePolkitAuthority`, `FakeLocalAuthentication`, `FakeWindowsUserPresence`) compose instead of each re-implementing it. |
| cross-OS presence-check port spec | NEW | `IRecordingPresencePortFake` + `PresenceCheckPortSpec` — the OS-agnostic "calls the port once per `Check` and maps the outcome" spec both `MacOsPresenceCheckTests` and `WindowsPresenceCheckTests` call, differing only in the fake type and the reason table. No prior art. |
| polkit test reply/call helpers | EXTRACT | `PolkitTestReplies` (the shared empty-reply-detail builder, formerly the duplicated `NoDetails()`) + `PolkitCall` (the captured-call value `FakePolkitAuthority` records), shared by `LinuxPresenceCheckTests` and `PolkitDecisionTests`. |
| per-OS outcome→`ApprovalReason` table | NEW | `MacOsPresenceOutcomes` / `WindowsPresenceOutcomes` — the one table per OS shared between that OS's `*CheckTests` and `*MapperTests`, so the mapping is spelled exactly once per OS. No prior art (presence is new). |

## What to do

1. **Abstractions.** Add `IPresenceCheck` (`Check` only), the `ApprovalReason` enum, `PresenceResult(ApprovalReason, string)`, and `PresenceRequest` (carrying the resolved prompt text only — the gate already holds the verb and keys `PresenceDialogText` and `ApprovalDecision.Action` with it). Add `Presence` to `IPlatformServices`. No other Abstractions change.
2. **Gate + timeout.** Change `ApprovalDecision`; make `ApprovalGate.RequireApprovalAsync(ISystemServices services, string action)` reach `services.Platform.Presence`/`services.Clock`, resolve the prompt via the owned resource keyed by `SetupVerb`, and race `Check` against `Task.Delay(PresencePolicy.Timeout, services.Clock, ct)` with `Task.WhenAny` (delay wins → cancel → `TimedOut`). `Install`/`Init`/`Remove` async + root `services`; replace `RunCommand` with `RunCommandAsync`; introduce `SetupVerb` as the single verb owner; each reason renders via the gate-populated `ApprovalDecision.Detail`.
3. **macOS impl.** `MacOsPresenceCheck` calls the internal `ILocalAuthentication` port; the port's sole implementer makes the `objc_msgSend` LocalAuthentication call (`LAPolicy.DeviceOwnerAuthentication`, `localizedReason` from `request.PromptText`, a FRESH `LAContext` per call) and returns a plain `LaResult`. `MacOsPresenceCheck` maps it: `UserCancel`→`Cancelled`, `AuthenticationFailed`→`Denied`, `NotAvailable`/`NotEnrolled`→`Unavailable`, `Success`→`Approved`, else→`Error`.
4. **Windows impl.** `WindowsPresenceCheck` calls the internal `IWindowsUserPresence` port; the port's sole implementer tries Windows **Hello via the WinRT `UserConsentVerifier` interop** (`IUserConsentVerifierInterop.RequestVerificationForWindowAsync` — WinRT activation, needs an HWND from `GetConsoleWindow()` or a message-only window; Hello has NO flat-Win32 entry point), fresh request per call. (Microsoft documents the interop method's minimum client as Windows 11 build 22000, though Hello and the WinRT class are Windows 10+; the real Win10-vs-11 floor is an IMPLEMENT verification — where Hello can't bind, the credential fallback handles that host.) When Hello is unavailable, un-configured, or can't bind (the version floor, no HWND, or single-file activation fails) it falls back to the secure-desktop credential prompt (`CredUIPromptForWindowsCredentials` + `CREDUIWIN_SECURE_PROMPT`, current-user scoped) validated with `LogonUser` (`LOGON32_LOGON_NETWORK`, domain `.`); a passwordless account → no method. It returns a plain `WindowsPresenceResult`, which `WindowsPresenceCheck` maps: verified (Hello or password)→`Approved`, user cancelled→`Cancelled`, no method available→`Unavailable`, wrong password / Hello failed→`Denied`, else→`Error`. Only `CredUIPromptForWindowsCredentials`/`LogonUser` mirror the flat `WindowsNativeMethods` P/Invoke pattern; Hello is WinRT. Wire the Windows CI smoke.
5. **Linux impl.** `TmdsPolkitAuthority` opens the system-bus `Connection` and calls `org.freedesktop.PolicyKit1.Authority.CheckAuthorization` at `/org/freedesktop/PolicyKit1/Authority`: subject kind `unix-process` built from the triple (`pid` and `start-time` = `/proc/self/stat` fields 1 and 22, both parsed from the last `)` to survive the comm-field gotcha; `uid` = the **real** UID, the first column of `/proc/self/status` `Uid:`), each written into the `a{sv}` dict with polkit's expected D-Bus variant type pinned explicitly — notably `start-time` as a **64-bit** type (a 32-bit start-time truncates past ~497 days of uptime and silently mismatches the process, and a fresh CI runner never reproduces it), `action_id` = the owned const `com.4thwaiv.agentguard.presence`, details `a{ss}` with `polkit.message` = `request.PromptText`, flags `AllowUserInteraction=1`, empty cancellation id, returning the plain-typed result `{bool isAuthorized, bool isChallenge, IReadOnlyDictionary<string,string> details}` behind the `IPolkitAuthority` port. `LinuxPresenceCheck` calls once and maps via the pure static `PolkitDecision`: authorized + no temp-auth-id → `Approved`; authorized WITH a temp-auth-id / retains marker → `Denied` (non-fresh backstop); not-authorized + challenge → `Unavailable`; `polkit.dismissed` → `Cancelled`; not-authorized + no challenge → `Denied`; any fault → `Error`; never `TimedOut`; no catch returns `Approved`. Ship `eng/polkit/agentguard-presence.policy` (filename does not embed the id) with `<action id>` = the C# const, and `allow_any`/`allow_inactive`/`allow_active` all `auth_self`, no `_keep`, no `yes`, and a generic `<message>`. `CreatePresence()` in the Linux fragment returns this impl.
6. **Dependency.** Pin `Tmds.DBus.Protocol` **`0.94.0`** in the `Directory.Packages.props` "Runtime dependencies" group; a versionless `<PackageReference>` only in `AgentGuard.CrossPlatform.Linux.csproj`.
7. **Tests.** `FakePresenceCheck` (default `Approved`, a `NeverCompletes` mode) + one port fake per OS on the builder — `FakePolkitAuthority`, `FakeLocalAuthentication`, `FakeWindowsUserPresence` (uniform across the three). Write: the gating tests and the fake-clock timeout test; a pure-mapper table test **per OS** — the Linux `PolkitDecision` (every reason, incl. the temp-auth-id `Denied` and the fault `Error`) AND the parallel macOS `LaResult`→`ApprovalReason` and Windows `WindowsPresenceResult`→`ApprovalReason` mappers (every reason) — so all three are table-tested at the seam, not just Linux; each per-OS `IPresenceCheck` impl against its port fake (`LinuxPresenceCheck`/`MacOsPresenceCheck`/`WindowsPresenceCheck`, always-interactive, one call per `Check`, fresh context); subject construction from fabricated `/proc/self` via `FakeFileSystem` (asserting the pinned wire types — including the 64-bit `start-time` — and the real-UID column); the policy-integrity test (parse the shipped `.policy`, assert all three scopes are `auth_self`, no `_keep`/`yes`, action id equals the C# const); the per-OS native smokes (real `Check` → non-approved on the headless runner); a RED fixture per new analyzer. If `CrossPlatform.Tests` cannot compile-time-reference Linux-only internals, add a Linux-leg test project — but confirm the existing project can't host them first.
8. **Entitlements.** Run `entitlements-verify-amend-if-needed`; amend only via a decision-14 amendment if required.
9. **Prove.** Build/test green locally and on all three CI legs; coverage gate green; the smoke steps pass.

## What the agent MAY do
- Add the Abstractions additions in Surfaces.
- Add macOS/Windows P/Invoke inside the `ILocalAuthentication` / `IWindowsUserPresence` port implementers; call polkit only through `TmdsPolkitAuthority`.
- Extend `SystemServicesBuilder` with the presence and polkit fakes and the required `With`/`Wrap`.

## What the agent MUST NOT do
- Read a password from the CLI, or use any in-band channel the caller controls, as presence.
- Create, spawn, or register a polkit authentication agent (`pkttyagent`, internal-agent flag, `RegisterAuthenticationAgent`).
- Touch `Tmds.DBus.Protocol` anywhere but `TmdsPolkitAuthority`; use the reflection-based high-level `Tmds.DBus` namespace anywhere.
- Cache a native auth context (`LAContext`, Hello request) as an instance/static field; grow a timeout inside a presence impl; return `Approved` from any catch.
- Define the polkit action with `yes` or any `_keep`; write `/etc/pam.d` or the `.policy` file from the guard binary.
- Branch on OS outside the `CrossPlatform.*` libraries; add P/Invoke outside those assemblies; change `entitlements` silently.
- Weaken/skip/delete a test; add or widen any analyzer suppression beyond the five granted in Decisions; apply `[ExcludeFromCodeCoverage]` to reach 75%.
- Change any decision above without Tim's words. Commit or push. Stop and report at any wall.

## Acceptance

Each check re-derivable by someone other than the worker.

1. `dotnet build -c Release` = 0/0; `dotnet test -c Release` = 0 failed (output pasted with exit codes).
2. Gating: an above-the-seam test proves each of install/init/remove denies-and-returns-early on each of `Denied/Unavailable/Cancelled/Error`, proceeds on `Approved`, and doctor makes no presence call; each reason yields its own CLI line.
3. Timeout: with presence set to never complete, advancing the fake clock past 60 s yields `TimedOut` and a deny, with no real wait.
4. Fresh interaction: `LinuxPresenceCheck` against `FakePolkitAuthority` calls `CheckAuthorization` exactly once per `Check` with `AllowUserInteraction` set; the `PolkitDecision` table maps a fabricated `temporary_authorization_id` to `Denied`. A test asserts no per-OS native port implementer (`ILocalAuthentication`/`IWindowsUserPresence`) holds a native auth-context field (the analyzer RED fixture covers it).
5. No self-answerable agent: `grep -rn "pkttyagent\|RegisterAuthenticationAgent\|PolicyKit1.AuthenticationAgent" src` finds nothing (the analyzer enforces it).
6. `grep -rn "LibraryImport\|DllImport" src` finds presence P/Invoke only under `CrossPlatform.MacOS`/`.Windows`; none under `CrossPlatform.Linux`. `grep -rn "Tmds" src` finds `Tmds.DBus.Protocol` only in `TmdsPolkitAuthority`, and no `Tmds.DBus` (high-level) anywhere.
7. Policy integrity: the wired test asserts the shipped `.policy` has all three scopes `auth_self`, no `_keep`/`yes`, and an action id equal to the C# const.
8. CI: all three legs green; the native smoke compiles and runs only when `ci.yml` sets `AgentGuardRunNativeSmoke` (excluded from all other compilation) and, bounded by a near-zero timeout the port honors, shows the real implementation returns a non-approved reason without hanging.
9. Coverage: each `CrossPlatform.<OS>` assembly and the aggregate meet the hard 75% gate; the only uncoverable line per OS is the interactive-success path; any `[ExcludeFromCodeCoverage]`+AG0032 waiver is the one Tim signs at the gate.
10. `git diff` shows `Abstractions/**` changed only by the additions in Surfaces, `analyzers/**` only by the signed-off rules (AG0101 changed only by the narrow family-scoped presence-port exemption on its P/Invoke/`Marshal` branches — Tim's personal yes — its File/Directory ban and its shared owner identity untouched; the paired presence-native-interop rule present), `Directory.Packages.props`/the Linux csproj changed only by the `Tmds.DBus.Protocol` pin, and `entitlements` unchanged unless a decision-14 amendment records a key.
11. Verb single-owner: `install`/`init`/`remove` appear as literals only in `SetupVerb`; `Program.cs` and `SetupCommands` reference it.
12. Suppressions: `grep -rn "SuppressMessage" src/AgentGuard.CrossPlatform.*/Presence` returns exactly the five granted in Decisions (site 1 `CA1031`, site 2 `CA1031`, site 3 `S6640`, site 4 `CA1031`, site 5 `CA1031`) and no other; `EvaluateReplyCallback` carries no `RCS1075` suppression (it was corrected, not granted).

## Rules (RULE-PHASE) — `rule-phase-ruleset`

This section is the `rule-phase-ruleset` the RULE-PHASE workflow consumes: each guardrail below is one to create, with what it forbids and its mechanism (Roslyn analyzer vs a wired test). Determined by DESIGN and signed off. RULE-PHASE writes each RED against a violating fixture (or notes it preventive where the presence code it governs does not exist yet), then the independent panel proves them. Provisional IDs; RULE-PHASE assigns the next contiguous free IDs (existing analyzers run through AG0037 + AG0101; AG0038 and AG0102+ are free).

**Carried unchanged from the pre-rework design (still apply):**
- **AG0102** — no `object`/`dynamic`/unconstrained-generic/variadic in any P/Invoke signature (macOS/Windows).
- **AG0103** — macOS native `bool` return/param marshalled as `UnmanagedType.I1`.
- **AG0038** — bounded waits/timeouts go through the injected `TimeProvider` (makes the 60 s testable).
- **AG0107** — no timeout inside a presence impl (the gate owns the one 60 s bound; `LinuxPresenceCheck` must not wrap its own CTS around `CheckAuthorization`).
- **AG0108** — `IPresenceCheck.Check` invoked only from `ApprovalGate` in production (`src/**`); the `CrossPlatform.Tests` native smoke is exempt and calls `Check` directly with its own test prompt.
- **AG0109** — no inline prompt literal at a production presence call site; the text comes from the owned resource keyed by `SetupVerb`.

**Survive, rescoped (DESIGN corrected my "drop" premise — flagging, not silently keeping):**
- **AG0104 / AG0105** — a managed callback crossing the native boundary must be a static `[UnmanagedCallersOnly(CallConvs=[CallConvCdecl])]` method reached by `&` (no `Marshal` delegate pointer), its whole body one `try/catch(Exception)` returning a native error code. Rescoped from the retired PAM callback to the macOS `evaluatePolicy:localizedReason:reply:` completion block, which is still a live native callback.

**Dropped:** the `NativeMemory` allocation rule — Linux allocates nothing now.

**Changed (APPROVED — Tim's personal yes):** AG0101 gets a narrow family-scoped edit — its P/Invoke and `Marshal` branches ALSO exempt the presence-port owners (`ILocalAuthentication`/`IWindowsUserPresence`); its `File`/`Directory`-divergent-member ban stays filesystem-owner-only. Its shared File/Directory owner identity (`ContractInterfaces.PlatformFileSystem`, used by AG0020) is NOT mutated. Paired with New rule 5, sharing one P/Invoke/`Marshal` detector.

**New:**
1. **Managed D-Bus owner-confinement** — register the `Tmds.DBus.Protocol` family in the owner table (reusing `OwnerClass.IsOwner`; owning interface = `IPolkitAuthority`, assembly-gate = `CrossPlatform.Linux`; ban is matched by assembly/namespace membership so it catches every type the package exposes). Its own rule, separate from AG0101. **Pair it with a single-implementer guard on each internal per-OS native port** — `IPolkitAuthority` (Linux), `ILocalAuthentication` (macOS), `IWindowsUserPresence` (Windows) — each scoped to its per-OS assembly (reuse `SecondImplementerGuard.Register`, `permittedImplementers: 1`): `OwnerClass.IsOwner` exempts *any* implementer of the owning interface, and these ports are internal, outside the `ISystemServices` tree, so AG0025/AG0030 do not cover them — without the guard "exactly one class owns each native library" is a convention, not a build invariant. Additionally ban the high-level `Tmds.DBus` namespace everywhere (AOT/single-file cleanliness). Stops a second ungoverned presence path via the one boundary primitive AG0008/0009/0011/0101 all miss.
2. **Never register/spawn an auth agent** — a NEW value-scan analyzer over `src/**` flagging any string or const literal whose VALUE is one of `"org.freedesktop.PolicyKit1.AuthenticationAgent"`, `"RegisterAuthenticationAgent"`, `"RegisterAuthenticationAgentWithOptions"`, `"pkttyagent"`. This is a genuinely new shape — matched by literal/const VALUE, unlike AG0035 (`NoLiteralFakeRootAnalyzer`), which matches by parameter name and never inspects the value; no existing analyzer compares a constant's value against a banned set. Scope to exact literal/const values (not substring matches in unrelated text). Acceptance #5's `grep` is the independent backstop. The process-spawn half is already covered by the live `Process` ban.
3. **No cached native auth context** — in any per-OS native port implementer (`ILocalAuthentication`/`IWindowsUserPresence`), a native auth-context handle (`LAContext`, Hello request, their `IntPtr`) must not be an instance/static field — it must be created per call, forcing a fresh interaction (the macOS/Windows analogue of the polkit no-cached-yes rule, unobservable from above the seam).
4. **Policy-file integrity** (a wired test, not a Roslyn rule) — parse the shipped `.policy`, assert all three scopes are exactly `auth_self`, no `_keep`/`yes`, and the XML action id equals the single-owner C# const.
5. **Presence native-interop owner (AG0113, family-scoped; APPROVED — Tim's personal yes).** Confine raw native P/Invoke call sites and the `Marshal`/native uses the presence ports need to the per-OS presence-port owners — `ILocalAuthentication` (in `CrossPlatform.MacOS`), `IWindowsUserPresence` (in `CrossPlatform.Windows`) — reusing `OwnerClass.IsOwner`. Paired with the narrow AG0101 edit above (which exempts the same owners on AG0101's P/Invoke/`Marshal` branches) and SHARING one P/Invoke/`Marshal` detector with it, so a legitimately-placed presence native call is clean under both rules and a presence port still cannot make `File`/`Directory`-divergent filesystem calls. Deferred: fold AG0101 + this rule into one owner→family map when issue #39 lands.

## Tier

L1 — a new cross-OS capability, a trust boundary (what counts as "present"), native interop on macOS/Windows and a new managed OS-boundary dependency on Linux, an async change to the setup-command gate, and a set of new analyzer rules; all roles run. Tim set the level.

## Scope

Change scope only by editing this file before the run starts.
