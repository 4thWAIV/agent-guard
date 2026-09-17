# Handoff — appd process paused after specification cleanup

## Current stop point

The appd process work is paused in `.dev/backlog/63-appd-1-process/` at Tim's request. The next intended work is static-class evaluation, followed by the channel before resuming the process delivery. The live issues still describe the per-user server and are not superseded by these unpublished drafts. Recheck git for the current commit and working-tree state.

Read [conversation.md](conversation.md) for the current brief. The next delivery is the communication transport, independently testable with both endpoints in one process. The process delivery then consumes it. Product identity verification, viewer launching, and presence-result validation remain separate deliveries.

The channel is now being planned in `.dev/inprocess/64-appd-2-channel/`; its conversation.md adds replaceable message structure and identity/verification interface requirements. Tim and Codex own the design. Claude handles authorized investigation and workflow execution, including GROUND, prior-art discovery, and the hidden-decision scan, returning findings to the design discussion. Reconcile and approve the replacement issue text before publication and GROUND. The process work remains paused. Use [review-package.md](review-package.md) for its remaining review steps.

## Read the rails first

Read these files completely, in this order:

1. `.agents/skills/rails-read-me/SKILL.md`.
2. `.agents/skills/rails-run-a-workflow/SKILL.md`.
3. `.agents/skills/rails-decisions/SKILL.md`.
4. `.agents/skills/rails-real-work/SKILL.md`.

Read `rails-explorer` for the investigation method and `rails-dry-code` for the reuse ledger. Read the applicable communication skills and `.dev/reference/best-practices-guide.md`. The rails remain the workflow authority; this handoff does not create an alternative workflow.

The workflow states: “The live issue is the authority: where it and anything in the folder disagree, the issue wins.”

## Inputs and their authority

- [conversation.md](conversation.md) is the consolidated current brief awaiting approval of its wording. Earlier decision wording is recoverable with `git show b4d3a00:.dev/inprocess/63-appd-1-process/conversation.md`.
- [replacement-brief.md](replacement-brief.md) contains the detailed architecture review and investigation background.
- [decision-reconciliation.md](decision-reconciliation.md) maps retained and proposed replacement decisions to their source wording.
- [issue-revisions.md](issue-revisions.md) contains the full fetched current and full proposed title/body for the parent and all five child issues, including source timestamps.
- [autostart-research.md](autostart-research.md) and [design-review-notes.md](design-review-notes.md) contain research. They distinguish documented capabilities from experiments; they are not implementation approval.
- [design-challenges.md](design-challenges.md) contains historical challenges. Its review notice identifies that status.
- `ground-output.json` and `design-output.json` are complete original outputs for the previous per-user architecture. Preserve their bytes. They are background, not verdicts on the new architecture.
- `review-model.json`, `review-view.html`, `render-review.mjs`, and `appd-review.html` are the earlier interface presentation. They are not the new design and selections do not constitute approval.

Read the live parent and child issues with `gh issue view`: parent architecture (#62), process (#63), channel (#64), dashboard (#65), identity (#66), and key/presence (#67). Read the related conversation records under `.dev/inprocess/64-appd-2-channel/` and `.dev/backlog/67-appd-5-key-and-presence/`. The dashboard currently has its requirements in the live issue, not a conversation.md file.

## State to rederive

At package preparation, the branch was `appd-1-process`, HEAD was `b4d3a00`, and the working tree was initially clean. Do not reuse that as current state; run `git status --short`, `git branch --show-current`, and `git log -1`.

The recorded original GROUND and DESIGN have `panelComplete: true` and no failed roles. No contract or later stage outputs exist in this work folder. This preparation adds documentation only; it does not constitute another stage result. Do not claim that the new architecture has completed GROUND.

The original DESIGN contains ten reuse instructions. The earlier handoff's claim that the contract carries no reuse instruction was incorrect. Read the original list directly and rerun capability-specific discovery for the new scope; do not copy its old names or verdicts as current facts.

## Proposed process scope for the next GROUND

Subject to approval and issue synchronization, investigate delivery of one machine-wide headless server using the same application binary as CLI and viewer roles. The process part installs, discovers, starts, stops, recovers, and diagnoses the server. It consumes the preceding channel delivery. Production viewer, identity protocol, and key/signing implementations remain later deliveries.

Investigate the later session-launch and delegated-presence requirements now where they constrain server architecture. Investigating them is not permission to build those features in the process part. The server does not need to draw UI; the viewer runs as its user and connects back.

Vocabulary remains provisional. Do not invent command names, DTOs, interfaces, paths, service identifiers, or crypto formats to fill gaps.

## Concrete investigation questions

### Machine-wide lifecycle and installation

Find the existing install/setup, approval, command execution, filesystem, diagnostics, and platform composition owners. Establish what changes are needed for OS system-service installation, protected binary/configuration locations, runtime accounts, singleton enforcement, and machine-wide discovery. Assess direct invocation and concurrent start requests.

Compare automatic startup, intentional stop, administrative disablement, explicit retry, and crash recovery. Map the old approved recovery values to the actual system managers or report a required design change. Establish whether saved settings take effect at the desired next start without silently restarting the running server. Distinguish server policy from viewer policy.

### Session launch and UI availability

Establish each OS's way to launch an unelevated viewer into the requesting user's graphical session. Obtain the actual viewer identity, not a launch-command PID. Windows requires simultaneous interactive sessions for the same account. Linux coverage includes Wayland without systemd session integration. The existing user-manager candidate alone is not evidence of that coverage.

Determine what can be detected from peer process/user/session evidence, what requires display context, and what happens for no graphical login, SSH, locked/disconnected desktops, logout races, and viewer launch failure. Report OS facts separately from the product decision about automatic CLI fallback.

### Presence and process identity reuse

Trace the existing presence implementations and their OS calls. Establish whether they can run in the server, viewer, or CLI and what their results establish. Investigate delegated viewer checks without requiring an OS-signed certificate as an invented prerequisite.

Reuse the planned process-identity system conceptually and discover its actual implementation state. Establish mutual peer verification, user/session association, exact launched-instance matching, PID reuse handling, and the relationship between launcher and viewer processes. Existing appd identity is a spec, not a completed subsystem.

The identity part owns connected-process identity and expected-instance matching. The key/presence part owns freshness, operation binding, replay rejection, and accepting the delegated result. A symmetric key or KDF is a candidate only. Evaluate authenticated IPC guarantees before proposing another mechanism. State the consequences of trusted-process tampering without inflating the project's threat model or claiming a MAC proves an OS check occurred.

### Multi-user state and downstream constraints

Identify the surfaces affected by machine-wide service scope: IPC ACLs, per-user requests and keys, viewer association, logs, enrollment, and privilege boundaries. Preserve existing per-user signer attribution. Identify changes needed to install-time key minting without deciding a shared machine key.

Preserve the .NET web-server, capable browser-engine, and React stack. Establish relevant constraints on whether the web server resides in the privileged server or the viewer; do not select a new transport or UI dependency. Preserve the XPC-from-.NET feasibility requirement and its existing helper/build-target constraints.

### Native ownership, reuse, and guardrails

Use the existing GROUND workflow's ledger path and its required discovery lenses. Inspect actual code across src, tests, analyzers, and eng. Identify existing owners and extensions instead of reimplementing primitives. The old sixteen rules are unapproved proposals, not the required count for the new architecture.

## Evidence and execution boundaries

A documented API is not a native launch experiment. Record the OS/version, command or probe, actual outcome, and limits of each result. Do not claim Linux behavior from a macOS run. A suitable Windows host was not available in the earlier local VM inventory; recheck access rather than asserting one exists.

Read-only research and code exploration are distinct from installing a temporary service or running a GUI probe. Obtain approval for the concrete temporary targets and any work exceeding the approved time. Do not install dependencies, create VMs, or change system configuration merely to remove an environment gap. This package grants no experiment permission.

Do not change source, tests, analyzers, workflow scripts, issue bodies, or approved design while doing the prepared GROUND. Follow the selected workflow's exact permissions once Tim authorizes the rerun. No commit, staging, push, or PR is authorized here.

## Before launching GROUND

1. Tim approves the replacement brief, the process scope, and the exact replacement issue wording.
   The consolidated brief to approve is conversation.md.
2. Tim authorizes publication; update the live issues and local issue copies and record explicit decision supersessions without changing historical approvals.
3. Re-read the live issues and approved conversation record. Resolve any input mismatch.
4. Obtain Tim's explicit authorization for the GROUND rerun and its time scope. Apply every launch precondition in rails-run-a-workflow. An unresolved product decision is not an instruction to let an explorer pick a default.
5. Preserve the old raw outputs under an explicitly agreed historical location before any stage tool writes the same filenames. Use this work folder, not a second work-item folder.

If the workflow's launch preconditions prevent the proposed investigation while its questions remain open, bring that exact conflict to Tim. This handoff does not waive those preconditions.

GROUND returns its complete recorded output and evidence paths. It does not proceed automatically into DESIGN, write the implementation contract, or rebuild the visual. Tim reviews the findings before authorizing the next completed-stage rerun.

## Communicating with Tim

The original user instruction says: “Ask before anything over ten minutes, with the estimate.” It also says: “Never push. Never commit, stage, or open a PR without my word for that act.”

Apply the current communication skill. Lead with the answer. Show actual C# when asking for interface approval. Use provisional role names until Tim settles vocabulary. OS investigation questions belong to the investigator, not an unexplained list of choices for Tim.

Every reply uses the plain fold:

---

OPTIONAL DETAILS (do not need to read)

This handoff replaces the earlier stale handoff as a review-stage orientation document. The original remains recoverable in git history. Historical GROUND/DESIGN output bytes and the original visual were not modified.
