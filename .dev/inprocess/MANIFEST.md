# MANIFEST — agent-guard rails & workflow system

The checked-in index of every part and its status. **Read this before working the system, and list from the files, not from memory.** Supersedes `STATE-OF-THE-WORK`; `RESUME.md` stays the verbatim-decision record.

_Verified 2026-08-08. Branch `proto/platform-interop` (a disposable spike) — everything below is UNCOMMITTED on it._

## Global (personal — `~/.codex`, `~/.claude`)
| Part | What | Status |
|---|---|---|
| Global rules | 5 rules in `~/.codex/AGENTS.md`; `~/.claude/CLAUDE.md` imports it | done |
| how-to-communicate | how to talk to Tim (`~/.claude/skills/`) | done |
| rewrite-in-tim | rewrite a draft before sending (`~/.claude/skills/`) | done |

## Rails (repo — `./.agents/skills/`, each fidelity-checked)
| Rail | What | Status |
|---|---|---|
| rails-read-me | on-ramp / working rules | done |
| rails-solid-code | SOLID checklist (SOLID adversary) | done |
| rails-dry-code | DRY checklist + prior-art-ledger (DRY adversary) | done |
| rails-real-work | anti-low-quality checklist (laziness-auditor) | done |
| rails-decisions | approval boundary (Lie-catcher) | done |
| rails-run-a-workflow | the process | done |
| rails-write-a-contract | contract guide | done |

## The process (inside rails-run-a-workflow)
- 8 stages: **GROUND → DESIGN → CONTRACT → RULE-PHASE → IMPLEMENT → REFUTE → GATE → REPORT** — done
- 9 roles (orchestrator, architect, rule-gen, worker, Prove-It, SOLID, DRY, laziness-auditor, Lie-catcher) — done
- RDD wired in; cleanup law + waiver; separation of powers; `.dev` lifecycle — done
- Lie-catcher points at `rails-decisions` as its checklist (role + prompt) — done

## Tools (scripts)
| Tool | What | Where | Status |
|---|---|---|---|
| prior-art-ledger | DRY prior-art search | `./.claude/workflows/prior-art-ledger.js` | done, proven once |
| hidden-decision-scan | forced-decision scan | `./.claude/workflows/hidden-decision-scan.js` | done, **never run** |
| GROUND script | fan-out (explorer + ledger) | `./.agents/workflows/` | to build |
| DESIGN script | fan-out (architect panel) | `./.agents/workflows/` | to build |
| REFUTE script | fan-out (all adversaries) | `./.agents/workflows/` | to build |
| tools' home | move the two real ones to `./.agents/workflows` + symlink | | to do |

## Fence (analyzers — `./analyzers/AgentGuard.Analyzers/`)
- AG0001–AG0007 (architecture rules) — existing
- new rules per task, written in the RULE-PHASE — ongoing

## Cross-tool / deploy
| Part | Status |
|---|---|
| Codex twins of the runner + both tools | to build |
| deploy-skill-set-to-projects (install rails into other repos) | to build — **issue owed** |
| prettier format-on-write hook | pending Tim's go |

## Product workstreams (the real work — untouched this session)
| Work | Status |
|---|---|
| CrossPlatform — 3 libs, delete `NativeInterop.cs`, CRLF fix; contract at `./.dev/inprocess/2026-08-07-cross-platform-engine-and-interop/contract.md` | locked, not built |
| CI/CD — PR #7, green on 3 OS; contract at `./.dev/inprocess/2026-08-03-ci-cd-build-sign-release/contract.md` | RED on the Windows leg (blocked on CrossPlatform) |
| interop CA rule + anti-`#if` analyzer | to build (in CrossPlatform's RULE-PHASE) |

## Repo hygiene
- **Commit:** all the reorg work is uncommitted on `proto/platform-interop` (disposable). Get it onto a real branch and commit. TO DO.

## Open your-calls
- DESIGN-doc dead path — `./.dev/completed/designs/DESIGN-guard-engine-and-file-guard.md` points to the deleted workflow-with-adversaries folder. Fix or leave.
- prettier hook — go or no.

## Detail lives in
- `RESUME.md` (verbatim decisions) · `2026-08-07-rules-and-process-reorg/workflow-and-rdd-fix.md` (the RDD-fix record) · `./.dev/README.md` (the folder flow)
