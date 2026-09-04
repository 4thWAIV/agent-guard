# 14 — Seam the filesystem across the engine + Setup — inventory of policy code reaching the OS directly

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/14

---

## Context
Companion to #13 (the rule + review + analyzer). #13 defines the rule that no external boundary is called without a seam our code owns; this issue is the concrete inventory of existing violations in `src/`, from the missing-abstractions scan (2026-08-09). When #13's analyzer lands, every site here goes RED and is cleaned up under Rule-Driven Development. The clock is already seamed (`TimeProvider`); there is no network / DB / cloud in `src/`. The gap is filesystem, pervasively.

## Highest-leverage seams
- `IDirectoryEnumerator` (new; introduced for the scanner in the cross-platform contract) — covers the scanner plus #1, #3, #4, and much of Setup.
- `IFileReader` (already exists; exposes `Exists` + read) — resolves #5 outright and part of #2 / #7 cheaply.

## Live request path (fix first — these decide security off raw disk)
1. `ContextStoreInspector.InspectAsync` — `ContextStoreInspector.cs:39,46,49` (`File.Exists`, `Directory.Exists`, `Directory.EnumerateFiles`). Runs every Pre hook; its Clean/Anomalous verdict (poisoned pre-image / tampered store) is a pure function of raw disk. Seam: `IDirectoryEnumerator` + `IFileReader.Exists`.
2. `GrantStore.GetActiveGrantsAsync` / `TryLoadAsync` — `GrantStore.cs:53,59,102` (`Directory.Exists`, `Directory.EnumerateFiles("*.token")`, `File.ReadAllTextAsync`). Which token files are read decides which System writes are permitted. Seam: `IDirectoryEnumerator` + `IFileReader`.
3. `ContextStore.SweepExpiredAsync` — `ContextStore.cs:70,76,79,81` (`Directory.Exists`, `EnumerateDirectories`, `GetLastWriteTimeUtc`, `Directory.Delete`). Also a second un-seamed time source (dir mtime). Seam: `IDirectoryEnumerator` + a last-write-time accessor. The keyed store ops are fine.
4. `BuildOutputSkipRule.ShouldSkip` — `BuildOutputSkipRule.cs:43` (`Directory.EnumerateFiles("*.csproj")`). Seam: `IDirectoryEnumerator`.
5. `ProjectRuleSource.Create` — `ProjectRuleSource.cs:43,48` (`File.Exists`, `File.ReadAllText`). Seam: `IFileReader` (already exists — cheapest fix).
6. `ClaudeCodeHostAdapter.Read` — `ClaudeCodeHostAdapter.cs:91` (`Directory.GetCurrentDirectory` cwd fallback). The only environment read in the live path. Seam: an injected working-directory/environment provider (model: injected `TimeProvider`).

## Composition root
7. `GuardEngine.LoadGrantPublicKey` — `GuardEngine.cs:116,123` (`File.Exists`, `File.ReadAllText` for the Ed25519 key). Seam: `IFileReader`. (`GuardEngine.cs:75` `Environment.GetFolderPath` is root-level env, acceptable.)

## Setup subsystem (systemic — no filesystem seam exists)
8. `InstallIntegrity` — HIGH, the self-check gating every `guard hook` — `InstallIntegrity.cs:31,42,56,81,82,97,98,106,127` (`File.Exists`/`Directory.Exists`/`ReadAllText`/`Sha256HexOfFile`/`FileInfo.UnixFileMode`). Its cross-platform-divergent bits (the `UnixFileMode` writable check, the symlink read) are cross-platform-contract work; the plain existence/read is filesystem-seam cleanup. Seam: `IFileReader`/`IDirectoryEnumerator` + a Unix-mode accessor.
9. `CreationHelper` — HIGH, single owner of imperative create/merge — `CreationHelper.cs:30,38,44,48,62,75,108,109,123,171,206,208` (`File.Exists`/`Directory.CreateDirectory`/`File.SetUnixFileMode`/`Directory.Delete`). Its executable-flag + symlink bits are cross-platform-contract work; the rest is cleanup. Seam: `IFileReader`/`IDirectoryEnumerator` + writer/chmod seams.
10. `SetupCommands` — `SetupCommands.cs:34,84` (`File.Exists`).
11. `ISetupCondition.Detect()` family (8 detectors) — `ConfigJsonCondition.cs:23`, `PathProfileCondition.cs:23`, `VersionStampCondition.cs:36`, `BinaryHashCondition.cs:26`, `CurrentSymlinkCondition.cs:22`, `BinSymlinkCondition.cs:22`, `GitignoreCondition.cs:22`, `VersionBinaryCondition.cs:20`. Seam: `IFileReader`/`IDirectoryEnumerator` + a symlink accessor.
12. Static FS helpers (correct DRY, but `static` so no caller can fake them) — `AtomicFile.cs:23,31,44,56,65,82,92` (incl. `Guid.NewGuid()` temp names), `SymlinkOps.cs:44,45,59,62,78,80`, `Hashing.cs:27`, `SafeRead.cs:27`, `MachineInspection.cs:22,31`, `ClaudeSettings.cs:22`, `IdempotentAppend.cs:26`. Convert to interface-backed adapters, or have callers depend on the seams above. `NativeInterop.cs:22` is handled by the cross-platform contract.
13. `ProjectPaths.IsInitialized` — `ProjectPaths.cs:76` (`Directory.Exists`). LOW.

## Already correct — do not touch
`FileReader`/`IFileReader`, `PathCanonicalizer`/`IPathCanonicalizer`, `PrivilegedWriter`/`IPrivilegedWriter`, `ContextStore` keyed record ops, `TimeProvider` injection, `SetupContext.ForCurrentProcess` (process state read once, passed downstream as immutable data).