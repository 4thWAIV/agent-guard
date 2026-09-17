# 3 — Require an OS presence check (Touch ID) for install / init / remove

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/3

---

The setup commands that change protected state — `install`, `init`, `remove` — must require an OS presence check the agent cannot pass. The agent runs as the user and can invoke commands, but cannot present the user's fingerprint.

**macOS:** LocalAuthentication (Touch ID / passcode) via a small signed helper built on the `net9.0-macos` first-party binding, invoked behind the `RequireApproval` seam the installer ships. The guard must verify the helper's code signature before trusting its result, so the agent cannot swap the helper for a stub.

**Signing:** self-signed locally via a `guard dev-cert` command; an individual Apple Developer ID certificate plus notarization for beta; the organization certificate for public release.

**Windows (later):** Windows Hello via `UserConsentVerifier` and a window handle.

Second of three builds (installer, this, config-file protection).