# appd-1-process — current conversation brief

Status: Consolidated wording for Tim's review before GROUND. CLI, server, and viewer are provisional role names. The live issues remain authoritative until their replacement wording is approved and published.

## Purpose and process roles

AgentGuard uses the same application in three cooperating roles. The CLI receives requests from a person or automation. One headless server runs per machine and coordinates requests. A separate viewer runs as the user in the graphical session where interaction is needed. Multiple users or sessions can have separate viewers connected to the same server.

Ordinary CLI use remains CLI use. Starting the server is a separate operation from running the application in server mode. Installation and explicit start requests use the OS service manager's canonical start path.

The server's lifetime is independent of graphical logins and viewer processes. Closing a viewer does not stop the server. The CLI provides a way to close viewers.

## Request flow and process identity

The server receives a CLI request and obtains the connecting process's identity from the OS. It identifies the appropriate user and session, then uses an authenticated viewer connection for that session. If no viewer is connected, it requests an OS-mediated launch, waits for an authenticated connection, and delivers the request.

The CLI, server, and viewer use the planned mutual process-identity mechanism. When the server launches a viewer, its returning connection must match the actual launched process instance. User and session association come from OS evidence. The identity check must distinguish that instance from a later process reusing its PID.

The viewer presents information and collects responses. The server retains responsibility for authorizing operations and, when the key work is implemented, signing.

## Presence and signing

The server may delegate presence checking to the viewer in the requesting user's session. The viewer calls the OS presence mechanism and returns its result over the authenticated connection. The server validates that result before using it to authorize the pending operation.

The identity part owns process verification, user/session association, and expected-instance matching. The key/presence part owns delegated-result freshness, operation binding, replay rejection, and authorization. The viewer part supplies the user-facing OS call.

Server startup is unattended. Presence checking and key loading happen when a request first needs them.

Every user working on a project mints their own key, and all the public keys are committed. The server keeps signing and approval associated with the correct user. The full key lifecycle belongs to the key-and-presence work.

## Graphical and CLI interaction

AgentGuard detects whether graphical interaction is available to the requesting client and uses that result to select the default interaction mode. An explicit interaction-mode option takes precedence over detection.

Every graphical view has a CLI alternative. A request without usable graphical interaction can be completed through its CLI interaction, subject to the same authorization and presence requirements.

The viewer operates in the requesting user's graphical session on Windows, macOS, and Linux. Coverage includes simultaneous Windows sessions for the same account and Linux Wayland desktops without systemd session integration.

The UI uses a .NET web server, a capable browser control, and React.

## Installation and administration

Installation establishes protected server binaries, configuration, and OS service definitions. Machine-wide installation requires the appropriate OS administrative authorization. Viewer processes run as their users.

Stopping the machine-wide server and changing its configuration are machine-wide administrative operations. Requests through the AgentGuard CLI require both a presence check and OS administrative authorization. Direct operations through the OS service manager are governed by the OS.

The presence check for CLI administrative operations must be callable without a running server.

The stop command asks the OS service manager to stop the server. It does not change autostart settings or prevent subsequent explicit or client-requested starts. An intentional stop does not trigger crash recovery.

Automatic startup is enabled by default and configurable. Disabling it affects automatic starts only. It does not stop a running server, prevent explicit or client-requested starts, or disable crash recovery for a server that is subsequently started.

## Recovery and settings

The recovery targets are:

- Windows waits 60 seconds and attempts three restarts.
- Linux waits 60 seconds and permits four starts within ten minutes, including the initial start.
- macOS throttles launches to once per 60 seconds and continues retrying without a count limit.

An explicit Linux start clears an exhausted retry counter and requests another start. Automatic client requests leave the counter intact and report how the user can retry.

Save valid changes immediately. Automatic-start enablement affects future automatic starts. Changing it neither starts nor stops the server.

Restart-policy and logging changes apply at the next daemon start. Report settings that are saved but not yet active. Never restart appd merely because someone changes a setting.

## Logging and diagnostics

Logs are kept in user-specific folders. Each daemon process run has its own folder, named with its UTC start time and a unique identifier.

- Keep the latest **five completed runs, plus all currently active runs**.
- Rotate each file at **10 MiB or 24 hours**, whichever comes first.
- Keep **five files per run**, including the current file.

Logs are UTF-8 text. The CLI displays logs and can follow them across file rotation and server restarts. Failures before the server can open its log remain diagnosable through OS-manager diagnostics.

Every failure a user sees says what went wrong, why, and how to fix it, and `guard doctor` can diagnose it. Status reports whether the server is running. Installation and doctor handle the OS service definition and its connection to the installed binary.

## Implementation boundaries

Platform contracts belong in `AgentGuard.Abstractions.Contracts`. Internal implementations belong in the corresponding `AgentGuard.CrossPlatform.*` assemblies, with private constructors and static `Create()` factories. Composition goes through `PlatformServices.Create()` and `IPlatformServices`.

No new static classes. New capabilities are instance classes reached through interfaces. Tim reviews the actual C# declarations before the interface shape is settled.

OS definitions that ship with the application are repository files embedded in the binary and read at installation. Their identifiers have one owner and are checked against the embedded definitions.

Raw OS and CLR primitives are reached through their owning interfaces. The existing approval covers adding owners for `System.IO.FileStream` and `System.Diagnostics.Process` to `OwnedPrimitives.cs` where this work needs them. New guardrail rules receive Tim's approval before they are written.

## Delivery order and GROUND

The communication transport is delivered first. It provides connection establishment, message sending and receiving, connection closure, and OS-derived peer information. Tests can exercise both endpoints through the actual OS transport within one process, including checking the peer identity against that process. The transport is independently usable by the CLI, server, and viewer.

The process delivery consumes that transport. Cross-process and installed-service integration exercise the boundaries that require distinct processes or users. Product identity verification, viewer launching, and presence-result validation belong to their respective deliveries.

The process delivery provides the machine-wide server's installation, single-instance lifetime, discovery, starting, stopping, recovery, configuration, and diagnostics.

The remaining deliveries provide the dashboard/viewer, mutual process identity, and key/presence functionality. Session launching and delegated presence are investigated during process GROUND wherever they constrain the server's architecture.

GROUND establishes the existing code and reusable owners, the OS capabilities needed by this brief, and the constraints on their implementation. It distinguishes documented capabilities from native experiments. The user experience should be as consistent across Windows, macOS, and Linux as their capabilities allow.

Research questions and remaining mechanism choices are kept in [handoff.md](handoff.md). This brief is the document to review before authorizing GROUND.
