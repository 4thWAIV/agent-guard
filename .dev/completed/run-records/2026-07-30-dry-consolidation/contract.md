# Consolidate the five DRY fractures and enforce single ownership at build time

A refactor of committed code. It removes five duplications and adds a build-time guard so this class of
duplication fails the build instead of relying on review. No behavior change to file protection.

## The standard

Frozen and untouchable: `src/AgentGuard.Engine/Abstractions/**` and `analyzers/**`. Do not change the File
Guard's protection *behavior* — after this, the same paths are protected and the same changes reverted; only the
plumbing is de-duplicated. Every existing test (75 engine + 45 analyzer) must still pass; adjust only tests that
referenced a file you delete. Golden build stays 0/0. Only permitted suppression remains `CA1031` on fail-closed
boundaries. No commit.

## The five fractures to consolidate (verified locations)

1. **Two project-config files.** `init` writes `.agentguard/config.json` (`GuardConfig` with `EnabledProviders`
   + `ProtectedPaths`, `GuardConfig.cs:15`) which nothing reads back; the engine reads project protected-paths
   from a different file `.agentguard/protected-paths.json` (`ProjectRuleSource.cs:20`).
   **Consolidation:** `.agentguard/config.json` becomes the single project-config file, read by the engine and
   written by setup.
   - The config type is ONE shared type in the `AgentGuard.Engine` namespace (move `GuardConfig` out of `Setup`;
     name it `ProjectConfig`). Its path is owned by `CoreSystemPaths` (one `internal const`), reused by both the
     engine reader and setup.
   - Its schema is ONLY the fields actually consumed: `ProtectedPaths`. **Remove `EnabledProviders`** — it is a
     dead field today (the provider set is compiled in at `GuardEngine.cs:34`). Do NOT re-add it until a build
     actually wires provider selection to it; a dead-but-authoritative field is the exact fracture.
   - `ProjectRuleSource` reads `config.json`'s `ProtectedPaths` (through the shared type + the source-generated
     JSON, no reflection) instead of `protected-paths.json`. Delete the `protected-paths.json` path and any test
     fixture that wrote it — migrate those tests to write `config.json`.
   - `CreationHelper.EnsureConfig`, `ConfigJsonCondition`, and `SetupJson`/`SetupJsonContext` use the shared
     `ProjectConfig` type.

2. **Hook wire-format split across writer and reader.** The command `guard hook <pre|post> --host claude-code
   --agentguard-owned` is composed from private tokens in `ClaudeSettingsWiring.cs:22-24` and independently
   re-declared to be parsed in `Program.cs`; host id `"claude-code"` lives in three places
   (`GuardHost.cs:22`, `ClaudeCodeHostAdapter.cs:44`, `ClaudeSettingsWiring.cs:24`); the sentinel in two
   (`ClaudeSettingsWiring.cs:22`, `Program.cs:56`).
   **Consolidation:** one owner of the hook command's wire tokens — the subcommand name, the `pre`/`post` event
   tokens, the `--host` flag and its `claude-code` value, and the `--agentguard-owned` sentinel — that BOTH the
   writer (`ClaudeSettingsWiring`) and the reader (`Program.cs`) build from. Host id comes from one constant
   (reuse `GuardHost.ClaudeCodeHost`; have the adapter's `Host` property return it too). Sentinel is one
   constant reused by the CLI option. After this, drift is impossible because there is one definition.

3. **The in-repo `.agentguard` directory name spelled in several owners** (`MachinePaths.cs:18`,
   `CoreSystemPaths.cs:25/30`, `ProjectRuleSource.cs:20`, and `ProjectPaths` reaching it two ways).
   **Consolidation:** `CoreSystemPaths` owns the `.agentguard` segment; `MachinePaths`/`ProjectPaths` derive the
   in-repo dir from it. The machine-home root name must not be reused as the in-repo dir name via a coincidental
   shared literal.

4. **Two atomic-file-write recipes** — `ContextStore.cs:36-38` hand-rolls the same `".tmp-" + Guid →
   File.Move(overwrite)` that `Setup/AtomicFile.cs:46-48` owns.
   **Consolidation:** give `AtomicFile` an async byte-write overload; `ContextStore` writes through it; delete the
   duplicate recipe.

5. **"Parse config.json as a JSON object" written twice** (`ConfigJsonCondition.cs:37`,
   `CreationHelper.cs:231-248`), and settings "read text / distinguish unreadable" split between `SafeRead` and
   inline try/catch in `CreationHelper.WireSettings`/`UnwireSettings`.
   **Consolidation:** one parse-as-object helper used by both; route the settings reads through `SafeRead`.

## Enforce single ownership at build time (the prevention)

Add a golden-build **test** (in the existing test project) that reads the `.cs` sources and FAILS if a canonical
owned token appears in more than its one designated owner file:
- `.agentguard` (segment) → only `CoreSystemPaths`
- `claude-code` (host id) → only its one owner constant
- `--agentguard-owned` (sentinel) → only its one owner constant
- the `config.json` / project-config filename → only `CoreSystemPaths`
- the `".tmp-" + Guid` atomic-write recipe → only `AtomicFile`

The test must pass now (after the consolidation) and must fail if a second copy is introduced (include a comment
explaining it exists because the token-based duplicate-code analyzer is blind to cross-file concept duplication).
Keep the allow-list of (token → owner) in one place in the test so adding an owned token is one edit.

## Acceptance

Re-runnable; paste output + exit code.
1. `dotnet build AgentGuard.sln` = 0 Warning(s) / 0 Error(s).
2. `dotnet test AgentGuard.sln` = 0 failed; the pre-existing engine + 45 analyzer tests still pass (engine count
   may change only by tests you migrated off `protected-paths.json` and the new single-ownership test).
3. `.agentguard/protected-paths.json` no longer exists in the code or tests; `.agentguard/config.json` is the one
   project-config file, read by `ProjectRuleSource` and written by setup; the same project paths are protected as
   before (a test proves a `config.json` `ProtectedPaths` entry produces a Project rule and its drift is
   reverted).
4. `grep` shows `claude-code`, `--agentguard-owned`, and the `.agentguard` segment each defined in exactly one
   owner; the single-ownership test enforces it.
5. `git diff --stat HEAD -- src/AgentGuard.Engine/Abstractions analyzers` is empty; `git status` shows no commit.

## What the agent MUST NOT do

- Change protection behavior, the frozen interfaces, or any analyzer.
- Re-add `EnabledProviders` or any field that nothing consumes.
- Introduce a new owner for a path/id/format/IO-recipe instead of reusing the existing one — if a genuine new
  owner is needed, STOP and escalate.
- Leave a `TODO`/`PENDING`/`NotImplementedException`; commit; write outside the repo except a test's throwaway
  HOME.
- Stop and escalate at any wall rather than deviate silently.

## Tier

FULL — refactor of the committed engine + installer; a fake pass would leave a fracture in place or quietly
change what is protected.

## Scope

In scope: the five consolidations above and the single-ownership test. Out: the "one invariant system" unification
(separate, still-being-aligned design), config-file protection (#2), the Touch ID gate (#3).
