# Install the `guard` engine and write-block the analysis config

## Standard
A write to a protected file is denied before it happens. An unauthorized change that lands is reverted to its pre-call bytes. Per-repo config only adds protection. The grant-token store and `.protected-snapshots/**` are never writable, even with a valid grant token.

## What to do
Copy source: `/Users/timothystockstill/code/macos/4thWAIV/fourth-waiv-ai`; its `docs/agent-governance/file-manifest.md` lists the copy set.

1. Copy the `agent-tools/` engine verbatim: `lib/`, `hooks/pre_tool_use/`, `hooks/post_tool_use/`, `security/*.ts`, `tsconfig.json`, `package.json`, all `*.test.ts`. Do NOT copy `scripts/hooks-baseline.ts`, `config/protected-baseline.json`, or the `eslint-plugin-fourth-waiv/` tree.
2. Copy `agent-tools/security/bypass-public-key.pem` verbatim.
3. Set the `op://` constant in `agent-tools/security/bypass.ts` to `op://4thWAIV_Engineering/ProtectedFilesBypassSigningKey/notesPlain`.
4. Remove every committed-baseline reference from the copied `scripts/hooks-setup.ts` and `scripts/hooks-verify.ts` (the `hashTree`/`writeBaseline`/`readBaseline` calls and the content-hash Tier-2 warn). Keep the wiring hard-fail intact. Delete any baseline-only test.
5. Author `agent-tools/config/protected-paths.json` globs — this file, not source, holds everything about the consuming project and the runtime wiring. Runtime wiring: `.claude/settings.json`, `.codex/config.toml`, `.codex/hooks.json`. Rarely-edited C# config: `**/Directory.Build.props`, `**/*.globalconfig`, `**/.editorconfig`, `**/stylecop.json`. Do NOT include `**/*.csproj`, `**/*.props`, `**/*.targets`, `Directory.Packages.props`, or `Proteus.sln`.
6. Author `agent-tools/config/authorized-commands.json` prefixes: `["guard hooks setup"]`.
7. Author `agent-tools/config/invocation-blocked.json` regexes: `guard grant (mint|extend|add|revoke)`, `op read .*ProtectedFilesBypassSigningKey`.
8. In `agent-tools/lib/protection-config.ts`: set `TIER1_NEVER_BYPASS_GLOBS` to the grant-token dir and `.protected-snapshots/**`; set `HARDCODED_PROTECTED_GLOBS` to ONLY the guard's own shipped code — `agent-tools/**`, `.claude/hooks/protect-locked-files.js`, `.codex/hooks/protect-locked-files.py`. Do NOT hardcode the runtime wiring (`.claude/settings.json`, `.codex/config.toml`, `.codex/hooks.json`) or any consuming-project file; those are entries in `protected-paths.json`. Remove any `eslint-plugin-fourth-waiv/**` entry.
9. Author the single `guard` CLI (one entry, `bun`), wrapping the copied engine: `guard hooks setup|verify` (call the de-baselined setup/verify); `guard grant mint|extend|add|revoke|list|show|verify` (call `security/bypass.ts`'s verbs). `mint`/`extend`/`add`/`revoke` are human-only; `list`/`show`/`verify` are agent-OK.
10. Author the Layer-A guards `.claude/hooks/protect-locked-files.js` and `.codex/hooks/protect-locked-files.py` so that each READS its block list at run time from `agent-tools/config/protected-paths.json`. Do NOT give either a hardcoded `LOCKED_FILES` array. Set `.codex/config.toml` `[features] codex_hooks = true`; author `.codex/hooks.json`.
11. Root `package.json`: the `guard` alias; a `check` script that runs `guard hooks verify` first.
12. Run `guard hooks setup`.
13. Add `.protected-snapshots/` and the grant-token dir to `.gitignore`.

## MAY
- Fix import paths broken by the copy.

## MUST NOT
- Change engine logic in copied files except items 3 and 4.
- Generate a keypair or read the private key.
- Copy 4thWAIV's tokens, snapshots, or baseline files.
- Write-block `**/*.csproj`, `**/*.props`, `**/*.targets`, `Directory.Packages.props`, or `Proteus.sln` — they are scanner-guarded (a later unit), not write-blocked.
- Hardcode any consuming-project path or runtime-wiring path anywhere. Only the guard's own shipped code (`agent-tools/**` and the two Layer-A hook files) is hardcoded in source; every consuming-project file and every wiring path is an entry in `protected-paths.json`, which the dispatcher hook and both Layer-A guards read.
- Commit.
- Deviate at a wall — stop and report.

## Acceptance (paste command, output, exit code)
1. `guard hooks verify` exits 0.
2. The copied `agent-tools` test suite passes.
3. A PreToolUse Edit payload targeting `Directory.Build.props`, piped to the dispatcher, exits 2 (denied). Use the payload shape the copied tests use.
4. A PreToolUse Edit payload targeting any `*.csproj` exits 0 (ALLOWED — proves `.csproj` is not over-blocked).
5. Apply a one-line change to `Directory.Build.props` on an allowed call, invoke the PostToolUse drift hook for that `tool_use_id`, and show the file byte-identical to before (sha256 match).
6. A PreToolUse Write creating `tests/sub/.editorconfig` exits 2 (pattern coverage).
7. A PreToolUse Write targeting the grant-token dir, with a valid grant token present, exits 2 (Tier-1 beats grant).
8. `git status` shows no commit and no token or snapshot files.

## Success definition
Every acceptance check passes with output pasted verbatim including exit codes; `guard hooks verify` is green; `dotnet build Proteus.sln` and `dotnet test` remain green.

## Surfaces
- The copied `agent-tools/` engine (baseline removed).
- `agent-tools/config/{protected-paths,authorized-commands,invocation-blocked}.json`.
- The two glob arrays in `protection-config.ts`.
- `agent-tools/security/bypass-public-key.pem`.
- The `guard` CLI.
- `.claude/**` and `.codex/**` wiring.
- The Tier-1 grant-token and snapshot stores.

## Tier
FULL — mutating; installs the enforcement core.

## Scope
Change scope only by editing this file before the run starts.
