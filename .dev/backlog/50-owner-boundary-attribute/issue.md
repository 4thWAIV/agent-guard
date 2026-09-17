# 50 — Option B: one production owner-boundary attribute as the single source for both the analyzers and the NativeSpec convention test

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/50

---

## What
Coverage sub-task (A) adds guardrail 1 — a NativeSpec convention test that requires every per-OS native-ops owner to have its own spec test. Per Tim's Option A, that test identifies owners by reflecting for a small marker attribute placed on the production owner interfaces (`IObjCRuntime`, `ICredentialPromptNativeOps`, `IPolkitAuthority`).

The existing analyzers (AG0106 / AG0113 / AG0114 / AG0115 / AG0116) still identify native-ops owners via the hardcoded `(namespace, name)` lists in `analyzers/AgentGuard.Analyzers/PresenceContracts.cs` (`AllNativeOpsOwners`, `AllNativePorts`). So after Option A, "which interfaces are native boundary owners" is represented in two places — the production marker attribute and `PresenceContracts` — with nothing keeping them in sync. Add a fourth native owner and forget one place, and either the analyzers stop governing it or the convention test stops requiring a spec test for it.

This is a DRY drift risk (duplicated knowledge), not a hard defect — the two representations serve a compile-time analyzer and a runtime test that genuinely cannot share a source today, and the sets are related but not identical (guardrail 1 needs `IPolkitAuthority`, which `AllNativeOpsOwners` excludes; `AllNativeOpsOwners` includes `IWindowsHelloNativeOps`, which guardrail 1 exempts as interactive-only).

## Do
Rewire `PresenceContracts` and the five analyzers that read it to derive their owner sets from the **same production marker attribute** (via the semantic model / `GetAttributes`), so there is ONE source of truth for the native-owner set: read at compile time by the analyzers and at runtime by the convention test.

Because the analyzers' concept (native-P/Invoke owner, port) and guardrail 1's concept (needs a spec test) are related but not identical, the attribute will likely need to encode role and the interactive-only exemption — design that in rather than forcing a single flat set.

## Provenance
Tim approved Option A now with Option B as a tracked cleanup: "I can accept Option A if we add a clean up to come back and fix it and do Option B."