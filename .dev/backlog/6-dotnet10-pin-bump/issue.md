# 6 — Move .NET 10 pin from 10.0.100 to current patch after Mac upgrades to Sequoia

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/6

---

global.json is pinned to SDK 10.0.100 (runtime 10.0.0) rollForward:disable because it is the only .NET 10 that launches on the Intel dev machine at macOS 14.6.1 (later patches are hardened against macOS SDK 15.5 and get AMFI-killed on Sonoma; decision 38). After upgrading to macOS 15 Sequoia (>=15.5): bump global.json to the current 10.0.x SDK and update the CI x64 --version to match. Contract: .dev/inprocess/2026-08-03-ci-cd-build-sign-release/contract.md (decision 38).