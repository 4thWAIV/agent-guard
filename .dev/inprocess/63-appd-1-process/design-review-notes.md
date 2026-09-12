# Design review preparation

## Machine-wide server exploration — 2026-09-10

This exploration does not replace the live issues' per-user architecture. Names, interfaces, and launch definitions remain unapproved. Proposed professional decision wording for later approval: Machine-wide server stop and configuration operations require both a presence check and operating-system administrative authorization. The CLI separately supports closing viewers. This wording has not yet been approved as written.

The macOS candidate registers the viewer itself as an on-demand Aqua LaunchAgent. The machine-wide daemon asks launchd to start that registered job in the authenticated user's gui/<uid> domain, through kickstart without -k. The job launches the viewer as that user; the viewer connects back to the server. Registration does not require the viewer process to stay running. This uses launchd as the session launcher rather than directly inheriting the system daemon's context. The installed launchctl manual documents gui domains, bootstrap, and kickstart. Apple's archived TN2083 describes the daemon/agent split and why an ordinary daemon child is in the wrong GUI context. Current runtime behavior and application packaging must be tested before selecting this implementation.

The Linux candidate registers an on-demand viewer unit in the user's systemd manager. A privileged server can address that local user's manager using the documented --user --machine=<user>@.host mechanism and request the unit start. The unit launches the viewer as the user. Its graphical environment must already have been imported by the desktop session, or supplied by a defined desktop integration mechanism. UID and PID alone do not supply DISPLAY, XAUTHORITY, or WAYLAND_DISPLAY. The manager is account-scoped, so this mechanism is not proof of arbitrary simultaneous graphical-session targeting for one account. systemd's documented desktop integration supports one graphical session per user. No custom always-running session helper is inherent in this candidate; the OS user manager is already running. Desktops without the required integration need an explicit supported mechanism rather than guessed display values.

Both candidates require an existing graphical login session, authenticate the returning viewer, and keep UI unelevated. Neither was installed or exercised during this research. They demonstrate documented launch facilities and an implementable direction, not completed cross-platform acceptance proof.

Sources:
- Installed macOS launchctl manual: gui/<uid>, bootstrap, kickstart, and asuser.
- https://developer.apple.com/library/archive/technotes/tn2083/_index.html
- https://raw.githubusercontent.com/systemd/systemd/main/man/user-system-options.xml
- https://systemd.io/DESKTOP_ENVIRONMENTS/
- https://dbus.freedesktop.org/doc/dbus-update-activation-environment.1.html

This is a working note, not an approved interface design or contract. Tim requested a comparison of autostart behavior across the three operating systems and an efficient visual presentation of the interface layers and proposed rules. The CLI stop command is approved and recorded in conversation.md.

## Proposed review presentation

One navigable layer diagram would show the CLI and dashboard at the top, the service interfaces they call, the middle-layer implementations, the OS abstraction interfaces, and the native wrappers beneath each OS implementation. The embedded-template reader and private-directory operation would appear under their actual callers rather than in a separate document.

Selecting an interface would show its actual C# declaration, callers, dependencies, and the current-versus-proposed difference. Existing declarations would come from the working tree. Proposed declarations would be marked unapproved. Unknown signatures would remain explicitly unwritten rather than reconstructed from prose. Comparing alternative groupings would retain the same operations and show which callers and owners change.

Selecting a proposed rule would show its full recorded wording, the exact interface or class it governs, one violating example, one compliant example, and how it is enforced. The sixteen entries in design-output.json are the starting proposals, not an approved or necessarily complete rule set for the expanded start/stop/configuration requirements. No example would be described as passing or failing an analyzer until that analyzer exists and has been exercised.

Selection in the visual would not constitute approval. Tim's explicit decision in conversation would be recorded before the corresponding proposal is marked approved. The presentation would identify its source revision and pending working-tree changes. Building an automatic regeneration system is a separate reusable-process decision; the first review presentation must not claim automatic freshness it does not have.

## Autostart comparison preparation

The comparison must include normal login, explicit start, explicit stop, disabling future autostart, re-enabling it, crashes, restart exhaustion, battery operation, session logout, and configuration changes while appd is running. Logging requires a location, rotation/retention behavior, and a common user-facing way to read it before any on-disk format is proposed as settled.

The local launchd.plist manual says KeepAlive implies RunAtLoad, including SuccessfulExit=false. Consequently changing RunAtLoad alone to false is not enough to disable login startup while retaining that restart setting. A macOS proposal must resolve this interaction explicitly. It must also distinguish stopping a running daemon from disabling future automatic starts.

Windows ExecutionTimeLimit=PT0S permits indefinite execution. IRunningTask.Stop stops one task instance. Selection of the instance corresponding to the caller's session remains part of the Windows mechanism design.

Sources:
- Local `man 5 launchd.plist`.
- https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-executiontimelimit-settingstype-element
- https://learn.microsoft.com/en-us/windows/win32/api/taskschd/nf-taskschd-irunningtask-stop

## Time approval

Tim approved the estimated 25–35 minutes for the autostart comparison and first usable interface-and-rule visual. This approval covers the review presentation, not approval of its proposed interfaces, policies, or guardrails. No production interface has been added.

## Review draft

The interactive review shows the three proposed top-service groupings, the two platform-container groupings, selectable call paths, existing declarations copied from source, and draft C# declarations. Each of the sixteen rules is copied verbatim from the recorded DESIGN and paired with illustrative examples and unresolved review concerns. The examples are not compiled analyzer tests. Selection and discussion buttons do not record approvals.

The common recovery-policy candidate uses no automatic crash retries and starts appd on the next client request through the OS service manager. This is recommended within the comparison because it gives the same recovery behavior across the three managers. It trades unattended recovery for uniform behavior. The comparison also includes each manager's native retry settings and their unequal retry limits. Neither policy is approved.

Windows session-targeted start, selection of a task instance to stop, Linux desktop connection/lifecycle, logging and retention, and synchronous setup integration remain unresolved. These are displayed in the review rather than treated as settled by its interface declarations.

The generated view is a source-stamped snapshot, not a live view. Its renderer rereads the listed sources when explicitly run. It does not implement a watcher or a reusable design-approval system.

## Presentation checks

During later diagnostics, the shared Chrome window was 1344 pixels wide while the automation-controlled tab remained at a 1024-pixel viewport after responsive-layout testing. The fragment filled its 992-pixel iframe, so measurements within the emulated viewport did not reveal the blank area in the physical browser window. Reloading retained the mismatch. A fresh tab in the same browser had a 1344-pixel viewport and a 1312-pixel iframe. The affected test tab was replaced with that fresh tab. This explains the later testing-induced mismatch; it does not establish the cause of the original user-reported flickering and shrinking. That original cause remains unresolved. Further responsive-size testing must use an isolated test window, not the shared review tab.

The grouping controls change the displayed declarations, call-path controls change the highlighted dependencies, and the rule selector displays the recorded rule wording. The discussion action was exercised with a captured host callback, not by submitting a real message. The final layout has no horizontal overflow at 736-pixel and 360-pixel browser widths in light and dark appearances. Existing source declarations and the raw rule text are embedded by the renderer. None of these checks exercises an OS adapter, a daemon, or a proposed analyzer.

## Native declaration review

The native area now presents proposed C# declarations for process objects and start options, FileStream lifetime, Mutex lifetime, Task Scheduler COM objects, assembly resource streams, shared text readers and their read-stream bridge, and Windows account identity. Each is a separate selectable node with its callers and API references. None of these declarations has been added to a production project or approved as a design.

The declarations reuse the selected BCL and OS primitives behind owned interfaces. Process and resource text readers share ITextReader. The resource service display derives its declaration from the native resource node rather than maintaining a second copy. The factories and disposal members are shown, not implied by comments. C# declaration consistency was checked in an isolated scratch compilation against the installed .NET 10 reference assemblies, with nullable checking and warnings treated as errors; the compiler exited 0. This does not establish adapter behavior, COM ABI correctness, analyzer acceptance, or cross-OS integration.

Mutex release is thread-affine. The daemon lifetime still needs a mechanism that keeps acquisition and release on the owning thread; the generic disposable lease and async await sketch do not establish it. Windows stop has a proposed instance selection using EnginePID and the process SessionId, which still needs native multi-session and process-exit-race proof. Preserving an Assembly-like stream interface requires a bridge for StreamReader's raw Stream constructor; its owned conversion requires review. A composed text-resource service would change that interface design instead. No one of these unresolved choices is silently selected by the declarations.

## Behavior refinement for approval

### Follow-up question preparation

The Linux start-limit behavior is ready for a human decision. Proposed question: After Linux stops retrying because appd keeps failing, should an explicit agentguard start clear the failure counter and try again immediately, while automatic client requests leave that counter intact? The proposed mechanism is a unit-scoped systemctl --user reset-failed agentguard-appd.service followed by the normal canonical start for an explicit retry after exhaustion. Automatic client requests would report the failure and the explicit retry command while the manager still refuses starts. This is recommended because client traffic would not defeat crash-loop protection. Clearing the counter for every client request is the alternative; it allows demand recovery immediately but lets repeated requests continually reset the limit. No new retry behavior is approved here. This explicit-versus-client distinction would require review of the service interface and must not be hidden in caller-specific wiring.

The other three previously listed items are engineering investigations, not currently answerable human choices. Windows needs evidence that automatic task recovery preserves the failed instance's session while another instance is running. macOS needs a concrete mechanism that disables login starts without disabling recovery for a manually started daemon or stopping the current one; changing RunAtLoad alone is insufficient with SuccessfulExit-based KeepAlive. Pending settings need a mechanism that meets the already approved next-start behavior even for OS-initiated starts, not just CLI starts. No alternative that weakens those approved behaviors is proposed. Any new mechanism required by these investigations must be presented for approval once it is concrete.

Subsequent approval: Tim approved the package presented in conversation, including five completed daemon runs rather than five login sessions. The exact presented wording is recorded under “Approved behavior, recovery, configuration, and logging package” in conversation.md. That record is authoritative for the approved package. The draft below remains historical proposal text; details that were not presented are not approved by association. The implementation questions explicitly left unresolved remain open. The interactive review snapshot has not been regenerated for this approval.

The following is proposed professional wording for review, not a decision record. It incorporates the latest discussion without copying conversational text. No production change is authorized by this note.

Proposed stop wording: The stop command asks the OS service manager to stop appd in the caller's daemon scope. It does not change autostart settings or prevent subsequent explicit or client-requested starts. An intentional stop does not trigger crash recovery.

Proposed autostart wording: Disabling autostart disables login-triggered starts only. It does not stop a running appd, prevent explicit or client-requested starts, or disable crash recovery for an appd that is subsequently started.

Restart options use the existing OS managers. Windows RestartOnFailure permits intervals from one minute through 31 days and counts from 1 through 255. Linux RestartSec controls the post-failure delay; StartLimitIntervalSec and StartLimitBurst limit starts within an interval, including manual starts. macOS KeepAlive with SuccessfulExit=false requests failure recovery; ThrottleInterval limits launch frequency rather than imposing a full delay after every failure. No retry-count setting was identified in the launchd plist reference. Identical bounded retries across all three need an additional recovery mechanism and cannot be promised by these template fields alone.

Recommended package for discussion: Use native failure recovery with a 60-second Windows/Linux restart delay and a 60-second macOS launch throttle. Windows allows three restart attempts. Linux permits four starts in ten minutes, including the initial start. macOS continues throttled retries without a retry-count cap. The reason is to retain OS-owned recovery. These limits are intentionally not described as equivalent. Linux demand-start handling after rate-limit exhaustion still needs an explicit resolution. Windows restart targeting across concurrent sessions still requires native proof. macOS disabling login starts while retaining recovery still requires a concrete registration mechanism; changing RunAtLoad alone cannot provide it.

Proposed settings behavior: Save valid settings immediately. Login enablement affects future logins without starting or stopping the current daemon. Restart-policy and logging changes take effect at the next daemon start. Report pending application explicitly. Configuration changes never silently restart appd. The per-OS implementation must establish when the manager loads pending definitions before this is treated as executable design.

Proposed logging behavior: Store logs under ~/.agentguard/logs/appd for each user. Treat a log session as one daemon process run, not an OS login session. Each run has a uniquely named folder containing UTC start time and a collision-resistant run identifier. Record OS session identity as metadata where relevant. Keep five completed run folders across the user account, plus all active run folders. Roll a text log at 10 MiB or the first write after 24 hours, whichever comes first, retaining five files per run including the current file. This gives a 50 MiB file-content budget per run and a 250 MiB budget for completed runs, plus 50 MiB per active run. Enforcing byte limits requires bounded records. Cleanup must distinguish dead runs from active runs and coordinate across simultaneous Windows sessions. These are requirements for the logging design, not a selected ownership/locking implementation.

Proposed reading behavior: Files are readable as UTF-8 text. A new agentguard logs command reads the most recent run for the caller's daemon scope; --follow follows that scope across file rotation and replacement daemon runs. The command name and options are unapproved. OS launch failures before appd can open its log still need service-manager diagnostics through doctor; a daemon-owned file cannot contain failures from code that never ran. Retaining only five completed runs can discard the original failure during a crash loop; that is an explicit retention tradeoff.

Sources consulted for this refinement:
- https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-restarttype-complextype
- https://raw.githubusercontent.com/systemd/systemd/main/man/systemd.service.xml
- https://raw.githubusercontent.com/systemd/systemd/main/man/systemd.unit.xml
- https://raw.githubusercontent.com/apple-oss-distributions/launchd/main/man/launchd.plist.5
