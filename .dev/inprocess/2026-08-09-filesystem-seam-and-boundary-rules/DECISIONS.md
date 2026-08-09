# Filesystem seam + boundary rules — DECISIONS (pre-contract record)

**Status.** Design settled with Tim across the 2026-08-09 session; every decision below carries his exact approving words. **This contract is PARKED — the cross-platform contract lands FIRST** (Tim ruled 2026-08-09; see Sequencing). GROUND has run (workflow `wf_102e11b0-164`) and its facts are captured below so they survive any context reset. When this contract resumes, its own contract.md gets written FROM the grounded facts + the reuse ledger + these decisions, then the hidden-decision scan runs over the draft, then RULE-PHASE. Nothing here lives in the orchestrator's memory — resume by READING this file.

## Sequencing (Tim ruled 2026-08-09)
Finish the cross-platform contract first — *"I agree we should finish it it is the worst objection and when we put the rules in place for the other items the rest will get clean also. SO let's extract all the corss-platform work fnow and fix it and then we can move on to the clean up of the CLR primitives (and unlock better testing)."*

**Cross-platform DELIVERS to this contract (inherited as done — do NOT rebuild):**
- `IDirectoryEnumerator` — built for the scanner fail-closed fix (`fail-closed-scanner-enumerator-seam` in the cross-platform contract). This contract REUSES it; it is one of the seven boundary interfaces but already exists.
- `IPlatformFileSystem` — defined in `AgentGuard.CrossPlatform` (its locked `namespace-crossplatform` decision). This contract composes it into `ISystemServices.Platform` and governs its members with AG0101; it does NOT define it.
- `NativeInterop.cs` deleted; the `CreationHelper.cs:46` chmod moved behind `IPlatformFileSystem.MakeExecutable`; the fail-closed test rewritten (no OS branch, no chmod); the config-protection CRLF fix.

**This contract then builds (what's LEFT):** extend `IFileReader`; new `IFileWriter`, `IEnvironment`, `IGuidFactory`, `IConsole`; the three assemblies (`Abstractions`, `Boundaries`, `TestHelpers`); `ISystemServices` + `SystemServices.Create()` + the two construction walls; AG0011–AG0017 + AG0101; the ~53 raw `Setup/` sites + the other non-adapter sites; the test system; and the one leftover decision — whether to pull `IPlatformFileSystem` into `Abstractions`.

## Grounded facts (from GROUND `wf_102e11b0-164`, 2026-08-09 — the seam contract is written from these)
- **106 boundary calls across 34 files** (the ~113 estimate was high). Categories:
  - **26** already behind Guard-facing `Abstractions/Contracts` interfaces (`FileReader`/`IFileReader`, `PathCanonicalizer`, `ClaudeCodeHostAdapter`, `ContextStore`, `ContextStoreInspector`, `GrantStore`, `ProtectedFileScanner`, `ProjectRuleSource`) — legitimate; become `Boundaries` adapters.
  - **5** behind internal-wiring interfaces (`PrivilegedWriter`/`IPrivilegedWriter`, `BuildOutputSkipRule`/`IDirectorySkipRule`) — legitimate.
  - **21** at the composition root (`GuardEngine`: `Environment.GetFolderPath` at :75 + `File.Exists`/`ReadAllText` for the grant key at :116,:123) and the CLI entry (`Program.cs`: 13 `Console` + `Environment.ProcessPath` at :153; `SetupContext.ForCurrentProcess`: 4 process-state reads at :50,:51,:55,:56).
  - **53 in `Setup/` with NO abstraction** — raw `File.Exists`/`ReadAllText`/`Directory.*` inside `ISetupCondition.Detect()`/`Repair()` decision code and the static helpers (`CreationHelper` 12 sites, `InstallIntegrity` 8 + 1 `Path.GetFullPath`, `MachineInspection`, `SetupCommands`, `AtomicFile`, `Hashing`, `SafeRead`, `IdempotentAppend`, `ClaudeSettings`, `SymlinkOps`, `ProjectPaths`). This half is the bulk of the cleanup.
  - `NativeInterop.cs:22` libc `rename` P/Invoke (deleted by cross-platform).
- **Zero** `DateTime.Now`/`UtcNow`, `Random`/`RandomNumberGenerator`, `Process`, `Stopwatch`, `Assembly.Location`/`AppContext.BaseDirectory` in live code. Only **1** `Guid.NewGuid` (`AtomicFile.cs:56`), **13** `Console` (all `Program.cs`), **4** single-arg `Path.GetFullPath` (`ContextStorePaths.cs:25`, `InstallIntegrity.cs:78`, +2), **2** `Marshal`/P-Invoke (`SymlinkOps.cs:67`, `NativeInterop`). Time is already fully seamed via injected `TimeProvider` — AG0015 is purely preventive.
- **Reuse:** `IFileReader`/`FileReader` (extend), `IPathCanonicalizer`, `IPrivilegedWriter`/`PrivilegedWriter` (rebuild on `IFileWriter`), `IContextStore`, `ProtectedFileScanner`, injected `TimeProvider`; the `static Create(deps)` factory + bundle idiom (`FileGuardServices`, `GuardEngineOptions`, ~30 files) — `ISystemServices` fits it; the analyzer infra (`CrossPlatformBoundary` assembly-gate model, `ContractPattern`, `WellKnownType`, `AnalyzerReleases.Unshipped.md`, `Directory.Build.props` `OutputItemType=Analyzer` + `TreatWarningsAsErrors`) for the new rules.
- **Test infra:** `InternalsVisibleTo` grants only `AgentGuard.Tests`; NO fakes exist for `IFileReader`/`IPathCanonicalizer`/`IPrivilegedWriter` (tests use the real `.Create()` against `FixtureProject`'s real temp dir via `Directory.CreateTempSubdirectory`); `ContextStoreSweepTests` reaches around the seam with `Directory.SetLastWriteTimeUtc` (goes RED — rebuild on the seam); `FailClosedHardeningTests` builds pipelines by hand. `TestHelpers` + the fakes are genuinely new.
- **Not built yet:** the `AgentGuard.CrossPlatform.*` assemblies and `IPlatformFileSystem` do not exist in live code (only AG0008/9/10 scaffolding + the RED `PosixSourceIsLinkSharedTests`); the disposable proto uses `AgentGuard.Platform` names, not `AgentGuard.CrossPlatform`.

**Branch** `rules-and-process` (not pushed). **Related issues:** #13 (rule + review must require a seam at every external boundary), #14 (the boundary-violation inventory — its ~13-site count is superseded; the real surface is ~113 sites / 34 files, to be re-derived by GROUND against the settled adapter list).

**Relationship to the cross-platform contract** (`../2026-08-07-cross-platform-engine-and-interop/contract.md`): this is a **separate, larger contract**. They overlap on exactly two things — `IDirectoryEnumerator` (the cross-platform contract borrows it for the fail-closed fix) and `IPlatformFileSystem` (which becomes the AG0101 boundary in this scheme). Coordination is an OPEN item in the cross-platform contract's Open section.

---

## Decisions (settled — Tim's exact words attached)

### `separate-new-contract`
The boundary framework is its own contract, not folded into the cross-platform one; far bigger scope (seven interfaces, three new assemblies, the container + construction walls, AG0011–AG0017 + AG0101, ~113-site cleanup, the test system). Tim asked *"Is this a new contract? or an inclusion into the other one?"* and, on being told new-with-two-overlaps, directed the process be run (see `grounding-first`).

### `assembly-lock-mechanism`
Enforcement is by assembly, not a marker attribute: a raw boundary call compiles in exactly one assembly and is a build error in every other — the same lock the interop rule already uses (native only in `AgentGuard.CrossPlatform.*`). Chosen over a `[BoundaryAdapter]` attribute because an attribute is self-grantable (a lazy pass adds it and launders raw calls); an assembly lock cannot be self-granted.
Tim: *"We probably need to restructure the assemblies, but I lean to this one it is stronger and WE always want to go stronger for ana-rules."* and *"YEP, this is easy and clean..."*

### `abstractions-assembly-holds-all-interfaces`
New assembly `AgentGuard.Abstractions` holds **every** interface — `IGuard`, `IHostAdapter`, `IPipeline`, all existing contracts, and the new boundary interfaces — plus data types (e.g. `DirectoryChild`). No OS calls, no implementations. Everyone references it. (The interfaces do NOT go in `Boundaries`; that would force a reference cycle.)
Tim (to "does this include IGuard/IHostAdapter/IPipeline too?"): *"YES all abtractions go here."*

### `boundaries-assembly`
New assembly `AgentGuard.Boundaries` is the ONLY place raw `System.IO` / `System.Environment` / `System.Random` / `Guid` / `System.Console` calls compile. It holds the adapter classes, the `ISystemServices` container, and the one `SystemServices.Create()` factory. References `Abstractions` (and `CrossPlatform` for `Platform.Create()`).

### `seven-boundary-interfaces`
Six new/extended engine interfaces plus the existing cross-platform one:
`IFileReader` (EXTEND the existing one), `IDirectoryEnumerator`, `IFileWriter`, `IEnvironment`, `IGuidFactory`, `IConsole`; and `IPlatformFileSystem` (OS-divergent, already defined by the cross-platform contract). Code below.

### `iconsole-seam`
Console is a boundary like the rest; it gets `IConsole` (read/write STDIO), and raw `System.Console` is legal only in the `Boundaries` `ConsoleAdapter`. (Supersedes the earlier "Console allowed only in the Cli" idea.)
Tim: *"We should abstract  console also ... IConsole or somethign so that we can read and write STDIO without problem."*

### `iguidfactory-seam`
`Guid.NewGuid()` (the one randomness in the code, `AtomicFile.cs:56`, for temp names) is abstracted behind `IGuidFactory` — like time behind `TimeProvider` — and helps make tests deterministic.
Tim: *"Okay so we need to abstract guids but it will help for testing."*

### `single-construction-point`
One container `ISystemServices` (the seven services), built by ONE factory `SystemServices.Create()`. Nothing else constructs an OS service. It is created once at the top and threaded down; there is exactly one place to mock.
Tim: *"WE need a simple single construction point that returns these OS services so that there is only one place to mock."* and *"I want to see somethign that keeps us from reconstructing this too many times."*

### `two-walls-stop-the-bypass`
The reconstruction/bypass is a build error, not a discipline:
- **Wall 1 (compiler):** the adapters are `internal` in `AgentGuard.Boundaries` with `private` constructors — nothing outside `Boundaries` can name or `new` one, so `FileReader.Create()` deep in the chain does not compile.
- **Wall 2 (analyzer AG0017):** `SystemServices.Create()` is a build error anywhere except the single composition method in `Program` (and the test builder — see `test-system`).
Together, the only way any class gets an OS service is by receiving it through its constructor.
Tim's requirement: *"NONE of this matters if 2 days from now you call a Create method for one of tese abstractions deep in the chain because you are too lazy to pass it through 5 layers and so now we are back to not testable and the same shape as we would be off the raw classes."*
Tim approving the construction design: *"YES this is the only way it can work without a full IOC for now."*

### `constructor-injection-no-container`
DI/IOC pattern = constructor injection wired by hand at the one composition point (the existing house style: `GuardEngine.CreatePipeline` builds the graph). No DI container library. Each class receives the specific interfaces it needs, pulled off the container at composition.
Tim: *"WE must follow a DI/IOC pattern some place."* + the construction-design approval above.

### `test-system` — `AgentGuard.TestHelpers` + `SystemServicesBuilder`
New assembly `AgentGuard.TestHelpers` (referenced only by test projects, never shipped) holds the ONE approved way tests build the container plus reusable fakes/proxies. `SystemServicesBuilder`: `Real()` / `Fake()` starts; `With(...)` to substitute a mock; `Wrap(...)` to wrap the current (real) service in a proxy. AG0017 allows `SystemServices.Create()` in exactly two places — the `Program` composition method and this builder — and **test projects may not call `SystemServices.Create()` directly**; they go through the builder.
Ships: `InMemoryFileSystem` (implements `IFileReader`+`IDirectoryEnumerator`+`IFileWriter` over a dictionary, no disk), `FixedGuidFactory`, `FakeEnvironment`, `RecordingConsole`, proxy bases (`RecordingFileReader`, `ThrowingDirectoryEnumerator`, …). `FakeTimeProvider` (Microsoft.Extensions.Time.Testing) is reused, not rebuilt.
Tim: *"SO we will need an 'Approved Test way to Create this'. THAT needs to be in a NEW assembly called 'TestHelpers' and that needs to allow the caller (a test class) to influencey how the class is constructed by providign substitutions or mocks."* / *"Create one real isntance, and substitute any service you need by either wriping the real one with a Proxy or providing a Mock."* / *"You will probably need to allow `Test` assemblies to call it to."* / *"THE builder approach is exactly right. LEt's lock it in the contract."*

### `tests-not-exempt-from-boundary-rules`
The boundary-call ban covers the test projects too. A test that needs real files on disk sets them up through `SystemServicesBuilder.Real()`'s `IFileWriter`/`IDirectoryEnumerator`, not raw `Directory.CreateDirectory`. `FixtureProject` moves into `TestHelpers` and is rebuilt on the real adapters. (A test-only exemption is the crack that lets the untestable shape back in.) Approved as part of `test-system-design`.

### `rule-set` — AG0011–AG0016, AG0017, AG0101
Approved. Table below. Tim on the boundary rules: *"THE REST however were good and make sense to me."* On the OS-divergent series numbering: *"THis should probably be AG0101 --- SO that more OS-divergent items can be added into the same 'series'."* On the legal edges: *"Okay."*

### `adversary-corrections-folded-in`
From the design + refutation passes, folded into the rule-set (not separate decisions — corrections to get it right):
- Single-argument `Path.GetFullPath(path)` is banned (it is NOT pure — it resolves a relative path against the current directory). Replacement is the pure two-argument `Path.GetFullPath(path, basePath)`. Live at `ContextStorePaths.cs:25`, `InstallIntegrity.cs:78`. (`PatternMatcherFactory.cs:39` uses only the pure `Path.IsPathRooted` — NOT a violation.)
- The ban-list gains `Marshal` and the P/Invoke call-site (AG0008 catches only the `[LibraryImport]` declaration, not the syscall), and the deployment-path reads `Assembly.Location` / `AppContext.BaseDirectory` / `AppDomain.BaseDirectory`.
- Detection must be symbol/semantic-based (Roslyn IOperation), covering PROPERTY reads (`DateTime.UtcNow`, `FileInfo.UnixFileMode`, `Environment.CurrentDirectory`, `Console.Out`) and surviving `using static` / aliases — not a syntax/text match.
- Member-level rules beat type-level: the symlink / Unix-mode members of `Directory` / `FileInfo` route to `IPlatformFileSystem` (AG0101), not the filesystem seams (AG0011).
- Real RED surface ~113 sites / 34 files (supersedes #14's ~13).
- The only compile-time-uncatchable evasion is reflection by string name — documented, not papered over.

### `legal-edges`
Stay legal everywhere: the pure `Path` members (`Combine`, `GetFileName`, `GetDirectoryName`, `IsPathRooted`, `DirectorySeparatorChar`), the hashing/signature crypto (`SHA256`, `Ed25519`), and reading an enum value as data (`UnixFileMode.UserRead`, `FileAttributes.Hidden`).

---

## The assemblies (assembly-lock)

```
AgentGuard.Abstractions   NEW. Every interface + data types (DirectoryChild). No OS calls, no code. Referenced by all.
        ▲ implemented by
AgentGuard.Boundaries     NEW. The adapters + ISystemServices + SystemServices.Create(). THE ONLY assembly where
                          raw System.IO / Environment / Random / Guid / Console calls compile. Refs Abstractions, CrossPlatform.
        ▲ wired at startup by
AgentGuard.Engine         EXISTING, slimmed. Guard logic + Setup + GuardEngine wiring. Refs Abstractions, Boundaries,
                          CrossPlatform. ZERO raw OS calls after cleanup.
        ▲
AgentGuard.Cli            EXISTING. Program.cs — the single composition method that calls SystemServices.Create().
                          Prints via IConsole (no raw Console). Refs Engine.

AgentGuard.CrossPlatform(.MacOS/.Linux/.Windows)   From the cross-platform contract. THE ONLY assembly where the
                          OS-divergent calls compile (SetUnixFileMode, symlink members, Marshal, libc rename). AG0101.

AgentGuard.TestHelpers    NEW. SystemServicesBuilder + fakes + proxies + FixtureProject. Refs Abstractions, Boundaries.
                          Referenced only by test projects.

AgentGuard.Analyzers      EXISTING. The rules; build-time only.
```

---

## The interfaces (as approved)

```csharp
// AgentGuard.Abstractions

public interface IFileReader   // EXTEND the existing one (Exists, ReadAllBytesAsync) with a text read
{
    bool Exists(string path);
    ReadOnlyMemory<byte> ReadAllBytesAsync(string path, CancellationToken ct);
    string ReadAllText(string path);
}

public interface IDirectoryEnumerator
{
    bool DirectoryExists(string path);
    IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath);        // scanner walk
    IReadOnlyList<string> EnumerateFiles(string directoryPath, string pattern);   // *.token, *.csproj, inspector
    IReadOnlyList<string> EnumerateDirectories(string directoryPath);             // context-store sweep
    DateTimeOffset GetLastWriteTimeUtc(string path);                             // sweep mtime
}
public readonly record struct DirectoryChild(string FullPath, bool IsDirectory, bool IsReparsePoint);

public interface IFileWriter
{
    void CreateDirectory(string path);
    Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken ct);
    void WriteAllText(string path, string contents);
    void Copy(string source, string destination);
    void Move(string source, string destination);
    void DeleteFile(string path);
    void DeleteDirectory(string path, bool recursive);
}

public interface IEnvironment
{
    string GetCurrentDirectory();
    string GetHomeDirectory();
    string? GetEnvironmentVariable(string name);
    string? GetProcessPath();
}

public interface IGuidFactory { Guid NewGuid(); }

public interface IConsole
{
    string ReadInput();            // stdin (the hook payload)
    void WriteOutput(string text); // stdout
    void WriteError(string text);  // stderr
}

// The single container. IPlatformFileSystem comes from Platform.Create(), composed in.
public interface ISystemServices
{
    IFileReader          FileReader  { get; }
    IDirectoryEnumerator Directories { get; }
    IFileWriter          FileWriter  { get; }
    IEnvironment         Environment { get; }
    IGuidFactory         Guids       { get; }
    IConsole             Console     { get; }
    IPlatformFileSystem  Platform    { get; }
}
```

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

---

## The rules

| Rule | DON'T call (anywhere but the allowed assembly) | Allowed only in | DO use instead |
|---|---|---|---|
| **AG0011 Filesystem** | `File`, `Directory`, `FileInfo`, `DirectoryInfo`, `FileSystemInfo`, `DriveInfo`, `FileStream`, `StreamReader`, `StreamWriter`, `FileSystemWatcher`, `System.IO.Enumeration.*` | `AgentGuard.Boundaries` | `IFileReader` / `IDirectoryEnumerator` / `IFileWriter` |
| **AG0012 Environment** | `System.Environment` (all), `Directory.GetCurrentDirectory`/`SetCurrentDirectory`, single-arg `Path.GetFullPath`, `Assembly.Location`/`GetEntryAssembly().Location`, `AppContext.BaseDirectory`, `AppDomain.CurrentDomain.BaseDirectory`, `RuntimeInformation` host-description props | `AgentGuard.Boundaries` | `IEnvironment`; two-arg `Path.GetFullPath(path, basePath)` |
| **AG0013 Process** | `System.Diagnostics.Process`, `ProcessStartInfo` | nowhere (no use today) | grow an interface first if ever needed |
| **AG0014 Randomness** | `System.Random`, `Guid.NewGuid()`, `RandomNumberGenerator` | `AgentGuard.Boundaries` | `IGuidFactory` |
| **AG0015 Time** | `DateTime.Now`/`UtcNow`/`Today`, `DateTimeOffset.Now`/`UtcNow`, `Stopwatch`, `Environment.TickCount` | nowhere | injected `TimeProvider` (already used everywhere — this is preventive) |
| **AG0016 Console** | `System.Console` | `AgentGuard.Boundaries` | `IConsole` |
| **AG0017 Construction** | `SystemServices.Create()` | the single `Program` composition method + `SystemServicesBuilder` | receive `ISystemServices` by constructor injection |
| **AG0101 OS-divergent FS** | `File.SetUnixFileMode`/`GetUnixFileMode`, `FileInfo`/`DirectoryInfo.UnixFileMode`, symlink members (`LinkTarget`, `CreateSymbolicLink`, `ResolveLinkTarget`), `Marshal`, the libc `rename`/`MoveFileEx` P/Invoke calls | `AgentGuard.CrossPlatform.*` | `IPlatformFileSystem` |

---

## What the test system can do (usage the design must support)

```csharp
// substitute one, everything else fake — the fail-closed test
var s = SystemServicesBuilder.Fake().With(new ThrowingDirectoryEnumerator()).Build();

// real everything, wrap one to spy — integration test
var s = SystemServicesBuilder.Real().Wrap<IFileReader>(real => new RecordingFileReader(real, log)).Build();

// pin the non-deterministic ones — repeatable
var s = SystemServicesBuilder.Fake().With(new FixedGuidFactory("…0001")).With(new FakeTimeProvider(t0)).Build();
```

---

## Open items carried into the contract (resolved when this contract resumes, NOT now)
- `IPlatformFileSystem` location: cross-platform builds it in `AgentGuard.CrossPlatform` (its locked `namespace-crossplatform` decision — unchanged by doing cross-platform first). Whether `abstractions-assembly-holds-all-interfaces` then pulls it into `AgentGuard.Abstractions` is a decision for THIS contract when it runs; moving it changes a locked decision → needs Tim's explicit yes. NOT decided.
- Issue #14's count is superseded (real surface is 106 sites / 34 files); rewrite it against the settled adapter list when this contract writes its contract.md.
- Sequencing is RULED: cross-platform first (see Sequencing above). This is no longer open.

## Process state
GROUND done (`wf_102e11b0-164`, facts captured above). This contract is **PARKED** behind the cross-platform contract. When cross-platform lands, resume by reading THIS file, then: write the contract.md from the grounded facts + reuse ledger + these decisions → hidden-decision scan over the draft → resolve survivors with Tim → RULE-PHASE (build AG0011–AG0017 + AG0101 RED against the residual sites) → IMPLEMENT → REFUTE → GATE.
