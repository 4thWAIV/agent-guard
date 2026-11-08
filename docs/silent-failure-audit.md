# Silent-failure audit — 2026-10-19

Rule: CLAUDE.md rule 1, "Nothing may ever fail silently; failures must be loud."

## Fixed

- MetaLang checker (`ts/metalang`): a value written against a type the checker could not find, a type
  that was not given, a written-out type, a constraint no logic applies, a pattern whose expression
  cannot be read, and a primitive nothing says how to read now each raise a blocker or problem. They
  used to `return` quietly. `unmetBy` throws when asked about a constraint that cannot be applied.
- `words.ts` `resolveVocabulary`: declarations it could not keep (not one kind, not a mapping, no id,
  duplicate id), and non-text `is`/`scope` entries, are now faults; they were dropped without a word.
- `eng/generate-dev-keys.sh`: a failed trust step and an unsupported OS now exit 1.
- `eng/generate-dev-keys.ps1`: the exit code of `dotnet run` is checked.
- `eng/ci-verify-signatures.sh`: with no RID given it exits 2 instead of writing `RESULT: PASS`.
- `eng/compute-build-id.sh`: a failed `git rev-parse` in CI exits 1 instead of stamping `+local`.
- `.github/workflows/ci.yml`: `curl -f` on the dotnet installer; a missing per-OS evidence directory
  fails the release instead of being omitted.

## Listed, not fixed (each needs a decision or touches fail-closed security code)

- `PrivilegedWriter.cs:43,47`, `Pipeline.cs:180-200`, `PosixFileSystem.cs:169,173`: best-effort cleanup
  catches that drop the failure. Surface a warning.
- `PlatformFileSystemBase.cs:41,45` (`ReadLinkTarget` returns null on IO error, so an unreadable symlink
  reads as "not a link"): return unreadable as its own state and have callers deny.
- `GrantStore.cs:133-141`, `GuardEngine.cs:156-164`, `Ed25519SignatureService.cs:56,62`: the cause of a
  rejected or unreadable grant/key is lost; carry the reason into the denial.
- `WindowsUserPresence.cs:154,175`, `BuildOutputSkipRule.cs:47,51`, `FileGuard.cs:383-387`: fail-closed
  but the exception text is dropped.
- `BoundaryServices.cs:54-72` and its consumers: if `ISystemServices` cannot be found the rules go inert.
  Add a test resolving the container from the live `AgentGuard.Abstractions`.
- `BuilderCompletenessAnalyzer.cs:82-95`: same hole for the builder.
- Test-cleanup empty catches (`TestTempDirectory.cs`, `SetupHarness.cs`, `FixtureProject.cs`,
  `CliMachine.cs`, `PlatformFileSystemSpecTests.cs:302`): cannot mask a product failure.
- `PresenceNativeSmokeTests.cs:37` is compiled out locally unless `AgentGuardRunNativeSmoke=true`.
- `ci.yml`: win-arm64 and linux-x64 legs are build-only; signing runs only on push; upload steps use
  `if-no-files-found: warn`.
- `eng/check-copied-modules.mjs` is not run by CI, and eslint/tsc do not cover `.agents/workflows/*.js`.
- `eng/local-cosign.*` and `eng/version.compute.targets:81,86` exit 0 / continue without signing or
  without a hash in the cases noted above.
- `.agents/workflows/*.js`: a thrown agent error becomes `{ executionError }`; it is loud only if the
  orchestrator treats `panelComplete === false` as a stop.
- `eslint.config.js:20-21`: `no-unsafe-*` is off for all of `ts/**`, not only the YAML-narrowing files.
