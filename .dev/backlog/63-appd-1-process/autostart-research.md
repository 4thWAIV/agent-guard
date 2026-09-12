# Autostart definition research — 2026-09-05

Research notes, not approved definitions or a replacement DESIGN result. No autostart entry has been installed or exercised. Tim approved the Windows task-name replacement below; conversation.md now carries that replacement. Research continues into Windows session selection and Linux desktop access.

## Windows task-name conflict

The recorded decision in conversation.md says: “The Windows Task Scheduler entry is a task named `appd` inside a folder called `\\AgentGuard\\`.”

Microsoft's schtasks-create documentation says: “Each task on the system must have a unique name”. Source: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/schtasks-create

Consequently, two users cannot each register a separate task at the same task path. The recorded path contains no user identifier, although the executable path and task principal would differ between installations.

Proposed replacement, awaiting approval: The Windows Task Scheduler entry is named `appd-<user SID>` inside `\AgentGuard\`. Installation substitutes the installing user's Windows SID into the task name and uses that same SID for the logon trigger and task principal. Every lookup and start request uses that user's task name.

The replacement above was subsequently approved as written and recorded in conversation.md. The original name and proposal text above preserve the research history; the task-name conflict is resolved.

## Windows session selection

InteractiveToken runs a task in an existing interactive session; the documentation does not establish that schtasks /Run selects the calling CLI's session when the same account has multiple sessions. The documented schtasks /Run syntax has no session-ID argument. IRegisteredTask::RunEx explicitly accepts a session ID with TASK_RUN_USE_SESSION_ID. This is a documentation gap in the currently selected command path, not proof that schtasks chooses the wrong session.

Microsoft's Task Scheduler protocol specifies that TASK_RUN_USE_SESSION_ID directs execution to the supplied login session. Its permissions checks permit the caller's own user/session combination without imposing a blanket administrator requirement. The task still needs appropriate read and execute permissions. A successful API return alone does not establish that the daemon acquired its lock.

Recommendation, not approved: The Windows start operation calls IRegisteredTask.RunEx for the installed task with TASK_RUN_USE_SESSION_ID and the calling process's session ID. Task Scheduler remains the process launcher. This replaces schtasks /Run for on-demand starts and requires an owned COM boundary, its tests, and approval of the corresponding analyzer changes. No PowerShell wrapper, dependency, or interface shape is selected here.

The Windows logon-trigger and restart behavior across two simultaneous sessions still requires an actual Windows experiment. No Windows execution environment has been used in this research; documentation establishes an explicit on-demand targeting API, not the end-to-end result of the proposed definition.

Additional source: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tsch/77f2250d-500a-40ee-be18-c82f7079c4f0

Sources:
- https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-logontype-principaltype-element
- https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/schtasks-run
- https://learn.microsoft.com/en-us/windows/win32/api/taskschd/nf-taskschd-iregisteredtask-runex

MultipleInstancesPolicy defaults to IgnoreNew and applies to the task. That setting would prevent a second task instance while one is running, including the instance needed in another session. Parallel permits another instance; the daemon's session-scoped lock would remain responsible for refusing a duplicate in the same session. Parallel is not yet approved.

Source: https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-multipleinstancespolicy-settingstype-element

## Other findings collected before the stop

Windows executable actions have only Command, Arguments, and WorkingDirectory fields. There is no stdout/stderr redirection field. A daemon-owned log remains a proposal requiring its own location, format, and retention decisions; no log file has been selected.

Source: https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-exectype-complextype

Windows RestartOnFailure requires both Count and Interval. The schema permits restart intervals from one minute through 31 days and a positive unsigned-byte count. A common ten-second restart interval cannot be expressed in this Windows setting.

Tim accepted the Windows one-minute minimum restart delay. The retry count and logging mechanism remain undecided.

Source: https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-restarttype-complextype

Windows DisallowStartIfOnBatteries and StopIfGoingOnBatteries both default to true. The definition must account for battery operation and the execution time limit before it can be approved for a long-running daemon.

Source: https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-settingstype-complextype

The installed macOS launchd.plist manual documents SuccessfulExit=false under KeepAlive as restarting on unsuccessful exit, and ThrottleInterval as a minimum interval between spawns, defaulting to ten seconds. That is not the same timing definition as waiting ten seconds after a failure. No retry-count field was identified. These settings are not approved.

Sources: local `man 5 launchd.plist`; https://github.com/apple-oss-distributions/launchd/blob/main/man/launchd.plist.5

The installed launchctl manual documents bootstrap as loading definitions into a domain and kickstart as starting a service. The -k option kills a running instance before restarting it, so it does not express an ensure-running operation.

Source: local `man launchctl`.

systemd's user default.target starts with the user service manager. graphical-session.target depends on graphical-session integration. Choosing default.target does not by itself establish that a process started before the desktop has the display environment needed to draw a window later. That remains to be resolved before claiming the future-window requirement is met.

Sources: https://github.com/systemd/systemd/blob/main/man/systemd.special.xml and https://github.com/systemd/systemd/blob/main/docs/DESKTOP_ENVIRONMENTS.md

systemd RestartSec specifies the delay before restarting; Restart=on-failure is available. Detailed restart-limit semantics and final Linux contents were not completed before the Windows naming conflict stopped preparation.

Source: https://github.com/systemd/systemd/blob/main/man/systemd.service.xml

The freedesktop.org rendered manual pages returned HTTP 403. The corresponding upstream systemd documentation source was read from GitHub instead.

## Linux desktop access: research result

The user manager supplies its environment when it starts a service process. systemctl --user import-environment changes that manager environment for later starts; it does not replace the environment of an existing appd process. dbus-update-activation-environment --systemd serves the same purpose for systemd and D-Bus activation.

DISPLAY selects the X11 display; XAUTHORITY can identify the authorization file. WAYLAND_DISPLAY selects the Wayland socket, normally relative to XDG_RUNTIME_DIR. These values belong to the actual graphical session and must not be guessed as :0 or wayland-0. A terminal login can start the user manager before these values exist. Starting the daemon then and importing the variables at graphical login does not fix the daemon's existing environment.

Two mechanisms can address this, neither approved by these notes:

1. Start appd only after the graphical session has exported its display environment. An integrated desktop starts the appropriate graphical-session target after importing the environment; desktops without that integration need a startup hook. This ties daemon availability to the desktop and does not cover a daemon already started from a headless login unless an additional lifecycle policy is chosen.
2. Keep appd independent of the desktop and explicitly provide the display connection information when its UI is initialized. A future desktop command could carry that information over the planned channel. Xlib and Wayland expose explicit connection APIs, so being started as a user service does not itself prevent drawing a window later. This is a proposed direction, not proof that an unchosen .NET UI toolkit supports the complete initialization, logout, and reconnection lifecycle. The channel fields, validation, and UI lifecycle would need design approval. No UI or channel implementation belongs to this research.

The upstream systemd desktop integration document states: “systemd only supports running one graphical session per user at a time.” This concerns simultaneous graphical sessions, not multiple terminal/SSH logins or different user accounts. A promise to display appd windows in multiple simultaneous graphical sessions for one Linux account cannot be inferred from the user service manager alone.

Sources:
- https://github.com/systemd/systemd/blob/main/man/systemctl.xml
- https://dbus.freedesktop.org/doc/dbus-update-activation-environment.1.html
- https://systemd.io/DESKTOP_ENVIRONMENTS/
- https://github.com/swaywm/sway/wiki/Systemd-integration (community-maintained wiki hosted by Sway; corroborating integration example, not a universal desktop guarantee)
- https://xorg.freedesktop.org/archive/current/doc/libX11/libX11/libX11.html (XOpenDisplay)
- https://wayland.freedesktop.org/docs/html/apb.html (wl_display_connect and wl_display_connect_to_fd)

The research answers why inherited environment is insufficient. It does not establish a tested Linux UI integration or approve either mechanism above.

## Capability follow-up after behavior approval

The approved desired behavior remains in conversation.md. These findings do not amend it.

Windows automatic recovery: Microsoft's RestartOnFailure schema and protocol documentation describe retry count and interval but do not establish preservation of the failed instance's session when the same task has concurrent interactive instances. RunEx documents session selection for an explicit invocation, not the scheduler's subsequent retries. Therefore the documentation reviewed does not settle automatic recovery targeting. A local read-only `prlctl list -a` returned only the column headings and no VMs. No Windows experiment was run. A Windows host supporting two simultaneous interactive sessions for the same account is needed for that experiment; ordinary single-session execution cannot prove the required case.

macOS login and recovery: The installed launchd.plist manual says SuccessfulExit-based KeepAlive implies RunAtLoad. The installed launchctl manual says disabling a service prevents loading it until enabled again, and bootstrap accepts a plist path. Apple's launch-job guide documents per-user LaunchAgents discovery at login. These are distinct operations: placing a definition where login discovers it, and loading that definition into the current launchd domain. A candidate mechanism keeps one definition outside the login-scanned folder, exposes it there through a link only when login startup is enabled, and explicitly bootstraps the same definition on manual start if not loaded. Disabling login would remove only the link, not unload the running job. This is a proposed change to the previously approved file placement and needs both local runtime validation and design approval. No file, link, or launchd job was created or removed during this documentation check.

Settings: Windows Set-ScheduledTask documentation explicitly permits updating a definition while an instance runs and says changes do not affect that current instance. Whether a pending automatic retry is a new instance for all relevant settings remains part of native testing. Linux daemon-reload reloads unit definitions separately from restarting their processes; restart-policy values are manager-owned, so exact activation timing must not be equated with appd reading a file. macOS stores a loaded job definition in its domain; no supported in-place definition-reload command was identified in the installed launchctl manual. Merely rewriting its disk plist does not establish that a crash respawn uses the changed policy. Applying changed throttle settings on the next automatic respawn without unloading the live job remains unsupported by the documented mechanism reviewed, not proven impossible by every macOS API. Daemon-owned logging settings are different: appd can read saved settings at its own process entry, regardless of which path started it; the settings format and owner remain undecided.

Sources:
- https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tsch/2ff4aa5a-7bc4-449f-bbb1-27475645867f
- https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-restartonfailure-settingstype-element
- https://learn.microsoft.com/en-us/windows/win32/api/taskschd/nf-taskschd-iregisteredtask-runex
- https://learn.microsoft.com/en-us/powershell/module/scheduledtasks/set-scheduledtask?view=windowsserver2025-ps
- https://raw.githubusercontent.com/systemd/systemd/main/man/systemctl.xml
- https://developer.apple.com/library/archive/documentation/MacOSX/Conceptual/BPSystemStartup/Chapters/CreatingLaunchdJobs.html
- Installed macOS launchctl and launchd.plist manual pages.
