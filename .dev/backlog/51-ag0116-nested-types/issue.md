# 51 — Extend AG0116 (no-state-in-native-ops) to a native-ops owner's nested/private types

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/51

---

## What
`AG0116` (NoStateInNativeOpsAnalyzer) forbids instance state on a native-ops owner (`ObjCRuntime`, `WindowsHelloNativeOps`, `CredentialPromptNativeOps`), but only checks the owner type itself. A private nested type inside an owner sits outside its scope.

## Why
`WindowsHelloNativeOps.AsyncCompletedHandler` (the WinRT completed-handler CCW) legitimately holds `private readonly Action<int> _onCompleted` — necessary state to route the async completion back to the orchestrator, so it is NOT a violation today. But it is invisible to AG0116 because it implements `IAsyncOperationCompletedHandlerUserConsent`, not one of the native-ops owner interfaces. A future COM-callback shape could reuse the same nested-type loophole to hide real, disallowed state with no analyzer signal.

Flagged by the coverage-refactor REFUTE (Prove-It). Not a defect in the current code; a mechanical-guard gap to close (fix the class, not the instance).

## Do
Extend AG0116 so a native-ops owner's own nested/private types are also scanned for disallowed instance state, with a documented exemption for the genuinely-necessary callback-delegate field on a COM/WinRT completed-handler CCW (or a tighter rule that permits exactly that shape).