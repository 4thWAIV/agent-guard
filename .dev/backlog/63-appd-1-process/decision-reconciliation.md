# Decision reconciliation for the machine-wide server

Status: Historical comparison for review. The current consolidated brief is conversation.md. In the tables below, references to the process conversation mean its earlier version, available with `git show b4d3a00:.dev/inprocess/63-appd-1-process/conversation.md`. Related work-folder records remain at their named paths. The full current and replacement issue text is in issue-revisions.md. Quotations below are exact source excerpts; proposed dispositions are not past approvals.

## Approval needed before GROUND

Approve the replacement architecture brief and the process investigation scope. Approve which old statements the replacement issue bodies supersede. Then synchronize the live issues and local decision records before authorizing the GROUND rerun. Mechanism choices the investigation is meant to resolve remain open; no panel receives an instruction to silently choose them.

The new architecture, delegated presence, and expected-process matching are captured as cleaned proposals. The conversation has established their direction, but the final combined wording and cross-spec allocation have not been approved as written. Recording or preparing this package is not that approval.

## Process decisions

The source for each quoted entry in this section is conversation.md unless a live issue is named.

| Recorded wording | Proposed disposition |
|---|---|
| “The CLI provides a `stop` command.” | Retain the operation. Reconcile its machine-wide scope and administrative authorization. Final vocabulary remains a separate discussion. |
| “It checks whether appd is running and, if needed, asks the OS service manager to launch `agentguard daemon`.” | Retain OS-mediated canonical starting. Resolve ordinary-client start permissions after a machine-wide administrative stop. |
| “The daemon verb is `daemon`.” | Preserve the historical name until Tim approves vocabulary changes. Provisional roles do not rename commands. |
| “The Windows autostart entry is a Task Scheduler task with a logon trigger.” | Replace for the server with an approved system-service definition. Do not transfer Task Scheduler behavior to Service Control Manager without evidence. |
| “The macOS autostart entry is a LaunchAgent plist under `~/Library/LaunchAgents`, and the Linux one is a systemd user unit.” | Replace server placement with machine scope. Evaluate user-session definitions for viewers rather than discarding them as irrelevant. |
| “`agentguard install` writes the autostart entry.” | Preserve installation ownership, but revise protected locations and machine-wide registration. Per-user installation paths are not suitable defaults for a privileged executable. |
| “The CLI decides whether appd is running by testing the lock.” | Reevaluate discovery and liveness with machine-wide access permissions. No new probe is selected by this package. |
| “appd holds a single-instance lock for as long as it runs, and exits immediately if it cannot acquire it.” | Retain the single-instance outcome. Reconcile the exact lock ownership, probe, and direct-invocation handling with machine scope. |
| “Windows uses a `Local\` named mutex” | Historical per-session primitive. It does not enforce one process per machine. Select a replacement only after native ownership and security review. |
| “Linux and macOS use an exclusive lock on a file in the user's own runtime directory” | Historical per-account path. Do not turn it into a machine-wide path by changing a literal without approval. |
| “A named mutex is not used on Linux or macOS.” | Preserve the earlier evidence and decision. A scope change does not itself approve replacing the primitive. GROUND must distinguish observed behavior from old explanatory claims. |
| “The daemon's scope is whatever the platform's own per-user runtime scope already is.” | Supersede with one machine-wide headless server upon approval. Viewer scope remains user/session-specific. |
| “The macOS lock path uses the shared temp root, and Tim approved an exemption from the rule that forbids it.” | Keep the approval attached to its original path and call. Do not reuse the exemption for a new mechanism or suppress a new diagnostic by association. |
| “The CLI gains a `status` verb that reports state, including whether appd is running.” | Retain status behavior. Reconcile machine-wide visibility; do not invent renamed vocabulary. |
| “There is no fixed list of commands that check whether appd is running.” | Retain capability-driven callers, subject to the new administrative start policy. |
| “A `desktop` verb that raises the dashboard is future work, not part of this issue.” | Preserve the process/viewer delivery boundary. Viewer-launch experiments establish feasibility, not production UI delivery. |
| “Two new owners are added to the analyzer's owned-primitive table.” | Preserve exact existing owner authorizations. Reevaluate which are still needed; additional native owners and analyzer changes require approval. |
| “The three autostart-entry identifiers follow the naming the repo already uses.” | Reconcile each identifier with the new registration type and scope. Windows `appd-<user SID>` is an approved per-user task name, not an approved machine-wide service name. |
| “`agentguard daemon` does not ask for the user's presence before it runs.” | Preserve unattended server startup. Distinguish a presence-gated CLI administrative request from the OS starting or recovering the service. |
| “Every failure a user sees says what went wrong, why, and how to fix it, and `guard doctor` can diagnose it.” | Retain unchanged as required diagnostic behavior. |
| “The three autostart-entry templates ship embedded in the binary.” | Preserve embedded definitions as the agreed approach where definitions remain needed. Reconcile count and type; do not silently add runtime string composition. |
| “The thing that makes appd start at login is called the autostart entry.” | Preserve the existing vocabulary historically. Tim will define the new vocabulary; do not force a login-specific term onto system-service and viewer definitions. |
| “No new static classes.” | Carry forward unchanged. |

The existing assembly-placement ruling remains the starting architecture constraint. Internal OS implementations remain behind the approved abstraction and composition direction unless Tim changes it. The number and grouping of new members were never approved. The old visual is not an interface approval record.

## Behavior package

Source: the process conversation at b4d3a00, “Approved behavior, recovery, configuration, and logging package”. The full original text remains in git.

| Exact recorded sentence | Proposed disposition |
|---|---|
| “The stop command asks the OS service manager to stop appd in the caller’s daemon scope.” | Replace caller-local scope with machine-wide administration. Presence and OS authorization apply to the CLI request; direct OS management remains the OS's responsibility. |
| “An intentional stop does not trigger crash recovery.” | Retain unchanged. |
| “Disabling autostart disables login-triggered starts only.” | Reconcile with the server's new automatic-start trigger. Do not assume boot startup or per-user viewer autostart. |
| “Windows waits 60 seconds and attempts three restarts.” | Preserve as the approved recovery intent pending an explicit Service Control Manager mapping or revision. |
| “Linux waits 60 seconds and permits four starts within ten minutes, including the initial start.” | Preserve the approved values pending machine-wide lifecycle review. |
| “macOS throttles launches to once per 60 seconds and continues retrying without a count limit.” | Preserve the approved values pending machine-wide lifecycle review. |
| “You run `agentguard start`: clear the exhausted counter and request another start.” | Preserve the explicit-versus-automatic retry distinction, with the machine-wide permission check resolved before use. |
| “A client needs appd while Linux still refuses starts: report the failure and tell the user how to retry.” | Retain without introducing automatic resets from repeated client requests. |
| “Save valid changes immediately.” | Retain desired behavior; decide what each manager can activate without interruption. |
| “Restart-policy and logging changes apply at the next daemon start.” | Preserve as desired behavior. Investigate platform feasibility rather than silently weakening it. |
| “Never restart appd merely because someone changes a setting.” | Retain unchanged. |
| “User-owned root: `~/.agentguard/logs/appd/`.” | Reconcile machine-wide operational logs with user-specific request logs and permissions. No replacement path is selected. |
| “Keep the latest **five completed runs, plus all currently active runs**.” | Preserve the approved retention intent. Define which process runs the new logs represent; do not silently count server and viewer runs together. |
| “Rotate each file at **10 MiB or 24 hours**, whichever comes first.” | Preserve the approved limits unless Tim changes them. |
| “Keep **five files per run**, including the current file.” | Preserve the approved limit. |

The approved UTF-8 logs and follow behavior remain in the decision record. Machine-wide visibility and multi-user isolation require review before implementing the reader. Viewer shutdown, crash recovery, and logging are not automatically governed by the server's policy.

## Channel, dashboard, identity, and key requirements

| Exact source wording | Proposed disposition |
|---|---|
| “The Windows pipe name carries the user's SID and the Terminal Services session id” — appd-2-channel/conversation.md | Reevaluate server endpoint discovery for machine scope. Preserve OS-derived peer identity regardless of naming. |
| “The Windows pipe is created with a security descriptor granting only the owning SID, and with `PipeOptions.FirstPipeInstance`.” — channel conversation | Reconcile access for multiple users to one server. Do not silently broaden ACLs or drop first-instance protection. |
| “The UI stack is fixed and not open to substitution.” — live dashboard issue | Retain. Only the process hosting the .NET web server is open; React and the capable browser engine are not replacement candidates. |
| “appd owns the window.” — live dashboard issue | Replace with a separate viewer process while the server coordinates requests. |
| “An empty dashboard opens with the process.” — live dashboard issue | Replace startup coupling with on-demand viewer behavior after approval. |
| “The UI is a .NET web server, a capable browser control, and React. No substitutions.” — live dashboard issue | Retain unchanged. |
| “and it works on a Wayland desktop with no systemd session integration.” — live dashboard issue | Retain coverage. A user-unit launch alone does not prove this requirement. |
| “The client never asserts who it is.” — live identity issue | Retain and apply to viewer connections as well. |
| “Same path as the running process.” — live identity issue | Reevaluate the sufficiency of this rung under the changed privilege boundary. Do not approve it as secure solely from the old prose. |
| “appd loads the key on the first request that needs it, not at startup.” — key conversation | Retain lazy loading. Reconcile the older tamper-check paragraph's startup wording explicitly. |
| “The key pair is minted by `agentguard install`, and the private key is protected by the guard itself rather than by an OS secure store.” — key conversation | Preserve key protection direction. Reconcile per-user minting/enrollment with machine-wide installation; do not silently switch to an OS store or one machine key. |
| “Both key directories are hard-blocked from the AI, for reading and for writing.” — key conversation | Retain unchanged. |
| “appd holds the private key in memory and checks it has not changed.” — key conversation | Retain tamper detection and signing from held material, with lazy loading and per-user state defined. |
| “Every user working on a project mints their own key, and all the public keys are committed.” — key conversation | Retain per-user attribution; a shared server is not a shared signing identity. |
| “There are two categories of signature, and they may eventually use different keys.” — key conversation | Preserve the original one-key-first decision and later split possibility without designing it here. |
| “The signer is identified in the committed key record, and the identity is captured when the key is minted rather than looked up when it is verified.” — key conversation | Retain. Do not use live external identity lookup for old signature verification. |
| “A key is retired rather than removed, and retirement pins the signatures it stays good for.” — key conversation | Retain retirement semantics. Do not substitute timestamps or deletion. |

The full dashboard requirements for build-derived state, age/staleness, decision presentation, communication filters, and the open-item register remain in the proposed dashboard issue. The full existing identity and key research remains visible in the issue comparison, with claims requiring revalidation identified rather than silently endorsed.

## New wording offered for approval

The complete architecture wording is in replacement-brief.md. The additions not previously recorded as approved are the machine-wide server and separate viewer, machine-wide administrative controls, CLI viewer closure, CLI alternatives to views, delegated presence, launched-process-instance matching, and the cross-spec allocation of those responsibilities.

The current conversation brief places the communication transport first, independently testable with both endpoints in one process. The process scope is lifecycle and diagnostic functionality, integration of that channel, and feasibility investigation. Viewer, product-identity verification, and signing remain in their respective parts. The full replacement issue wording remains for review before publication.

## Unresolved by design

GROUND should establish facts about launch, graphical availability, peer/process-instance identity, presence APIs, native ownership, existing code, and service-manager capability. Tim decides remaining mechanisms after evidence is available. The package does not select a symmetric key, KDF, result format, new dependency, runtime account, storage path, UI-host process, request timeout, or automatic viewer-failure fallback.

The earlier approved wording remains in git and the related work folders. Review the consolidated conversation.md and update live issues and local copies after approval, before running the new GROUND. Historical approval does not constitute approval of the rewritten brief.
