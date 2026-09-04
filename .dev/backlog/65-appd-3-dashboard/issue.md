# 65 — appd 3 of 5: the dashboard — the first thing that shows appd doing something useful

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/65

---

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





