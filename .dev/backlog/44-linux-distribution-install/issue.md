# 44 — Linux distribution + polkit policy install: from install.sh to a real distribution system

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/44

---

agent-guard ships as bare self-contained binaries via GitHub Releases — no package manager installs the root-owned polkit `.policy` at `/usr/share/polkit-1/actions/`. Per the presence contract's fail-closed decision, with no policy installed the guard denies, so Linux presence is dead-on-arrival without an install path.

Interim (in the presence contract): ship an `install.sh` with the release that `sudo install`s the `.policy` (and the binary). Linux presence is fail-closed until a user runs it; the policy-integrity test only checks the repo copy, never the installed one, so nothing is red.

Follow-up (this issue): adopt a real distribution system that installs the policy as part of install — candidates Tim named: npm, a Claude plugin, or an OS package (.deb/.rpm). Until then the install.sh is the path.

Ref: the presence contract, decision linux-policy-root-install-fail-closed.