# Open decisions to resolve before the contract locks — OS presence check

Merged from GROUND's explorer open-questions and the hidden-decision-scan (`wf_8d8f9fd1-947`: 7 findings, 2 dropped as noise). These are the forced, lasting choices DESIGN must settle with Tim before the CONTRACT's Decisions section can lock. Ordered by weight. Most are binding-independent (about presence *semantics*), so they hold whether the macOS binding lands as A (P/Invoke) or B (net10.0-macos).

## HIGH

### 1. The wiring mechanism (GROUND crux)
`ApprovalGate.RequireApproval` is a bare `static ApprovalDecision RequireApproval(string)` — no context, no services, not substitutable — yet its comment and the plan say the real check "drops in with no change to the callers." Reaching a per-OS presence service (on `IPlatformServices`) from a static, and staying fake-substitutable, can't both hold. Options: (a) change `RequireApproval`'s signature to take the `SetupContext`/presence service — changes all three call sites; (b) `SetupContext` gains a presence capability the gate reads; (c) replace the static with an injectable `IApprovalGate`. Settle in DESIGN.

### 2. Linux PAM — service name + a NEW root-owned install footprint (scan)
`pam_start()` needs a literal service name. A dedicated `agent-guard` PAM stack needs a root-owned `/etc/pam.d/agent-guard` file — a NEW privileged install footprint the current user-space (`~/.agentguard`) install does not have. Options: dedicated service+file (new root scope) / reuse `login`/`sudo` (inherits its policy, may lack `pam_fprintd`) / require admins to hand-author it (deny everywhere until they do). Scan recommends the dedicated service AND surfacing the root-owned install as an explicit new-scope decision — Tim's call whether v1 Linux presence takes on a root install at all.

### 3. macOS security policy — biometric-only vs biometric-or-passcode (scan)
The `LAPolicy` constant IS the macOS security claim. `=1` biometric-only bricks presence on Touch-ID-less Intel Macs (which we ship for); `=2` biometric-or-password works everywhere but a typed account password satisfies the gate (an agent that knows the password passes "presence"). Scan recommends `=2`, symmetric with the already-decided Linux fallback ladder.

## MEDIUM

### 4. Windows Hello — Win32 vs WinRT + package identity; run a Windows spike (scan)
WinRT `UserConsentVerifier` is the clean API but likely needs a package-identity manifest (sparse package/appxmanifest) the flat single-file `guard.exe` lacks; Win32 is heavier but needs no identity. We spiked macOS but never Windows. Scan recommends a Windows feasibility spike (mirror macOS) before the mechanism locks — needs a Windows machine or CI (Tim is on a Mac).

### 5. Prompt timeout / cancellation (scan)
No OS presence API self-deadlines and `RequireApproval` is synchronous. Options: block forever / bounded wait that denies on expiry / bounded + cancel when no interactive session. Recommend a bounded, fail-closed wait with one shared value — an unbounded wait silently defeats the decided fail-closed floor in exactly the headless environments it protects.

### 6. ApprovalDecision reason code (scan + GROUND)
Today `(bool IsApproved, string Action, string Detail)`, `Detail` unread, one generic "X was not approved" line. Options: add a reason-code enum (unavailable / declined / cancelled / native-error) + per-reason CLI text / surface `Detail` / keep generic. Recommend the enum now — the field is greenfield and retrofitting it later changes a record downstream code depends on.

### 7. OS dialog text (scan)
macOS `evaluatePolicy` `localizedReason` and the Windows Hello message are non-empty caller strings that ship into the OS's own security dialog. Recommend one reviewed, action-specific English string per command, as a single owned resource (not an inline literal); defer localization but keep the seam.

### 8. Presence is NOT POSIX-uniform (GROUND)
Unlike the filesystem (macOS+Linux link-share one Posix source), presence is three separately authored impls (macOS objc_msgSend / Linux libpam / Windows Hello). Confirm this is the intended shape — no shared body across the three.

## LOW

### 9. macOS entitlements verification (scan)
The spike used the ad-hoc-signed apphost and never called `evaluatePolicy`. Verify `evaluatePolicy` works against the real CI hardened-runtime signed binary; change `eng/signing/agentguard.entitlements` only via a decision-14 amendment if a key is needed.

### 10. Coverage-exclusion shape (GROUND)
Whether the one untestable line (the `evaluatePolicy` success path) needs `[ExcludeFromCodeCoverage]` + an AG0032 waiver, or can be structured so it lives where AG0032 already permits / as a one-per-OS owner (the AG0101 interim-waiver precedent). A DESIGN/impl-shape call.
