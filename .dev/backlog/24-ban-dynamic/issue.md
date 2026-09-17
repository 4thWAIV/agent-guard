# 24 — Consider banning `dynamic` in shipping assemblies (silently bypasses every boundary analyzer)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/24

---

From the lockdown+coverage DESIGN sweep (2026-08-11), deferred by Tim as possibly too limiting right now — record and revisit if it becomes a real problem.

A `dynamic`-typed receiver (`dynamic fs = ...; fs.Delete(path);`) dispatches through IDynamicInvocation / IDynamicMemberReference operation kinds that `MemberUseScanner` (MemberUseScanner.cs:26-30) never registers — it registers only Invocation/ObjectCreation/PropertyReference/MethodReference. So ONE `dynamic` call slips past AG0011-16, AG0101, and every AG0019-23 rule at once.

Verified: zero `dynamic` usage in the tree today, so a ban would cost nothing now. Deferred because a blanket `dynamic` ban may be too limiting later; decide whether to add the rule (candidate AG0026) or handle another way.