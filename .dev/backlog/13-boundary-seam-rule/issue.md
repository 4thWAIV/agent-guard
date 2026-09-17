# 13 — Rules + review must require an abstraction seam at every external boundary (OS/DB/cloud/clock/env)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/13

---

## Problem
A recurring design failure: code calls a boundary outside itself — the OS filesystem, a database, cloud storage, the system clock, the network, the process/environment, even a BCL primitive that reaches the OS — **directly, with no interface seam our code owns in between**. Two costs, both silent:

1. It cannot be faked, so the behavior at that boundary can only be tested by manipulating the real thing.
2. It silently bakes in a platform/behavior decision that surfaces commits later.

Concrete instance that triggered this: `ProtectedFileScanner` (`src/AgentGuard.Engine/ProtectedFileScanner.cs`) called `Directory.Exists` and `new DirectoryInfo(...).EnumerateFileSystemInfos(...)` inline. Its fail-closed guarantee — an inaccessible directory denies the call and is never silently skipped — could therefore only be tested by `chmod 000` on a real directory, which is POSIX-only, forced an `OperatingSystem.IsWindows()` branch in the test, and left the security property tested on Unix alone.

## Rule to add (rails-solid-code + rails-read-me)
Any call that crosses into an external or side-effecting boundary MUST go through an interface our code owns, with a production adapter and a test fake. "It is in the standard library" is **not** an exemption — `System.IO`, `DateTime.Now`/`UtcNow`, `Environment`, `HttpClient`, `Process`, crypto RNG all count. The seam is mandatory even when only one implementation exists today. Boundaries in scope: filesystem, clock/time, environment/process, network, database, cloud storage, randomness, native/OS interop. `System.IO.Path` string math is pure and is exempt.

## Review / adversary enforcement
- Add a lens to the laziness-auditor / REFUTE stage that hunts direct boundary calls sitting in policy/domain code with no seam.
- The hidden-decision scan treats a bare boundary call as a silently-defaulted platform/behavior decision and surfaces it before a contract is finalized.

## Analyzer (RDD, future)
An `AG00xx` that flags direct use of the boundary types (`System.IO` file/dir APIs, `DateTime.Now`/`UtcNow`, `Environment`, `HttpClient`, randomness) outside a small set of sanctioned interface-backed adapter classes — the same shape as the interop-boundary analyzers already in `analyzers/AgentGuard.Analyzers`.

## Acceptance
- The rule is written into `rails-solid-code` and `rails-read-me`.
- The REFUTE/laziness-auditor lens and the hidden-decision-scan lens exist.
- The existing-violations list from the companion missing-abstractions scan is triaged into fix/keep, each with an owner.