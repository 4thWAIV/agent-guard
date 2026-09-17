# 56 — Cross-RID self-contained publish builds the same projects twice into one output folder

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/56

---

## What is wrong

A self-contained cross-RID publish of the CLI builds most projects **twice**, and both instances write to the **same** `bin/Release/net10.0/` output, because that path does not vary by RID or by `AgentGuardPlatformRid`.

Reproduced locally and deterministically:

```
$ eng/check-single-platform-lib.sh linux-x64        # counts of "Project -> output" lines
   2   AgentGuard.CrossPlatform.Linux
   2   AgentGuard.CrossPlatform
   2   AgentGuard.Cli
   2   AgentGuard.Analyzers
   2   AgentGuard.Abstractions
   1   AgentGuard.Engine
   1   AgentGuard.Boundaries

$ dotnet build -c Release --no-incremental AgentGuard.sln
   every project exactly 1
```

So the doubling is specific to the cross-RID self-contained publish, not to building the solution.

## Why it doubles

The publish evaluates the reference graph under two different global-property sets: the pass where `RuntimeIdentifier` is set, and the pass where the SDK strips it to build the app's library references. `src/AgentGuard.Cli/AgentGuard.Cli.csproj` documents that strip in the comment above its `ProjectReference` items — the `AgentGuardPlatformRid` threading exists precisely because of it.

The discriminator supports this: the only two projects that build **once** are `AgentGuard.Engine` and `AgentGuard.Boundaries`, which are exactly the two the CLI references with `AdditionalProperties=AgentGuardPlatformRid=$(RuntimeIdentifier)`. Every project not pinned to a single property set is the one that doubles.

## How it bit us

Two MSBuild nodes reached the same generated file at the same instant and the step died:

> `error MSB4018: The "GenerateDepsFile" task failed unexpectedly. System.IO.IOException: The process cannot access the file '.../AgentGuard.CrossPlatform.deps.json' because it is being used by another process.`

That failed the macOS leg and the gate on the `dev` merge of PR #52 (run 33440630677), on a commit whose macOS leg had passed four consecutive times on the PR minutes earlier. The double-build has been latent in every cross-RID publish we have ever run; only the timing is intermittent.

## Current mitigation, not a fix

`.github/workflows/ci.yml` now passes `-m:1` to that step, forcing a single MSBuild node so the two instances serialize. Verified to pass locally. That removes the race, not the double-build — the wasted duplicate compilation stays.

## The actual fix

Stop the same project being evaluated under two property sets in one publish, or make the two instances write to distinct output paths so they can never share a file. Either touches the RID threading in `AgentGuard.Cli.csproj` and `src/AgentGuard.CrossPlatform/PlatformImplementation.targets`, which was itself added to fix a real correctness bug (a cross-RID publish bundling both per-OS libraries). That is a contract-level change, not a quick edit — it needs GROUND on how the SDK strips the RID, and the existing `eng/check-single-platform-lib.sh` as the regression guard proving exactly one per-OS library still ships.

## Done when

The cross-RID publish builds each project once, `-m:1` is removed from the CI step, and `eng/check-single-platform-lib.sh` still passes for a non-host RID.