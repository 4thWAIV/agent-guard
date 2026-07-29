# Implement the File Guard v1 — full Pre/Post engine, C# Provider, Claude Code host

## The standard / what we're building

Non-negotiable: the contract interfaces under `src/AgentGuard.Engine/Abstractions/` and the analyzers under
`analyzers/` are frozen. Implement against them; do not change them. Every failure path fails closed.

End state: `guard hook pre|post` runs as a Claude Code hook and enforces the File Guard.

- At Pre it denies any write to the core system — the grant-token store, the snapshot store, the guard's own
  shipped code, and `.claude/settings.json` — and captures a per-call snapshot of the whole configurable
  protected set on disk. Any Pre error at all — an unreadable file, a directory it cannot fully enumerate, a
  failed snapshot write — denies the call; it never proceeds on a partial snapshot.
- At Post it re-scans the configurable protected set, diffs it against the snapshot, and reverts any
  unauthorized change to its pre-call bytes and deletes any protected file the call created. A missing, corrupt,
  or ruleset-mismatched snapshot denies. A Post that cannot complete its scan or read after a retry returns a
  deny with no effects (never an allow), leaving the drift visible in the host message.
- Protected paths come from four rule sources: built-in Sealed rules, built-in System rules, the C# Provider
  baseline, and the repo's Project config.
- A valid Ed25519-signed grant covering the path authorizes an otherwise-blocked System or configurable change.
- The whole solution passes the golden build at 0 warnings / 0 errors and all tests pass.

## Success definition

All acceptance checks met, AND no errors in the system as a result (build, tests, and analyzers all green), AND
this end state holds: the File Guard blocks core-system writes at Pre, captures the whole configurable protected
set, and at Post reverts unauthorized drift to any configurable protected file — including drift caused by a
shell command whose target was never parsed — fails closed on every failure path, honors a valid grant, all
under the frozen interfaces, for C# via the C# Provider, on Claude Code. Any restatement or weakening of this
definition to fit the result is a top-line Lie-catcher finding.

## Surfaces

A "this path is protected / is not protected" or "this change was reverted" claim must be checked against all of
these, never one:
1. the built-in Sealed rules (grant-token dir, snapshot store);
2. the built-in System rules (guard's shipped code, `.claude/settings.json`);
3. the Project-config protected paths;
4. the C# Provider baseline;
5. the per-call snapshot store (the pre-image the revert reads from);
6. the grant store (the tokens Coverage reads).

## What to do

Implement each class behind its existing interface, following the private-constructor + static `Create` factory
pattern the AG analyzers require. The only new interface is `IProtectedFileScanner`, already in the abstractions;
everything else uses the committed interfaces unchanged.

1. Engine composition root and `IPipeline` (`src/AgentGuard.Engine/`): for one `HookEvent`, run the registered
   `IGuard`s in order, aggregate `Verdict`s (any single deny blocks). On Post, execute each
   `PostcheckResult`'s `Effect`s through the Engine's privileged writer, then `IContextStore.DeleteAsync`. The
   Engine maps any Post-phase exception to a deny, never to an allow.
2. `IGuardRegistry`: expose the compiled-in guards (the File Guard).
3. `FileGuard : IGuard`:
   - Precheck: canonicalize each target from `ToolCall.Input` (`FileWriteInput` paths; `ShellCommandInput`
     command) via `IPathCanonicalizer`; `IProtectedSet.Match`; a Sealed match denies unconditionally; a System
     match denies unless an active `Grant` covers it; call `IContextStoreInspector.InspectAsync` and deny on an
     anomalous or indeterminate result. For a `ShellCommandInput`, deny on any reference to a built-in
     core-system path token (a literal list, since a matcher cannot yield its tokens).
   - Capture: `IProtectedFileScanner.ScanAsync` the whole configurable protected set on disk; read each with
     `IFileReader`; serialize the path-to-bytes map plus a hash of the active ruleset; write it through
     `IContextWriter`. Return `CaptureSucceeded`, or `CaptureFailed` on ANY error — an unreadable file, an
     incomplete scan, a size-ceiling breach, or a failed write.
   - Postcheck: read the snapshot through `IContextReader`. `ContextMissing`, a corrupt blob (read as missing),
     or a ruleset-hash mismatch each deny. Otherwise scan and read the current tree, run the differ to produce
     `FileCreated`/`FileModified`/`FileRemoved`, and per change run Coverage then Conformance, emitting
     `RestoreFileEffect`/`DeleteFileEffect` for unauthorized drift. If the current scan or read cannot complete
     after a retry, return `Verdict.Deny` with no effects.
4. `IProtectedFileScanner` impl: walk the tree under `CallEnvironment.ProjectRoot`, do not descend into the
   Sealed skip-list build-output locations, match each remaining candidate against `IProtectedSet`. Use
   `EnumerationOptions.IgnoreInaccessible = false` so an inaccessible directory is an error, not a silent skip;
   surface any enumeration failure so Capture/Post deny.
5. `IContextStore` impl + the scoped `IContextWriter`/`IContextReader`/`IContextStoreInspector`: store one
   per-call snapshot in-repo under `.protected-snapshots/<hash-of-project>/<toolUseId>`, keyed by `ContextKey`.
   The store is internal; only the Engine hands out the scoped handles. `SweepExpiredAsync` removes only
   genuinely orphaned snapshots (calls whose Post never ran) by a generous age, using an injected `TimeProvider`.
   No content-addressing, no dedup, no cross-call sharing.
6. Internal helper classes (pure logic, not interfaces): a snapshot serializer with a versioned, length-framed
   format, per-file and total size ceilings that return `CaptureFailed`, and a truncated blob read as missing;
   a differ that takes the pre-image map and the current map, both materialized, and returns `FileChange`s with
   no IO; a coverage checker that answers whether an active grant covers a path, reusing `IPathMatcher.Matches`.
7. `IPathCanonicalizer` impl: absolute, `.`/`..` and symlinks resolved, producing `CanonicalPath`.
8. `IFileReader` impl, and the Engine-internal privileged writer that executes `RestoreFileEffect` and
   `DeleteFileEffect`.
9. `IRuleSource` impls — two, because `Origin` is a single value: a Sealed source (the grant-token dir and the
   snapshot store) and a System source (the guard's shipped code — `src/AgentGuard.Engine/**`,
   `src/AgentGuard.Cli/**`, `analyzers/**` — and `.claude/settings.json`); plus a Project-config source; plus an
   `IProtectedSet` that assembles all sources in precedence.
10. `CSharpProvider : IProvider`: a Baseline whose Rules protect every instance of `Directory.Build.props`,
    `*.globalconfig`, `.editorconfig`, `stylecop.json`, and `global.json`, each with a matcher and the default
    verifier; its `IRuleSource`; and its Sealed skip-list entries (`bin`, `obj`, anchored to project directories
    on canonical form). Do NOT include `.csproj`, `.sln`, or `Directory.Packages.props` — a later build scanner
    covers them.
11. `NoChangeVerifier : IVerifier`: any drift is unauthorized; return a deny that produces the revert effect.
12. `IGrantStore` impl: load tokens from the grant dir, Ed25519-verify each against a committed public key using
    `BouncyCastle.Cryptography`, drop expired ones via the injected `TimeProvider`, return the active grants.
    No minting.
13. Claude Code `IHostAdapter`: `Read` parses Claude Code's PreToolUse/PostToolUse payload into a
    `NormalizedCall` with typed `ToolInput` (edit tools to `FileWriteInput` carrying every target path, the shell
    tool to `ShellCommandInput`, other tools to a null Input), and returns `HostReadUnparsable` on a malformed or
    empty payload; `Render` maps a `Verdict` to a `HostDecision` exit code, with the reason in `Message`.
14. `src/AgentGuard.Cli/Program.cs`: `guard hook <event>` reads stdin, selects the Claude Code adapter, runs the
    pipeline, and emits the `HostDecision`. An `HostReadUnparsable` denies.
15. A C# fixture project and xUnit tests proving each acceptance behavior below.

## What the agent MAY do

- Create the implementation classes, the fixture, and the tests.
- Add these package references (versions come from central package management, which already pins them):
  `BouncyCastle.Cryptography` on the Engine and test projects, `Microsoft.Extensions.TimeProvider.Testing` on
  the test project, and a throwaway Ed25519 test keypair used only to sign grant fixtures.
- Add `.protected-snapshots/` and the grant-token dir to `.gitignore`.

## What the agent MUST NOT do

- Change any file under `src/AgentGuard.Engine/Abstractions/` or under `analyzers/`.
- Weaken, skip, `xfail`, or delete any test; disable, downgrade, or `NoWarn` any analyzer; relax the golden build.
- Fail open anywhere. A capture failure, an incomplete Pre scan, a missing/corrupt/mismatched snapshot, an
  un-diffable Post, an unparsable payload, or an indeterminate store inspection must deny.
- Use `EnumerationOptions.IgnoreInaccessible = true`, or any walk that skips inaccessible entries silently.
- Content-address, deduplicate, or share snapshots across calls; each call has its own per-`toolUseId` snapshot.
- Expand scope: no TypeScript or Rust Provider, no Codex adapter, no grant minting CLI, no shell-command parser,
  no approved-command allowance, and no in-code suppression governance / lint gate.
- Commit, or write any file outside the repo root.
- Leave a `TODO`, `PENDING`, or `NotImplementedException` in any enforcement path.
- Stop and escalate at any wall rather than deviate silently.

## Acceptance

Each check is re-runnable; paste the command output with its exit code. An adversary re-runs every check.

1. `dotnet build AgentGuard.sln` prints `0 Warning(s)` and `0 Error(s)`.
2. `dotnet test AgentGuard.sln` reports 0 failed; the existing analyzer tests still number 45 and still pass.
3. Behavioral tests exist and pass:
   a. A Write or Edit to `.claude/settings.json` is denied at Pre.
   b. A Bash command that references the snapshot store or the grant-token dir is denied at Pre.
   c. An unauthorized edit to `Directory.Build.props` is reverted to its pre-call bytes at Post.
   d. A Bash command that edits a `Directory.Build.props` — a target Pre never parsed — is reverted at Post,
      proving whole-tree capture, not target-only capture.
   e. A protected file the call created is deleted at Post.
   f. A Pre scan that cannot fully enumerate the tree denies the call (fail closed), and no snapshot is used.
   g. A drifted protected file whose snapshot is missing is denied (fail closed).
   h. A capture failure denies the call (fail closed).
   i. A change covered by a valid test-signed Ed25519 grant is allowed.
   j. A write through a symlink that points at a protected file is denied (canonicalization).
4. `git diff --stat HEAD -- src/AgentGuard.Engine/Abstractions analyzers` is empty.
5. `git status` shows no commit; `.protected-snapshots/` and the grant-token dir are gitignored and appear in no
   tracked file.

## Tier

FULL — new mutating code that is the enforcement core; a fake pass would certify an unprotected build as safe.

## Scope

Change scope only by editing this file before the run starts.
