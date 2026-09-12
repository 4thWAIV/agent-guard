# Appd issue revisions — approval package

Status: Draft, not published. Each Current block is the complete live title/body fetched for this review. Each Proposed block is the complete replacement offered for approval. Existing source wording is preserved in Current even where it is stale or incorrect; quoting it does not endorse it. The substantive changes need Tim's approval before publication. New titles are proposed issue descriptions, not final CLI vocabulary.

The five delivery parts remain separately scoped. The proposed diagnostic-only early stages retain their demonstration purpose while preventing unauthenticated privileged operations. This safety boundary and any resulting dependency changes require review, not an automatic workflow waiver.

## Parent architecture

Live issue: https://github.com/4thWAIV/agent-guard/issues/62

Source updatedAt: 2026-09-05T04:36:26Z

### Current title and body

````markdown
appd: a long-running guard process that holds the signing key and only serves verified guard clients

> **This is the parent issue. The work is split across five parts — build them in order.**
>
> 1. **#63 — the process.** appd runs as a per-user process and the CLI can start it on demand.
> 2. **#64 — the channel.** The CLI talks to appd and gets answers. No security yet. Settles the XPC-from-.NET question.
> 3. **#65 — the dashboard.** The first visible proof the shape works. Needs only the process and the channel.
> 4. **#66 — identity.** The three-rung ladder, run in both directions.
> 5. **#67 — the key and presence.** appd holds the key and signs for verified callers. This makes signed decisions possible.
>
> This issue holds the design, the decisions, and the research findings. The parts hold the work.

## Why

Signing a decision needs a private key. Today the guard never holds one — the grant path is verify-only and `GrantStore` never mints. Presence shipped, so proving Tim is physically present is solved, but nothing turns that into a signature.

The OS secure stores do not solve this. Research established that no cross-platform mechanism lets a CLI authenticate once and then sign without prompting on later runs: macOS Authorization Services caches credentials across processes but its Touch ID path is hard-coded to Apple-signed executables, so a third-party CLI only ever gets a password prompt; Windows `KeyCredentialCacheConfiguration` is entirely undocumented; and polkit on Linux caches per shell rather than per binary and suppresses the per-decision message on a second, different decision, which is a correctness failure for decision signing.

A long-running process avoids all of it. It holds the key, authenticates once when the first request needing it arrives, and signs on request from then on.

## What appd is

The same guard binary run in daemon mode. Not a system service — a per-user process in the login session, because it must be able to draw a dashboard window. A macOS LaunchAgent, a Windows per-user logon process, a Linux systemd user unit. The CLI checks whether it is running and starts it if not.

## The flow

1. The client checks whether appd is running. If not, it starts it. If it cannot, it fails.
2. The client sends its request.
3. appd asks the OS which process connected — peer credentials on a Unix socket, the client process id on a Windows named pipe, the audit token on macOS XPC. The client never asserts its own identity.
4. appd verifies that process is guard.
5. It serves the request, or refuses.

The presence check sits behind that door. Because signing only happens through appd, and appd only answers a verified guard binary, the AI cannot sign by calling the crypto itself — it has to come through the door, and it cannot fake the presence check. **That is the point of the whole design.**

## Step 4 — the identity ladder

Three rungs, cheapest first, checked in order. Any one passing is sufficient.

1. **Same path as the running daemon.** It is literally the same file. A running binary cannot be swapped underneath itself. No further work.
2. **Different path, same hash.** It is the same binary in another directory. One file read.
3. **Different hash, signed as the same product.** A different version, still legitimate. Only this rung needs a signature check, and only when the first two miss.

Rung three is per-OS: macOS checks the code signature against the team identity and Windows checks Authenticode, both locally with no network. Linux verifies the binary against the cosign signature CI already produces — the only case needing a network call. Cache that result keyed by the binary's hash, so a given version is verified once and later requests from it hit the cache.

Hash the calling binary on every request. It is a local file read and it removes any stale-cache problem.

## Known holes, and why they are acceptable

**The connect-then-exec trick.** A client can connect, send its request, then `exec` a legitimate binary before appd looks. The request is already queued in the kernel and does not care what the process became. This was proven by running it, not argued from documentation. It defeats the Linux and Windows paths. Mitigation: check the caller again after the exchange rather than only before, so a process that swapped identity no longer matches.

**Linux cannot see past an interpreter.** A Python or Node client's `/proc/PID/exe` is the interpreter, never the script. Anything that checks the binary either rejects every legitimate script or is loose enough to be meaningless. Not fixable.

**Windows named-pipe squatting.** One line of PowerShell can take the pipe name before appd starts, and a permission-bit collision means no ACL prevents it. An impostor could sit there collecting whatever the real CLI sends. Needs a decision.

None of these matter much against the actual adversary. The threat model is a lazy AI taking the easy path, and a lazy AI does not attack the IPC channel — it lies about what was approved. This design's real value is that it forces every signing request through one door with the presence check on it.

## Decided by Tim

**The dashboard is owned by appd.** appd draws it. The CLI can tell it to pop for the user, the user can pop it themselves through the CLI, and a tray icon where the OS supports one. The AI can drive it when it needs to put something in front of the user for signoff. Decided by Tim: appd owns the UI. The CLI can raise it for the user, the user can raise it themselves through the CLI, and a tray icon where the OS supports one. The AI can drive it when it needs to put something in front of the user for signoff.

**Two modes, always in the spec; the promotion shows only when it applies.** appd detects the environment's protection state at start and runs either way — it never refuses to hold the key because of an OS setting. The dashboard shows visually which mode is active. When the OS is already in the strong mode there is nothing to display; when it is in the weaker one the dashboard shows the option and how to move up. Whether a given OS even has two modes is per-OS and unresearched; where it has only one, nothing is shown. Decided by Tim: where an OS offers a stronger posture, warn the user in the dashboard when appd starts and tell them how to reach it. The spec always describes both modes; the dashboard shows the upgrade path only where the OS has one and the machine is in the weaker mode.

**Windows pipe squatting is solved by running the identity ladder in both directions.** Windows gives the server the client's process id and gives the client the server's process id via `GetNamedPipeServerProcessId`, so the same three-rung check runs symmetrically. An impostor daemon fails the client's check exactly as a fake client fails the server's. No challenge-response protocol and no extra key needed. Decided by Tim: use the same OS facilities to identify the process on both ends of the pipe, and run the same check in both directions.

Open: whether Linux can do the same — whether a client on a Unix socket can learn what process is serving it. Probably yes through the same peer credentials, unverified. macOS on XPC is already mutual.

**macOS uses XPC.** XPC is strictly better than the alternative: it is always available, and the OS supplies the caller's verified code identity rather than the daemon having to work it out.

**The recorded fallback, if and only if XPC proves unreachable.** XPC is a C API built around Objective-C blocks, and reaching it from a plain .NET binary means hand-building a block structure through P/Invoke — opening a connection without one crashes the process outright. Research indicated this is achievable in roughly fifty lines with no native helper and no `net10.0-macos` workload, but it is unproven in this codebase. If it turns out unreachable, or unreachable without a native helper or a macOS-specific build target that Tim has already rejected once, then macOS uses the same Unix socket as Linux with the same three-rung identity ladder. The signature check on rung three is local on macOS with no network call, so the fallback costs only the OS-supplied caller identity, not the identity check itself. This is a build feasibility question and must be settled by a spike before the design locks — it is not a design preference.

## Not in scope

The decision format itself, what gets signed, and the enforcement that flags contradictions between an agent's claims and a signed decision. Those are separate work that depends on this.

## Established by research, for whoever picks this up

- Never use a TCP loopback port. It carries no identity of the connecting process and any local process can connect. No firewall rule fixes it — the `owner` match only works on outbound.
- Never use a Linux abstract socket. Socket permissions have no meaning for them; every process on the machine can connect. Use a pathname socket in `$XDG_RUNTIME_DIR`, which is mode 0700.
- Set the socket mode explicitly. systemd's `SocketMode=` defaults to 0666.
- Read peer credentials on the accepted connection, not the listening socket. Under socket activation the listening socket reports systemd, not the client.
- A token file in a 0600 file buys nothing over socket permissions. Both stop other users, both fall to one line from the same user, and the token adds a secret at rest that can be copied or committed.
- `SO_PEERPIDFD` requires Linux 6.5 or later.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s





````

### Proposed title and body

````markdown
appd: one machine-wide server with CLI clients and per-session viewers

## Purpose

AgentGuard uses one application in three roles. CLI clients submit requests. One headless server runs per machine. A viewer runs as the user in the graphical session where interaction is needed. Role names are provisional; this issue does not select final CLI vocabulary.

## Why

Signing a decision needs a private key. Today the guard never holds one — the grant path is verify-only and `GrantStore` never mints. Presence shipped, so proving Tim is physically present is solved, but nothing turns that into a signature.

The OS secure stores do not solve this. Research established that no cross-platform mechanism lets a CLI authenticate once and then sign without prompting on later runs: macOS Authorization Services caches credentials across processes but its Touch ID path is hard-coded to Apple-signed executables, so a third-party CLI only ever gets a password prompt; Windows `KeyCredentialCacheConfiguration` is entirely undocumented; and polkit on Linux caches per shell rather than per binary and suppresses the per-decision message on a second, different decision, which is a correctness failure for decision signing.

A long-running process avoids all of it. It holds the key, authenticates once when the first request needing it arrives, and signs on request from then on.

## Responsibilities and flow

The CLI connects to the server and submits an operation. The server obtains peer identity from the OS, validates the client, and determines the requesting user and session. It delivers the interaction to a matching authenticated viewer, or requests an OS-mediated viewer launch and waits for its authenticated connection.

A viewer performs user-facing interaction. Presence may be performed there and returned to the server through an authenticated, request-bound result. The server authorizes operations and retains signing responsibility. A viewer that the server launched must connect as that same process instance, not merely reuse a PID. An independently started viewer needs an explicitly designed admission policy.

The authenticated connection and request binding must prevent another user, another viewer, or an old response from authorizing the operation. Giving a viewer a secret is a candidate, not a selected protocol. A MAC authenticates a message; the trusted viewer implementation is responsible for calling the OS presence mechanism correctly.

The server can run without a graphical login. Closing a viewer does not stop it. Server management is machine-wide. CLI requests to stop or configure it require presence and OS administrative authorization. Direct OS service-manager operations remain governed by the OS. The CLI also provides viewer closure, with vocabulary and exact scope to be designed.

## Five delivery parts

1. **Channel (#64).** Deliver local message transport and OS peer information independently of server installation. Exercise both endpoints through the actual OS transport within one process.
2. **Process (#63).** Consume the channel while installing, discovering, starting, stopping, and recovering one machine-wide headless server.
3. **Dashboard/viewer (#65).** Launch or reuse a viewer in the requesting session and provide graphical and CLI interaction. Before identity and presence enforcement, demonstrations do not authorize protected operations.
4. **Identity (#66).** Verify connected AgentGuard processes in both directions, establish user/session association, and bind a launched viewer connection to its expected process instance.
5. **Key and presence (#67).** Delegate presence where necessary, validate fresh operation-bound results, and sign for the correct user.

This retains five delivery parts as a proposed organization. It does not permit unauthenticated privileged execution in the early channel or viewer demonstrations. If launching a privileged server's viewer cannot be isolated safely before identity enforcement, revise the dependency order before implementation.

## Interaction mode

AgentGuard detects whether graphical interaction is available to the requesting client and uses that result to select the default interaction mode. An explicit interaction-mode option takes precedence over detection.

Every graphical view has a CLI alternative. Terminal presentation does not replace presence or authorize unattended approval. Viewer launch failure is distinct from absence of a usable graphical session. Automatic fallback after viewer failure remains undecided. Terminal-only and non-interactive switches are later work; no names or unattended-approval behavior are selected here.

## Existing requirements carried forward

The dashboard uses a .NET web server, a capable current browser engine, and React. Moving the window into a viewer does not replace that stack. The web-server process and its browser transport require a decision in the dashboard work. Wayland without systemd session integration remains part of desktop support.

Connections use OS-derived identity, not identity asserted in request fields. Local IPC transport and the XPC-from-.NET feasibility question remain in the channel and identity work. The old same-path/hash/signature ladder is an input requiring review across the new privilege boundary, not a claim that a pathname alone proves executable identity.

Keys remain attributable to individual users. One server does not imply one shared machine key. Existing key minting, protection, retirement, and signer-attribution decisions remain inputs to the key work; their installation assumptions must be reconciled with machine-wide installation.

## Not in scope

The decision format itself, what gets signed, and the enforcement that flags contradictions between an agent's claims and a signed decision. Those are separate work that depends on this.

## Investigation and decision boundaries

GROUND establishes OS service lifecycle, session launch, graphical availability, peer/process-instance identity, existing presence capabilities, and reusable code. It distinguishes documented facilities from native experiments. Implementation mechanisms, runtime accounts, protected paths, endpoint permissions, startup defaults, recovery settings, and data formats are not selected by tool defaults.

The current process contract does not yet exist. Each part receives its own approved implementation scope; this umbrella does not authorize implementing the entire product in the process part.
````

## Process

Live issue: https://github.com/4thWAIV/agent-guard/issues/63

Source updatedAt: 2026-09-05T04:36:28Z

### Current title and body

````markdown
appd 1 of 5: the process — a per-user guard daemon the CLI can start on demand

Part 1 of the appd work. Parent: #62.

## What this delivers

The same guard binary, run in daemon mode, staying up as a per-user process. Nothing else — no key, no dashboard, no identity checking, no channel. Just a process that starts, stays up, and can be found.

## Requirements

**One canonical start path.** Decided by Tim: autostart at login is the normal case, and an installer starts it where one exists. The CLI checks whether appd is running and, when it is not, triggers that same canonical start rather than launching it some other way. This covers installing onto an already-running login session without a second, divergent startup mechanism.

**It must be able to draw a window later**, which constrains what kind of process it is on every platform. Not a system service. On macOS a LaunchAgent in the user session rather than a LaunchDaemon; on Windows a per-user process rather than a service, because services run in session 0 and cannot reach the desktop; on Linux a systemd user unit rather than a system unit. This part does not draw anything, but choosing the wrong process type here blocks the dashboard entirely.

**Exactly one runs at a time, within the scope the platform itself uses.** On macOS and Linux that is the user account — one appd per account, across every session that account has open. On Windows it is the logon session — one appd per account per logon session — because Windows runs two interactive sessions for one person at once, a console login and a remote one, each with its own desktop. Two CLI invocations racing to start it must not produce two daemons.

**The CLI detects whether it is already running** before deciding to start it.

**Login startup registration is part of the canonical path, not an optional extra.** The CLI's check-and-start is the fallback that covers a machine where login startup has not yet taken effect — it is not a replacement for it.

## Out of scope

The channel, the dashboard, identity checking, and the key. Each is its own part.

## Done when

On all three platforms: appd starts at login, the CLI starts it through the same canonical path when it is not running, no second instance starts when one is already up, appd survives the CLI exiting, and the process type chosen can draw a window when part 3 needs it to.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s


````

### Proposed title and body

````markdown
appd 2 of 5: the process — one machine-wide headless server

Second delivery of the appd work. Parent: #62. Depends on the independently delivered channel (#64).

## What this delivers

The same AgentGuard binary runs as one machine-wide headless server. The operating system manages its lifetime. The CLI can find it and request permitted lifecycle operations. This delivery consumes the existing channel implementation and integrates it with the installed server. Viewer, process-identity protocol, and signing remain separate deliveries.

## Requirements

**One server per machine.** Multiple user accounts and simultaneous sessions share the server process without requiring a server in each session. Direct invocation and racing start requests must not create duplicate servers.

**OS-managed lifetime.** Installation registers the server with the system's service manager. The selected mechanism supports automatic startup, explicit start, explicit stop, and failure recovery independently of user logins. Startup trigger, default enablement, runtime account, recovery policy, and permissions must be decided before the implementation contract locks.

**One canonical start path.** The installer and CLI use the OS service manager to start the installed server. Installing on an already-running machine must not require a reboot or another startup mechanism.

**Machine-wide administration.** CLI requests to stop the server or change its configuration require both presence and OS administrative authorization. The CLI can perform its administrative presence check without depending on the server being available. Direct operations through the OS service manager remain governed by the OS.

**Stop and disable are different operations.** Stopping the server does not itself change automatic startup configuration. An intentional stop does not trigger crash recovery. Which principals may explicitly or automatically restart an administratively stopped server must be resolved for machine scope; do not inherit per-user permissions silently.

**Diagnostics.** Status reports whether the server is running. Failures explain the cause and corrective action, and doctor diagnoses installation and lifecycle problems. Logging and retention remain required; reconcile the existing per-user logging package with a machine-wide process before selecting locations and access rules.

**No desktop dependency in the server process.** The server stays running when a viewer closes or a user logs out. GROUND must establish that the selected arrangement supports later OS-mediated launches of unelevated viewers in the requesting user's graphical session. This requirement is capability investigation here, not production viewer implementation.

**Configuration.** Reconcile the approved saved-versus-active settings behavior with each system manager. No setting change silently restarts the server. Exact definitions, persistent paths, and configuration apply behavior require approval before implementation.

## GROUND scope

Explore system-service installation, lifecycle, singleton enforcement, discovery, diagnostics, logging, native ownership, and existing presence reuse. Investigate session launch and process identity only as needed to establish feasibility for the later viewer and presence design. Include Windows simultaneous sessions and Linux Wayland without systemd session integration. Record unavailable test environments explicitly.

The previous per-user lock paths, Local\\ mutex scope, user pipe names, Task Scheduler task, and user-service definitions are historical inputs, not the selected machine-wide mechanisms. The old recovery values are approved historical behavior needing an explicit mapping or revision, not permission to silently choose new defaults.

## Out of scope

The channel implementation is delivered first and reused here. Production viewers, the identity protocol, delegated signing presence, key minting, and signing belong to the later parts. Existing CLI presence reuse for administrative lifecycle operations belongs here. Final CLI vocabulary remains unsettled; no new names are implied by provisional roles.

## Done when

On macOS, Linux, and Windows, an administratively installed server can be found, started through the canonical OS path, stopped through the authorized path, and recovered after failure according to the approved policy. It remains independent of client processes and graphical logins, refuses duplicate instances, and provides actionable diagnostics and approved logging. CLI administrative operations enforce presence and OS authorization. Required future session-launch capabilities have evidence or an explicitly reported unresolved limitation; an unresolved feasibility dependency prevents declaring the part complete.

The installed server and CLI also demonstrate a diagnostic exchange using the preceding channel implementation. The implementation contract must supply the exact approved startup, recovery, storage, and permission decisions before code is written.
````

## Channel

Live issue: https://github.com/4thWAIV/agent-guard/issues/64

Source updatedAt: 2026-09-03T10:12:53Z

### Current title and body

````markdown
appd 2 of 5: the channel — CLI talks to appd, no security yet

Part 2 of the appd work. Parent: #62. Depends on #63.

## What this delivers

The CLI connects to appd, sends a request, and gets an answer back. No identity checking and no key — a working channel, demonstrably carrying traffic.

Decided by Tim: this part can demonstrate success on its own, before any security is added.

## Requirements

**The channel lives where only the user's account can reach it.** A Unix domain socket on macOS and Linux in a directory the user alone can read, a named pipe on Windows. This stops other users without a single line of security code, because it is filesystem permissions doing the work.

**Never a TCP loopback port.** It carries no identity of the connecting process, any local process can connect, and no firewall rule fixes it — the `owner` match only applies to outbound traffic.

**Never a Linux abstract socket.** Socket permissions have no meaning for them; every process on the machine can connect regardless of ownership or mode.

**Set the socket mode explicitly.** systemd's `SocketMode=` defaults to 0666.

## The XPC spike, and why it belongs here

macOS should use XPC rather than a Unix socket, because XPC gives the receiving side the caller's verified code identity from the kernel — the OS does the identity work that parts 4 has to do by hand everywhere else.

XPC is a C API built around Objective-C blocks, and reaching it from a plain .NET binary means hand-building a block structure through P/Invoke. Opening a connection without one crashes the process outright. Research indicated this is achievable in roughly fifty lines with no native helper and no `net10.0-macos` workload, but it is unproven in this codebase.

**Settle it with a spike in this part**, because the answer decides what macOS uses. If XPC proves unreachable, or only reachable through a native helper or a macOS-specific build target — which Tim has already rejected once for the presence work — then macOS uses the same Unix socket as Linux. The identity check still works there; the fallback costs only the OS-supplied caller identity, not the identity check itself.

## Out of scope

Identity checking, the dashboard, and the key.

## Done when

On all three platforms the CLI reaches appd, a request goes out and an answer comes back, another user cannot reach the channel, and the XPC question is settled with evidence rather than opinion.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s

````

### Proposed title and body

````markdown
appd 1 of 5: the channel — independently testable local communication

First delivery of the appd work. Parent: #62. The process delivery (#63) consumes this channel.

## What this delivers

An independently usable communication transport provides connection establishment, message sending and receiving, connection closure, and OS-derived peer information. Tests can run both endpoints in one process through the actual OS transport and check the returned identity against that process. CLI, server, and viewer integration use this same implementation.

## Requirements

**Local IPC with explicit access control.** Define how multiple local users connect to one server without gaining access to each other's requests or state. Decide socket/pipe names, ownership, and permissions for machine scope. Old per-user locations and Windows SID/session pipe suffixes must not silently become the machine-wide endpoint design.

**Never a TCP loopback port.** It carries no identity of the connecting process, any local process can connect, and no firewall rule fixes it — the `owner` match only applies to outbound traffic.

**Never a Linux abstract socket.** Socket permissions have no meaning for them; every process on the machine can connect regardless of ownership or mode.

**Set the socket mode explicitly.** systemd's `SocketMode=` defaults to 0666.

**Preserve peer evidence.** The channel exposes OS-derived peer information needed by the identity part. Read credentials on the accepted connection, not a socket-activation listener. A user/session value supplied inside a request is not authentication.

**No privileged application operation before enforcement.** The early diagnostic exchange does not expose signing, protected mutations, delegated approval acceptance, or an unauthenticated arbitrary process launcher.

## The XPC spike, and why it belongs here

macOS should use XPC rather than a Unix socket, because XPC gives the receiving side the caller's verified code identity from the kernel — the OS does the identity work that parts 4 has to do by hand everywhere else.

XPC is a C API built around Objective-C blocks, and reaching it from a plain .NET binary means hand-building a block structure through P/Invoke. Opening a connection without one crashes the process outright. Research indicated this is achievable in roughly fifty lines with no native helper and no `net10.0-macos` workload, but it is unproven in this codebase.

**Settle it with a spike in this part**, because the answer decides what macOS uses. If XPC proves unreachable, or only reachable through a native helper or a macOS-specific build target — which Tim has already rejected once for the presence work — then macOS uses the same Unix socket as Linux. The identity check still works there; the fallback costs only the OS-supplied caller identity, not the identity check itself.

The spike establishes transport feasibility independently of the installed appd service. Investigate the requirements for subsequent machine-wide server and user-session integration. OS-provided peer credentials are inputs to code-identity validation, not automatic proof that the peer is AgentGuard.

## Out of scope

OS-managed server installation and lifetime belong to the process delivery. Full product-identity verification, viewer UI, trusted presence-result acceptance, and key/signing operations belong to their respective deliveries. This issue does not choose the browser-to-web-server transport for the dashboard.

## Done when

On all three platforms, transport endpoints connect, exchange messages, and close without requiring an installed appd service. Same-process tests exercise the real transport and verify the OS reports the expected peer. Peer information is available to subsequent identity checks, concurrent connections keep their responses separate, and the XPC feasibility question has evidence. Tests requiring different processes or users cover those boundaries separately; installed-service integration belongs to the process delivery. The approved endpoint access policy is enforced. No protected operation becomes available merely because a caller can connect.
````

## Dashboard and viewer

Live issue: https://github.com/4thWAIV/agent-guard/issues/65

Source updatedAt: 2026-09-03T10:47:24Z

### Current title and body

````markdown
appd 3 of 5: the dashboard — the first thing that shows appd doing something useful

Part 3 of the appd work. Parent: #62. Depends on #63 and #64.

## Why this comes third, before identity and the key

This is the first part that produces something visible. Everything before it is plumbing you cannot see; everything after it is protection you cannot see. The dashboard is the proof the shape works.

## What this delivers

appd draws a window. It can be raised three ways: the user asks the CLI to pop it, the CLI pops it as part of some other command, and where the OS supports a tray icon, from there. The AI can also drive it — telling appd to put something in front of the user when it needs a signoff.

## The UI stack — NOT NEGOTIABLE

**The UI stack is fixed and not open to substitution.**

The dashboard is a .NET web server hosted inside appd, rendered by a browser control, with the pages themselves written in React.

The browser control must be a capable, current engine — Chromium, WebKit, or an equivalent that runs on all three platforms. It must not be a cut-down, legacy, or platform-default embedded view.

This is not a recommendation or a starting point. Any proposal to change it is rejected without discussion unless Tim changes it himself.

Approved by Tim as written.

## Requirements

**appd owns the window.** The CLI never draws it. The CLI sends a request over the channel from #64 and appd raises it.

**An empty dashboard opens with the process.** appd starting means the dashboard exists.

**Closing the window must not end the process.** The user dismisses the window with a button in the UI; appd keeps running. A CLI command brings it back. Round-tripping that — open, dismiss, reopen from the CLI, all with appd alive throughout — is the working demonstration that this part is done.

**It can be shown and hidden repeatedly**, not drawn once at startup. appd runs for a whole login session; the window comes and goes.

**A tray icon where the OS supports one.** Per-OS, not required everywhere.

## The Linux display problem, which is real and needs settling here

A systemd user unit does not inherit the graphical session's environment. The user manager is started by PID 1 before any compositor exists and is shared across all the user's sessions, so it has no display to inherit. On X11 systemd ships a script that imports the display variables, so it usually works. On Wayland a unit with no display variable silently connects to whatever compositor happens to own the default socket — which means it works after a re-login and fails at first login, the worst possible failure.

There is also no display environment at all on desktops with no systemd session integration. sway and i3 ship nothing; on those, gating the dashboard on the graphical session target means it silently never appears.

**The alternative that sidesteps all of it:** the CLI passes its own display environment to appd when asking for the window, since the CLI is running in the user's session and has it. Worth deciding here rather than discovering it on sway.

## What the dashboard is for

Decided by Tim: this is a communication channel, not a status page. It is how the user learns what they need to know without being told in prose. Anything that reaches the user only because an agent chose to mention it is a reporting failure this replaces.

**Signal first, details on demand.** The top of the view is the state of the world at a glance — build, lint, coverage, what is waiting on the user — each a single verdict readable in a second. Any of them drills through to the detail behind it. Nothing the user does not need appears on the first screen.

**Every value carries its age and whether it still means anything.** A green test result from four hours ago and six commits back is not evidence. Green-but-stale must read differently from green-as-of-the-current-commit.

**It populates itself from every build.** Not something an agent updates and not something an agent remembers to mention. If coverage drops, the user sees it because the build wrote it.

**Decisions waiting on the user appear here.** Read the cleaned wording, approve with a click. Once the signing work lands, that approval is what the signature covers and what an adversary checks an agent's claims against. Before then, the list alone is worth having.

## The open-item register

Decided by Tim: the system tracks open items in a register rather than treating them as a single blocking flag. Every open item names the stage by which it must be resolved. An item may stay open while work proceeds if its deadline stage has not arrived; a stage refuses to run while any item due by that stage is still open. The dashboard shows the register as the task's progress and health, so nothing unresolved reaches the user after a commit. Which items are due at which stage is settled when the work is done.

## Signal-to-noise control

Decided by Tim: the dashboard also exists to give fine-grained control over signal-to-noise. Two things provide it. The contract has no field for editorial, so an agent cannot rank its own output or wrap data in commentary. And the filter agent actively edits — stripping padding and self-referential framing, and reordering so the most important item leads, regardless of how the producing agent wrote it. These habits have been ruled against repeatedly and in skill instructions and still recur, so the design removes the opportunity rather than relying on compliance.

## How data reaches the UI

Decided by Tim: stage results feed the dashboard through communication filters — agents whose only job is shaping. A filter takes a stage result and produces data in a strict contract both sides understand. The React side owns presentation; the filter owns nothing but the shape.

A filter edits: it strips padding and self-referential framing and reorders so the most important item leads. It cannot decide what matters, cannot drop a finding, and cannot soften a verdict. Because the contract is fixed, a filter that tries produces something the UI can reject as malformed rather than something that merely reads differently. The schema is the check.

This is the point: it removes the orchestrator's judgement from what the user sees.

## Out of scope

The strong-versus-best-effort mode indicator. That belongs with the key work in part 5, because there is nothing to report on before a key exists.

## Done when

On all three platforms: the dashboard opens when appd starts, the user dismisses it with a button and appd keeps running, a CLI command brings it back, that cycle repeats, and it works on a Wayland desktop with no systemd session integration.

The UI is a .NET web server, a capable browser control, and React. No substitutions.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s






````

### Proposed title and body

````markdown
appd 3 of 5: the dashboard — an on-demand viewer in the requesting user's session

Part 3 of the appd work. Parent: #62. Depends on #63 and #64. Protected interactions additionally depend on #66 and #67.

## What this delivers

A separate viewer process presents the dashboard for the requesting user and session. The server launches it on demand or reuses its authenticated connection when available. The CLI can request the dashboard directly or as part of another operation. A tray icon is supported where the OS permits it. An AI can request that information or an approval request be presented, but cannot thereby provide the human approval.

## Demonstration boundary

This part can demonstrate display and diagnostic interaction before identity and signing are finished. It does not accept protected approvals or expose an unauthenticated privileged launch interface. Decide whether that boundary requires a dependency-order change before implementation.

## The UI stack — NOT NEGOTIABLE

**The UI stack is fixed and not open to substitution.**

The dashboard uses a .NET web server, is rendered by a browser control, and has pages written in React. The web server's placement in the headless server or viewer requires an explicit decision; separating the window does not authorize changing the stack.

The browser control must be a capable, current engine — Chromium, WebKit, or an equivalent that runs on all three platforms. It must not be a cut-down, legacy, or platform-default embedded view.

This is not a recommendation or a starting point. Any proposal to change it is rejected without discussion unless Tim changes it himself.

The original stack requirement was approved by Tim. The revised process placement wording above is part of this replacement proposal.

## Requirements

**Separate process lifetime.** The machine-wide server does not draw the window. Viewer closure does not terminate the server. The viewer can be shown, dismissed, and raised again. The CLI supports closing viewers; distinguish hiding a window from terminating a viewer, and decide the target scope before implementing that command.

**On-demand launch and reuse.** Resolve the requesting user and graphical session, find an eligible connected viewer or launch one, and wait for its connection before delivering the interaction. Define concurrent launch behavior, pending-request handling, timeouts, disconnections, and admission of independently launched viewers. The identity mechanisms are implemented in #66.

**Graphical availability.** AgentGuard detects whether graphical interaction is available to the requesting client and uses that result to select the default interaction mode. An explicit interaction-mode option takes precedence over detection.

**CLI alternatives.** Every graphical view has a CLI alternative with the same authorization and presence requirements. A terminal response is not automatically human presence. Final terminal-only/non-interactive switches and automation semantics are later work, not part of the process delivery.

**Viewer failure differs from no graphical session.** Automatic CLI fallback after a viewer failure remains undecided. An error by default with an explicit fallback option is still a candidate.

## Linux display support

The viewer must work on Wayland desktops without systemd session integration as well as supported integrated desktops. A user-manager start is not sufficient evidence unless the viewer receives the correct session's graphical environment. Investigate session-local launching or securely associated display information without guessing DISPLAY or WAYLAND_DISPLAY. The selected mechanism must also handle requests without an available graphical session.

## What the dashboard is for

Decided by Tim: this is a communication channel, not a status page. It is how the user learns what they need to know without being told in prose. Anything that reaches the user only because an agent chose to mention it is a reporting failure this replaces.

**Signal first, details on demand.** The top of the view is the state of the world at a glance — build, lint, coverage, what is waiting on the user — each a single verdict readable in a second. Any of them drills through to the detail behind it. Nothing the user does not need appears on the first screen.

**Every value carries its age and whether it still means anything.** A green test result from four hours ago and six commits back is not evidence. Green-but-stale must read differently from green-as-of-the-current-commit.

**It populates itself from every build.** Not something an agent updates and not something an agent remembers to mention. If coverage drops, the user sees it because the build wrote it.

**Decisions waiting on the user appear here.** Read the cleaned wording, approve with a click. Once the signing work lands, that approval is what the signature covers and what an adversary checks an agent's claims against. Before then, the list alone is worth having.

## The open-item register

Decided by Tim: the system tracks open items in a register rather than treating them as a single blocking flag. Every open item names the stage by which it must be resolved. An item may stay open while work proceeds if its deadline stage has not arrived; a stage refuses to run while any item due by that stage is still open. The dashboard shows the register as the task's progress and health, so nothing unresolved reaches the user after a commit. Which items are due at which stage is settled when the work is done.

## Signal-to-noise control

Decided by Tim: the dashboard also exists to give fine-grained control over signal-to-noise. Two things provide it. The contract has no field for editorial, so an agent cannot rank its own output or wrap data in commentary. And the filter agent actively edits — stripping padding and self-referential framing, and reordering so the most important item leads, regardless of how the producing agent wrote it. These habits have been ruled against repeatedly and in skill instructions and still recur, so the design removes the opportunity rather than relying on compliance.

## How data reaches the UI

Decided by Tim: stage results feed the dashboard through communication filters — agents whose only job is shaping. A filter takes a stage result and produces data in a strict contract both sides understand. The React side owns presentation; the filter owns nothing but the shape.

A filter edits: it strips padding and self-referential framing and reorders so the most important item leads. It cannot decide what matters, cannot drop a finding, and cannot soften a verdict. Because the contract is fixed, a filter that tries produces something the UI can reject as malformed rather than something that merely reads differently. The schema is the check.

This is the point: it removes the orchestrator's judgement from what the user sees.

## Out of scope

The strong-versus-best-effort mode indicator. That belongs with the key work in part 5, because there is nothing to report on before a key exists.

Trusted delegated presence-result verification and signing are delivered in #67 using the identity work in #66. This part supplies their user-facing process, not a replacement cryptographic protocol.

## Done when

On all three platforms, a request can raise or reuse a viewer in the correct user's graphical session, display an interaction, dismiss the window, and raise it again while the machine-wide server stays alive. CLI viewer closure has its approved scope. Graphical availability selects the default interaction mode, and equivalent CLI interactions exist. The cycle works on Wayland without systemd session integration. The fixed .NET web-server, browser-control, and React stack is used, and the approved dashboard data, decision, and drill-down requirements are met.

The contract resolves UI hosting, viewer lifecycle, no-GUI behavior, and launch-failure behavior before implementation.
````

## Identity

Live issue: https://github.com/4thWAIV/agent-guard/issues/66

Source updatedAt: 2026-09-03T10:12:55Z

### Current title and body

````markdown
appd 4 of 5: identity — appd and the CLI each verify the other is guard

Part 4 of the appd work. Parent: #62. Depends on #64.

## What this delivers

appd refuses to serve anything that is not guard, and the CLI refuses to talk to anything that is not guard. The same check, run in both directions.

## The three-rung ladder

Checked in order, cheapest first. Any one passing is sufficient.

1. **Same path as the running process.** It is literally the same file. A running binary cannot be swapped underneath itself. No further work.
2. **Different path, same hash.** The same binary in another directory. One file read.
3. **Different hash, signed as the same product.** A different version, still legitimate. Only this rung needs a signature check, and only when the first two miss.

**Hash the calling binary on every request.** It is a local file read, it costs nothing, and it removes any stale-cache problem. There is no expected value to store — appd compares against its own binary, because the CLI and appd are the same application in different modes.

## Rung three is per-OS

**macOS** checks the code signature against the team identity. Local, no network.

**Windows** checks Authenticode on the process image. Local, no network.

**Linux** verifies the binary against the cosign signature CI already produces. This is the only case needing a network call, so cache the result keyed by the binary's hash — a given version is verified once and later requests from it hit the cache. A machine realistically sees one or two versions, so that is one call after an upgrade and none after.

Decided by Tim: cache whatever material further validation needs on the platform that requires it, which is Linux and cosign. macOS and Windows use their built-in verification and cache nothing, since neither makes a network call.

## Getting the caller's identity from the OS, never from the caller

Peer credentials on a Unix socket, the client process id on a Windows named pipe, the audit token on macOS XPC. The client never asserts who it is.

## Both directions

Windows gives the server the client's process id and gives the client the server's process id via `GetNamedPipeServerProcessId`, so the same ladder runs symmetrically. This is what defeats pipe squatting — one line of PowerShell can take the pipe name before appd starts and no ACL prevents it, but an impostor fails the client's check exactly as a fake client fails the server's. No challenge-response protocol and no extra key.

Decided by Tim: use the same OS facilities to identify the process on both ends of the pipe, and run the same check in both directions.

**Open, and needs answering in this part:** whether a Linux client can learn which process is serving it. Probably yes through the same peer credentials, unverified. macOS on XPC is already mutual.

## Known holes, and why they are acceptable

**The connect-then-exec trick.** A client can connect, send its request, then `exec` a legitimate binary before appd looks. The request is already queued in the kernel and does not care what the process became. This was proven by running it, not argued from documentation, and it defeats the Linux and Windows paths. Mitigation: check the caller again after the exchange as well as before, so a process that swapped identity no longer matches.

**Linux cannot see past an interpreter.** A Python or Node client's `/proc/PID/exe` is the interpreter, never the script. Anything checking the binary either rejects every legitimate script or is loose enough to be meaningless. Not fixable.

Neither matters much against the actual adversary. The threat model is a lazy AI taking the easy path, and it does not attack the IPC channel — it lies about what was approved. The value here is that signing can only happen through appd, and appd only answers verified guard, so the presence check in part 5 cannot be bypassed.

## Done when

On all three platforms appd refuses a non-guard caller, the CLI refuses a non-guard server, all three rungs work, the Linux cosign result is cached by hash, and the connect-then-exec case is handled.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s

````

### Proposed title and body

````markdown
appd 4 of 5: identity — verified clients, server, and expected viewer processes

Part 4 of the appd work. Parent: #62. Depends on #64. Its enforcement is required before earlier demonstrations expose privileged application operations.

## What this delivers

The server, CLI, and viewer verify the AgentGuard process at the other end of their connection. The server associates a verified connection with its OS-derived user and session. For a viewer it launched, the connecting process must be the expected process instance.

## Existing identity design to reevaluate

## The three-rung ladder

Checked in order, cheapest first. Any one passing is sufficient.

1. **Same path as the running process.** It is literally the same file. A running binary cannot be swapped underneath itself. No further work.
2. **Different path, same hash.** The same binary in another directory. One file read.
3. **Different hash, signed as the same product.** A different version, still legitimate. Only this rung needs a signature check, and only when the first two miss.

**Hash the calling binary on every request.** It is a local file read, it costs nothing, and it removes any stale-cache problem. There is no expected value to store — appd compares against its own binary, because the CLI and appd are the same application in different modes.

The preceding ladder is the existing recorded approach. Its claims about pathname equality and a running executable's stability must be verified against each OS and the new privilege boundary before carrying them into the contract. A shared pathname is not accepted as proof merely because the old prose says so. Propose any required replacement for Tim's approval rather than quietly weakening identity checks.

## Rung three is per-OS

**macOS** checks the code signature against the team identity. Local, no network.

**Windows** checks Authenticode on the process image. Local, no network.

**Linux** verifies the binary against the cosign signature CI already produces. This is the only case needing a network call, so cache the result keyed by the binary's hash — a given version is verified once and later requests from it hit the cache. A machine realistically sees one or two versions, so that is one call after an upgrade and none after.

Decided by Tim: cache whatever material further validation needs on the platform that requires it, which is Linux and cosign. macOS and Windows use their built-in verification and cache nothing, since neither makes a network call.

## Getting the caller's identity from the OS, never from the caller

Peer credentials on a Unix socket, the client process id on a Windows named pipe, the audit token on macOS XPC. The client never asserts who it is.

## Both directions

Windows gives the server the client's process id and gives the client the server's process id via `GetNamedPipeServerProcessId`, so the same ladder runs symmetrically. This is what defeats pipe squatting — one line of PowerShell can take the pipe name before appd starts and no ACL prevents it, but an impostor fails the client's check exactly as a fake client fails the server's. No challenge-response protocol and no extra key.

Decided by Tim: use the same OS facilities to identify the process on both ends of the pipe, and run the same check in both directions.

**Open, and needs answering in this part:** whether a Linux client can learn which process is serving it. Probably yes through the same peer credentials, unverified. macOS on XPC is already mutual.

Extend mutual verification to viewer/server connections. Research claims and unresolved questions in the existing text above must be resolved against current code and OS evidence; they are not proof of a finished mechanism.

## Bind a launched viewer to its process instance

The server obtains the actual viewer process identity from the launch mechanism. A launcher command's PID is not the viewer's PID. The returning connection must match that expected instance as well as pass AgentGuard code-identity checks and user/session checks.

A numeric PID alone is insufficient after exit because PIDs can be reused. Select the OS-supported process-instance evidence and lifetime handling before implementation. Establish the path for launchd/systemd-mediated starts and the policy for reusing viewers or admitting a viewer the server did not launch.

## Boundary with presence

This part authenticates processes and their connections. It does not certify that a human completed a check merely because a process is AgentGuard. The key/presence part owns challenge freshness, operation binding, replay rejection, and acceptance of delegated results. It reuses this identity mechanism instead of building a second one.

No symmetric key, KDF, new wire format, or extra identity handshake is selected by this issue. Evaluate whether the authenticated IPC channel already provides the required protections before adding another protocol.

## Existing attack research to reevaluate

## Known holes, and why they are acceptable

**The connect-then-exec trick.** A client can connect, send its request, then `exec` a legitimate binary before appd looks. The request is already queued in the kernel and does not care what the process became. This was proven by running it, not argued from documentation, and it defeats the Linux and Windows paths. Mitigation: check the caller again after the exchange as well as before, so a process that swapped identity no longer matches.

**Linux cannot see past an interpreter.** A Python or Node client's `/proc/PID/exe` is the interpreter, never the script. Anything checking the binary either rejects every legitimate script or is loose enough to be meaningless. Not fixable.

Neither matters much against the actual adversary. The threat model is a lazy AI taking the easy path, and it does not attack the IPC channel — it lies about what was approved. The value here is that signing can only happen through appd, and appd only answers verified guard, so the presence check in part 5 cannot be bypassed.

The existing threat assumptions above were recorded for a per-user process. Reevaluate their applicability to the privileged machine-wide server and reject any implicit waiver of cross-user isolation. OS-backed identity is not equivalent to resistance to arbitrary injection into a trusted process; state the achieved protection and any remaining limits.

## Done when

On all three platforms, impostor clients, viewers, and servers are rejected; the approved executable-verification paths work; and the approved Linux verification cache is keyed by executable hash. OS-derived user/session association is correct. A launched viewer can connect only as the expected process instance, with tests covering wrong processes, PID reuse, launcher-versus-viewer identity, and relevant executable-change races. The contract specifies the policy for pre-existing viewers. Delegated presence can consume this identity result without inventing another identity subsystem.
````

## Key and presence

Live issue: https://github.com/4thWAIV/agent-guard/issues/67

Source updatedAt: 2026-09-05T04:36:30Z

### Current title and body

````markdown
appd 5 of 5: the key and presence — signing becomes possible

Part 5 of the appd work. Parent: #62. Depends on #63, #64, #65 and #66.

## What this delivers

appd holds the Ed25519 private key and signs on request for verified callers only. It authenticates the user the first time a client asks for work that needs it, not when it starts. This is the part that makes signed decisions possible.

## Why it has to work this way

Today the guard never holds a private key — the grant path is verify-only and `GrantStore` never mints. Presence shipped, so proving the user is physically present is solved, but nothing turns that into a signature.

The OS secure stores cannot solve it. Research established that no cross-platform mechanism lets a CLI authenticate once and then sign without prompting on later runs. macOS Authorization Services caches credentials across processes, but its Touch ID path is hard-coded to Apple-signed executables, so a third-party CLI only ever gets a password prompt. Windows `KeyCredentialCacheConfiguration` is entirely undocumented — Microsoft publishes no description for any of its fields. polkit on Linux caches per shell rather than per binary, and its own manual states a retained authorization succeeds even when the details differ, so a second, different decision inside the window would be approved without ever showing its message. For a decision-signing tool that is a correctness failure, not a UX one.

A long-running process avoids all of it. Hold the key, authenticate once when the first request needing it arrives, sign on request from then on.

## The point of the whole design

Because signing only happens through appd, and appd only answers a verified guard binary, the AI cannot sign by calling the crypto itself. It has to come through the door, and the presence check sits at that door. That is what makes the check unfakeable.

## The two modes

**appd runs either way and never refuses to hold the key because of an OS setting.** It detects the environment's protection state at start. The dashboard shows visually which mode is active. When the OS is already in the strong mode there is nothing to display; when it is in the weaker one the dashboard shows the option and how to move up.

Decided by Tim: where an OS offers a stronger posture, warn the user in the dashboard when appd starts and tell them how to reach it. The spec always describes both modes; the dashboard shows the upgrade path only where the OS has one and the machine is in the weaker mode.

**Which OS settings matter, and whether each platform even has two modes, is unresearched.** macOS has System Integrity Protection, which is a real setting the user can turn back on — Tim's machine currently has it disabled, most likely from the .NET-on-Intel work. Linux has `ptrace_scope`, a sysctl. Windows is unknown, and if it has nothing to promote to, Windows shows one mode with no upgrade path. Establish this before building the indicator.

## Done when

On all three platforms appd starts without prompting, prompts once when the first request needing it arrives, holds the key, signs for a verified caller, refuses an unverified one, the dashboard shows the active mode, and shows the upgrade path only where the OS has one.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s


````

### Proposed title and body

````markdown
appd 5 of 5: key and presence — trusted user checks and per-user signing

Part 5 of the appd work. Parent: #62. Depends on #63, #64, #65 and #66.

## What this delivers

The machine-wide server holds the appropriate user's Ed25519 private key and signs only authorized requests from verified AgentGuard clients. It can delegate a presence check to the verified viewer in that user's session and accept the result only for the pending operation. CLI interactions require a corresponding trusted presence path.

## Why it has to work this way

Today the guard never holds a private key — the grant path is verify-only and `GrantStore` never mints. Presence shipped, so proving the user is physically present is solved, but nothing turns that into a signature.

The OS secure stores cannot solve it. Research established that no cross-platform mechanism lets a CLI authenticate once and then sign without prompting on later runs. macOS Authorization Services caches credentials across processes, but its Touch ID path is hard-coded to Apple-signed executables, so a third-party CLI only ever gets a password prompt. Windows `KeyCredentialCacheConfiguration` is entirely undocumented — Microsoft publishes no description for any of its fields. polkit on Linux caches per shell rather than per binary, and its own manual states a retained authorization succeeds even when the details differ, so a second, different decision inside the window would be approved without ever showing its message. For a decision-signing tool that is a correctness failure, not a UX one.

A long-running process avoids all of it. Hold the key, authenticate once when the first request needing it arrives, sign on request from then on.

## User and key ownership

One machine-wide server does not create one shared signing identity. Preserve the existing per-user key, committed public-key, protected-directory, tamper-detection, signer-attribution, and retirement requirements recorded in this work folder's conversation.md.

Reconcile per-user key minting with machine-wide installation. Decide storage locations and permissions, first-use enrollment for another account, unlocked-state scope, and logout behavior. The earlier instruction to snapshot at startup conflicts with the later lazy-loading decision; resolve it explicitly without reintroducing a startup prompt.

## The point of the whole design

Because signing only happens through appd, and appd only answers a verified guard binary, the AI cannot sign by calling the crypto itself. It has to come through the door, and the presence check sits at that door. That is what makes the check unfakeable.

## Delegated presence

Use the existing process-identity work to verify the viewer and its user/session. When the server launched it, require the exact expected process instance. The viewer invokes the OS presence mechanism and reports the result to the server.

The server accepts only a result bound to the intended user, pending operation, and a fresh single-use challenge or an equivalently justified mechanism. Reject replay, mismatched requests, wrong viewers, and results from invalidated connections. Define expiry, cancellation, reconnect, and server-restart behavior.

A symmetric key, MAC, or key derivation is a candidate rather than a selected design. First establish what the authenticated IPC channel already guarantees. If additional keying is required, decide secure delivery, lifetime, erasure, and result encoding. A key derived solely from public process/user/session identifiers is not a secret. Authenticating a response does not by itself prove the OS check happened; trust depends on the verified viewer's implementation and the OS protection actually available.

Investigate each OS's existing presence implementation before assigning the check to the server, viewer, or CLI. No-GUI presentation does not permit automation to manufacture human approval. Presence for machine-wide CLI administration must also work while the server is unavailable; direct OS-service-manager operations remain governed by the OS.

## The two modes

**appd runs either way and never refuses to hold the key because of an OS setting.** It detects the environment's protection state at start. The dashboard shows visually which mode is active. When the OS is already in the strong mode there is nothing to display; when it is in the weaker one the dashboard shows the option and how to move up.

Decided by Tim: where an OS offers a stronger posture, warn the user in the dashboard when appd starts and tell them how to reach it. The spec always describes both modes; the dashboard shows the upgrade path only where the OS has one and the machine is in the weaker mode.

**Which OS settings matter, and whether each platform even has two modes, is unresearched.** macOS has System Integrity Protection, which is a real setting the user can turn back on — Tim's machine currently has it disabled, most likely from the .NET-on-Intel work. Linux has `ptrace_scope`, a sysctl. Windows is unknown, and if it has nothing to promote to, Windows shows one mode with no upgrade path. Establish this before building the indicator.

Reevaluate these protection modes for the split privileged-server/unelevated-viewer architecture. The earlier machine-specific observations are historical, not current environment facts. Separate process authenticity from resistance to tampering with an already trusted process. Do not silently turn a previous per-user threat assumption into a machine-wide security waiver.

## Done when

On all three platforms the server starts without prompting for signing presence, loads a user's key only when a request needs it, and signs for that user after the approved identity, authorization, and presence checks succeed. It refuses unverified callers and mismatched, replayed, expired, or otherwise invalid delegated results. A server-launched viewer's response is accepted only from the expected verified process instance. Users cannot approve or sign one another's requests.

The existing key lifecycle and dashboard protection-mode requirements are met, with equivalent required CLI interaction where no GUI is available. Storage, enrollment, unlocked-state scope, result protocol, and platform presence placement are decided before the implementation contract locks.
````

