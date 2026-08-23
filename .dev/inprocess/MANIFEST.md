# MANIFEST — agent-guard rails & workflow system

The checked-in index of every part and its status. **Read this before working the system, and list from the files, not from memory.** Supersedes `STATE-OF-THE-WORK`; `RESUME.md` stays the verbatim-decision record.

_Verified 2026-08-23. Shipped to `dev` and CI-green on all 3 OS: CrossPlatform, CI/CD (six signed binaries, branch protection on `dev`+`main`), and the whole **CLR-primitive lockdown + coverage gate** umbrella — the lockdown itself, the FileInfo/IFileSystem bridge, the cross-OS test system, the 75% coverage gate, and the cross-OS simulator + evidence. Those run-records are all filed under `./.dev/completed/run-records/2026-08-11-lockdown-and-coverage/`. `dev` is ahead of `main`; no release to `main` is cut yet, by Tim's call. Branch protection: PRs to `dev`/`main` need 1 approval (ruleset 21224660) and a green `gate`; repo admins bypass both (`enforce_admins` off). Where a row below still carries an older status, this header is authoritative._

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
- The run-record moves inprocess→completed **inside the shipping PR, before merge**; REPORT reconciles any straggler left in inprocess — done (mechanical enforcement tracked as an issue)
- Lie-catcher points at `rails-decisions` as its checklist (role + prompt) — done

## Tools (scripts)
| Tool | What | Where | Status |
|---|---|---|---|
| prior-art-ledger | DRY prior-art search | `./.agents/workflows/prior-art-ledger.js` | done, proven |
| hidden-decision-scan | forced-decision scan | `./.agents/workflows/hidden-decision-scan.js` | done, proven (CrossPlatform contract) |
| GROUND script | fan-out (explorer + ledger) | `./.agents/workflows/ground.js` | done — run for real (filesystem-seam / lockdown GROUND) |
| DESIGN script | fan-out (architect panel) | `./.agents/workflows/design.js` | built; lockdown design done interactively + via ad-hoc design/refute agents, not yet script-driven |
| REFUTE script | fan-out (all adversaries) | `./.agents/workflows/refute.js` | run — drove the REFUTE rounds on the test-system + cross-OS work |
| RULE-PHASE script | rule-gen writes rules → independent refute panel | `./.agents/workflows/rule-phase.js` | run several times — CrossPlatform (AG0008/9/10), lockdown, test-system, cross-OS rule phases |
| IMPLEMENT script | single fresh worker, canonical prompt | `./.agents/workflows/implement.js` | built; lockdown IMPLEMENT run via agents, not yet script-driven |
| tools' home | the two real ones live in `./.agents/workflows`; `./.claude/workflows` symlinks to it | done |

## Fence (analyzers — `./analyzers/AgentGuard.Analyzers/`)
- AG0001–AG0007 (architecture rules) — existing
- AG0008 (interop-only-in-crossplatform), AG0009 (no-OS-branching), AG0010 (factory-returns-container) + link-shared-source test — CrossPlatform rule phase, committed
- The CLR-primitive lockdown, bridge, test-system, and cross-OS rule phases **shipped** their analyzer rules: the consolidated owner rule (AG0011) + construction/container pins (AG0017/AG0033/AG0034) + the OS-divergent series (AG0101), the test-system rules (AG0018/AG0019), the store/leaf/temp-root rules (AG0027/AG0030/AGS5443), and the cross-OS seed rules (AG0035/AG0036/AG0037). Some early per-primitive rules were folded/retired in the bridge — see the run-records and issue #28 for the exact final set.
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
| **CrossPlatform — DONE (shipped 2026-08-10)** — 3 libs, `IPlatformFileSystem` + `IDirectoryEnumerator`, `NativeInterop.cs` deleted, chmod→`MakeExecutable`, fail-closed fix, writable-check removed, CRLF fix; contract at `./.dev/completed/run-records/2026-08-07-cross-platform-engine-and-interop/contract.md` | DONE — merged to `dev`, full CI green on all 3 OS. The per-OS `IPlatformFileSystem` libraries replace the Unix-only interop; the RID-selection double-build was fixed (Cli `AdditionalProperties` made conditional on a non-empty RID). |
| **CLR-primitive lockdown + coverage gate — DONE (shipped to `dev` 2026-08-23)** — 3 new assemblies (Abstractions/Boundaries/TestHelpers), the owned interfaces, `ISystemServices` + construction walls, the analyzer fence, the Setup-site cleanup, the FileInfo/IFileSystem bridge, the cross-OS test system, and the 75% coverage gate | DONE — CI green on all 3 OS. Executed through the umbrella run-record `./.dev/completed/run-records/2026-08-11-lockdown-and-coverage/`: `contract.md` (the merged lockdown+coverage spec), `bridge-contract.md` (`8367242`, src-tree route + FileInfo/IFileSystem), `test-system-contract.md` (TestHelpers/builder + test migration + coverage gate), and `cross-os-simulator-and-evidence-contract.md` (Windows/Linux simulator + CI evidence, PR #35). Deferred tails as issues: #27 (native case query), #28 (rule folds), #34 (IVT shrink), #31 (temp-file member). |
| CI/CD — DONE; contract at `./.dev/completed/run-records/2026-08-03-ci-cd-build-sign-release/contract.md` | DONE — full pipeline (build/test/sign/verify/release) green on GitHub all 3 OS; six signed binaries published; branch protection on `dev`+`main` (gate required). `main` last cut at PR #19; no new `main` release cut yet, by Tim's call. |
| interop CA rule + anti-`#if` analyzer | done — AG0008/AG0009 committed in CrossPlatform's rule phase |

## Repo hygiene
- **Commit:** reorg committed on `rules-and-process` (branched off `52cb508`, disposable spike excluded) — `70d020e` (reorg) + `0ec176b` (bracket/tier reconciliation). Not pushed.

## Open your-calls
- prettier format-on-write hook — parked (real config decision, but blocks nothing; may collide with config-region protection).

## Detail lives in
- `./.dev/completed/run-records/2026-08-11-lockdown-and-coverage/RESUME.md` (verbatim decisions of the lockdown run) · `2026-08-07-rules-and-process-reorg/workflow-and-rdd-fix.md` (the RDD-fix record) · `./.dev/README.md` (the folder flow)
