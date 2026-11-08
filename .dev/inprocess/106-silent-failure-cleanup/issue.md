Six of the nineteen findings from the silent-failure audit of 19 October 2026 are fixed and shipped in pull request 105. Thirteen remain. This issue is the whole of the remainder, read against the live code on 8 November 2026 and grouped by what the silence costs.

Tim settled two questions about it on 8 November 2026, through the signed package `the-silent-failures-still-in-the-code-1.0.0-local07b`:

- **An unreadable symlink throws, and the callers catch it.** Not a third answer on the contract, which was the recommendation. `ReadLinkTarget` keeps returning null for a file that is not a link, and raises on a link it cannot read.
- **All five groups ship as one piece of work**, with one contract and one adversary round. Not the symlink fix first on its own, which was the recommendation.

## 1. A wrong answer the guard acts on

`PlatformFileSystemBase.ReadLinkTarget` catches `IOException` and `UnauthorizedAccessException` and returns null, and the contract's null means "not a link". So a link the process may not read is indistinguishable from a file that is not a link. `IsLinkTarget` is defined as that read being non-null, so it reports false for a link it cannot read.

Four places act on the answer: `PathCanonicalizer.cs:68`, `SymlinkOps.cs:40`, `BinSymlinkCondition.cs:28` and `CurrentSymlinkCondition.cs:28`. `PathCanonicalizer` is the one that matters — it stops resolving at a link it cannot read, so a path can be judged by the link rather than by what the link points at.

This is the only one of the thirteen where the silence produces a wrong answer rather than a lost reason.

## 2. Seven of the fence's own rules stop firing

`BoundaryServices.ResolveTree` returns null when `ISystemServices` is not in scope, and `Resolve` then returns `DerivedServices.Empty`. Seven analyzers return without a diagnostic when that happens: one-owner-per-interface, no-service-as-parameter, no-static-service-holder, leaf-platform-single-implementer, system-services-member-must-be-service-accessor, container-interface-single-owner, and builder-completeness.

The audit recorded this as three rules. It is seven. A compilation that cannot see the container is a compilation nothing is enforced against, and it is green. The audit's proposed fix stands: a test that resolves the container from the live `AgentGuard.Abstractions`, so the rules cannot go inert unnoticed.

## 3. A denial with its reason thrown away

Six places decide correctly and lose why. `GrantStore.cs:133-141`, `GuardEngine.cs:156-164` and `Ed25519SignatureService.cs:56,62` drop the cause of a rejected or unreadable grant or key. `WindowsUserPresence.cs:154,175`, `BuildOutputSkipRule.cs:47,51` and `FileGuard.cs:383-387` fail closed and drop the exception text.

Nothing here is unsafe: the guard denies. What is gone is any way to tell a corrupt key from a missing one, or a refused tool call from a broken reader. Carry the reason into the denial.

## 4. Cleanup that swallows the failure

Best-effort cleanup catches that drop the failure, in the product: `PrivilegedWriter.cs:43,47`, `Pipeline.cs:180-200`, `PosixFileSystem.cs:169,173`. Surface a warning rather than nothing.

And in test cleanup, where a swallowed error can hide a product failure behind a green test: `TestTempDirectory.cs`, `SetupHarness.cs`, `FixtureProject.cs`, `CliMachine.cs` and `PlatformFileSystemSpecTests.cs:302`.

## 5. A pipeline that can report success having checked nothing

- `ci.yml`: the win-arm64 and linux-x64 legs are build-only; signing runs only on a push; the upload steps use `if-no-files-found: warn`.
- `eng/check-copied-modules.mjs` is not run by CI, and neither eslint nor tsc covers `.agents/workflows/*.js`.
- `eng/local-cosign.*` and `eng/version.compute.targets:81,86` exit 0 or carry on without signing and without a hash.

## Not silent failures, and recorded here so they are not mistaken for some

- `PresenceNativeSmokeTests.cs:37` is compiled out locally unless `AgentGuardRunNativeSmoke=true`. That is absent coverage, not a hidden failure.
- `.agents/workflows/*.js` turn a thrown agent error into `{ executionError }`, which is loud or quiet depending on whether the orchestrator stops on `panelComplete === false`. It is an orchestrator contract, and those scripts move to TypeScript under #58.
- `eslint.config.js:20-21` switches `no-unsafe-*` off for all of `ts/**` rather than only the files that narrow YAML. A rule visibly off in the configuration is not a silent failure.

## Where the material is

- `docs/silent-failure-audit.md` — the audit of 19 October 2026, with its fixed and unfixed lists.
- Pull request 105 — the six that are fixed.
