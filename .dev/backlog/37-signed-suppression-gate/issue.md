# 37 — Gate coverage-exclusions and rule-suppressions behind a signed validation

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/37

---

## What
Once the crypto-minting/grant system exists, every "avoid-from-requirement" operation must carry a signed grant (a human-present mint) or the build fails. Two operation classes:
- a **coverage-exclusion**: `[ExcludeFromCodeCoverage]` on product code (which also needs an AG0032 waiver today), and
- a **rule-suppression**: `[SuppressMessage]` / `#pragma warning disable` / `NoWarn`.

No unsigned exclusion or suppression survives the build.

## Interim (until this lands)
Follow the existing minor-suppression pattern: a Tim-approved, manually-monitored `[ExcludeFromCodeCoverage]` + AG0032 `[SuppressMessage]` (both visible and greppable in source, never a silent `.runsettings` exclude). Same standing as any approved rule waiver.

## First real driver
The OS-presence implementations (LocalAuthentication / Windows Hello / PAM). The real sensor **success** path cannot be covered on CI — no human can authenticate on the runner — so the thin native-call member must be coverage-excluded. The per-OS assemblies are gated standalone at a hard 75% (eng/coverage-gate.sh), and AG0032 bans the exclusion in exactly those assemblies, so the interim waiver is required; the eventual signed gate is what makes it honest.

## Relates to
- #33 — S5443 under signed-suppression control (one concrete instance of this general rule).
- #3 — the OS presence check (the first driver).
- #12, #22 — the coverage gate this protects.
- Part of the crypto-minting + presence workstream (.dev/backlog/PLAN-crypto-minting-and-presence.md).