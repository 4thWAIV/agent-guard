# CLR-primitive lockdown — every OS primitive behind an owned interface, a raw call a build error

This contract forces every direct call to a CLR or OS primitive — files, directories, environment, console, GUIDs — through an interface this codebase owns, and makes a raw call a compile error in every assembly but the one where that primitive is allowed. The result: the whole engine is mockable, and nothing touches the OS unwatched. It builds on the shipped cross-platform work (`../../completed/run-records/2026-08-07-cross-platform-engine-and-interop/contract.md`), which delivered `IPlatformFileSystem`, `IPlatformServices`, `Platform.Create()`, and the internal `IDirectoryEnumerator`; those are inherited as done and are not rebuilt here.

Every decision below carries Tim's exact words, lifted from this task's DECISIONS record. GROUND ran against the current post-cross-platform tree (workflow `wf_aae6c34a-a96`, 2026-08-10): 106 raw boundary calls across 34 files, and the reuse ledger below.

## Execution (two-phase, per Rule-Driven Development)

1. **Rule phase** — an adversary-backed analyzer workflow creates and wires AG0011–AG0018 and AG0101. Each goes **RED** where it hits a live raw call and **preventive** where the codebase is already clean. (Rule-generation authority; edits `analyzers/**`.)
2. **Implementation phase** — a fresh agent takes this same contract, is told the rules, builds the three assemblies and the container, and **cleans up every raw boundary call** until the build is green under the rules. It has no authority to weaken, suppress, or exempt a rule.

## Decisions

### `separate-new-contract`
The boundary framework is its own contract, not folded into the cross-platform one; far bigger scope (the boundary interfaces, three new assemblies, the container plus construction walls, AG0011–AG0018 and AG0101, the whole-tree cleanup, and the test system).
Tim asked *"Is this a new contract? or an inclusion into the other one?"* and, on being told new-with-two-overlaps, directed the process be run.

### `assembly-lock-mechanism`
Enforcement is by assembly, not by a marker attribute: a raw boundary call compiles in exactly one assembly and is a build error in every other — the same lock the interop rule already uses (native only in `AgentGuard.CrossPlatform.*`). An attribute is self-grantable, so a lazy pass could add it and launder raw calls; an assembly lock cannot be self-granted.
Tim: *"We probably need to restructure the assemblies, but I lean to this one it is stronger and WE always want to go stronger for ana-rules."* and *"YEP, this is easy and clean..."*

### `abstractions-assembly-holds-all-interfaces`
A new assembly `AgentGuard.Abstractions` holds **every** interface — `IGuard`, `IHostAdapter`, `IPipeline`, all the existing contracts, and the new boundary interfaces — plus the data types they use (for example `DirectoryChild`). No OS calls and no implementations live in it. Every assembly references it. The interfaces do not go in `Boundaries`; that would force a reference cycle.
Tim, when asked whether this includes `IGuard`/`IHostAdapter`/`IPipeline`: *"YES all abtractions go here."*

### `boundaries-assembly`
A new assembly `AgentGuard.Boundaries` is the only place raw `System.IO`, `System.Environment`, `System.Random`, `Guid`, and `System.Console` calls compile. It holds the adapter classes, the `ISystemServices` container, and the one `SystemServices.Create()` factory. It references `Abstractions` and `CrossPlatform`.

### `seven-boundary-interfaces`
The engine interfaces plus the existing cross-platform one: `IFileReader` (extend the existing one), `IDirectoryEnumerator` (extend the existing one), `IFileWriter`, `IEnvironment`, `IGuidFactory`, `IConsole`; and `IPlatformFileSystem` (OS-divergent, already defined by the cross-platform contract). Decision `scan-resolutions` item 3 later split `IDirectoryWriter` off `IFileWriter`, so the full set is eight. The exact definitions are in "The interfaces" below and were approved as that code.

### `iconsole-mirrors-console`
`IConsole` follows the shape of the type it wraps — `Write`, `WriteLine`, `ErrorWrite`, `ErrorWriteLine`, and `Task<string> ReadToEndAsync(CancellationToken ct)` — so its newline behavior and its read cancellation are exactly `Console`'s. Raw `System.Console` is legal only in the `Boundaries` console adapter. (This is `scan-resolutions` item 2; it replaces the earlier `WriteOutput`/`WriteError`/`ReadInput` shape.)
Tim: *"we should when posible follow the patter of what we are wrapping"* and *"why is this not WriteLine Write"*.

### `iguidfactory-seam`
The one randomness call in the codebase — `Guid.NewGuid()`, used to build a unique temporary file name — is abstracted behind `IGuidFactory`, the same way time is abstracted behind `TimeProvider`, which also makes tests deterministic.
Tim: *"Okay so we need to abstract guids but it will help for testing."*

### `guid-seam-lives-in-crossplatform`
The temp-name guid generation — `PlatformFileSystemShared.TemporarySiblingPath` at `PlatformFileSystemShared.cs:38` — lives in `AgentGuard.CrossPlatform`, because the OS-uniform shared helper the per-OS implementations use needs it and cannot depend on `Boundaries` or `Engine`. `IGuidFactory` is defined in `AgentGuard.Abstractions`, which `CrossPlatform` already references, so the helper takes an `IGuidFactory` and calls `NewGuid()` on it; the raw `Guid.NewGuid()` stays in the Boundaries `GuidFactory` adapter (AG0014, raw only in `Boundaries`), threaded in via `SystemServices.Create()` → `Platform.Create()`. The interface and its test determinism are kept; nothing goes raw in CrossPlatform.
Tim: *"if it's needed in CrossPlatform IT MUST fucking live in cross platform ... THERE Is no other way!"*

### `single-construction-point`
One container `ISystemServices` holds the services, built by one factory `SystemServices.Create()`. Nothing else constructs an OS service. It is created once at the top and threaded down, so there is exactly one place to mock.
Tim: *"WE need a simple single construction point that returns these OS services so that there is only one place to mock."* and *"I want to see somethign that keeps us from reconstructing this too many times."*

### `two-walls-stop-the-bypass`
Reconstructing or bypassing the container is a build error, not a matter of discipline:
- **Wall 1 (compiler):** the adapters are `internal` in `AgentGuard.Boundaries` with `private` constructors, so nothing outside `Boundaries` can name or `new` one — a `FileReader.Create()` call deep in the chain does not compile.
- **Wall 2 (analyzer AG0017):** `SystemServices.Create()` is a build error anywhere except the single composition method in `Program` and the test builder.
Together, the only way any class gets an OS service is by receiving it through its constructor.
Tim's requirement: *"NONE of this matters if 2 days from now you call a Create method for one of tese abstractions deep in the chain because you are too lazy to pass it through 5 layers and so now we are back to not testable and the same shape as we would be off the raw classes."*
Tim approving the construction design: *"YES this is the only way it can work without a full IOC for now."*

### `constructor-injection-no-container`
The dependency-injection pattern is constructor injection wired by hand at the one composition point (the existing house style, where `GuardEngine.CreatePipeline` builds the graph). No dependency-injection container library. Each class receives the specific interfaces it needs, pulled off the container at composition. No class takes a lone service as a method argument, and no class is a `static` helper that reaches around the container.
Tim: *"WE must follow a DI/IOC pattern some place."* plus the construction-design approval above.

### `static-helpers-become-instances` (`scan-resolutions` item 4)
`ContextStorePaths` and `InstallIntegrity` are `static` helper classes today, so they hold no service. Under `single-construction-point` they become instances built at the one composition point, and their constructors receive the `IEnvironment` unpacked from the one `ISystemServices`. `PathCanonicalizer` is already an instance and receives it the same way.
Tim: *"I THOUGHT we only passed ONE servers everywhere HOW DO THESE methods have any SERVICE if they don't have the ONLY ONE SERVICE everythign hangs off of?"*

### `test-system` — `AgentGuard.TestHelpers` and `SystemServicesBuilder`
A new assembly `AgentGuard.TestHelpers`, referenced only by test projects and never shipped, holds the one approved way tests build the container plus reusable fakes and proxies. `SystemServicesBuilder` starts from `Real()` or `Fake()`, uses `With(...)` to substitute a mock, and `Wrap(...)` to wrap the current real service in a proxy. AG0017 allows `SystemServices.Create()` in exactly two places — the `Program` composition method and this builder — and test projects may not call `SystemServices.Create()` directly; they go through the builder. It ships `InMemoryFileSystem` (implementing `IFileReader`, `IDirectoryEnumerator`, `IFileWriter`, and `IDirectoryWriter` over a dictionary, no disk), `FixedGuidFactory`, `FakeEnvironment`, `RecordingConsole`, and proxy bases such as `RecordingFileReader` and `ThrowingDirectoryEnumerator`. `FakeTimeProvider` (Microsoft.Extensions.Time.Testing) is reused, not rebuilt.
Tim: *"SO we will need an 'Approved Test way to Create this'. THAT needs to be in a NEW assembly called 'TestHelpers' and that needs to allow the caller (a test class) to influencey how the class is constructed by providign substitutions or mocks."* / *"Create one real isntance, and substitute any service you need by either wriping the real one with a Proxy or providing a Mock."* / *"You will probably need to allow `Test` assemblies to call it to."* / *"THE builder approach is exactly right. LEt's lock it in the contract."*

### `testhelpers-locked-by-ag0018` (`scan-resolutions` item 5)
`AgentGuard.TestHelpers` must be a public assembly and `SystemServicesBuilder.Fake().Build()` returns an `ISystemServices` without calling `SystemServices.Create()`, so neither construction wall covers it. A new Roslyn analyzer, **AG0018**, built on the AG0008/AG0011 assembly-name gate (`CrossPlatformBoundary.IsCrossPlatformLibrary`), makes it a build error to reference any type from `AgentGuard.TestHelpers` from an assembly whose name is not a test assembly. So the shipped guard cannot be run against fake state; only a test project can.
Tim: *"ARE you talking an ANA rule if so FUCKING SAY THAT."*

### `tests-not-exempt-from-boundary-rules`
The boundary-call ban covers the test projects too. A test that needs real files on disk sets them up through `SystemServicesBuilder.Real()`'s `IFileWriter` and `IDirectoryWriter`, not raw `Directory.CreateDirectory`. `FixtureProject` moves into `TestHelpers` and is rebuilt on the real adapters. A test-only exemption is the crack that lets the untestable shape back in. Approved as part of `test-system`.

### `complete-the-set-not-a-test-fake` (`scan-resolutions` item 3)
When a test cannot run end to end because a production interface is missing a method, the interface is incomplete and must gain the method — a test-only fake method is not the answer. Directory-mutating operations split from `IFileWriter` into a new `IDirectoryWriter` (`CreateDirectory`, `DeleteDirectory`, `SetLastWriteTimeUtc`); `IFileWriter` keeps file writes only. `SetLastWriteTimeUtc` completes the set so `ContextStoreSweepTests` sets an old directory time through the real interface instead of raw `Directory.SetLastWriteTimeUtc`.
Tim: *"GENERALLY if we can't test somethign else because we are missing a method to run end to end test, we have not 'completed the set' for the production interface."* and *"FIND the IDirectoryWriter interface OR REPORT TO ME THAT WE ARE MISSING SOMETHIGN."*

### `overwrite-parameter-on-copy-move` (`scan-resolutions` item 1)
`IFileWriter.Copy` and `IFileWriter.Move` each take a `bool overwrite = false` parameter. False, the default, throws when a file is already at the destination; true replaces it. `AtomicFile`'s commit calls both with `overwrite: true`.
Tim: *"Add a parameter if suplied as true it overwrites if supplied as false (default) it throws."*

### `platform-interfaces-to-abstractions` (Tim approved 2026-08-10)
Both platform interfaces — `IPlatformFileSystem` and its container `IPlatformServices` — move from `AgentGuard.CrossPlatform` into the new `AgentGuard.Abstractions` assembly. They move together: `IPlatformServices.FileSystem` returns `IPlatformFileSystem`, so leaving either behind would force an `Abstractions → CrossPlatform` reference and a cycle. The implementations (`PosixFileSystem`, `WindowsFileSystem`, the concrete `PlatformServices`) and the per-OS `Platform.Create()` factories stay in the `CrossPlatform.*` assemblies. The `AgentGuard.CrossPlatform` namespace keeps its name; only the two interface definitions relocate.
Tim: *"I agree we should record this and I now approve the move of IPlatformServices to the Abstractions assembly."*

### `platform-create-internal` (Tim approved 2026-08-10)
`Platform.Create()` becomes `internal` in each per-OS assembly, with `InternalsVisibleTo` for `AgentGuard.Boundaries` (the one production caller, inside `SystemServices.Create()`) and `AgentGuard.CrossPlatform.Tests` (so `PlatformFileSystemSpecTests` keeps its direct call). This closes the gap where `Platform.Create()` was `public static` and any engine code could bypass the container by calling it directly.
Tim chose `platform-create-internal`, and on the test visibility: *"we would need to make it visiable to it's test also (which should be allowed to direct create)."*

### `single-container-subsumes-platform-services`
`ISystemServices` is the one container, built by the one `SystemServices.Create()` in `Boundaries`. That factory composes the OS-uniform adapters with the OS-divergent `IPlatformFileSystem` obtained from the per-OS `Platform.Create()`, and exposes it as `ISystemServices.Platform`. So `IPlatformServices` is subsumed. The two interface families split by why they exist: `IPlatformFileSystem` is OS-divergent (symlinks, the executable bit, case sensitivity — different per OS, governed by AG0101); the OS-uniform ones (file, directory, environment, console, GUID) are identical on every OS, governed by AG0011–AG0016.
This is the merge design Tim approved with the move above.

### `case-sensitivity-detected-per-filesystem` (`scan-resolutions` item 6)
Case sensitivity is a property of the filesystem, not the OS. One owner detects it: `bool IsCaseSensitive(string path)` grows `IPlatformFileSystem` (AG0101), and the `EnumerateFiles` adapter reads it to set `EnumerationOptions.MatchCasing`. Growing `IPlatformFileSystem` is an interop-interface change the cross-platform contract locked; granted here.
The base plan, settled with Tim: detect against the exact directory being searched, in three layers that reuse one probe.
1. A native per-OS query first, because it answers even an empty directory — macOS `pathconf(path, _PC_CASE_SENSITIVE)`, Windows `GetFileInformationByHandleEx` with `FileCaseSensitiveInfo`.
2. A shared read-only probe as the fallback whenever the native query is unavailable or unclear (Windows without the flag, macOS on a `pathconf` error, and Linux always, since it has no query): in the searched directory, take an existing entry that has a letter, stat that same name in flipped case, and if it resolves to the same entry the volume is case-insensitive. One implementation in `PlatformFileSystemShared`, called by all three per-OS implementations.
3. A documented default (case-sensitive) only when even the probe cannot tell — an empty directory, or entries with no letter to flip.
Every layer runs against the searched directory, so it targets the correct volume even across mount points. The native calls live in the per-OS implementations; the probe lives once in `PlatformFileSystemShared`.
Tim: *"it is not ALWAYS case incensitive on Windows nor ALWAYS sensitive on Mac ... WE must detect if we are NEEDED to be sensitive or insensitive based on how the FS is setup and THAT MUST be designed with SOLID/DRY principles."* and *"should we not apply the same 'probe' process we use for Linux if we don't get a clear signal on WIndows from GetFileInfor..."*

### `rule-set` — AG0011–AG0016, AG0017, AG0018, AG0101
Approved as the table in "The rules" below.
Tim on the boundary rules: *"THE REST however were good and make sense to me."* On the OS-divergent series numbering: *"THis should probably be AG0101 --- SO that more OS-divergent items can be added into the same 'series'."* On the legal edges: *"Okay."* On the TestHelpers rule: *"ARE you talking an ANA rule if so FUCKING SAY THAT."*

### `adversary-corrections-folded-in`
Folded into the rule set from the design and refutation passes (corrections to get it right, not separate decisions):
- Single-argument `Path.GetFullPath(path)` is banned; it is not pure, because it resolves a relative path against the current directory. The replacement is the pure two-argument `Path.GetFullPath(path, basePath)`, with `basePath` supplied from `IEnvironment.GetCurrentDirectory()` at the call site (`static-helpers-become-instances`).
- The ban list gains `Marshal` and the P/Invoke call site (AG0008 catches only the `[LibraryImport]` declaration, not the syscall), and the deployment-path reads `Assembly.Location`, `AppContext.BaseDirectory`, and `AppDomain.CurrentDomain.BaseDirectory`.
- Detection is symbol- and semantic-based (Roslyn `IOperation`), covering property reads (`DateTime.UtcNow`, `FileInfo.UnixFileMode`, `Environment.CurrentDirectory`, `Console.Out`) and surviving `using static` and aliases — never a syntax or text match.
- Member-level rules beat type-level: the symlink and Unix-mode members of `Directory` and `FileInfo` route to `IPlatformFileSystem` (AG0101), not the filesystem interfaces (AG0011).
- The only compile-time-uncatchable evasion is reflection by string name; it is documented, not papered over.

### `legal-edges`
These stay legal everywhere: the pure `Path` members (`Combine`, `GetFileName`, `GetDirectoryName`, `IsPathRooted`, `DirectorySeparatorChar`); the hashing and signature crypto (`SHA256`, `Ed25519`); and reading an enum value as data (`UnixFileMode.UserRead`, `FileAttributes.Hidden`).
Tim: *"Okay."*

## What we're building

Three new assemblies and the whole-tree rewiring that uses them, so no engine, CLI, or test code touches an OS primitive directly.

`AgentGuard.Abstractions` holds every interface and its data types, references nothing, and is referenced by all. `AgentGuard.Boundaries` holds the adapters, the `ISystemServices` container, and the one `SystemServices.Create()`; it is the only assembly where raw `System.IO` / `Environment` / `Random` / `Guid` / `Console` compiles. `AgentGuard.TestHelpers` holds `SystemServicesBuilder`, the fakes and proxies, and the relocated `FixtureProject`, and is referenced only by test projects. The engine and CLI are rewired: every raw boundary call is replaced by a call on a service received through the constructor, the two `static` path helpers become instances that receive the container, `Program` becomes the single composition point that calls `SystemServices.Create()` and threads the container down, and the tests build their world through the builder. AG0011–AG0018 and AG0101 make any regression a build error.

Non-negotiable rule: after the cleanup, a raw `System.IO` / `Environment` / `Random` / `Guid` / `Console` call compiles only in `AgentGuard.Boundaries`, and a raw OS-divergent call (Unix mode, symlink members, case-sensitivity probe, `Marshal`, the P/Invoke sites) compiles only in `AgentGuard.CrossPlatform.*`.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change.

End state:
- `AgentGuard.Abstractions`, `AgentGuard.Boundaries`, and `AgentGuard.TestHelpers` exist and build; every interface (including `IPlatformFileSystem` and `IPlatformServices`, relocated) lives in `AgentGuard.Abstractions`; the boundary adapters, `ISystemServices`, and `SystemServices.Create()` live in `AgentGuard.Boundaries`.
- `SystemServices.Create()` is called in exactly one production place — the single `Program` composition method — and the container is threaded from there by constructor injection; no other production class constructs an OS service, and no production class is a `static` helper reaching around the container.
- `Platform.Create()` is `internal` in each per-OS assembly, visible only to `AgentGuard.Boundaries` and `AgentGuard.CrossPlatform.Tests`.
- AG0011–AG0018 and AG0101 exist and are enforced as build errors; every raw boundary call in the engine, the CLI, and the test projects is gone, replaced by the owning interface; a reference to `AgentGuard.TestHelpers` from a shipping assembly is a build error (AG0018).
- The test system is rebuilt on `SystemServicesBuilder`; `FixtureProject` lives in `AgentGuard.TestHelpers` and uses the real adapters; no test calls `SystemServices.Create()` directly or makes a raw boundary call; `ContextStoreSweepTests` sets its old directory time through `IDirectoryWriter.SetLastWriteTimeUtc`.
- `dotnet build -c Release` = 0 warnings / 0 errors and `dotnet test -c Release` = 0 failed, locally and in CI on all three OS.

Any restatement or weakening of this to fit the result is a top-line Lie-catcher finding.

## Surfaces

Every file the change touches, grouped by the interface that will own its raw calls. The raw-call counts and file:line anchors are from the GROUND run.

**New assemblies and their contents:**
- `src/AgentGuard.Abstractions/` — the interface-only assembly: all relocated engine interfaces and data types, the relocated `IPlatformFileSystem` and `IPlatformServices`, and the new/extended boundary interfaces plus `ISystemServices`.
- `src/AgentGuard.Boundaries/` — the adapters (`FileReader`, the directory enumerator, `FileWriter`, `DirectoryWriter`, `EnvironmentAdapter`, `GuidFactory`, `ConsoleAdapter`), the `SystemServices` container implementation, and `SystemServices.Create()`.
- `src/AgentGuard.TestHelpers/` (referenced by test projects only) — `SystemServicesBuilder`, `InMemoryFileSystem`, `FixedGuidFactory`, `FakeEnvironment`, `RecordingConsole`, the proxy bases, and the relocated `FixtureProject`.

**Filesystem reads and writes → `IFileReader` / `IFileWriter` / `IDirectoryWriter` / `IDirectoryEnumerator`:**
- `src/AgentGuard.Engine/Setup/AtomicFile.cs` (writes, copy, rename, `Directory.CreateDirectory` at :23,:31,:44,:66,:83,:93; its commit calls `Copy`/`Move` with `overwrite: true`).
- `src/AgentGuard.Engine/Setup/CreationHelper.cs` (12 raw `File.Exists`/`Directory.CreateDirectory`/`Directory.Delete` sites at :26,:34,:40,:64,:79,:114,:115,:129,:177,:212,:214; the `MakeExecutable` block at :47–50 already goes through `IPlatformFileSystem` and stays).
- `src/AgentGuard.Engine/Setup/InstallIntegrity.cs` (`File.Exists`/`Directory.Exists`/`File.ReadAllText` at :33,:44,:83,:84,:99,:100,:108; the single-arg `Path.GetFullPath` at :80; the `IsLinkTarget` call at :101 stays).
- `src/AgentGuard.Engine/Setup/MachineInspection.cs` (:22,:31), `SetupCommands.cs` (:34,:84), `Hashing.cs` (:27), `SafeRead.cs` (:27), `IdempotentAppend.cs` (:26), `ClaudeSettings.cs` (:22), `ProjectPaths.cs` (:76).
- The eight `ISetupCondition.Detect()` implementations, each a raw `File.Exists`: `BinSymlinkCondition.cs:22`, `CurrentSymlinkCondition.cs:22`, `BinaryHashCondition.cs:26`, `ConfigJsonCondition.cs:23`, `GitignoreCondition.cs:22`, `PathProfileCondition.cs:23`, `VersionBinaryCondition.cs:20`, `VersionStampCondition.cs:36` (the two symlink conditions' `ReadLinkTarget` calls stay).
- `src/AgentGuard.Engine/ContextStore.cs` (:43,:48,:57,:59,:70,:76,:79,:81), `ContextStoreInspector.cs` (:39,:46,:49), `GrantStore.cs` (:53,:59,:102), `ProjectRuleSource.cs` (:43,:48).
- `src/AgentGuard.Engine/FileReader.cs` (the existing `IFileReader` adapter, moves to `Boundaries`), `SystemDirectoryEnumerator.cs` (the existing `IDirectoryEnumerator` adapter, moves to `Boundaries`).

**Environment and deployment-path reads → `IEnvironment` (and two-arg `Path.GetFullPath`):**
- `src/AgentGuard.Engine/Setup/SetupContext.cs` (`Environment.GetFolderPath`/`GetEnvironmentVariable`/`ProcessPath` and `Directory.GetCurrentDirectory` at :58,:59,:63,:64; the `Assembly.GetEntryAssembly()` version read at :71–84).
- `src/AgentGuard.Engine/GuardEngine.cs` (`Environment.GetFolderPath` at :81; the grant-key `File.Exists`/`File.ReadAllText` at :124,:131).
- `src/AgentGuard.Engine/ClaudeCodeHostAdapter.cs:91` (`Directory.GetCurrentDirectory` fallback).
- `src/AgentGuard.Engine/ContextStorePaths.cs:25` and `src/AgentGuard.Engine/Setup/InstallIntegrity.cs:78,:80` (single-arg `Path.GetFullPath` → two-arg with `IEnvironment.GetCurrentDirectory()`; both classes become instances — `static-helpers-become-instances`).
- `src/AgentGuard.Engine/PathCanonicalizer.cs` (single-arg `Path.GetFullPath` at :29,:65 → two-arg; already an instance).
- `src/AgentGuard.Cli/Program.cs:56–62,:153` (`typeof(Program).Assembly` version read; `Environment.ProcessPath`).

**OS-divergent reads → `IPlatformFileSystem` (AG0101):**
- `src/AgentGuard.Engine/PathCanonicalizer.cs` (the `.LinkTarget` read at :75 and the `DirectoryInfo`/`FileInfo` construction at :72–74; its `Directory.Exists` routes to `IDirectoryEnumerator`).
- `IPlatformFileSystem` grows `bool IsCaseSensitive(string path)` for the `EnumerateFiles` case-sensitivity decision (`case-sensitivity-detected-per-filesystem`).
- `src/AgentGuard.CrossPlatform.*` — the existing `Marshal.GetLastPInvokeError` and `[LibraryImport]` sites already confined here — governed by AG0101 and AG0008, no move.

**Console → `IConsole`:**
- `src/AgentGuard.Cli/Program.cs` (the 13 `Console.*` calls at :60,:61,:62,:147,:151,:158,:169,:185,:192,:211,:214,:219).

**GUID → `IGuidFactory`:**
- `src/AgentGuard.CrossPlatform/PlatformFileSystemShared.cs:38` (the one `Guid.NewGuid()`; the helper takes an `IGuidFactory` and calls it — `guid-seam-lives-in-crossplatform`).

**Composition and threading:**
- `src/AgentGuard.Cli/Program.cs` — becomes the single `SystemServices.Create()` composition point.
- `src/AgentGuard.Engine/Setup/GuardHost.cs` (`ExecuteHookAsync` at :48–49 and `RunAsync` at :96 receive the container instead of constructing) and `src/AgentGuard.Engine/Setup/SetupContext.cs:67` (`Platform.Create().FileSystem` → injected). `tests/AgentGuard.CrossPlatform.Tests/PlatformFileSystemSpecTests.cs:30` is the one direct `Platform.Create()` caller that stays.
- `src/AgentGuard.Engine/GuardEngine.cs:58–119` — the current composition root; its hardcoded `.Create()` calls become services pulled off the container.

**Test projects (`tests-not-exempt-from-boundary-rules`):**
- `tests/AgentGuard.Tests/FixtureProject.cs` (moves to `TestHelpers`, rebuilt on the real adapters), `SetupHarness.cs`, `ContextStoreSweepTests.cs` (the raw `Directory.SetLastWriteTimeUtc` at :29–32 becomes `IDirectoryWriter.SetLastWriteTimeUtc` on `Fake()`), `FailClosedHardeningTests.cs`, `PathCanonicalizerTests.cs`, and the other test files carrying raw boundary calls (21 files in `AgentGuard.Tests` plus `PlatformFileSystemSpecTests.cs`).

**Analyzers (rule phase):**
- `analyzers/AgentGuard.Analyzers/` — the new AG0011–AG0018 and AG0101 analyzers plus `AnalyzerReleases.Unshipped.md`, built on the existing `CrossPlatformBoundary`, `WellKnownType`, `ContractPattern`, and `Directory.Build.props` infra.

## Reuse ledger (from the prior-art-ledger run, 2026-08-10)

| Capability | Ruling | Owner / note |
|---|---|---|
| `IFileReader` (add a text read) | extend | Existing interface `src/AgentGuard.Engine/Abstractions/Contracts/IFileReader.cs`; adapter `src/AgentGuard.Engine/FileReader.cs`. Both relocate; the interface gains `ReadAllText`. |
| `IDirectoryEnumerator` (add three members) | extend | Exists internal in `src/AgentGuard.Engine/IDirectoryEnumerator.cs` (`DirectoryExists`, `EnumerateChildren`). Grows `EnumerateFiles`, `EnumerateDirectories`, `GetLastWriteTimeUtc`; relocates to `Abstractions`. |
| `IFileWriter` (file writes) | extract | The file-write calls in `src/AgentGuard.Engine/PrivilegedWriter.cs:17` and `Setup/AtomicFile.cs:15`. No single interface exposes the full file-write surface — collapse into one. Gains the `overwrite` parameter on `Copy`/`Move`. |
| `IDirectoryWriter` (directory writes) | extract + new | `CreateDirectory`/`DeleteDirectory` extracted from `Setup/CreationHelper.cs:40` and the per-OS `PosixFileSystem.cs:37` / `WindowsFileSystem.cs:59`; `SetLastWriteTimeUtc` is new, added to complete the set for `ContextStoreSweepTests`. |
| `IEnvironment` | extract | Duplicated across `Setup/SetupContext.cs:58`, `GuardEngine.cs:81`, `ClaudeCodeHostAdapter.cs:91`, `Cli/Program.cs:153`, and two test files. No injectable interface exists; `SetupContext` is a concrete record. |
| `IGuidFactory` | reuse (single owner) | The sole `Guid.NewGuid()` is `src/AgentGuard.CrossPlatform/PlatformFileSystemShared.cs:38`; `AtomicFile.TemporarySiblingPath` already delegates to it. The interface wraps this one owner. |
| `IConsole` | new | 13 raw `System.Console` calls in `Cli/Program.cs`; all three lenses found no existing console interface. Shaped to mirror `System.Console`. |
| `ISystemServices` container + `SystemServices.Create()` + `SystemServicesBuilder` | new | No OS-services container exists. Follows the `GuardEngine.CreatePipeline`/`FileGuardServices` bundle idiom and the `Platform.Create()` container idiom as precedent. |
| AG0011–AG0018 + AG0101 analyzers | new | None exist (`AnalyzerReleases.Unshipped.md` lists AG0001..AG0010 only). Built on the existing analyzer infra (below), which is reused. |
| Analyzer infra (`CrossPlatformBoundary`, `WellKnownType`, `ContractPattern`, `Directory.Build.props` wiring) | reuse | Existing; the new rules build on it. |
| Time interface (`TimeProvider`) | reuse | Already injected everywhere; the one real-clock instantiation is `Setup/GuardHost.cs:96`. AG0015 is preventive. |

## The interfaces (as approved)

```csharp
// AgentGuard.Abstractions

public interface IFileReader   // EXTEND the existing one with a text read
{
    bool Exists(string path);
    ReadOnlyMemory<byte> ReadAllBytesAsync(string path, CancellationToken ct);
    string ReadAllText(string path);
}

public interface IDirectoryEnumerator   // read-only directory access
{
    bool DirectoryExists(string path);
    IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath);        // scanner walk
    IReadOnlyList<string> EnumerateFiles(string directoryPath, string pattern);   // *.token, *.csproj, inspector
    IReadOnlyList<string> EnumerateDirectories(string directoryPath);             // context-store sweep
    DateTimeOffset GetLastWriteTimeUtc(string path);
}
public readonly record struct DirectoryChild(string FullPath, bool IsDirectory, bool IsReparsePoint);

public interface IFileWriter   // file writes only
{
    Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken ct);
    void WriteAllText(string path, string contents);
    void Copy(string source, string destination, bool overwrite = false);   // false throws if a file exists there
    void Move(string source, string destination, bool overwrite = false);
    void DeleteFile(string path);
}

public interface IDirectoryWriter   // directory writes (split off IFileWriter, decision 3)
{
    void CreateDirectory(string path);
    void DeleteDirectory(string path, bool recursive);
    void SetLastWriteTimeUtc(string path, DateTimeOffset time);              // completes the set for the sweep test
}

public interface IEnvironment
{
    string GetCurrentDirectory();
    string GetHomeDirectory();
    string? GetEnvironmentVariable(string name);
    string? GetProcessPath();
}

public interface IGuidFactory { Guid NewGuid(); }

public interface IConsole   // mirrors System.Console
{
    void Write(string text);                            // Console.Out.Write
    void WriteLine(string text);                        // Console.Out.WriteLine
    void ErrorWrite(string text);                       // Console.Error.Write
    void ErrorWriteLine(string text);                   // Console.Error.WriteLine
    Task<string> ReadToEndAsync(CancellationToken ct);  // Console.In.ReadToEndAsync
}

// The single container. IPlatformFileSystem comes from Platform.Create(), composed in.
public interface ISystemServices
{
    IFileReader          FileReader      { get; }
    IDirectoryEnumerator Directories     { get; }
    IFileWriter          FileWriter      { get; }
    IDirectoryWriter     DirectoryWriter { get; }
    IEnvironment         Environment     { get; }
    IGuidFactory         Guids           { get; }
    IConsole             Console         { get; }
    IPlatformFileSystem  Platform        { get; }
}
```

`IPlatformFileSystem` (defined in `AgentGuard.Abstractions` after the relocation) grows one member for the case-sensitivity decision: `bool IsCaseSensitive(string path)`, implemented per OS.

```csharp
// AgentGuard.Boundaries — the one factory; adapters are internal with private ctors.
public static class SystemServices { public static ISystemServices Create(); }  // AG0017-pinned to Program + the test builder
```

```csharp
// AgentGuard.TestHelpers
public sealed class SystemServicesBuilder
{
    public static SystemServicesBuilder Real();   // real adapters (the one allowed SystemServices.Create() in tests)
    public static SystemServicesBuilder Fake();   // in-memory fakes, no real OS
    public SystemServicesBuilder With(IFileReader reader);            // …one per service…  (mock)
    public SystemServicesBuilder With(TimeProvider clock);
    public SystemServicesBuilder Wrap(Func<IFileReader, IFileReader> proxy);  // …one per service…  (proxy)
    public ISystemServices Build();
}
```

## The rules

| Rule | DON'T call (anywhere but the allowed assembly) | Allowed only in | DO use instead |
|---|---|---|---|
| **AG0011 Filesystem** | `File`, `Directory`, `FileInfo`, `DirectoryInfo`, `FileSystemInfo`, `DriveInfo`, `FileStream`, `StreamReader`, `StreamWriter`, `FileSystemWatcher`, `System.IO.Enumeration.*` | `AgentGuard.Boundaries` | `IFileReader` / `IDirectoryEnumerator` / `IFileWriter` / `IDirectoryWriter` |
| **AG0012 Environment** | `System.Environment` (all), `Directory.GetCurrentDirectory`/`SetCurrentDirectory`, single-arg `Path.GetFullPath`, `Assembly.Location`/`GetEntryAssembly().Location`, `AppContext.BaseDirectory`, `AppDomain.CurrentDomain.BaseDirectory`, `RuntimeInformation` host-description props | `AgentGuard.Boundaries` | `IEnvironment`; two-arg `Path.GetFullPath(path, basePath)` |
| **AG0013 Process** | `System.Diagnostics.Process`, `ProcessStartInfo` | nowhere (no use today) | grow an interface first if ever needed |
| **AG0014 Randomness** | `System.Random`, `Guid.NewGuid()`, `RandomNumberGenerator` | `AgentGuard.Boundaries` (the `GuidFactory` adapter) | `IGuidFactory` |
| **AG0015 Time** | `DateTime.Now`/`UtcNow`/`Today`, `DateTimeOffset.Now`/`UtcNow`, `Stopwatch`, `Environment.TickCount` | nowhere | injected `TimeProvider` (already used everywhere — preventive) |
| **AG0016 Console** | `System.Console` | `AgentGuard.Boundaries` | `IConsole` |
| **AG0017 Construction** | `SystemServices.Create()` | the single `Program` composition method + `SystemServicesBuilder` | receive `ISystemServices` by constructor injection |
| **AG0018 TestHelpers isolation** | any reference to a type in `AgentGuard.TestHelpers` (`SystemServicesBuilder`, the fakes) | test assemblies only (assembly name ends in `.Tests`) | shipping code never references `TestHelpers`; tests build the container through `SystemServicesBuilder` |
| **AG0101 OS-divergent FS** | `File.SetUnixFileMode`/`GetUnixFileMode`, `FileInfo`/`DirectoryInfo.UnixFileMode`, symlink members (`LinkTarget`, `CreateSymbolicLink`, `ResolveLinkTarget`), a filesystem case-sensitivity probe, `Marshal`, the libc `rename`/`MoveFileEx` P/Invoke calls | `AgentGuard.CrossPlatform.*` | `IPlatformFileSystem` |

## What to do

1. **Build `AgentGuard.Abstractions`** (`abstractions-assembly-holds-all-interfaces`): a new interface-only assembly referencing nothing. Move every existing engine interface (`IGuard`, `IHostAdapter`, `IPipeline`, the `Abstractions/Contracts/` set, `IFileReader`, the internal `IDirectoryEnumerator`, `DirectoryChild`) into it; move `IPlatformFileSystem` and `IPlatformServices` here from `AgentGuard.CrossPlatform` (`platform-interfaces-to-abstractions`); add the new `IFileWriter`, `IDirectoryWriter`, `IEnvironment`, `IGuidFactory`, `IConsole`, and `ISystemServices`; extend `IFileReader` with `ReadAllText`, `IDirectoryEnumerator` with its three members, and `IPlatformFileSystem` with `IsCaseSensitive`. Every other assembly references it.
2. **Build `AgentGuard.Boundaries`** (`boundaries-assembly`, `two-walls-stop-the-bypass`): the adapters for the OS-uniform interfaces, each `internal` with a `private` constructor; the `SystemServices` container; and `SystemServices.Create()`, which composes the adapters with the `IPlatformFileSystem` from `Platform.Create()` and exposes it as `ISystemServices.Platform` (`single-container-subsumes-platform-services`). The `GuidFactory` adapter is the one place raw `Guid.NewGuid()` lives, and it is passed into `Platform.Create()` so the CrossPlatform temp-name helper uses it (`guid-seam-lives-in-crossplatform`). Move the existing `FileReader` and `SystemDirectoryEnumerator` adapters here.
3. **Make `Platform.Create()` internal** in each per-OS assembly with `InternalsVisibleTo` for `AgentGuard.Boundaries` and `AgentGuard.CrossPlatform.Tests` (`platform-create-internal`).
4. **Make `Program` the single composition point** (`single-construction-point`, `constructor-injection-no-container`): call `SystemServices.Create()` once and thread the `ISystemServices` down by constructor injection into every entry that needs it — `GuardHost.ExecuteHookAsync` (uses `services.Platform` for the integrity check, then passes `services` into `GuardEngine.CreatePipeline`) and `SetupContext.ForCurrentProcess` (home, shell, current directory, process path via `services.Environment`; the file system via `services.Platform`). Convert `ContextStorePaths` and `InstallIntegrity` from `static` helpers to instances built here, receiving `IEnvironment` (`static-helpers-become-instances`). Remove the two production `Platform.Create()` call sites at `SetupContext.cs:67` and `GuardHost.cs:49`, and the hardcoded `.Create()` calls in `GuardEngine.CreatePipeline`.
5. **Route every raw boundary call through its interface** (the Surfaces list is the worklist): filesystem reads and writes to `IFileReader`/`IFileWriter`/`IDirectoryWriter`/`IDirectoryEnumerator`; environment and deployment-path reads to `IEnvironment`; single-arg `Path.GetFullPath` to the two-arg form with `IEnvironment.GetCurrentDirectory()` as `basePath`; the `.LinkTarget`, Unix-mode, and case-sensitivity members to `IPlatformFileSystem`; the 13 `Console.*` calls to `IConsole`. After this pass, a raw OS-uniform call compiles only in `Boundaries` and a raw OS-divergent call only in `CrossPlatform.*`.
6. **Build the test system** (`test-system`, `tests-not-exempt-from-boundary-rules`): `AgentGuard.TestHelpers` with `SystemServicesBuilder` (`Real`/`Fake`/`With`/`Wrap`), `InMemoryFileSystem`, `FixedGuidFactory`, `FakeEnvironment`, `RecordingConsole`, the proxy bases, and the relocated `FixtureProject` on the real adapters; reuse `FakeTimeProvider`. Rewire every test to build its world through the builder and drop its raw boundary calls, including `ContextStoreSweepTests`, which sets its old directory time through `IDirectoryWriter.SetLastWriteTimeUtc` on `Fake()`.
7. **Implement the case-sensitivity detection** (`case-sensitivity-detected-per-filesystem`): the native per-OS query in each per-OS implementation (`pathconf(_PC_CASE_SENSITIVE)` on macOS, `GetFileInformationByHandleEx`/`FileCaseSensitiveInfo` on Windows), the shared read-only probe in `PlatformFileSystemShared` as the fallback, and the documented case-sensitive default when the probe cannot tell. `IsCaseSensitive(string path)` is the one member; the `EnumerateFiles` adapter reads it to set `EnumerationOptions.MatchCasing`.
8. **The rules are written in the rule phase** (Execution): AG0011–AG0018 and AG0101, RED where they hit live raw calls, preventive where clean, on the existing analyzer infra. The implementation phase cleans up under them with no suppression or exemption.
9. **Prove:** `dotnet build`/`test` green locally under the new rules; then the CI pipeline green on all three OS.

## What the agent MAY do

- Relocate and rename internal implementation members while routing raw calls, as long as the interface shapes above are unchanged and the assembly boundaries hold.
- Split `SystemServices.Create()`'s composition into private helpers as long as `SystemServices.Create()` is the one public entry.

## What the agent MUST NOT do

- Change any interface shape in "The interfaces" without Tim's sign-off, or add a boundary interface beyond those listed.
- Suppress, weaken, exempt, or narrow any of AG0011–AG0018 or AG0101 to force a green build; a raw call is fixed by routing it through the interface, never by silencing the rule.
- Leave a raw boundary call anywhere but its allowed assembly, including in test projects (`tests-not-exempt-from-boundary-rules`).
- Add a second `SystemServices.Create()` call site, reconstruct an OS service anywhere but the one composition point and the test builder, or reference `AgentGuard.TestHelpers` from shipping code.
- Weaken, skip, delete, or re-point a test to make it pass; commit; or expand scope. On any wall the plan does not cover, stop and report.

## Acceptance

1. Local: `dotnet build -c Release` = 0/0; `dotnet test -c Release` = 0 failed. CI: green on macOS, Linux, and Windows.
2. `AgentGuard.Abstractions`, `AgentGuard.Boundaries`, and `AgentGuard.TestHelpers` build; `IPlatformFileSystem` and `IPlatformServices` resolve from `AgentGuard.Abstractions` (their definitions no longer in `AgentGuard.CrossPlatform`).
3. A raw `File`/`Directory`/`Environment`/`Console`/`Guid.NewGuid`/single-arg `Path.GetFullPath` outside `AgentGuard.Boundaries` is a build error (proven by a would-fail probe the rule phase adds, or a grep showing zero such calls outside `Boundaries`/`CrossPlatform.*`).
4. A raw OS-divergent call (`SetUnixFileMode`, symlink members, the case-sensitivity probe, `Marshal`, the P/Invoke sites) compiles only in `AgentGuard.CrossPlatform.*` (grep + AG0101 red-then-clean).
5. `SystemServices.Create()` appears in exactly two places: the one `Program` composition method and `SystemServicesBuilder` (grep). AG0017 makes a third call a build error.
6. `Platform.Create()` is `internal` in each per-OS assembly; the only callers are `SystemServices.Create()` and `PlatformFileSystemSpecTests.cs:30` (grep + the `InternalsVisibleTo` entries).
7. A reference to `AgentGuard.TestHelpers` from any shipping assembly is a build error (AG0018 red-then-clean); no test project calls `SystemServices.Create()` directly or makes a raw boundary call; `FixtureProject` lives in `AgentGuard.TestHelpers`.
8. `IFileWriter.Copy`/`Move` with `overwrite: false` throw when a file is at the destination; `ContextStoreSweepTests` sets its old directory time through `IDirectoryWriter.SetLastWriteTimeUtc`; `EnumerateFiles` matches case per `IsCaseSensitive`.
9. The reuse ledger is honored: `IFileWriter`/`IDirectoryWriter`/`IEnvironment` are extracted from their listed copies (no new duplicate), `IGuidFactory` wraps the single `PlatformFileSystemShared` owner, and no `reuse`/`extract` capability was built fresh.

## Tier

FULL — three new assemblies, an interface relocation across assemblies, a whole-tree boundary rewrite, nine analyzer rules with a RED-then-clean rule phase, and the test system rebuilt; all roles.

## Open

- The "mirror the type we wrap" principle is written into the best-practices guide as Layer-2 principle 2c under Category 2 (`.dev/inprocess/DRAFT-best-practices-guide.md`); Tim to correct the wording there if it needs it.

## Scope

Change scope only by editing this file before the run starts.
