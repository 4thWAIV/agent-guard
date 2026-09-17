# 53 — Windows presence: WebAuthn as the true-cancellation primary; UserConsentVerifier as backup

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/53

---

Follow-up to the Windows honor-cancellation work (#47). The interim ships **UserConsentVerifier** with a `CheckAvailabilityAsync` fast-fail on headless runners and cooperative cancellation (the await returns on cancel). That unblocks CI and returns promptly on cancel, but it does **not** truly dismiss the on-screen Windows Hello prompt on cancel — the weaker guarantee. This issue tracks bringing Windows up to the same bar as macOS (LAContext `invalidate`) and Linux (polkit `CancelCheckAuthorization`): cancellation actually tears down the prompt.

## Goal
When the cancellation token fires, the Windows presence prompt is genuinely dismissed and the call returns `Cancelled` — matching the dismiss behavior of the other two OSes.

## Mechanism (from Microsoft Learn research)
WebAuthn (`webauthn.dll`) is the only Windows API with documented, thread-safe cancellation of an in-progress Hello prompt:
1. `WebAuthNGetCancellationId(out GUID)` before starting.
2. Put the GUID in `WEBAUTHN_AUTHENTICATOR_GET_ASSERTION_OPTIONS.pCancellationId`.
3. `WebAuthNAuthenticatorGetAssertion(hWnd, ...)` on a worker thread (blocking, shows the Hello UI).
4. On token fire, `WebAuthNCancelCurrentOperation(ref cancellationId)` from any thread — documented to stop the authenticator prompting; the blocked call returns aborted.

Sources: learn.microsoft.com webauthn.dll pages (WebAuthNCancelCurrentOperation, WebAuthNGetCancellationId, WebAuthNAuthenticatorGetAssertion).

## Same biometrics, no loss
WebAuthn's platform authenticator IS Windows Hello — fingerprint / facial recognition / PIN (and iris where supported). No biometric modality is lost versus UserConsentVerifier.

## Setup UX cost
WebAuthn verifies against a registered passkey, so it needs a one-time credential enrollment: a single Windows Hello gesture during `agentguard` setup to create the credential (we automate the RP id / challenge / storage; the gesture itself is user-required by OS design). Gate-time verify is then the same Hello prompt as today.

## Architecture
WebAuthn becomes the **primary** behind the existing `IPresenceCheck` / `IWindowsUserPresence` seam; UserConsentVerifier demotes to the **backup** for machines where WebAuthn can't run (no enrolled credential, older Windows). Both live behind the same OS-agnostic entry point.

## Wildcard to investigate first
`WebAuthNPluginPerformUserVerification` (Windows 24H2+) may do a pure Hello user-verification with cancellation and NO credential enrollment. If viable and its version coverage is acceptable, it removes the setup gesture entirely and largely collapses the primary/backup split. Verify feasibility before committing to full credential enrollment.

## Acceptance
- On token fire, the Windows prompt is dismissed and the port returns `Cancelled`.
- Proven by a deterministic fake test asserting `WebAuthNCancelCurrentOperation` is invoked on cancel (same pattern the Linux polkit-cancel test uses), plus the CI Windows leg's native cancellation smoke stays green.
- Full L1 pipeline (this is the clean-up level for the whole presence-cancellation umbrella).