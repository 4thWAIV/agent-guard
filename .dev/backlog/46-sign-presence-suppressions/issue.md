# 46 — Sign the five granted presence-code suppressions when the flag-unsigned mechanism lands

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/46

---

Five `[SuppressMessage]` attributes in the presence code are approved exceptions to the analyzer fence, granted by Tim on 2026-08-27 during the OS presence-check contract (`.dev/inprocess/2026-08-23-os-presence-check/`). They are recorded as decisions with his verbatim words in `decisions-settled.md` and the contract's Decisions section ("Granted rule suppressions"). Today the recorded waiver *is* the whole authorization — there is no cryptographic signature yet.

When the flag-unsigned / signing mechanism lands (the interim-suppression model), each of these must be signed, and `refute.js`'s Lie-catcher charge must be updated to verify the signature (not just the recorded waiver). Tim: *"We'll have to update it again later to verify the signature but right now we don't have a signature."*

The five granted suppressions:
1. `LinuxPresenceCheck.Check` — CA1031 (fail-closed boundary returns Error).
2. `LocalAuthentication.EvaluateReplyCallback` — CA1031 (AG0105 mandates the native-block-frame catch-all).
3. `LocalAuthentication.ReplyInvokePointer` — S6640 (AG0104 mandates `&Method`, legal only in `unsafe`).
4. `WindowsUserPresence.TryVerifyWithHelloAsync` — CA1031 (Hello degrades to the password prompt).
5. `WindowsUserPresence.AsyncCompletedHandler.Invoke` — CA1031 (COM completed handler, native frame).

Related: #37 (coverage-exclusion waiver, same "decide/sign at the gate" pattern).