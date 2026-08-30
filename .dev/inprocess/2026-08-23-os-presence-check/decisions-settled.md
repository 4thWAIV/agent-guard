# Settled decisions — OS presence check (the contract's Decisions section, in progress)

Resolved with Tim on 2026-08-23. These lock the contract's Decisions when it is written.

## Binding
Flat `net10.0` + P/Invoke on all three OS (macOS `objc_msgSend`→LocalAuthentication, Windows Hello, Linux PAM). Option B (net10.0-macos + first-party binding) is a proven fallback. Both spike-proven (`spike-objc-msgsend.md`, `spike-net10macos-b.md`).

## Wiring — presence is an owned service, consumed via DI
> **Updated 2026-08-23:** `IPresenceCheck` is `Check`-only (Setup dropped — Tim: "Yes drop it we moved it to the comand handler many turns back"). Presence is reached at the gate as `services.Platform.Presence` (not stored as a separate `SetupContext` property — SOLID/DRY found that redundant). The gate is async.
- Presence is a first-class owned service interface, `IPresenceCheck`, exposed per-OS as `IPlatformServices.Presence` (sibling to `FileSystem`; per-OS owner in the `CrossPlatform.*` libs; reached as `services.Platform.Presence`). Governed by the existing one-owner / container / builder analyzers with no rule edits.
- The presence capability has one operation: `Check` (evaluate presence — the approval gate consumes it).
- The approval gate stops being a bare static that always approves; it consumes the injected `IPresenceCheck`. `RequireApproval` gains the context/service, the three call sites already hold `context`, and the "no change to callers" comment is dropped as placeholder aspiration.
- Tim: "WHy doesn't Presense becomse a Service interface and have an owner class (IPresence or something)? We treat it like all our other abstractiosn?"

## Platform-contributed CLI commands (no OS branching in the CLI)
> **SUPERSEDED in part 2026-08-23 (polkit rework):** the Linux `enable-fingerprint`/`disable-fingerprint` commands are gone (polkit uses the system's own fingerprint config; the guard never writes `/etc/pam.d`). Whether the generic contribution mechanism survives at all — e.g. for a one-time policy-install command — is a question for the polkit DESIGN run below.
- `IPlatformServices` (per-OS) contributes its CLI commands generically — each a `(name, description, handler)`. The CLI is a generic host that appends `services.Platform`'s contributions to its command set at startup. AG0009-clean (no OS branch in the CLI); open/closed (the CLI never changes for a new per-OS command); no phantom no-op commands.
- Linux contributes `presence enable-fingerprint` (+ a matching disable). macOS/Windows contribute none.
- Tim's design: "A Platform service that 'specifies additional switches' ... The CLI calls this and if it returns new methods/switches it configures them. Only Linux would return it."

## Linux presence — password baseline (no root), fingerprint opt-in (root)
> **SUPERSEDED 2026-08-23 — replaced entirely by the polkit rework section below.** Reason: reading the password from the CLI's own stdin is *in-band* — the caller (the AI) controls stdin and can supply it, so it is not a presence proof. Linux presence now goes through polkit's out-of-band agent, like the macOS/Windows OS dialogs. The password baseline, the `/etc/pam.d/agent-guard` file, and the enable/disable-fingerprint commands are all dropped.

(Original, superseded:)
- Baseline, out of the box, no root: authenticate the current user against an existing system PAM service (password, plus opportunistic fingerprint where the distro already wired `fprintd`). Best-effort → password → deny. The exact reused service is a portability detail for DESIGN.
- Guaranteed fingerprint, opt-in, root: `sudo guard presence enable-fingerprint` (name TBD) writes `/etc/pam.d/agent-guard` (`auth sufficient pam_fprintd.so` → `auth required pam_unix.so` → deny). The file's existence is the switch; the guard owns/writes/removes it. The main `guard install` stays user-space.
- Tim: "It kind of has to work out of the box on linux... best effort and fall back to password validation" and "If we support password and an extra step to provide fingerprint, then that would be okay."

## macOS security policy — biometric-or-password
- `LAPolicy.DeviceOwnerAuthentication` (=2): Touch ID OR the account password satisfies presence. Works on Touch-ID-less Intel Macs; symmetric with the Linux ladder. Accepted tradeoff: a known account password passes the gate.
- Tim: "biometric-or-password".

## Prompt timeout — 60s, fail-closed
A bounded wait, one shared value across the three OS (~60s), that denies on expiry (fail-closed). Tim: "sure 60s sounds right."

## Denial reason code
`ApprovalDecision` gains a reason enum — Approved / Denied / Unavailable / Cancelled / TimedOut / Error — each surfaced as its own CLI line. Tim: "yes, and those are good options to start, let's go with them."

## OS dialog text
One reviewed, action-specific English string per command (install/init/remove), held as a single owned resource (not an inline literal); localization deferred. Tim: "Okay."

## Per-OS presence impls are not shared
Three separately authored presence implementations (macOS `objc_msgSend`, Linux `libpam`, Windows Hello); no macOS→Linux link-share like the filesystem. Tim: "Agreed."

## Windows validation — via CI, not a separate spike
Windows presence is built for real in IMPLEMENT and validated by the Windows CI leg: the runner builds it (surfacing any WinRT package-identity problem), a smoke test calls the availability query (returns unavailable on the Hello-less runner, auto-proving the unavailable→deny path), everything above the interface runs against the fake, and the interactive success path is a manual pre-release checklist line (same as macOS). Lean: bind the **Win32** API (a single-file CLI should not need package identity); WinRT only with an early CI smoke-test proving it loads from a single-file exe. Tim: "Is there a way we couold validate this as part of our normal development and run it through the CI?" Tim confirmed the Win32 lean: "Both good, write the contract."

## macOS entitlements — verify, amend only if needed
At IMPLEMENT, call `evaluatePolicy` on the REAL hardened-runtime CI-signed binary (the spike only proved the ad-hoc-signed apphost); if it needs an entitlement, add it to `eng/signing/agentguard.entitlements` via a decision-14 amendment (that file's no-silent-change rule), never silently. Most likely no new key is needed. Tim confirmed: "Both good, write the contract."

## Coverage-exclusion — DEFERRED to the coverage gate
Keep the uncoverable surface to the one `evaluatePolicy`-success line (thin per-OS member, AG0101 precedent); measure the per-OS coverage when the code exists and decide the `[ExcludeFromCodeCoverage]` + AG0032 waiver (issue #37) THEN, against the real numbers. Tim: "We'll make the final decision when we get to the coverage gate and see the numbers."

---

## 2026-08-23 — polkit rework (Linux presence redesigned; supersedes the Linux password/PAM decisions above)

Grounded in `research: polkit presence` (freedesktop polkit reference + man pages + CVE-2024-37408). The driving realization: a masked stdin read in the CLI is *in-band* — whoever launched the process (possibly the AI) controls stdin — so it is not a presence proof. Presence must be an *out-of-band* prompt shown by a channel the calling process cannot answer. macOS LocalAuthentication and Windows Hello already are; Linux now uses polkit.

### Linux uses polkit as the sole out-of-band channel (replaces the direct-libpam design)
Linux presence is a single polkit `CheckAuthorization` call. polkit's session authentication agent shows the dialog and runs PAM in its own process; where the distro wired fingerprint into `/etc/pam.d/polkit-1`, that dialog already offers biometric-or-password (one call, like macOS). polkit's desktop agent is a vetted attention front-end explicitly NOT affected by CVE-2024-37408, whereas a hand-rolled `pam_fprintd` path IS the vulnerable side — so polkit is primary and safer, and there is NO separate hand-rolled fingerprint path. This drops the entire direct-libpam design: the PAM conversation callback, `NativeMemory`, `/etc/pam.d/agent-guard`, the enable/disable-fingerprint commands, `IConsole.ReadPassword`, and (DESIGN to confirm) `IEnvironment.GetUserName`.
Tim: *"Yes, polkit so we have a similar structure to the other OSs and we make better use of trusted system services."*

### One-time root install of the polkit policy; "policy not installed" is fail-closed
A proper `auth_self` presence action must be installed as a root-owned `.policy` file under `/usr/share/polkit-1/actions`; there is no unprivileged runtime registration, and no stock action safely means "prove yourself." So Linux presence needs a one-time privileged install step (folded into the installer/packaging). Without the policy installed, the guard denies (fail-closed) — it does NOT fall back to a weaker channel. This reverses the old "works out of the box, no root" Linux baseline.
Tim: *"Yes, for linux due to technical requirements, this is okay."*

### .NET calls polkit via a pinned managed NuGet
The polkit call is D-Bus. Use `Tmds.DBus.Protocol` (pure-managed, zero transitive deps, single-file/AOT-clean, passes the race-free `pidfd` subject), pinned. This is a recorded reuse-a-primitive-behind-an-owner decision (a new managed dependency). Alternative considered and not taken: P/Invoke into `libsystemd` sd-bus.
Tim: *"Yes, a trusted NuGet package is prefered here. We'll just pin it."*

### Force a fresh human interaction every check; never accept a cached "yes" (all three OS)
The presence check must force a live human interaction on every call and never return a cached/retained authorization. On Linux: the custom action sets `allow_any`/`allow_inactive`/`allow_active` all to `auth_self` (never `yes`, never any `_keep`); pass `AllowUserInteraction`; reject any result that comes back authorized without an actual challenge; defensively check/revoke temporary authorizations. The same "no cached yes" principle applies to macOS (fresh `LAContext` per check) and Windows (fresh Hello request). Residual, out-of-scope risk: a machine admin can override via `/etc/polkit-1/rules.d/`.
Tim: *"Agreed."*

### Rely only on a pre-existing session agent; never create or spawn one; add a rule
The guard uses only an authentication agent already registered by the trusted login session (the DE at login, which holds the single per-session slot so a later AI process cannot displace it). The guard NEVER creates, spawns, or registers an agent — no `pkttyagent`, no internal-agent flag, no registering its own — because that would hand the caller a channel it can answer. No pre-existing agent → deny. Accepted residual risk: in a session where a hostile process has already registered its own agent, polkit routes to it and the guard cannot tell it from the real one (an already-compromised-session scenario, above this tool's lazy-AI threat bar). Add an analyzer rule banning our code from spawning `pkttyagent` or registering a polkit authentication agent, so the weak channel can't be reintroduced.
Tim: *"Yes, add the rule and send it through DESIGN."*

### Resolved 2026-08-24 (sign-off on the polkit DESIGN output)
- **Action id:** `com.4thwaiv.agentguard.presence` (released under 4thwaiv). Confirm at IMPLEMENT: the `4thwaiv` segment starts with a digit, which strict reverse-DNS / D-Bus naming disallows; if polkit rejects it, spell it `com.fourthwaiv.agentguard.presence`. Tim: *"It should be `com.4thwaiv.agentguard.presence` I think we will release this under 4thwaiv."* and *"Okay we'll spell it out — only if we have to."*
- **CLI-contribution mechanism dropped:** the generic platform-command mechanism (`IPlatformServices.CliCommands`, `IPlatformCliCommands`, `IPlatformCliCommand`, the AG0034 wrapper) and the fingerprint enable/disable commands are removed; no OS contributes any CLI command. Tim: *"Agreed we don't need the setup command now."*
- **Presence native interop — a family-scoped AG0101 edit + a paired rule AG0113 (Tim's personal yes; corrected 2026-08-24 after RULE-PHASE):** AG0101's P/Invoke and `Marshal` branches ban ANY P/Invoke/`Marshal` outside the filesystem owner (unconditional — NOT filesystem-member-only), so they are narrowly edited to ALSO exempt the presence-port owners `ILocalAuthentication` (macOS `objc_msgSend`) and `IWindowsUserPresence` (Windows: Hello via the WinRT `UserConsentVerifier` interop — **no flat-Win32 entry point** — plus the `CredUIPromptForWindowsCredentials`+`LogonUser` secure-desktop fallback). AG0101's `File`/`Directory` member ban stays filesystem-owner-only; its shared owner identity (`ContractInterfaces.PlatformFileSystem`, used by AG0020) is NOT mutated. AG0113 confines the presence ports to those owners; the two rules SHARE one P/Invoke/`Marshal` detector. Linux is native-call-free. **Correction:** the earlier "separate rule, AG0101 unchanged" plan was wrong — AG0101's catch is unconditional, so unchanged left the presence ports permanently RED (the RULE-PHASE rule-refute, SOLID+DRY, caught it). #39 folds AG0101+AG0113 into one owner→family map later. Tim: *"I approve"* (the narrow family-scoped AG0101 edit).
- **Subject identity — the triple, for now:** name the calling process to polkit with the classic unix-process triple (pid + start-time + uid, read from `/proc/self` through the owned file reader), no native call on Linux. Required anyway for the polkit on RHEL 8 / older Ubuntu LTS, which reject a pidfd subject; pidfd would be a redundant second path against a race outside the threat model. Upgrading to prefer pidfd where supported (with the triple as fallback) is deferred to **GitHub issue #40**. Tim: *"Triple for now but file an issue to improve this to support both and via a process to provide the modern solution where it is supported."*
- All four sign-off items are now settled; the contract's Linux sections are rewritten from the polkit DESIGN and the adversary panel re-run.

---

## 2026-08-27 — Granted rule suppressions (IMPLEMENT)

The IMPLEMENT worker reached a green build by adding six `[SuppressMessage]` attributes across five sites in the presence code. Each was adjudicated against the live code: each is a place where a contract rule or an unavoidable native-frame constraint mandates a construct that a general analyzer (Microsoft `CA1031`, Sonar `S6640`, Roslynator `RCS1075`) forbids. One of the six is corrected in code rather than granted. The remaining five are approved as the cleanup law's "explicit waiver from the user"; no cryptographic signature exists yet, so each is signed later when the flag-unsigned mechanism lands (tracking issue #46).

Granted (the suppression stays):
1. `LinuxPresenceCheck.Check` — `CA1031`. Fail-closed boundary: any fault becomes `ApprovalReason.Error`, never `Approved`, so the catch is broad; same pattern already approved in `Pipeline.cs`/`Program.cs`.
2. `LocalAuthentication.EvaluateReplyCallback` — `CA1031`. AG0105 mandates a native-callback body of one `catch(Exception)` that never rethrows, so nothing unwinds across the Objective-C block frame; `CA1031` forbids that catch.
3. `LocalAuthentication.ReplyInvokePointer` — `S6640`. AG0104 mandates the callback be reached by `&Method`, legal only in `unsafe`; the only alternative is the marshalled delegate AG0104 exists to forbid.
4. `WindowsUserPresence.TryVerifyWithHelloAsync` — `CA1031`. The broad catch is the "Hello degrades to the password prompt" behavior — any Hello binding failure falls back rather than denying the user.
5. `WindowsUserPresence.AsyncCompletedHandler.Invoke` — `CA1031`. A COM completed handler invoked across the native frame must catch everything; it already completes the awaiting task with a non-verified outcome.

Corrected, not granted:
- `LocalAuthentication.EvaluateReplyCallback` — `RCS1075` (empty catch). The callback is rewritten to complete the awaiting `TaskCompletionSource` with a fail-closed result inside the catch. AG0105 permits a non-empty catch (it requires only catch-all-that-never-rethrows), so this removes the empty-catch suppression and, as a bonus, denies immediately on a callback fault instead of hanging until the gate's 60-second timeout. Only `CA1031` remains at that site (granted, item 2).

REFUTE recording: the contract's `## Decisions` carries a "Granted rule suppressions (settled 2026-08-27)" subsection; acceptance check #12 pins the presence code to exactly these five suppressions and no other; `refute.js`'s Lie-catcher charge was updated to treat a suppression recorded here with Tim's words as the waiver, not a FAIL (signature verification is a later addition).

Tim: *"Record these exceptions and authorized."* — against the enumerated five-item grant list. On the `refute.js` change: *"Make refute look at the decisions. We'll have to update it again later to verify the signature but right now we don't have a signature."*

---

## 2026-08-28 — Native smoke: CI-only compilation, near-zero bound, port honors cancellation

The `PresenceNativeSmokeTests` native smoke drives the REAL presence implementation, so on an interactive Mac it showed the OS auth dialog, which waited for a human and never self-dismissed — it popped repeatedly (including under REFUTE, whose adversaries run the full test command) and it could hang the CI runner. A first attempt to gate it with a custom `[CiOnlyFact]` attribute was correctly REJECTED by our own `NoOsSkipInTestsAnalyzer` (AG0026) — a custom Fact attribute that sets `Skip` from an environment check is exactly the OS-skip dodge that rule bans. The fix has two parts, both settled with Tim:

### The smoke is compiled and run ONLY when the CI workflow opts in, opted-out by default
`tests/AgentGuard.CrossPlatform.Tests.csproj` removes `Presence/PresenceNativeSmokeTests.cs` from compilation unless the dedicated `AgentGuardRunNativeSmoke` MSBuild/env flag is set (`<Compile Remove ... Condition="'$(AgentGuardRunNativeSmoke)' != 'true'" />`); `.github/workflows/ci.yml` sets `AgentGuardRunNativeSmoke: "true"` on the three OS test jobs (macOS/Windows/Linux) and nothing else does, so the smoke compiles and runs on the CI legs and is absent everywhere else — no local build or test run, including an adversary's, can pop the dialog, and there is no filter to remember. This is a build-directive opt-out, not a code-level `Skip`, so AG0026 stays clean; the smoke still runs visibly on CI, so nothing silently stops running.
Correction, made after this first popped during REFUTE (2026-08-28): the gate was briefly keyed on the generic `CI` environment variable, which is WRONG — agent/automation contexts (including the REFUTE subagents on Tim's own interactive machine) set `CI=true`, so the smoke compiled and popped there. A dedicated flag that only ci.yml sets is the fix. Tim: *"Now we need to not showing up on interactive builds."*
Tim: *"IT HAS TO GO THE OTHER WAY!!!!!... We have to have tests opted out locally and only run on the CI when the flag is SET!"*

### The smoke sets a near-zero bound, and the presence port honors the cancellation token
The smoke bounds the real `Check` with a near-zero timeout through the injected clock (`new CancellationTokenSource(NearZeroBound, services.Clock)`, AG0038-clean), currently **1 second** (reducible to ~500 ms later; the floor is a few hundred ms, below which the check is cancelled before the prompt renders and throws instead of returning a clean non-approved result). To make the bound actually dismiss the dialog (not just abandon the wait), the macOS port now HONORS the cancellation token: when the token fires it messages `[LAContext invalidate]`, which cancels the pending evaluation and dismisses the dialog, and LA fires the reply block with a cancel error that maps to a non-approved reason. Honoring a passed-in token is NOT minting a timeout, so AG0107 stays clean — and this also fixes production, where the gate's real 60-second timeout will now dismiss the prompt instead of leaving it up. Proven locally on 2026-08-28: the dialog appeared and auto-dismissed after the bound with no human input; the smoke passed in ~2 s (at the 2 s value) instead of hanging.
Tim: *"THE real smoke should set a near 0 time out not full time out."*, *"Great, let's prove it locally first."*, *"1 second is fine we can reduce to 500ms later."*

### Windows Hello honor-cancellation — deferred to the Windows CI leg
The Windows Hello path (`WindowsUserPresence`) does not yet honor the cancellation token during its await, so the near-zero bound does not cancel a truly-blocking Hello/credential call there. Assessed as lower-risk than macOS: a headless Windows runner has no Hello device (returns DeviceNotPresent fast) and no interactive secure desktop (the credential prompt errors to NoMethod fast), so both paths should complete without hanging. Deferred: verify on the Windows CI leg and add the analogous honor-cancellation only if it actually blocks. Tracked in issue #47.

---

## 2026-08-29 — Coverage-refactor REFUTE closures + sub-task A REFUTE clean, before the first CI push

Folded in before pushing the whole presence workstream to CI for the first time.

### Coverage-refactor REFUTE (Prove-It) closures
- **Two stale interface doc comments corrected.** `ILocalAuthentication` and `IWindowsUserPresence` still claimed their sole implementer was "the only class that binds" the native framework; after the deep-native-ops-seam refactor the ports are fake-testable orchestrators and the raw binding lives one layer below in the native-ops owners (`ObjCRuntime`; `IWindowsHelloNativeOps` + `ICredentialPromptNativeOps`). Both docs now say so.
- **AsyncCompletedHandler nested-field / AG0116-scope gap → issue #51.** `WindowsHelloNativeOps.AsyncCompletedHandler` (the WinRT completed-handler CCW) legitimately holds a callback-delegate field but sits outside AG0116's owner check. Not a defect; a mechanical-guard gap to close. Filed as #51.
- **Acceptance #3 (relocated AG0113/AG0101 flag the pre-refactor flow-port raw calls) — proof state recorded.** Prove-It wanted the RED demonstrated against the literal pre-refactor `LocalAuthentication.cs`/`WindowsUserPresence.cs`. Those files survive in the session scratch backup, but swapping one whole over the refactored tree does not compile (its old shape does not fit the new DI surface), so the diagnostics can't be shown cleanly that way. The invariant IS proven, and permanently: the analyzer fixtures `PresenceNativeInteropOwnerAnalyzerTests.PresencePInvoke_InFlowPort_IsReported` and `Marshal_InFlowPort_IsReported` assert exactly that a raw presence P/Invoke or `Marshal` call left in a flow port is RED, and the IMPLEMENT worker additionally reintroduced a raw `objc_msgSend` into the flow-port orchestrator and captured the AG0113/AG0101 RED before reverting (run wp1ir278u). Accepted as sufficient; the permanent fixtures are the durable, re-runnable proof.

### Sub-task A REFUTE — clean
Five-adversary REFUTE (run we0dgydks): SOLID, laziness-auditor, and Lie-catcher PASS; Prove-It and DRY each raised documentation/hygiene findings, all resolved — the `MessageBuffer`-dispose contract prose corrected (the type is not `IDisposable` in Tmds 0.94.0, verified by reflection), the acceptance-#8 left-uncovered accounting added, the representative polkit subject deduped into `PolkitSubjectFixtures` (Tim: *"CLEAN IT ... I HATE DRY VIOLATIONS WITH A PASSION THAT BURNS"*), and the Option-A/#50 owner-marker split recorded as a formal decision. Measured: macOS 88.37%, Linux 82.58% (both local, off-bus); Windows clears on its CI leg.

Tim: *"get everything in that needs to go in from all pending work so we can see what CI gives us, we may have to go back through the workflow, but let's see how it does."*
