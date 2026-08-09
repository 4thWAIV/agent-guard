# MANIFEST — agent-guard rails & workflow system

The checked-in index of every part and its status. **Read this before working the system, and list from the files, not from memory.** Supersedes `STATE-OF-THE-WORK`; `RESUME.md` stays the verbatim-decision record.

_Verified 2026-08-09. Branch `rules-and-process` (not pushed). Latest commit `8976948`._

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
| prior-art-ledger | DRY prior-art search | `./.agents/workflows/prior-art-ledger.js` | done, proven once |
| hidden-decision-scan | forced-decision scan | `./.agents/workflows/hidden-decision-scan.js` | done, proven once (CrossPlatform contract) |
| GROUND script | fan-out (explorer + ledger) | `./.agents/workflows/ground.js` | **running now** (filesystem-seam, `wf_102e11b0-164`) — first real run |
| DESIGN script | fan-out (architect panel) | `./.agents/workflows/design.js` | built, **never run** (filesystem-seam design done interactively + via ad-hoc design/refute agents) |
| REFUTE script | fan-out (all adversaries) | `./.agents/workflows/refute.js` | built, **never run** |
| RULE-PHASE script | rule-gen writes rules → independent refute panel | `./.agents/workflows/rule-phase.js` | run once — CrossPlatform AG0008/9/10 + link test committed (`adf1be5`) |
| IMPLEMENT script | single fresh worker, canonical prompt | `./.agents/workflows/implement.js` | built, **never run** |
| tools' home | the two real ones live in `./.agents/workflows`; `./.claude/workflows` symlinks to it | done |

## Fence (analyzers — `./analyzers/AgentGuard.Analyzers/`)
- AG0001–AG0007 (architecture rules) — existing
- AG0008 (interop-only-in-crossplatform), AG0009 (no-OS-branching), AG0010 (factory-returns-container) + link-shared-source test — CrossPlatform rule phase, committed
- AG0011–AG0017 + AG0101 (boundary-call ban + construction pin + OS-divergent series) — filesystem-seam contract, to build in its RULE-PHASE
- new rules per task, written in the RULE-PHASE — ongoing

## Cross-tool / deploy
| Part | Status |
|---|---|
| Codex twins of the runner + both tools | to build — issue #10 |
| deploy-skill-set-to-projects (install rails into other repos, self-injected managed block) | to build — issue #8 |
| prettier format-on-write hook | pending Tim's go |

## Product workstreams (the real work)
| Work | Status |
|---|---|
| **CrossPlatform (DO FIRST — Tim ruled 2026-08-09)** — 3 libs, `IPlatformFileSystem` + `IDirectoryEnumerator`, delete `NativeInterop.cs`, chmod→`MakeExecutable`, fail-closed fix, CRLF fix; contract at `./.dev/inprocess/2026-08-07-cross-platform-engine-and-interop/contract.md` | ACTIVE; contract updated + rule phase's AG0008/9/10 committed; **next is IMPLEMENT**; 3 InstallIntegrity writable-check decisions still open (don't block the build — not RED sites) |
| Filesystem seam + boundary rules — 3 new assemblies (Abstractions/Boundaries/TestHelpers), the remaining 5 interfaces, `ISystemServices` + 2 construction walls, AG0011–AG0017 + AG0101, ~53 Setup-site cleanup, the test system; decisions + GROUND facts at `./.dev/inprocess/2026-08-09-filesystem-seam-and-boundary-rules/DECISIONS.md` | **PARKED behind CrossPlatform**; design settled + GROUND done (106 sites/34 files, all captured in DECISIONS.md); resumes after CrossPlatform ships |
| CI/CD — PR #7, green on 3 OS; contract at `./.dev/inprocess/2026-08-03-ci-cd-build-sign-release/contract.md` | RED on the Windows leg (blocked on CrossPlatform) |
| interop CA rule + anti-`#if` analyzer | done — AG0008/AG0009 committed in CrossPlatform's rule phase |

## Repo hygiene
- **Commit:** reorg committed on `rules-and-process` (branched off `52cb508`, disposable spike excluded) — `70d020e` (reorg) + `0ec176b` (bracket/tier reconciliation). Not pushed.

## Open your-calls
- prettier format-on-write hook — parked (real config decision, but blocks nothing; may collide with config-region protection).

## Detail lives in
- `RESUME.md` (verbatim decisions) · `2026-08-07-rules-and-process-reorg/workflow-and-rdd-fix.md` (the RDD-fix record) · `./.dev/README.md` (the folder flow)
