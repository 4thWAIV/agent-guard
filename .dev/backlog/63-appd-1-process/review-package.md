# Appd replacement review package

Status: Paused in backlog after local specification cleanup. No issue changes were published and no new workflow stages were run. Recheck git for the current commit and working-tree state. Static-class evaluation is the next intended work, followed by the channel before resuming process delivery.

Resume with the independently testable communication transport, then the appd process that consumes it. The current process brief is conversation.md; the full restart context is handoff.md. Channel planning is now in `.dev/inprocess/64-appd-2-channel/`; its brief includes replaceable message structure and verification interface requirements that must be reconciled into the replacement issue wording before publication.

## Read and approve

1. Read [conversation.md](conversation.md) for the consolidated current brief. Approve this wording before GROUND.
2. Use [replacement-brief.md](replacement-brief.md) and [decision-reconciliation.md](decision-reconciliation.md) when reviewing the detailed rationale or earlier decisions. The earlier process conversation is preserved in git at b4d3a00.
3. Read [issue-revisions.md](issue-revisions.md) for the full current and proposed titles and bodies of the parent and five delivery issues. Approve the exact proposed wording before publication.
4. Use [handoff.md](handoff.md) for Claude only after approved issue text and decision supersessions are synchronized and Tim authorizes the GROUND rerun.

The source issue bodies were fetched from GitHub for this package and carry their source updatedAt values. Re-fetch before publication. If a live body changed, reconcile the difference before replacing it; do not overwrite newer decisions with this snapshot.

## Proposed delivery boundaries

| Delivery | Proposed result |
|---|---|
| Channel, first | Independently usable local transport for connecting, sending and receiving messages, closing connections, and obtaining OS peer information. Both endpoints can be tested in one process through the actual transport. |
| Process, second | One administratively installed, discoverable, startable, stoppable, recoverable machine-wide headless server with diagnostics and integration of the preceding channel. Later viewer-launch and presence capabilities are investigated, not implemented here. |
| Dashboard/viewer | On-demand graphical interaction in the requesting session and CLI alternatives, using the fixed UI stack. Before security is complete, demonstrations do not authorize protected operations. |
| Identity | Mutual AgentGuard identity, OS-derived user/session association, and matching a launched viewer to the exact expected process instance. |
| Key and presence | Per-user key lifecycle, delegated OS presence where needed, fresh operation-bound result validation, and signing for the correct user. |

These boundaries are proposed wording, not a claim that the new architecture is approved. Preserving five parts does not settle every dependency. If safe viewer-launch demonstration requires identity first, the issue order must change by approval before implementation.

## Decisions not hidden in the package

Final CLI vocabulary remains open. The package selects no service identifiers, protected paths, runtime accounts, new interface signatures, crypto algorithms, KDFs, result formats, or viewer-launch timeout.

Automatic server startup must be reconciled with machine scope; boot startup is not silently substituted for login startup. Permission to restart an administratively stopped server remains explicit. Recovery values, configuration timing, and log retention from the prior package are preserved as approved historical requirements pending their machine-wide mapping or revision.

The existing per-user key ownership and attribution decisions are retained. New-user enrollment after machine installation and per-user unlocked-state lifetime still need design. The server's privilege does not authorize it to confuse one user's request with another user's approval.

The viewer's OS presence result is trusted through verified code and authenticated, request-bound communication. A MAC alone is not proof that a presence API ran. An additional keying scheme is only a candidate. OS protection limits and process-instance lifetime remain investigation work.

The dashboard's .NET web server, capable browser engine, and React stack remain required. Its hosting process needs a decision. Linux Wayland without systemd session integration remains required; no supported-desktop list is narrowed by the proposed user-unit launch mechanism.

The CLI alternative to each graphical view is included in the proposed viewer scope. Automatic fallback after viewer failure is undecided. Terminal-only and non-interactive switches remain later work with no ticket created in this preparation.

## Later process GROUND

GROUND establishes the existing code owners, reuse ledger, affected surfaces, native service lifecycle, session-launch feasibility, graphical-availability detection, and presence/identity constraints. It distinguishes experiments from documentation and reports unavailable environments. It does not silently decide the open mechanisms or build the viewer, protocol, or signing feature.

The workflow's existing launch preconditions remain in force. This review package does not create an exception for unanswered choices. Before launching, establish that the approved investigation brief satisfies those preconditions or bring the exact conflict to Tim.

## Publication and launch remain separate

After Tim approves the proposed text and authorizes publication, update the existing GitHub issues, their local issue.md copies, and explicit supersession notices in the relevant conversation records. Preserve the prior approved wording as history. Do not create a second work-item folder or rewrite the original raw stage outputs.

After synchronization, prepare the channel in its existing work folder before resuming process delivery. Recheck the inputs and obtain Tim's explicit authorization for the relevant workflow. The process GROUND rerun remains later work. Preserve the original output files before a tool would overwrite them. No DESIGN rerun, implementation, commit, staging, push, or PR follows automatically.

## Preserved artifacts

The original ground-output.json and design-output.json remain unchanged. The model, renderer, HTML visual, and native review proposals remain unchanged and are marked as previous-architecture artifacts in the handoff and review notes. Rebuilding them belongs after the new DESIGN output, not before GROUND.

The handoff contains current review-stage instructions. conversation.md has been rewritten as one current brief, with superseded mechanisms and correction history removed. Earlier wording remains recoverable with `git show b4d3a00:.dev/inprocess/63-appd-1-process/conversation.md`. Historical design challenges remain intact beneath a status notice.
