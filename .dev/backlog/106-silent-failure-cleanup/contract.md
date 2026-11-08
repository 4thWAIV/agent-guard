# Make the thirteen remaining silent failures loud

## Decisions

**An unreadable symlink throws, and the callers catch.** Tim approved this wording on 2026-11-08 in the signed package `the-silent-failures-still-in-the-code-1.0.0-local07b`, decision `symlink-shape`: "Throw, and callers catch — a smaller change that raises an expected case as an exception." The recommendation put to him was a third answer on the contract, and he chose against it.

**All five groups ship as one piece of work.** Approved in the same package, decision `scope`: "All five as one piece — one pass over every file, one adversary round for all of it." The recommendation put to him was the symlink fix first on its own, and he chose against it.

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
- `src/AgentGuard.Abstractions/Contracts/IPlatformFileSystem.cs:36` — the contract's own description of what null means.
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
- `tests/AgentGuard.TestHelpers/TestTempDirectory.cs`
- `tests/Shared/SetupHarness.cs`
- `tests/AgentGuard.Tests/FixtureProject.cs`
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

Every mechanism this work needs already exists and is reused: `Verdict.Deny(string reason)` at `src/AgentGuard.Abstractions/Verdict.cs:38` already carries a deny reason, `Verdict.Warn(string message)` at `src/AgentGuard.Abstractions/Verdict.cs:46` is the existing advisory surface, and the symlink read raises the `IOException` and `UnauthorizedAccessException` the framework already throws. No new exception type, no new result type, no new logging or diagnostic mechanism is added.

## What to do

**Group 1.** Remove the `catch (IOException)` and `catch (UnauthorizedAccessException)` blocks from `ReadLinkTarget` so both propagate. Keep null as the answer for a path that is not a link. Update the `IPlatformFileSystem.ReadLinkTarget` documentation to say null means not a link and that an unreadable link raises.

At each of the four call sites, catch both exceptions and deny. `PathCanonicalizer` must not treat an unreadable link as a non-link: it returns a denial carrying the reason, through `Verdict.Deny`, rather than continuing to canonicalise. `SymlinkOps`, `BinSymlinkCondition` and `CurrentSymlinkCondition` report the failure rather than reading it as "the link does not point where it should".

`IsLinkTarget` raises for the same reason `ReadLinkTarget` does, because it is that read.

Update `ManagedPlatformFileSystem` and `InMemoryFileSystemStore` so a link marked unreadable in a test raises the same way, and add the fake state needed to mark one.

**Group 2.** Add one test in `analyzers/AgentGuard.Analyzers.Tests/` that resolves `ISystemServices` from the live `AgentGuard.Abstractions` assembly through `BoundaryServices.ResolveTree` and fails when it returns null. The test exists so the seven analyzers cannot go inert without a red test: one-owner-per-interface, no-service-as-parameter, no-static-service-holder, leaf-platform-single-implementer, system-services-member-must-be-service-accessor, container-interface-single-owner and builder-completeness.

**Group 3.** At each of the six places, pass the cause into the result instead of discarding it. A rejected or unreadable grant or key carries why through to the denial. A fail-closed path carries the exception's message into `Verdict.Deny`. No place in this group changes whether it denies — only what the denial says.

**Group 4.** In the three product places, surface the cleanup failure through `Verdict.Warn` where a verdict is being produced, and otherwise report it to the caller. In the five test-cleanup places, let the exception surface, or assert on it where the test is about cleanup; a catch with nothing in it is removed.

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
5. A test proves `PathCanonicalizer` denies rather than continuing when a link on the path cannot be read, and the denial carries a reason that is not empty.
6. `grep -n "catch (IOException)" src/AgentGuard.CrossPlatform/PlatformFileSystemBase.cs` returns nothing.
7. The group 2 test fails when `ISystemServices` cannot be resolved. Prove it by running it against a compilation without that assembly and pasting the failure.
8. For each of the six places in group 3, a test asserts the denial's message contains the cause. Six assertions, each naming its place.
9. `grep -rn "catch\s*(\s*)\?\s*{\s*}" src tests --include="*.cs"` returns nothing for the eight places in group 4. Paste the command and its output.
10. `grep -n "if-no-files-found: warn" .github/workflows/ci.yml` returns nothing.
11. `eng/check-copied-modules.mjs` appears in a CI step. Paste the step.
12. `eslint.config.js` names each file the `no-unsafe-*` disable applies to, and no directory glob. Paste the block.
13. Every one of the thirteen findings in `issue.md` is marked fixed in `docs/silent-failure-audit.md`, or carries a recorded reason for staying. An adversary reads both files and confirms the count is thirteen.
14. An adversary re-derives checks 6, 9, 10 and 12 by reading the bytes, not by the worker's word.

## Level

L1 — the change touches fail-closed security code, the analyzer tree, and the signing and release path.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path.
