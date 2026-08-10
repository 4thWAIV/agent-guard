# DRAFT — the best-practices guide (category spine)

**Status: DRAFT, in progress.** Built with Tim this session. Categories 1 and 2 have their Layer-2 principles locked; 3–6 still need Layer-2 ruled. When complete, it wires into the hidden-decision scan so best-practice choices auto-resolve and never reach Tim.

## What it is and how it works
A layered reference the DESIGN stage and the GROUND hidden-decision scan both consult, so any forced choice with one correct engineering answer is taken and recorded automatically, and only genuine tradeoffs reach the human.

- **Layer 1 — Categories (Tim owns).** The domains where forced choices recur.
- **Layer 2 — Principles / the constitution (Tim owns).** A few governing rules per category. A Layer-2 principle must (a) decide situations we haven't hit yet, (b) tell the AI when to STOP and raise to Tim, and (c) have several Layer-3 resolutions fall under it. A rule that only restates one settled decision is Layer 3, not Layer 2.
- **Layer 3 — Resolutions (auto).** The concrete choice→answer, each tagged with the principle it obeys. Auto-resolved, cite the governing principle, never approved one-by-one.

Tim sets the "in the weeds" depth per category — for some he wants to see resolutions, for others just principles. Security (category 3) is pinned at depth zero: it never auto-resolves.

The rule that keeps it honest: a choice auto-resolves ONLY if an approved principle covers it. If none does, it is a real decision — it surfaces (via the Design Review), and Tim's answer either stays a one-off or he promotes it to a new principle. The constitution grows only when a genuinely new KIND of choice appears.

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

**3. Security & trust honesty.** *Purpose:* protections match the real threat — a lazy AI, not a same-user admin — so we never ship security theater that defends nothing, and we keep the boundaries that do matter tight.
Layer 2 — **RULE: this category NEVER auto-resolves.** Any choice touching what's trusted, signed, verified, or defended STOPS and comes to Tim (depth pinned at zero). It grants zero power to decide — it only forces escalation, so an agent can't invoke it to cut a real protection. Specific principles not yet ruled.

**4. Durable form and public names.** *Purpose:* anything committed, shipped, or written into a user's file whose shape is a choice — encodings, line endings, serialization, on-disk layout, and the names/flags/versions people build against — is chosen deliberately, because it locks the moment it lands and changing it later breaks people or corrupts data. (May split: data-shape vs public-names are two purposes.) Layer 2 NOT yet ruled.

**5. Fail safe, speak clearly.** *Purpose:* when something can't be done the guard fails closed, never silently destroys or widens access, and gives the user something actionable instead of a cryptic error. Tim's addition: **throw rather than fail silently** — a throw is loud and he can dial back one he disagrees with; a silent failure hides. Layer 2 NOT yet ruled beyond that.

**6. Proof.** *Purpose:* "done" means proof someone else can re-run — test-first, real fakes instead of the real OS in tests, no metric wired to pass. It drives design too: the principle "every dependency is injected so a test can fake it" auto-resolves the recurring "interface or new-up its own dependency?" choice to always the interface. Layer 2 NOT yet ruled.

## Next
Rule Layer 2 for categories 3–6 (one category at a time, code/interfaces shown where relevant), then wire the guide into `hidden-decision-scan.js`'s filter so a finding matching an approved principle is auto-resolved and never surfaced.
