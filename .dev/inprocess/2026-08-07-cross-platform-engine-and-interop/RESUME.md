# CrossPlatform contract — COMPACT + RESUME (handoff 2026-08-09)

Read this first on resume. The CrossPlatform contract is at `./.dev/inprocess/2026-08-07-cross-platform-engine-and-interop/contract.md`. Everything below is on branch `rules-and-process` (NOT pushed). Verify against the live contract and code; do not trust memory.

---

## COMPACT — the story, so nothing is lost

**What this is.** agent-guard's product work: make the engine run on macOS/Linux/Windows by replacing the one Unix-only native call (`libc rename`) with a governed native-interop framework behind a locked interface, delete `NativeInterop.cs`, and fix a config-protection CRLF gap. It unblocks the CI/CD Windows leg (PR #7, RED). This session ALSO built the whole rules-and-process system that runs work (rails skills + workflow scripts + the 8-stage process). The through-line failure all session: I keep deferring real work as an "option," making Tim say NO, giving fake no-shit decisions while making real design decisions silently, reporting things-to-do instead of things-done, and hand-waving instead of grounding in code.

**The process system (built, on `rules-and-process`).**
- Rails skills in `./.agents/skills/` (symlinked from `./.claude/skills`): `rails-read-me`, `rails-solid-code`, `rails-dry-code`, `rails-real-work`, `rails-decisions`, `rails-run-a-workflow`, `rails-write-a-contract`.
- Workflow scripts in `./.agents/workflows/` (symlinked from `./.claude/workflows`): `ground.js`, `design.js`, `refute.js`, `rule-phase.js` (has a fix-mode via an optional `refutation` arg), `implement.js`, `prior-art-ledger.js`, `hidden-decision-scan.js`. Run through the Workflow tool.
- **CRITICAL: launch workflows by `scriptPath` (absolute path to the `.agents/workflows/*.js` file), NOT by `name:`.** `name:` resolution used a STALE cached snapshot once (the first rule-phase run used the old version of the script despite a committed fix). `scriptPath` always runs the on-disk file.
- 8 stages: GROUND → DESIGN → CONTRACT → RULE-PHASE → IMPLEMENT → REFUTE → GATE → REPORT.
- Model assignments (per Tim): hidden-decision-scan hunters = Sonnet, its decider/filter = Opus; the Lie-catcher = Opus; the risk-first architect = Opus; everything else = Sonnet; orchestrator (me) = Opus.
- rule-phase separation of powers: a rule-gen agent writes rules, then INDEPENDENT adversaries refute them (it never grades its own rules — that self-grading bug was fixed).

**dotnet works.** SDK 10.0.100 GA at `~/.dotnet` (+ 8/9). `dotnet build` is green on healthy code. The old "dotnet is dead" memory was WRONG and was corrected/cleared. The one caveat: stay on 10.0.100; patches 10.0.2+ are AMFI-killed on this macOS 14.6.1 until a macOS 15 upgrade.

**The engine build is INTENTIONALLY RED right now.** The rule phase committed the analyzers, which fire against existing code as the RDD forcing function: `error AG0008` at `NativeInterop.cs:22`, `error AG0009` at `CreationHelper.cs:46`. `dotnet build` on the engine FAILS by design until IMPLEMENT cleans up. The analyzer project itself builds 0/0; its tests are 67 passed / 1 failed (the 1 is the intended rule-4 RED, `PosixSourceIsLinkSharedTests`, RED until the Linux project exists).

**The CrossPlatform design is DONE and APPROVED. The interface (Tim approved) — `IPlatformFileSystem`, 8 methods:**
```csharp
// link-target ops (renamed from prototype Repoint/Remove for consistency; IsLinkTarget added)
bool    IsLinkTarget(string linkPath);
string? ReadLinkTarget(string linkPath);
void    MakeLinkTarget(string linkPath, string relativeTarget);   // was Repoint
void    RemoveLinkTarget(string linkPath);                        // was Remove
// executable bit (Tim's design)
bool NeedsExecutableFlag();          // POSIX true, Windows false — callers guard the others on this
bool IsExecutable(string path);      // POSIX: is set; Windows: throws
void MakeExecutable(string path);    // POSIX: chmod +x; Windows: throws
void MakeNonExecutable(string path); // POSIX: chmod -x; Windows: throws
```
Container `IPlatformServices { IPlatformFileSystem FileSystem { get; } }`, from `Platform.Create()`. Caller guards `MakeExecutable` behind `NeedsExecutableFlag() && !IsExecutable(path)` (idempotent, same skip-if-correct as link-target). Namespaces: interfaces + factory in `AgentGuard.CrossPlatform`; POSIX impl in `AgentGuard.CrossPlatform.Posix` (authored in MacOS, `<Compile Link>`-shared into Linux); Windows in `AgentGuard.CrossPlatform.Windows`. Same namespace/type in two assemblies is legal because the engine references exactly one per RID; a real second COPY = duplicate-type compile error (why rule-4 only checks the `<Compile Link>`).

**Threading (Tim approved).** `SetupContext` (a `public sealed record` at `src/AgentGuard.Engine/Setup/SetupContext.cs`, namespace `AgentGuard.Setup`) gains `public required IPlatformServices Platform { get; init; }`, constructed in `SetupContext.ForCurrentProcess()` via `Platform.Create()`. `SymlinkOps` takes an `IPlatformFileSystem` (idempotency compare stays engine-side). `CreationHelper` reads `context.Platform.FileSystem`. `InstallIntegrity.Check` (a `public static` taking only a string today) grows an `IPlatformFileSystem` parameter.

**The 4 rule-phase guardrails (committed `adf1be5`, adversary-clean):** AG0008 (P/Invoke only in `AgentGuard.CrossPlatform.*`), AG0009 (no OS-branch outside it), AG0010 (factory returns the container), and a test that the Linux csproj `<Compile Link>`s the MacOS POSIX source. Boundary matches the 4 EXACT assembly names (`AgentGuard.CrossPlatform`, `.MacOS`, `.Linux`, `.Windows`) — so `.Tests` is NOT exempt. Root name has one owner: `CrossPlatformBoundary.RootName`. Files: `analyzers/AgentGuard.Analyzers/{InteropOnlyInCrossPlatformLibrariesAnalyzer,NoOsBranchingOutsideCrossPlatformAnalyzer,PlatformFactoryMustReturnContainerAnalyzer,CrossPlatformBoundary,WellKnownType}.cs` + their tests.

**The config-protection design smell (found via a design agent, Tim approved the fix IN THIS CONTRACT).** The drift check has TWO equality layers with NO owning composer:
- `SnapshotDiffer.Diff` (`src/AgentGuard.Engine/SnapshotDiffer.cs:24`, pure) — whole-file raw byte `SequenceEqual` (`:35`). The TRIGGER for the whole postcheck.
- `RegionDiffer.RegionMatches` (`src/AgentGuard.Engine/RegionDiffer.cs:39`) via `RegionVerifier` — parsed per-region compare; `adapter.Read` (e.g. `JsonRegionAdapter`) discards line-endings/whitespace/key-order. Synchronous, pure — NOT IO-bound.
- The JOIN is smeared across `FileGuard.PostcheckAsync` (`FileGuard.cs:144`), `ReconcileAsync` (`:231`), `ReconcileRegionsAsync` (`:282-313`). No class answers "did this drift?".
- `SnapshotDiffer`, `RegionDiffer`, `RegionVerifier`, `RegionRestore` have ZERO unit tests — proven only through full-pipeline `tests/AgentGuard.Tests/AcceptanceConfigProtectionTests.cs`. Not an access problem (`InternalsVisibleTo("AgentGuard.Tests")` at `AgentGuard.Engine.csproj:18`).
- **The contract's CRLF plan is WRONG**: it says "normalize CRLF in `RegionDiffer`/canonicalizer," but that layer ALREADY ignores CRLF (parse). The "2 config-protection CRLF failures" are NOT backed by any test in the repo. The REAL fix (approved): introduce a pure IO-free `RegionDriftEvaluator.Evaluate(before, after, region, grant) → DriftDecision` owning the composed decision (byte-gate short-circuit → build `FileChange` → `RegionVerifier` compare → `RegionRestore` effect); move the `ReconcileRegionsAsync:289-312` loop body into it; `FileGuard` keeps only IO orchestration; then write the CRLF spec TEST-FIRST to find the true failing layer.

**Issues filed this session (tell Tim by title, never a bare number):** #8 deploy the rails into other repos (managed-block inject); #9 shared-vocabulary glossary; #10 Codex twins of the workflow scripts; #11 (CLOSED — a foot-gun that recorded an unauthorized rule-phase deferral; reverted); #12 force TDD + zero-effort code coverage + gate ≥75%.

---

## RESUME — do this next, in order

**The interrupt happened MID-INVESTIGATION.** I was grepping the full AG0008/AG0009 blast radius (Tim's point: existing code the new rules will RED that the contract doesn't name = the IMPLEMENT agent making HIDDEN decisions). Tim then said: STOP wasting orchestrator context on grep/read — an AGENT does that — and give the handoff. So: **spin the blast-radius + fix determination out to an agent, don't do it inline.**

**Blast-radius grep results so far (partial, verify with an agent):**
- AG0008 (P/Invoke) REAL engine site: ONLY `src/AgentGuard.Engine/Setup/NativeInterop.cs:22`. (Hits in `analyzers/AgentGuard.Analyzers.Tests/InteropOnly*Tests.cs` are fixture strings inside the CrossPlatform-boundary test cases, not real violations.)
- AG0009 (OS-branch) REAL sites: `src/AgentGuard.Engine/Setup/CreationHelper.cs:46` (`if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(...)` — the chmod; FIX = the executable-flag design, `fs.NeedsExecutableFlag()`+`MakeExecutable`), and **`tests/AgentGuard.Tests/AcceptanceFailClosedTests.cs:20` (`if (System.OperatingSystem.IsWindows())`) — a test that OS-branches, NOT in Surfaces. This is the hidden-decision hole Tim flagged.** (Hits in `NoOsBranchingOutsideCrossPlatformAnalyzerTests.cs` are the analyzer's own intended fixtures.)
- GROUND also noted `UnixFileMode`-based platform behavior in `CreationHelper.cs` / `InstallIntegrity.cs` / test files that is platform-conditional-in-effect but is NOT an `OperatingSystem.Is*`/`#if`/P-Invoke construct, so AG0008/AG0009 do NOT fence it — OPEN QUESTION whether it's in scope for cleanup.

### The ordered work (all authorized unless marked)

1. **Determine the AcceptanceFailClosedTests.cs:20 fix and any other RED sites — via an AGENT.** Read what `AcceptanceFailClosedTests.cs:20` actually does (it OS-branches, probably to skip a Unix-only assertion on Windows), determine how it should become AG0009-clean (mock the platform interface so the test is OS-agnostic, OR remove the branch, OR it is genuinely OS-specific and needs a decided answer). Report to Tim: WHAT the code is, HOW it breaks the rule, the FIX — these are decisions needing his approval. Confirm no other RED sites exist beyond the ones above.

2. **Rewrite the contract's CRLF parts (AUTHORIZED, do it, report done).** Replace the wrong "normalize CRLF in RegionDiffer/canonicalizer" plan everywhere it appears — "What we're building" (`contract.md:95`), success definition "the 2 config-protection CRLF failures are gone" (`:105`), Surfaces "the config-protection canonicalizer (CRLF normalization)" (`:114`), the reuse-ledger `config-crlf-normalization` row (`:128`, which wrongly claims no CRLF normalization exists in the path), and What-to-do step 5 (`:137`) — with the `RegionDriftEvaluator` restructure + a TEST-FIRST CRLF spec that finds the true failing layer. The restructure rides INSIDE this contract (Tim authorized).

3. **Write the executable-flag design into the action sections (AUTHORIZED, do it, report done).** The decision `platform-executable-flag` is recorded but What-to-do/Surfaces still describe only symlink methods. Add: the 4 executable methods in each per-OS impl; their round-trip spec tests; and `CreationHelper.cs:46`'s chmod moving to `fs.MakeExecutable` behind `NeedsExecutableFlag() && !IsExecutable()`. Add the SetupContext/InstallIntegrity threading specifics to Surfaces/What-to-do too.

4. **Add every RED site + its fix to Surfaces + What-to-do** (from step 1's agent), so no site is a hidden IMPLEMENT decision. Include `AcceptanceFailClosedTests.cs:20`.

5. **RUN `hidden-decision-scan` on the updated contract** (via `scriptPath`) to catch any remaining hidden decisions before IMPLEMENT. Tim: "HIDEN decisions should catch that if you run it." Resolve every surviving finding with Tim's words before IMPLEMENT.

6. **THEN IMPLEMENT** (via `implement.js` by `scriptPath`): build the 3 `AgentGuard.CrossPlatform.*` libs (port the proven prototype from commit `d2d6be8` under `proto/platform-interop/` — it is on an UNMERGED branch, not in the tree; retrieve with `git show d2d6be8:proto/platform-interop/<file>` and rename `AgentGuard.Platform`→`AgentGuard.CrossPlatform`), the 1 OS-agnostic spec project, rewire `SymlinkOps`+consumers, delete `NativeInterop.cs`, do the CRLF restructure, add the Dev-Mode error. **IMPLEMENT is TEST-FIRST with a component spec for each new class** (per issue #12). Then REFUTE (via `refute.js`), GATE.

### Open problems / landscape
- CRLF plan wrong (step 2). Executable design not in action sections (step 3). RED-site hidden decisions unresolved (steps 1, 4). hidden-decision-scan not re-run on the updated contract (step 5). IMPLEMENT not started (step 6).
- `UnixFileMode` platform-conditional behavior NOT fenced by AG0009 — decide if in scope.
- Success definition still claims "2 CRLF failures gone" — unsubstantiated; part of the CRLF rewrite.
- Coverage is UNMEASURED: `coverlet.collector` is referenced in `tests/AgentGuard.Tests/AgentGuard.Tests.csproj` but nothing runs it (no CI step, no `.runsettings`, no threshold). Issue #12 tracks fixing that + forcing TDD + ≥75% gate.
- `rules-and-process` branch is NOT pushed. CI/CD PR #7 RED on Windows, unblocked only when CrossPlatform ships. Codex twins (#10), deploy (#8), shared-vocab (#9), prettier hook (parked) still open.

### Hard behavior rules (Tim's — these are the whole point)
- Report what you HAVE DONE, not what you "need to do." Do the authorized work; never make Tim say NO to something he already told you to do.
- STOP offering deferral as an option. When he told you to do it, DO it and report done.
- Don't give trivial no-shit "decisions" to look collaborative while making real DESIGN decisions silently. Surface the real (hidden) decisions; auto-do the trivial. A DRY violation is NEVER a decision — just fix it.
- No scoreboard/counts/confidence labels ("N of M failed", "proven live"). Give the finding and the fix, nothing wrapped around it.
- Full human sentences with hard stops. No fragment-stacking, no telegraph shorthand. Show interfaces AS interface code. Full file paths before every code block. Answer the specific instance, never the general concept.
- Use AGENTS for grep/read/investigation to save orchestrator context; run workflows by `scriptPath` not `name:`.
- Tell Tim when you file a GitHub issue (by title, never a bare number). Verify against live code, never memory.
