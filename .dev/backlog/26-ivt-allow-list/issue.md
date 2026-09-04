# 26 — AG0030: allow-list every InternalsVisibleTo grant (needs a config-file design)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/26

---

From the lockdown+coverage DESIGN sweep (2026-08-11). Tim agreed in principle but wants to come back to it — the open piece is the config file that stores the approved grants.

Hole: an `[assembly: InternalsVisibleTo("X")]` grant hands the grantee access to EVERY internal member of the grantor. Today one grant exists (AgentGuard.Engine -> AgentGuard.Tests); the lockdown adds more (CrossPlatform.* -> Boundaries per platform-create-internal; core CrossPlatform -> Boundaries per boundaries-calls-one-crossplatform-factory). Nothing stops a future or widened grant silently expanding Wall 1's trust boundary.

Candidate rule (AG0030): every IVT grant must match a small, named, contract-approved (source-assembly, target-assembly) allow-list; any other grant is a build error.

Open design question: where the allow-list lives. Proposal — a checked-in analyzer AdditionalFile, e.g. `eng/ivt-allowlist.txt`, one `Source => Target` per line, read by the analyzer. Decide the location/format before building the rule.