# 43 — Revisit proactive revoke-before-check once real polkit behavior is observed (Linux)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/43

---

The presence "no cached yes" guarantee rests on DOCUMENTED polkit behavior we have not yet executed: that an `auth_self` action with no `_keep` mints no cached/temporary authorization and re-prompts every call, and that a retained grant surfaces `polkit.temporary_authorization_id` in the CheckAuthorization result. Verified so far only against the freedesktop docs and the fake — never a real polkit call (the whole Linux path first runs in IMPLEMENT/CI).

The current design DROPS the proactive revoke-before-check: it needs a session subject we do not build (our subject is the unix-process triple), and it is redundant given `auth_self`-no-keep plus the reject-on-`temporary_authorization_id` backstop.

Potential improvement, CONTINGENT on Linux runtime findings: if IMPLEMENT / the CI Linux leg / the manual pre-release check reveals polkit mints or retains authorizations we did not expect (a distro or version quirk, a `rules.d` interaction), add a proactive revoke-before-check. That would require obtaining the session id (logind / `/proc`) and a real-polkit integration test — deferred until the data says it is needed.

Refs: research-polkit.md §7; the presence contract fresh-interaction-no-cached-yes decision.