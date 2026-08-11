# DRAFT — the best-practices guide (category spine)

**Status: DRAFT — all six areas ruled.** Built with Tim this session. Every area's Layer-2 principles are locked as of 2026-08-10. What remains is the wiring: the GROUND and DESIGN skills consult it, the hidden-decision scan checks it before surfacing anything to Tim, and the policing adversary enforces it.

## What it is and how it works
A layered reference the DESIGN stage and the GROUND hidden-decision scan both consult, so any forced choice with one correct engineering answer is taken and recorded automatically, and only genuine tradeoffs reach the human.

- **Layer 1 — Categories (Tim owns).** The domains where forced choices recur.
- **Layer 2 — Principles / the constitution (Tim owns).** A few governing rules per category. A Layer-2 principle must (a) decide situations we haven't hit yet, (b) tell the AI when to STOP and raise to Tim, and (c) have several Layer-3 resolutions fall under it. A rule that only restates one settled decision is Layer 3, not Layer 2.
- **Layer 3 — Resolutions (auto).** The concrete choice→answer, each tagged with the principle it obeys. Auto-resolved, cite the governing principle, never approved one-by-one.

Tim sets the "in the weeds" depth per category — for some he wants to see resolutions, for others just principles. Security (area 3) auto-resolves the standard practices in its "do these without asking" list, but its design and trust decisions are reserved for Tim and never auto-resolve.

The rule that keeps it honest: a choice auto-resolves ONLY if an approved principle covers it. If none does, it is a real decision — it surfaces (via the Design Review), and Tim's answer either stays a one-off or he promotes it to a new principle. The constitution grows only when a genuinely new KIND of choice appears.

## How it wires into the process (Tim's requirements, 2026-08-10)

This guide is the "if you have a question, look here first" reference. It is wired in three ways:
- **GROUND and DESIGN reference and use it.** Both skills consult the guide as they run; a forced choice covered by an approved principle is auto-resolved and cites the principle, never carried forward as an open decision.
- **Consulted before anything asks Tim to rule.** Every gate that is about to escalate a decision to Tim — the hidden-decision scan's filter, and any agent or the orchestrator — checks the guide first. A choice covered by an approved principle never reaches Tim.
- **Policed by an adversary.** The Lie-catcher or the Prove-It adversary enforces that a guide-covered choice was auto-resolved and cited (not escalated to Tim, and not silently defaulted where the guide doesn't cover it) — unless this job earns its own dedicated adversary. The Lie-catcher is the natural fit, since this is the approval-boundary domain, but which adversary is Tim's call.

Tim: *"IT SHOULD be part of GROUND and DESIGN skills (they should reference it and use it) and it should be policed by either Lie or Prove unless it earns it own. IT SHOULD also be consulted by anythign that is ABOUT TO FUCKING ASK me to rule on something that is covered in the guide."*

## The six categories

**1. Cross-OS uniformity.** *Purpose:* the guard behaves identically on macOS, Linux, and Windows, and anything that genuinely differs by OS is sealed behind one interface — so no OS is second-class and a build for one OS can't silently ship another's code.
Layer 2 — LOCKED:
- **1a.** Same behavior on all three, or on none — if it can't be identical, it's removed or raised, never left to differ.
- **1b.** OS-agnostic by default; OS-difference is the rare exception, isolated behind the platform interface and declared (the interface change comes to Tim).
- **1c.** Act for the target, never assume you're on the machine that matters — decide by the thing you're acting on (build target, the file's own contents, the user's environment), not the current host/OS/dev box. (RID selection, CRLF source, path separators all fall here.)

**2. Boundary abstraction.** *Purpose:* the code never calls the outside world — filesystem, clock, environment, console, randomness — directly; it goes through an interface we own, so every path is testable with a fake and nothing touches the machine unwatched.
Layer 2 — LOCKED:
- **2a.** Nothing reaches the outside world except through an interface we own.
- **2b.** Built in one place, injected everywhere — nothing reaches for its own dependency deep in the chain.
- **2c.** (added 2026-08-10) Mirror the surface of the type you wrap — when a boundary interface wraps a known type (`IConsole` over `System.Console`), give it that type's method shapes and names, so callers meet a familiar surface and no vocabulary is invented. Stop and raise only if mirroring would expose a member that breaks another principle.
  - Layer 3: `IConsole` mirrors `System.Console` — `Write`, `WriteLine`, `ErrorWrite`, `ErrorWriteLine`, `ReadToEndAsync(CancellationToken)`.
- **2d.** Dependencies point one way — a service lives in the assembly that uses it, so a lower, depended-upon assembly never has a service injected into it from a higher assembly that depends on it. When a lower assembly needs a service, the default fix is to move that service down into it; injecting the service up, or bypassing a rule to allow the backwards call, is forbidden. A backwards dependency STOPS and needs Tim's personal sign-off — anywhere, in any repo — because only Tim rules whether the reorder cost is too high, and it flags the SOLID and Lie-catcher reviews. (Example: the GUID service lives in `AgentGuard.CrossPlatform`, not injected up from `AgentGuard.Boundaries`, which depends on it.)

**3. Security & trust honesty.** *Purpose:* apply the standard security and trust practices by default so nothing is left insecure, while the design and trust decisions — the ones with no single right answer — are the human's, so protection is real and never quietly weakened.
Layer 2 — LOCKED:

**3a — do these without asking:**
- Never put a secret, key, token, or password in source, in committed config, in logs, in error messages, or in output; read them from the environment or a secret store.
- Validate and sanitize anything crossing a trust boundary — user input, network data, file contents — before using it.
- Use parameterized queries for database access; never build a query by string concatenation.
- Escape or encode output for wherever it lands (HTML, shell, SQL) so it cannot be injected.
- Use a standard, vetted crypto library; never invent your own; never hardcode a key.
- Store passwords only as a slow, salted hash (argon2, bcrypt, scrypt), never reversibly.
- Compare secrets and tokens in constant time.
- Take the least privilege and the narrowest scope that works.
- Fail closed on any security check: if it cannot complete, deny (the global fail-closed default from area 5, applied here).
- Verify a download or dependency against a published checksum or signature, and apply routine security patches.

**3b — these come to Tim; this is his area, and none of it auto-resolves:**
- The authentication and authorization model, and the threat model — who we defend against and what we treat as trusted.
- Adding a trust boundary, or deciding what is trusted versus untrusted at one.
- Weakening, removing, or narrowing any protection, guard, or check that already exists.
- Any security choice that is a genuine tradeoff with no single right answer, or where the correct approach would take a major investment of time or money.

**4. Durable form and public names.** *Purpose:* anything committed, shipped, or written into a user's file whose shape is a choice — encodings, line endings, serialization, on-disk layout, and the names, flags, and versions people build against — is chosen deliberately, because it locks the moment it lands and changing it later breaks people or corrupts data.
Layer 2 — LOCKED:

**4a — do these without asking:**
- Text is UTF-8 without a byte-order mark, unless an existing file or a required format says otherwise.
- Files use the operating system's line-ending format, which is deterministic and available from the BCL (`Environment.NewLine`).
- Store and exchange timestamps as ISO 8601 in UTC. A user interface may, and generally should, convert to the client's local time zone for display and convert back to UTC before storing.
- Use a standard, well-supported serialization format and its conventional idioms; never invent a bespoke one.
- Version numbers follow semantic versioning.
- Internal identifiers follow the language's own naming conventions.

**4b — these come to Tim; anything that locks other people in:**
- Any public name people build against: a command, a flag, an environment variable, a config key, a package name, a public API, a file or directory name in the user's tree, a URL or endpoint.
- The on-disk layout of anything we write into the user's project or machine.
- The serialization shape of anything committed or shipped — the schema, the field names, the format itself.
- The versioning and release scheme — how versions are assigned, and what a release is named or tagged.
- Any change to a name, format, or layout that has already shipped.

**5. Fail safe, speak clearly.** *Purpose:* when something cannot be done, fail closed — deny or stop, never silently continue, destroy, or widen access — and give the user an actionable message, not a cryptic error.
Layer 2:
- **5a — GLOBAL default, applies in every area.** Fail closed is the default. Assert every expectation, and when one does not hold, fail closed by throwing an exception — never a silent ignore. A throw is loud and Tim can dial back one he disagrees with; a silent failure hides.
- **5b — speak clearly, do these without asking (none of it comes to Tim):**
  - When something fails, the message names what failed, why, and what the user can do about it — never a bare stack trace or an opaque code as the whole message.
  - Name the specific thing in the message — the file, the setting, the value — not a generic "an error occurred."
  - Never leak a secret, a token, an internal path, or a stack internal into a user-facing message.
  - Match the message to its reader — a plain actionable sentence for the user, the full detail and stack in the developer log.
  - Keep the original error as the cause when you wrap it; never swallow it.
  - Exit non-zero on failure, so scripts and CI can tell it failed.

**6. Proof.** *Purpose:* "done" means proof someone else can re-run, and every dependency is injected so a test can fake it — the guarantee that the code actually works and stays testable.
Layer 2 — LOCKED:

**6a — do these without asking:**
- Prove behavior with a test, and watch it fail before it passes, so the test actually exercises the behavior.
- Tests run against fakes for the outside world — filesystem, clock, network, environment — never the real OS or a live service, so they stay deterministic.
- Every dependency is injected through an interface so a test can substitute it; a class never news-up its own dependency. This settles the recurring "take the interface or build my own dependency" choice — always the interface.
- "Done" means a check anyone can re-run for the same result: the exact command and its real output, never "I ran it."
- Never scaffold or wire a check to pass, and never hard-code an expected value just to match what the code currently does.
- Never weaken, skip, or delete a test to get a green build; fix the code instead.

**6b — these come to Tim:**
- How much rigor a piece of work needs — the level or tier.
- Repinning a deliberately-fixed baseline, such as a golden output or a recorded expected value, when the correct value genuinely changed.
- Waiving or accepting a known-failing or deferred check.

## Next
1. Wire it in per "How it wires into the process": GROUND and DESIGN reference and consult it; `hidden-decision-scan.js`'s filter auto-resolves a finding covered by an approved principle and never surfaces it; the policing adversary enforces that a guide-covered choice was auto-resolved and cited.
2. Open for Tim: which adversary polices it (Lie-catcher, Prove-It, or its own); and the 2c wording under area 2 to correct if needed.
