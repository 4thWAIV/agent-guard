# AgentGuard process architecture — replacement brief

Status: Draft for Tim's approval. Recorded on 2026-09-11 at Tim's request. Recording this brief does not approve every proposed sentence, supersede the live issues, authorize production changes, or authorize rerunning completed stages. CLI, server, and viewer are provisional role names, not selected command or interface names.

The consolidated current brief is [conversation.md](conversation.md). This document retains the detailed review background. References below to earlier process-conversation wording mean the version available with `git show b4d3a00:.dev/inprocess/63-appd-1-process/conversation.md`. Full issue replacements are in [issue-revisions.md](issue-revisions.md), and the prepared Claude handoff is [handoff.md](handoff.md).

## Proposed direction

AgentGuard uses the same application in three cooperating roles. The CLI receives requests from a person or automation. One headless server runs per machine and coordinates requests. A separate viewer runs as the user in the graphical session where interaction is needed. Multiple users or sessions can have separate viewers connected to the same server.

The server receives a CLI request and obtains the connecting process's identity from the OS. It identifies the appropriate user and session, then uses an authenticated viewer connection for that session. If no viewer is connected, it requests an OS-mediated launch, waits for an authenticated connection, and delivers the request. Request correlation, launch races, timeouts, and reconnect behavior remain to be designed.

The viewer presents information and collects responses. The server retains responsibility for authorizing operations and, when the key work is implemented, signing. Neither an ordinary CLI response nor a graphical response by itself substitutes for the required presence check.

### Delegated presence and expected viewer identity

Proposed addition from the subsequent discussion: The server may delegate presence checking to the viewer in the requesting user's session. The viewer calls the OS presence mechanism and returns its result over the authenticated connection. The server validates that result before using it to authorize the pending operation.

Reuse the existing planned mutual process-identity mechanism. This is recorded design, not implemented appd code. When the server launched the viewer, the connecting identity must also match the actual viewer process instance returned or resolved through that launch. The PID of launchctl, systemctl, or another launch-request helper is not the viewer PID. PID reuse after exit must not let a different process inherit this trust.

The identity part owns process verification, user/session association, and expected-instance matching. The key/presence part owns delegated-result freshness, operation binding, replay rejection, and authorization. The viewer part supplies the user-facing OS call. Define these boundaries without a second identity subsystem.

A symmetric key or key derivation may authenticate results, but neither is selected. First establish whether the authenticated IPC connection already supplies the necessary protection. Additional keying requires a secure way to establish and deliver a secret; public PID, user, and session values alone cannot provide one. Message authenticity is not an OS presence certificate. Trust in the result also depends on the verified viewer's code and the OS protection of that process.

GROUND examines existing presence code and OS APIs to establish where each platform's check can run, what it returns, and what the server can validate. It must cover CLI-only interactions and the absence of a running server during administrative operations. It reports limitations against the project's threat model rather than claiming cryptography protects a compromised viewer.

The server's lifetime is independent of graphical logins and viewer processes. Closing a viewer does not stop the server. The CLI provides a way to close viewers. The command vocabulary and the exact scope of that close operation remain to be selected.

## Proposed administrative behavior

Stopping the machine-wide server and changing its configuration are machine-wide administrative operations. Requests through the AgentGuard CLI require both a presence check and OS administrative authorization. Direct operations through the OS service manager are governed by the OS; AgentGuard does not intercept them.

The presence check for CLI administrative operations must be callable without a running server. The implementation should reuse the existing presence capability where it meets the requirement. No particular new interface or privilege mechanism is selected by this brief.

Installation establishes protected server binaries, configuration, and OS service definitions. The server's runtime account and privileges require an explicit per-OS design. Administrator permission during installation alone does not settle runtime privileges. Viewer processes run as their users, not as the privileged server.

The server's automatic startup trigger, default enablement, recovery behavior, and start permissions must be reconciled with machine-wide operation. This brief does not silently change login startup to boot startup or give ordinary clients permission to restart an administratively stopped server.

## Interaction mode

The following wording is already approved in [conversation.md](conversation.md), under “Interaction-mode detection”:

> AgentGuard detects whether graphical interaction is available to the requesting client and uses that result to select the default interaction mode. An explicit interaction-mode option takes precedence over detection.

The following scope and unresolved behavior are also recorded there:

> The future terminal/non-interactive switches belong in a separate backlog issue. They should not become implementation scope for the process work.

> Automatic CLI fallback after a viewer failure remains undecided. An error by default with an explicit fallback option is still a candidate.

Proposed wording for the broader interaction requirement: Every graphical view has a CLI alternative. A request without usable graphical interaction can be completed through its CLI interaction, subject to the same authorization and presence requirements. Providing terminal interaction does not authorize unattended approval.

Detection must distinguish a missing graphical session from a viewer that failed to launch or connect. Exact detection criteria, including locked or disconnected sessions and requests originating over SSH, need design and evidence. Switch names and scripting behavior are later work; no backlog ticket for them has been created.

## OS launch mechanisms to establish

These are research candidates, not approved or tested implementations. The evidence and sources are in [design-review-notes.md](design-review-notes.md), under “Machine-wide server exploration”, and [autostart-research.md](autostart-research.md).

- On Windows, a service can obtain the target session's user token and use CreateProcessAsUser to launch an unelevated viewer in that session. WTSQueryUserToken requires LocalSystem and SeTcbPrivilege. Test session selection, returned viewer identity, logout races, and behavior when no usable desktop is available.
- On macOS, register the viewer itself as an on-demand LaunchAgent and request its launch in the authenticated user's graphical launchd domain. Test that the job runs as the intended user, can display the chosen UI, and connects back correctly. An additional permanently running AgentGuard helper is not selected.
- On Linux, a privileged server can address a particular user's systemd manager to start a viewer unit. That mechanism needs the correct graphical environment and does not by itself solve desktops without systemd session integration. Establish a launch mechanism for those desktops as well; do not guess display names or narrow the requirement to integrated desktops.

The live dashboard issue explicitly requires:

> On all three platforms: the dashboard opens when appd starts, the user dismisses it with a button and appd keeps running, a CLI command brings it back, that cycle repeats, and it works on a Wayland desktop with no systemd session integration.

Source: [appd dashboard](https://github.com/4thWAIV/agent-guard/issues/65), read on 2026-09-11. The proposed on-demand viewer changes the startup behavior in that sentence, but does not remove its Wayland coverage. Both must be reconciled in the approved issue update.

No Windows VM was listed by the local Parallels inventory during the earlier research. Cross-session Windows proof needs a suitable host. No live OS launch experiments are authorized or claimed completed by recording this brief.

## Reconcile existing decisions

The following compares the earlier decision record with the proposed architecture. It is review history, not a second current brief. Earlier approvals remain attributable to their original wording in git.

| Existing source wording | Proposed treatment |
|---|---|
| “Not a system service.” — live process issue | Replace with one machine-wide headless server, after approval. |
| “appd owns the window.” — live dashboard issue | Replace process-level window ownership with the separate viewer. Preserve the server's coordination responsibility. |
| “An empty dashboard opens with the process.” — live dashboard issue | Reconcile with launching viewers on demand. Do not carry this startup coupling into the new design by accident. |
| “The Windows autostart entry is a Task Scheduler task with a logon trigger.” — conversation.md | Replace the server's Task Scheduler mechanism with the selected Windows service mechanism. Viewer launching is a separate concern. |
| “The macOS autostart entry is a LaunchAgent plist under `~/Library/LaunchAgents`, and the Linux one is a systemd user unit.” — conversation.md | Rework server installation for machine scope. Evaluate user-session definitions for viewers separately. |
| “The daemon's scope is whatever the platform's own per-user runtime scope already is.” — conversation.md | Replace server scope with machine scope. Reevaluate singleton enforcement, lock locations, discovery, and direct invocation. |
| “The CLI decides whether appd is running by testing the lock.” — conversation.md | Reevaluate against a machine-wide service and its access permissions. No replacement detection mechanism is selected here. |
| “The channel lives where only the user's account can reach it.” — live channel issue | Define how multiple users reach one server while their requests and data remain isolated. A shared endpoint is not automatically permission to access another user's state. |
| “The Windows pipe name carries the user's SID and the Terminal Services session id” — channel conversation.md | Reevaluate endpoint naming for machine-wide service discovery. Do not use a name supplied by a client as authentication. |
| “The three autostart-entry templates ship embedded in the binary.” — conversation.md | Preserve embedded definitions as the agreed approach where definitions remain needed. Reconcile the number and contents with the selected server and viewer mechanisms. |
| “The macOS lock path uses the shared temp root, and Tim approved an exemption from the rule that forbids it.” — conversation.md | Preserve the original approval. Do not transfer the exemption to a different machine-wide path or operation. |
| “No new static classes.” — conversation.md | Carry forward unchanged. |

The original recovery, stop, configuration, logging, and explicit Linux retry wording remains in the process conversation at b4d3a00. The consolidated conversation carries the current behavioral targets. Their machine-wide mechanisms require investigation and design approval.

The existing assembly direction and interface ownership remain the starting constraints. New service, viewer, privilege, and connection interfaces still need actual C# declarations for review. Existing owner-table approvals do not authorize arbitrary new native owners. The sixteen old guardrail proposals and ten DESIGN reuse instructions must be reevaluated rather than accepted or discarded wholesale.

## Preserve requirements beyond the process work

The live dashboard issue states:

> The dashboard is a .NET web server hosted inside appd, rendered by a browser control, with the pages themselves written in React.

The separate viewer requires an explicit decision about where that web server runs and how it is reached. React and the capable browser engine are not proposed for replacement. No new TCP transport or UI packaging dependency is selected here. The distinction between any browser-to-web-server transport and the authenticated CLI/server channel must be explicit.

The live identity issue states:

> Peer credentials on a Unix socket, the client process id on a Windows named pipe, the audit token on macOS XPC. The client never asserts who it is.

Source: [appd identity](https://github.com/4thWAIV/agent-guard/issues/66). Preserve OS-derived identity and mutual verification as requirements. Extend the design to returning viewers and per-user authorization. Reevaluate whether the earlier same-path/hash/signature assumptions are sufficient across a privilege boundary; this brief does not certify them as sufficient or select replacements.

The XPC-from-.NET feasibility requirement remains in [appd channel](https://github.com/4thWAIV/agent-guard/issues/64). Cross-user communication changes its execution context and must be included in that investigation. No native helper or platform-specific build target is approved by implication.

The key remains part of the later key/presence work. The existing key conversation records: “Every user working on a project mints their own key, and all the public keys are committed.” Preserve this attribution requirement; a machine-wide server does not imply one shared machine key. Storage permissions, enrollment after machine-wide installation, unlocked-state scope, logout behavior, and which viewer may approve which request need concrete design. Preserve the other recorded key requirements in their existing work folder while reconciling installation assumptions.

## Decisions and investigations before a contract

Tim reviews the cleaned architecture wording and the replacements identified above. The remaining lasting choices include key enrollment and unlocked-state scope, viewer lifecycle, request completion and recovery, protected installation locations, runtime privileges, automatic server startup, administrative start permissions, UI hosting, and the scope of CLI viewer closure. These are not requests to solve OS API facts; investigation first establishes the viable mechanisms.

The agent establishes OS launch and graphical-availability capabilities. Unknown API behavior is investigation work, not a preference question for Tim. If an experiment requires a different mechanism or exposes an unsupported requirement, present the result and the concrete choice before proceeding.

Deliver the communication transport before the process work. Exercise connection establishment, message exchange, connection closure, and OS peer information with both endpoints in one process through the actual transport. The process delivery consumes that implementation and adds installed-service integration. Product identity verification, viewer launching, and presence-result validation remain in their respective deliveries. The proposed parent, process, and channel issues carry this order.

## Rebuild the interim work products

The work remains in this folder. Preserve the original GROUND and DESIGN results and research evidence as history. After approval, distinguish superseded records from active inputs without hand-editing old panel outputs into new verdicts.

Update the parent and affected child issues only after their replacement wording is approved. Update their local issue copies and the approved conversation record consistently. Keep provisional vocabulary visibly provisional.

Rerun GROUND and DESIGN only with Tim's explicit authorization and an approved brief. GROUND establishes live-code facts and the reuse ledger for the changed capabilities. DESIGN produces the new approach, interfaces for review, reuse instructions, and proposed guardrails. Existing evidence may be reused where it remains applicable, but previous verdicts are not verdicts on the new architecture.

Regenerate the interface review from the new design. Reuse render-review.mjs and review-view.html where suitable. Replace the model content rather than preserving obsolete per-user-server groupings. Show C# declarations, callers, ownership, and native dependencies. Reassess each guardrail with its violating and compliant examples. Regenerate appd-review.html and update design-challenges.md, design-review-notes.md, and handoff.md to match the active work.

Draft the contract after the new design and decisions are ready. Run the hidden-decision scan before locking it. Only then proceed through approved rules, signature review, acceptance tests, implementation, and verification.

## Current execution boundary

Two workflow stages completed for the old architecture: GROUND and DESIGN. There is no contract in this work folder. The approved preparation task creates and updates local review documents only. It does not publish GitHub changes, approve the proposed architecture text, implement code, regenerate the visual, or rerun stages. No commit, staging, push, or PR is part of this work.
