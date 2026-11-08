# Make the thirteen remaining silent failures loud

## Decisions

**An unreadable symlink throws, and the callers catch.** Tim approved this wording on 2026-11-08 in the signed package `the-silent-failures-still-in-the-code-1.0.0-local07b`, decision `symlink-shape`: "Throw, and callers catch — a smaller change that raises an expected case as an exception." The recommendation put to him was a third answer on the contract, and he chose against it.

**All five groups ship as one piece of work.** Approved in the same package, decision `scope`: "All five as one piece — one pass over every file, one adversary round for all of it." The recommendation put to him was the symlink fix first on its own, and he chose against it.

**A place that cannot carry a reason raises instead, and the catch goes where a reason fits.** Tim approved this wording on 2026-11-08 in the signed package `the-contract-asks-for-three-things-the-c-1.0.0-local9d1`, decision `where`: "Raise it, catch where it fits — no new type, no interface change, nothing public moves." The alternative put to him was changing four approved contract interfaces so each carries its own reason out, and he chose against it.

GROUND found three instructions in this contract that the code cannot carry out, which is why that decision was needed. `PathCanonicalizer.Canonicalize` returns `CanonicalPath`, not a verdict. Five of the six places in group 3 return a bool, a nullable grant, a byte buffer or a list, none of which holds a reason. All three cleanup places in group 4 run where no verdict exists yet. Every one is now written to raise, with the catch placed at the nearest layer that already returns a reasoned result.

**This contract covers a change under `analyzers/**`, which is a decision-gated area.** The change there is one test file, not a rule. Approving this contract is the approval for it.

## Rules to add

None. No analyzer rule is added by this work. The seven analyzers that go inert do so when a type is absent from a compilation, which no analyzer rule can observe; a test resolving the container from the live assembly is what catches it, and that test is in What to do.

## The standard

Nothing fails silently. Every place listed in Surfaces either reports what went wrong or raises it to a caller that does. No place listed may continue, return a value indistinguishable from success, or report a result as checked when it was not.

An unreadable symlink is raised, not flattened into "not a link". A denial carries the reason it was made. A cleanup failure is surfaced. A pipeline step that checked nothing does not report success.

## Success definition

ALL criteria met AND no errors in the system as a result of the change.

This run's end state: every item in Surfaces is changed as What to do says; `dotnet build` reports 0 warnings and 0 errors; `dotnet test` reports 0 failed; `bun run typecheck`, `bun run lint` and `bun test` are clean with the 75 percent floor held; every numbered Acceptance check passes; and the thirteen findings listed in `.dev/backlog/106-silent-failure-cleanup/issue.md` are each either changed or recorded in that file as deliberately unchanged with the reason.

## Surfaces

Group 1, the unreadable symlink:

- `src/AgentGuard.CrossPlatform/PlatformFileSystemBase.cs:32-50` — `ReadLinkTarget`, and `IsLinkTarget` at line 29 which is defined as that read being non-null.
- `src/AgentGuard.Engine/PathCanonicalizer.cs:68`
- `src/AgentGuard.Engine/Setup/SymlinkOps.cs:40`
- `src/AgentGuard.Engine/Setup/BinSymlinkCondition.cs:28`
- `src/AgentGuard.Engine/Setup/CurrentSymlinkCondition.cs:28`
- `src/AgentGuard.Abstractions/Contracts/IPlatformFileSystem.cs:23-36` — the contract's own description of what null means, which is silent on an unreadable link.
- `src/AgentGuard.Engine/Setup/CreationHelper.cs:235-243` — `RepairLink`, the one caller of `EnsurePointsTo`.
- `src/AgentGuard.Engine/Setup/InstallIntegrity.cs:126-131` — `HasCurrentEntry`, an `IsLinkTarget` caller the first draft of this list missed.
- `src/AgentGuard.CrossPlatform.MacOS/Posix/PosixFileSystem.cs:56,79` and `src/AgentGuard.CrossPlatform.Windows/Windows/WindowsFileSystem.cs:73,80,93,98` — the per-OS link writers, `IsLinkTarget` callers the first draft missed.
- `tests/AgentGuard.CrossPlatform.Tests/PlatformFileSystemSpecTests.cs:123-156`
- `tests/AgentGuard.TestHelpers/ManagedPlatformFileSystem.cs:36`
- `tests/AgentGuard.TestHelpers/InMemoryFileSystemStore.cs:351-360,552`

Group 2, the seven analyzers that stop firing:

- `analyzers/AgentGuard.Analyzers/BoundaryServices.cs:52-75` — `ResolveTree` and `Resolve`.
- `analyzers/AgentGuard.Analyzers.Tests/` — the new test named in What to do.

Group 3, denials with no reason:

- `src/AgentGuard.Engine/GrantStore.cs:133-141`
- `src/AgentGuard.Engine/GuardEngine.cs:156-164`
- `src/AgentGuard.Boundaries/Ed25519SignatureService.cs:56,62`
- `src/AgentGuard.CrossPlatform.Windows/Presence/WindowsUserPresence.cs:154,175`
- `src/AgentGuard.Engine/BuildOutputSkipRule.cs:47,51`
- `src/AgentGuard.Engine/FileGuard.cs:383-387`

Group 4, cleanup that swallows the failure:

- `src/AgentGuard.Engine/PrivilegedWriter.cs:43,47`
- `src/AgentGuard.Engine/Pipeline.cs:180-200`
- `src/AgentGuard.CrossPlatform.MacOS/Posix/PosixFileSystem.cs:169,173`
- `tests/Shared/TestTempDirectory.cs:23-37` — the first draft of this list put it under `AgentGuard.TestHelpers`, which is wrong.
- `tests/AgentGuard.Tests/SetupHarness.cs:207-223` — the first draft put it under `tests/Shared`, which is wrong.
- `tests/AgentGuard.TestHelpers/FixtureProject.cs` — the first draft put it under `tests/AgentGuard.Tests`, which is wrong.
- `tests/AgentGuard.Tests/CliMachine.cs`
- `tests/AgentGuard.CrossPlatform.Tests/PlatformFileSystemSpecTests.cs:302`

Group 5, a pipeline that can report success having checked nothing:

- `.github/workflows/ci.yml` — the win-arm64 and linux-x64 legs, the upload steps using `if-no-files-found: warn`.
- `eng/check-copied-modules.mjs` — not run by CI.
- `eslint.config.js:20-21` — `no-unsafe-*` off for all of `ts/**`.
- `eng/local-cosign.sh`, `eng/local-cosign.ps1`
- `eng/version.compute.targets:81,86`

The exact path of each test-cleanup file is to be confirmed against the tree by the worker before it is changed; a file this list names at the wrong path is this list being wrong, not a reason to skip it.

## Reuse ledger

None — this change introduces no new capability.

Every mechanism this work needs already exists and is reused. `Verdict.Deny(string reason)` at `src/AgentGuard.Abstractions/Verdict.cs:38` already carries a deny reason, and `FileGuard` reaches it one frame above `TryReadCurrentAsync`. `CaptureFailed(string Reason)` at `src/AgentGuard.Abstractions/CaptureFailed.cs:10` already carries a capture failure's cause, and `FileGuard.CaptureAsync` already produces one from either exception. `ConditionState.Broken` already carries a message, which is where the two setup conditions report an unreadable link. `RepairOutcome.NotRepairable(string)` already carries one, which is where a failed link repair reports. And the symlink read raises the `IOException` and `UnauthorizedAccessException` the framework already throws. No new exception type, no new result type, no new logging or diagnostic mechanism is added.

`Verdict.Warn(string message)` at `src/AgentGuard.Abstractions/Verdict.cs:46` was cited here in the first draft as the surface for group 4. GROUND established that no verdict exists at any of the three cleanup places, so it is not reachable there and this work does not use it.

## What to do

**Group 1.** Remove the `catch (IOException)` and `catch (UnauthorizedAccessException)` blocks from `ReadLinkTarget` so both propagate. Keep null as the answer for a path that is not a link. Update the `IPlatformFileSystem.ReadLinkTarget` documentation to say null means not a link and that an unreadable link raises.

`PathCanonicalizer` lets the exception through rather than treating an unreadable link as a non-link. It returns no verdict: `Canonicalize` returns `CanonicalPath` and its signature does not change. The file already throws `IOException` at the link-depth ceiling, so an exception leaving `Canonicalize` is its existing shape. The catch goes where a reason fits — `FileGuard.CaptureAsync` already turns both exceptions into `CaptureFailed` carrying the message, and `FileGuard.PrecheckWriteAsync` produces a `Verdict`.

`BinSymlinkCondition` and `CurrentSymlinkCondition` catch and return `ConditionState.Broken` carrying the real cause, because that type already carries a message. They must stop reporting an unreadable link as a link that "points at ''".

`SymlinkOps.EnsurePointsTo` lets the exception through. Today an unreadable link is not equal to the wanted target, so it calls `MakeLinkTarget` and overwrites the link; that must stop. Its one caller, `CreationHelper.RepairLink`, already catches `UnauthorizedAccessException` and returns `RepairOutcome.NotRepairable(message)`, and now catches `IOException` and does the same.

`IsLinkTarget` raises for the same reason `ReadLinkTarget` does, because it is that read. Its other callers — `InstallIntegrity.HasCurrentEntry`, `PosixFileSystem`, `WindowsFileSystem` and `InMemoryFileSystemStore` — let the exception through unless DESIGN places a catch nearer. The per-OS link writers already throw `IOException` from `MakeLinkTarget`, so this is their existing shape.

Update `ManagedPlatformFileSystem` and `InMemoryFileSystemStore` so a link marked unreadable in a test raises the same way, and add the fake state needed to mark one.

**Group 2.** Add one test in `analyzers/AgentGuard.Analyzers.Tests/` that resolves `ISystemServices` from the live `AgentGuard.Abstractions` assembly through `BoundaryServices.ResolveTree` and fails when it returns null. The test exists so the seven analyzers cannot go inert without a red test: one-owner-per-interface, no-service-as-parameter, no-static-service-holder, leaf-platform-single-implementer, system-services-member-must-be-service-accessor, container-interface-single-owner and builder-completeness.

**Group 3.** No place in this group changes whether it denies — only whether the cause survives.

Where the cause is an exception, the place stops swallowing it and lets it through, and the catch goes at the nearest layer that already returns a reasoned result. The three catches in `GrantStore.TryLoadAsync`, the three in `GuardEngine.LoadGrantPublicKey`, the two in `Ed25519SignatureService.Verify`, the two in `WindowsUserPresence` and the two in `BuildOutputSkipRule.ShouldSkip` each name the exception and stop discarding it. `FileGuard.TryReadCurrentAsync` returns the exception's message rather than null, and its sole caller already turns that into `Verdict.Deny` one frame up, so that one place carries the reason into a denial exactly as first written.

`Ed25519SignatureService`'s class documentation states that `Verify` never throws. DESIGN settles whether that statement changes or the cause is reported another way, because this contract forbids changing `ISignatureService.Verify`'s signature.

Where the cause is not an exception — an invalid signature, an expired grant, a key of the wrong length, an absent key file — the place is not failing and nothing raises. DESIGN settles how each of those is reported, under the same ban on a new type or a changed public interface.

**Group 4.** No verdict exists at any of the three product places, so none of them surfaces a warn. `IPrivilegedWriter.ExecuteAsync` returns a bare `Task` and runs before the verdicts are aggregated. `Pipeline.SweepBestEffortAsync` runs at the start of the pre-check, before any verdict exists. `PosixFileSystem.TryRemoveLink` sits at the filesystem boundary, where `MakeLinkTarget` returns void.

Each therefore raises, or folds its failure into the exception already being thrown. `TryRemoveLink`'s second call site is immediately before `MakeLinkTarget` throws `IOException` with the errno, so a failed cleanup there is folded into that exception. DESIGN settles the other two, under the same ban on a new result type.

In the five test-cleanup places, let the exception surface, or assert on it where the test is about cleanup. A catch whose body is only a comment is removed.

**Group 5.** Make the win-arm64 and linux-x64 legs run the tests they build, or record in `docs/silent-failure-audit.md` why a leg stays build-only. Change the upload steps from `if-no-files-found: warn` to `error`. Run `eng/check-copied-modules.mjs` from CI. Narrow the `no-unsafe-*` disable in `eslint.config.js` to the files that narrow YAML, named one by one. Make `eng/local-cosign.sh`, `eng/local-cosign.ps1` and `eng/version.compute.targets:81,86` fail rather than exit 0 or continue when they sign nothing or compute no hash.

**All groups.** Update `docs/silent-failure-audit.md` so each of the thirteen moves from "Listed, not fixed" to "Fixed", or stays with the reason recorded.

## What the agent MAY do

- Add test fakes and test state needed to exercise an unreadable link.
- Change the documentation comment on any member it changes.
- Add a test file where this contract names one.

## What the agent MUST NOT do

- Weaken, skip, delete or re-point any existing test. A test that goes red because of this change is fixed in the code.
- Add a new exception type, result type, logging mechanism or diagnostic sink. The Reuse ledger rules every one of them out.
- Change whether any place denies or allows. Group 3 changes what a denial says, never whether it is made.
- Suppress an analyzer diagnostic, or reshape code to slip past a rule. A rule that blocks the clean design is a wall: stop and report it.
- Commit, push, open a pull request, or merge anything.
- Expand scope. A finding outside the thirteen in `issue.md` is reported, not fixed.
- Continue past a wall. Stop and report.

## Acceptance

1. `dotnet build` reports 0 warnings and 0 errors. Paste the summary line.
2. `dotnet test` reports 0 failed. Paste the summary line.
3. `bun run typecheck`, `bun run lint` and `bun test` are clean, with the coverage floor held. Paste the test summary and the coverage total.
4. A test proves `ReadLinkTarget` raises on a link it cannot read, and still returns null for a path that is not a link. Both cases, named in the test output.
5. A test proves `PathCanonicalizer` lets the exception through rather than treating an unreadable link as a non-link, and a second test proves the reason reaches a reasoned result: `FileGuard`'s capture returns `CaptureFailed` carrying the message, or the denial one frame up carries it. Name both tests in the output.
6. `grep -n "catch (IOException)" src/AgentGuard.CrossPlatform/PlatformFileSystemBase.cs` returns nothing.
7. The group 2 test fails when `ISystemServices` cannot be resolved. Prove it by running it against a compilation without that assembly and pasting the failure.
8. For each of the six places in group 3, a test asserts the denial's message contains the cause. Six assertions, each naming its place.
9. No catch in the eight places in group 4 has a body that only comments. Prove it with a multi-line search that matches a comment-only body, run over the whole tree and pasted with its output:
   `python3 -I -c "import re,pathlib,sys; bad=[(str(f),m.start()) for f in list(pathlib.Path('src').rglob('*.cs'))+list(pathlib.Path('tests').rglob('*.cs')) for m in re.finditer(r'catch\s*(?:\([^)]*\))?\s*\{(?:\s|//[^\n]*\n)*\}', f.read_text())]; print(bad or 'none'); sys.exit(1 if bad else 0)"`
   The first draft of this check used a single-line regex, which matched none of the eight before the work and would match none after, so it could not prove the group either way.
10. `grep -n "if-no-files-found: warn" .github/workflows/ci.yml` returns nothing.
11. `eng/check-copied-modules.mjs` appears in a CI step. Paste the step.
12. `eslint.config.js` names each file the `no-unsafe-*` disable applies to, and no directory glob. Paste the block.
13. Every one of the thirteen findings in `issue.md` is marked fixed in `docs/silent-failure-audit.md`, or carries a recorded reason for staying. An adversary reads both files and confirms the count is thirteen.
14. An adversary re-derives checks 6, 9, 10 and 12 by reading the bytes, not by the worker's word.

## Level

L1 — the change touches fail-closed security code, the analyzer tree, and the signing and release path.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path.
