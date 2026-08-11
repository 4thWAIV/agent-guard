# CrossPlatform + session RESUME (2026-08-09, mid-session handoff)

Read this first on resume. Branch `rules-and-process` (NOT pushed). Verify against the live contract and code; do not trust memory.

## WHERE WE ARE RIGHT NOW
- The CrossPlatform contract is IMPLEMENTED and committed as a **local checkpoint `82ab9ef`** — NOT pushed, NOT yet accepted.
- **REFUTE round 3** (run `wf_9fad5efd-02e`): Prove-It, SOLID, laziness, and the **Lie-catcher all PASS**. DRY FAILs on ONE finding (below). Prove-It ran a live `guard install`/`doctor` on macOS — the real POSIX path works end to end (symlink created, exec bit set + read/write preserved, idempotent, no stray temp).
- All decisions are recorded in `./contract.md` (link-target interface, executable flag, enumerator seam, remove-writable-check, `windows-atomic-repoint` = `FSCTL_SET_REPARSE_POINT`, `scan-resolutions` 1–7, `refute-round2-resolutions`, the `[UnsupportedOSPlatformGuard]` idiom).
- Exception type: **`PlatformNotSupportedException` is approved** (Tim: "PlatformNotSupported is fine"; his earlier decision word "NotImplemented" was shorthand). The code already throws it — NO change.

## IMMEDIATE NEXT STEPS (do these, in order)
1. **One L2 fix pass** — the ONLY remaining code fix (the round-3 DRY FAIL): the Windows link-kind block (infer directory-vs-file, then call the right `CreateSymbolicLink`) is duplicated verbatim in `WindowsFileSystem` and the engine test double `ManagedPlatformFileSystem`. Collapse it into `PlatformFileSystemShared` (the shared OS-uniform managed home) and have both call it. Worker: no push, no commit.
2. **Re-REFUTE (round 4)** via `refute.js` by scriptPath. Derive `changedFiles` from `git show --stat <commit>` (round 3 flagged that I hand-curated an incomplete list). The always-gate holds: REFUTE + Lie-catcher run on every L2 output.
3. If clean → **CrossPlatform is ACCEPTED locally.** Commit. The only thing left is Tim's **PUSH** (his call, his timing) to run the three real CI legs — which finally proves Linux + Windows and unblocks the CI/CD contract (PR #7).

## WHAT EXACTLY WORKS NOW (honest)
- **macOS:** fully proven — build 0/0, ~180 tests, live install works, POSIX ops proven by the 13-test spec suite against the real impl.
- **Linux:** the SAME POSIX source as macOS (one authored file, `<Compile Link>`-shared, not a copy), so identical by construction — but NOT yet run on Linux.
- **Windows:** compiled and fully wired (FSCTL re-point, UTF-16 bindings, RID-based selection) — the native calls have NEVER executed; provable only on Windows CI. **This is an accepted-deferred item — do NOT re-raise it as a caveat in reports.**
- **Engine:** OS-clean — no native interop or OS-branch outside `AgentGuard.CrossPlatform.*`; `NativeInterop.cs` and the writable-check are gone; symlink/exec/rename go through `IPlatformFileSystem`.

## ROADMAP after CrossPlatform (order is Tim's call; my recommendation below)
1. **CLR-primitive lockdown** — the next big product contract Tim sequenced ("then the CLR-primitive cleanup"). GROUND done, design settled, all captured in `../2026-08-09-clr-primitive-lockdown/DECISIONS.md`. Ready to write the contract (3 new assemblies Abstractions/Boundaries/TestHelpers, 7 boundary interfaces + `ISystemServices` container + 2 construction walls, AG0011–AG0017 + AG0101, ~53 Setup-site cleanup, the test system).
2. **Finish the process pieces started this session** (recommended FIRST — they make the lockdown run cleaner):
   - **Best-practices guide** — `../DRAFT-best-practices-guide.md`. Categories 1&2 Layer-2 locked; 3–6 still to rule (one at a time, show code); then wire into `hidden-decision-scan.js`'s filter.
   - **Design Review process** — DRAFT at `../DRAFT-design-review-process.md`. Codify into `rails-run-a-workflow` when Tim's satisfied, then delete the draft.
   - **Accept-and-silence** mechanism — issue #15.
   - **REFUTE-adversary isolation** — owed to Tim as a Design Review (an adversary edited the working tree round 2; mitigated for now by committing before REFUTE).
3. **CI/CD contract (PR #7)** — finishes itself when Tim pushes; the pipeline goes green on all three OS.
4. **Backlog issues:** #12 (force TDD + code-coverage gate ≥75%), #10 (Codex twins of the workflow scripts), #8 (deploy rails into other repos), #9 (shared-vocab glossary), #13 (seam-at-every-boundary rule — feeds the lockdown), #14 (boundary-violation inventory).

## PROCESS/SYSTEM CHANGES CODIFIED THIS SESSION
- **L1/L2/L3 level model** in `rails-run-a-workflow` (replaced self-action/low/normal). L1 = gold standard (all roles, nothing optional). L2 = lighter delegated (worker + Prove-It + Lie-catcher ALWAYS; DESIGN/DRY/Laziness if needed; NO SOLID → L2 only where a SOLID violation isn't possible, else L1). L3 = orchestrator direct (Lie-catcher only when Tim asks at assignment; no contract).
- **L2-worker contract** + the **always-REFUTE+Lie-catcher gate** codified in `rails-run-a-workflow`.

## HARD BEHAVIOR RULES (Tim's — the whole point)
- NEVER surface a best-practice / one-correct-answer choice as a decision — take it, record it. (Failed twice this session: the RID pick, the exception type.)
- Once you put a decision in front of Tim you can NEVER retract or self-decide it — only Tim rules; continue until he does.
- Every decision: the PROBLEM FIRST in the plainest words (the forcing thing, e.g. "the OS-specific libs must use this and can't depend on the Engine"), then the options (pick marked), then the actual code/interface shown. No backreferences, no mid-sentence asides, no walls, no bare IDs like "fix 3".
- Verify against live code; never trust a worker's self-report — REFUTE + Lie-catcher always.
- Report what you HAVE DONE, not to-dos. No PSA on accepted limitations.
- Announce every GitHub issue by title + URL. Use L2 agents for the heavy work; run workflows by scriptPath.

## WHERE THINGS LIVE
- CrossPlatform contract: `./contract.md`. Checkpoint commit `82ab9ef`.
- CLR-primitive lockdown: `../2026-08-09-clr-primitive-lockdown/DECISIONS.md`.
- Design Review draft: `../DRAFT-design-review-process.md`. Best-practices guide draft: `../DRAFT-best-practices-guide.md`.
- Index: `../MANIFEST.md`. Workflows: `.agents/workflows/*.js` (run by scriptPath). Issues #8–#15 at github.com/4thWAIV/agent-guard.
