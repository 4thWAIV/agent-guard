# 23 — CLI tests: cross-OS HOME isolation — remove the POSIX-only skip once the environment seam lands

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/23

---

## What

The CLI tests (`tests/AgentGuard.Cli.Tests`) isolate the machine install by redirecting the `HOME` environment variable. The production entry point resolves the home directory with `Environment.GetFolderPath(SpecialFolder.UserProfile)` (`src/AgentGuard.Engine/Setup/SetupContext.cs` `ForCurrentProcess`), which returns `$HOME` on POSIX but the **registry profile** on Windows — neither `HOME` nor `USERPROFILE` redirects it there.

So the HOME redirect isolates on macOS/Linux but **not** on the Windows CI leg, where the home-dependent CLI tests would run against the real user profile of the runner.

## Interim (done on the coverage-gate branch)

The home-dependent CLI tests (`install`/`init`/`doctor` success paths, and the not-installed `init`/`doctor` refusals) are marked `[PosixOnlyFact]` — they run on macOS/Linux and are skipped with a reason on Windows. The aggregate coverage gate that counts the CLI (`guard`) runs on the Linux leg, so this loses no gated coverage; the Windows leg never counted the CLI. Tests with no home dependency (`version`, the hook fail-closed paths, `remove`) still run on Windows.

## What unblocks the real fix

The injected environment seam from the clr-primitive-lockdown (issues #13, #14): once the CLI entry point resolves home through an `IEnvironment` the test can drive, the CLI success-path tests isolate cross-OS and run on Windows too.

## Acceptance

- The CLI entry point resolves the home directory through the injected environment boundary (not `GetFolderPath` directly).
- The CLI test harness isolates that boundary, cross-OS.
- `PosixOnlyFactAttribute` and every `[PosixOnlyFact]` are removed; the success-path tests run and pass on the Windows CI leg, isolated.