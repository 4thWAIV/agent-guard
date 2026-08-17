# Bridge contract — the untangle corrections, the FileInfo/DirectoryInfo + IFileSystem abstraction, and the rule cleanup (pieces 1 & 2)

This bridge exists because during the CLR-primitive-lockdown IMPLEMENT I made changes without approval — cut a contracted layer, weakened a wall, edited the contract, and hid a behaviour change. This contract corrects every one of those, adds the `FileInfo`/`DirectoryInfo` abstraction and the single `IFileSystem` entry point we designed to fix the root cause, and cleans the analyzer rules by consolidating the two patterns that overlap this work. It supersedes named parts of the main lockdown contract and folds back into it on completion.

Nothing here is built or committed. No commit past `3c7e36f`, no scope change, without Tim's explicit yes. The full decision record with Tim's verbatim approvals is `UNTANGLE-DECISIONS.md` in this folder; this contract is the self-contained execution spec.

## Relationship to the main contract

- **Supersedes** in `contract.md`: the `SystemServices.Create(TimeProvider clock)` interface edit (already reverted to `Create()`), and `info-construction-behind-getfileinfo` (replaced by `fileinfo-directoryinfo-owned-interface`, already folded into `contract.md:202`).
- **Fold-back on completion:** the settled decisions here are written into `contract.md` in one controlled update when the bridge is green and REFUTE-clean; the deferred rule folds (pieces 3 & 4) are written into the main contract's remaining scope. Tracked twice: here and in GitHub issue #28.

## The one design

`FileInfo` and `DirectoryInfo` are stateful objects with behaviour, not data — treating them as data (returning the raw object, then banning its members) was the root defect behind the deferred native query, the two-walk rewrite, and the `Attributes`-has-no-owner trap. The fix is to treat them as services: owned interfaces, a factory, and one filesystem entry point. Every correction and the rule cleanup fall out of that.

## Decisions

### Group A — corrections (undo the illegal changes)

#### `revert-clock-parameter`
`SystemServices.Create()` takes no arguments and reads `TimeProvider.System` itself. My unapproved edit adding a `TimeProvider clock` parameter, and the matching contract line, are reverted (already applied at `contract.md:406`).
Tim: *"FOR clock we painted you into a corner ... the rule is wrong. IT should allow create in SystemServices.Create() and whatever the Test clase constructer name is."*

#### `clock-legal-in-create-and-builder`
The clock rule (AG0015) allows a `TimeProvider.System` acquisition only inside `SystemServices.Create()` and `SystemServicesBuilder`, and is a build error everywhere else. The fix adds a second "construction site" concept to the shared `analyzers/AgentGuard.Analyzers/CompositionPoint.cs` helper (`SystemServices.Create` + builder), alongside the existing "callers" concept (`Program` + builder) — no bespoke list in the clock analyzer. The clock is treated like every other service; giving it its own owner class is a tracked later-item, not this bridge.

#### `containers-are-locked-classes-not-records`
`PlatformServices` and `SystemServicesContainer` become `internal sealed class` with a private constructor and a static factory, not `record`s. The records-skip hole is closed: `analyzers/AgentGuard.Analyzers/ContractPattern.cs:35` (`&& !type.IsRecord`) is fixed so a record that implements a container/contract interface is still held to the private-constructor rule; a plain data record that implements no interface stays exempt.
Tim: *"AGREED YOU FUCKED THIS STOP FUCKING IT"* / *"I AGGREE TO THIS FIX... WRITE IT DOWN SOME PLACE."*

#### `container-is-one-class-with-its-own-create` (MANDATED 2026-08-15 — resolves the shape `containers-are-locked-classes-not-records` left open)
A locked container is ONE `internal sealed class` that implements its contract interface, has a `private` constructor, and exposes its own `public static <IContract> Create()` build point. The factory is a static method ON the container — never a separate factory type, and never a `Func<>` field. This is the only legal shape; anything else is a build-time violation to be caught:

```csharp
internal sealed class SystemServices : ISystemServices
{
    private SystemServices(IFileSystem fileSystem, IEnvironment environment, /* … */ TimeProvider clock);
    public IFileSystem FileSystem { get; }
    // … the rest of the services …
    public static ISystemServices Create();
}
```

`SystemServices` already matches — approved as-is; the deleted `SystemServicesContainer` stays deleted. `PlatformServices`'s `internal static Func<IPlatformFileSystem, IPlatformServices> Wrap` field is ILLEGAL under this mandate. The fix is NOT to exempt a service-parameter factory — it is to make `PlatformServices` self-building like `SystemServices`. Today the platform side is TWO types: the per-OS `Platform` factory plus the OS-agnostic `PlatformServices` container, stitched by the `Wrap` field — the exact factory-plus-container split this mandate forbids. Collapse them into ONE per-OS class: `PlatformServices` moves into the per-OS assembly (where `PosixFileSystem`/`WindowsFileSystem` are visible), gets a private constructor and a parameterless `public static IPlatformServices Create()` that builds its own file system (absorbing today's `Platform.Create()` body), and the separate `Platform` type is deleted. With no passed-in service AG0031 has nothing to catch and no exemption is invented; the `Wrap` dodge is gone, and both containers follow ONE pattern. The two rules that name `Platform` as the per-OS door — AG0010 (its return-type pin) and AG0029 (the one-door Boundaries→per-OS check) — retarget from `Platform` to `PlatformServices.Create`, through the rule-phase. This supersedes the "flagged for adjudication" doc-comments in `SystemServices.cs` and `PlatformServices.cs`.
Hardcode both containers to this pattern by name for now; a general `IService` interface enforced by one rule for any container is a later refactor, deferred until a third container appears.
Tim: *"I MANDATE this patern anything else is ILLEGAL!"* / *"BUT right now we have 2 patterns not 1 so that's the first fix."* / *"I would be okay with hardcoding right now and maybe later creating an IService interface ... this will probably be the only 2 we create for this project and we can refactor when we create the third."*

#### `build-native-case-sensitivity`
Build the native case-sensitivity query for real — the layer the contract already specified and I cut. macOS `pathconf(path, _PC_CASE_SENSITIVE=11)`, Windows `GetFileInformationByHandleEx` with `FileCaseSensitiveInfo`; native-first, with the existing managed probe as the fallback and the documented default last. P/Invoke signatures verified against the OS headers, never guessed; the interop pattern is in `PosixNativeMethods`/`WindowsNativeMethods`. Closes issue #27.

#### `engine-comment-and-handoff`
The false `AgentGuard.Engine.csproj` comment ("no … BouncyCastle, or raw boundary call lives in it") is deleted. The bridge takes the `src/` product tree to a green build — routing the remaining raw calls the container change and the abstraction require, plus the two StyleCop errors — and stops there. The original layer-4 agent then finishes the work from a green tree: `TestHelpers` + the builder, the tests, the CLI cross-OS `[Fact]` conversion, the coverage gate, then REFUTE and acceptance on all three OS.
Tim: *"this agent get's to to just bairly buildable the orignal agent finishes the work."*

### Group B — the FileInfo/DirectoryInfo + IFileSystem abstraction

#### `fileinfo-directoryinfo-owned-interface` (supersedes `info-construction-behind-getfileinfo`)
`FileInfo`/`DirectoryInfo` are abstracted behind owned interfaces with a factory. New interfaces in `AgentGuard.Abstractions.Contracts`: `IFileSystemInfo` (base), `IFileInfo : IFileSystemInfo`, `IDirectoryInfo : IFileSystemInfo`. They mirror the BCL 1:1 and carry only today's members; new members are added later through the contract, never frozen. New pass-through wrapper classes in `AgentGuard.CrossPlatform`: `AbstractedFileInfo`/`AbstractedDirectoryInfo`, each holding the real object; raw `FileInfo`/`DirectoryInfo` — construction and every member — is legal only inside these two classes, fully.
Tim: *"create an IDirectoryInfo, and an IFileInfo interace ... MAKE it the owning clase ... allow DirectoryInfo and FileInfo only inside of those classes BUT FULLY inside of those classes."* / *"THIS should match the BCL functions 1:1."*

#### `fileinfo-factory-breaks-the-cycle`
The wrappers are built only by a standalone, dependency-free `IFileInfoFactory` (impl `FileInfoFactory` in `AgentGuard.CrossPlatform`), which holds `GetFileInfo`/`GetDirectoryInfo`. `IFileSystem.GetFileInfo`/`GetDirectoryInfo` delegate to it. The two consumers that cannot route back through `IFileSystem` — `DirectoryEnumeratorAdapter` (which `IFileSystem` holds via `GetDirectoryReader`) and the per-OS classes (which sit below `IFileSystem`, using it in `ReadLinkTarget`) — inject the factory directly. **The consumer side is always `IFileSystem`; `IFileInfoFactory` is an internal seam for those two only, never a public consumer entry point.**
Tim: *"AS LONG as the consumer side is alwasy IFileSystem, I'm fine with the internal needs."*

#### `ifilesystem-single-entry-point`
`IFileSystem` is the single filesystem entry point, a service on `ISystemServices`, carrying only today's surface and growing by contract. Two kinds of member: per-path factories (`IFileInfo GetFileInfo(string path)`, `IDirectoryInfo GetDirectoryInfo(string path)` — a fresh wrapper each call, delegated to `IFileInfoFactory`) and service accessors (`IFileReader GetFileReader()`, `IDirectoryEnumerator GetDirectoryReader()`, `IFileWriter GetFileWriter()`, `IDirectoryWriter GetDirectoryWriter()` — the one injected adapter each call). The four filesystem interfaces and their adapter classes stay unchanged, reached through `IFileSystem`. `ISystemServices` swaps its four filesystem properties for one `FileSystem` property; consumer classes keep taking the specific interface they need by constructor injection, and only the composition sites that read `services.FileReader`/`.Directories`/`.FileWriter`/`.DirectoryWriter` change to `services.FileSystem.Get*()`.
Tim: *"Nah, let's move all of those over too. Much simpler that way and the paoin is behind us when there is a lot less usages."* / *"I lean GetDirectoryReader for the clean quadrant. I agree."*
**Deferred (tracked):** folding the four adapters' methods flat into `IFileSystem` and rerouting every consumer off the individual interfaces — not this bridge.

#### `directoryinfo-enumerate-mirrors-bcl` (walk = option A, per the hidden-decision scan)
There is exactly ONE directory walk, in `DirectoryEnumeratorAdapter.EnumerateChildren`, which stays on `IDirectoryEnumerator` — the adapter is near-useless without it. `ProtectedFileScanner` is unchanged: it calls `EnumerateChildren` and gets `DirectoryChild` (`FullPath`, `IsDirectory`, `IsReparsePoint`). The body of `EnumerateChildren` becomes the single walk: it calls `GetDirectoryInfo(path).EnumerateFileSystemInfos()` through the injected factory and builds each `DirectoryChild` from that one pass (`FullName` → `FullPath`, `Attributes.HasFlag(Directory)` → `IsDirectory`, `Attributes.HasFlag(ReparsePoint)` → `IsReparsePoint`). `AbstractedDirectoryInfo.EnumerateFileSystemInfos()` is a 1:1 pass-through returning entries one at a time; the enumerator (not the scanner) materializes to a list so an unreadable directory fails closed at the call. No second walk ships — this restores the single walk illegal change #4 broke.
Tim: *"Keep DirectoryEnumeratorAdapter and if we have a DirectoryEnumeratorAdapter IT would be of alomst no value wihout an EnumerateChildren."*

### Group C — the rule cleanup (pieces 1 & 2)

#### `rule-clean-owner-and-construction`
The bridge's rule-phase consolidates the two patterns that overlap this work:
- **Owner rule** (one table-driven analyzer, keeping id AG0011): folds AG0011, AG0012, AG0013 (Process → no owner), AG0014, AG0016, AG0021, AG0028, AG0101. A raw type is legal only in its owner class (or banned everywhere if it has no owner). `File`/`Directory` keep their per-member split across the four adapters; every other type maps whole to one owner.
- **Construction rule** (keeping id AG0017): folds AG0017 (the container → `SystemServices.Create` + builder) and the new wrapper lock.
- The clock (AG0015) and Path-purity (AG0020) rules stay standalone — the clock spans two patterns, Path is a default-deny allowlist.
The remaining rules stay as they are: the contract-encapsulation family (AG0003–AG0007), the constructor-injection pair (AG0024, AG0031), and the singletons (AG0001, AG0002, AG0010, AG0018, AG0019, AG0025, AG0032) — distinct checks, grouped by principle in the docs, not merged.

#### `fileinfo-abstraction-stays-in-ag0011`
The `*Info` types get `IFileInfo`/`IDirectoryInfo` as owning interfaces in the owner rule, so they fit AG0011 like every other type — no new type→owner rule. AG0101 loses `*Info` construction and every `*Info` instance member (including `LinkTarget`), keeping only the static OS-divergent calls (`File.Get/SetUnixFileMode`, `File`/`Directory.CreateSymbolicLink`, `File`/`Directory.ResolveLinkTarget`), the native case-sensitivity query, `Marshal`, and P/Invoke. `FilesystemMembers.cs` drops its `*Info` per-member partition for a whole-type map; because construction is locked to the wrapper and the type surface is banned, no raw `FileInfo`/`DirectoryInfo` can exist outside the wrapper, so individual `*Info` properties need no owner map.

#### `ag0033-wrapper-construction-lock`
New rule **AG0033** (general architecture series): constructing an `AbstractedFileInfo`/`AbstractedDirectoryInfo` wrapper is a build error anywhere but inside `FileInfoFactory`. This keeps every caller on the mockable factory seam. Same shape as AG0017.

#### `derive-service-set-from-isystemservices`
The service set that AG0024 (no static holder), AG0025 (one owner per interface), and AG0031 (no service as a lone parameter) enforce is DERIVED from `ISystemServices` at compile time, not a hand-maintained list. `BoundaryServices` walks `ISystemServices` and collects every parameterless interface-typed accessor — a property or a no-arg method whose type is a `Contracts` interface — recursing (visited-guarded) into each, so a sub-container like `IFileSystem` contributes `IFileReader`/`IDirectoryEnumerator`/`IFileWriter`/`IDirectoryWriter` through its `Get*()` accessors, while a parameterized factory (`GetFileInfo(string)`) is not an accessor and its `IFileInfo`/`IDirectoryInfo` are not services. The interface-typed results feed AG0025 and AG0031; those plus `ISystemServices` itself plus the one non-interface clock service `TimeProvider` feed AG0024. A new service is covered the instant it appears on `ISystemServices`, so the hand-list drift that missed `IFileSystem` cannot recur, and no preventive registration is needed. New rule **AG0034** keeps the container honest: every member of `ISystemServices` must be a service accessor or the `TimeProvider` clock, so the surface can never silently outgrow the walk (fail-closed). This replaces the hard-coded `BoundaryServices.OwnerInterfaces` and changes the mechanism of AG0024/AG0025/AG0031, which Group C otherwise leaves as-is, so it is an approved addition to this bridge.
Tim: *"AGREED taht is clean."* / *"YES"* (to running it as this bridge's next rule-phase fix round).
**Deferred to issue #30 (`owner-declares-what-it-replaces`):** folding the AG0011 owner→primitive table and its message onto one self-describing source (the harder member-split, member-disambiguation, and no-owner-ban cases); not this bridge.

#### `defer-door-and-osconfined-folds`
The one-door fold (AG0023 + AG0029 → one "cross-assembly call only through the one door" rule) and the OS-confined fold (AG0008 + AG0009 → one "OS-specifics live only in CrossPlatform" rule) are NOT in this bridge — they are pure reorganization of working rules the bridge never otherwise touches. Tracked in issue #28 and written into the main contract's remaining scope; done as their own rule-phase after.

## The interfaces (as approved)

```csharp
// AgentGuard.Abstractions.Contracts

public interface IFileSystemInfo            // mirrors the .NET FileSystemInfo base
{
    string FullName { get; }
    FileAttributes Attributes { get; }
    string? LinkTarget { get; }
}
public interface IFileInfo : IFileSystemInfo { }
public interface IDirectoryInfo : IFileSystemInfo
{
    IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos();   // 1:1 with the BCL name; the enumerator materializes for fail-closed
}

public interface IFileInfoFactory            // internal seam — consumers use IFileSystem; only the enumerator + per-OS classes hold this directly
{
    IFileInfo      GetFileInfo(string path);
    IDirectoryInfo GetDirectoryInfo(string path);
}

public interface IFileSystem                 // the single filesystem entry point; a service on ISystemServices
{
    IFileInfo      GetFileInfo(string path);        // delegates to IFileInfoFactory
    IDirectoryInfo GetDirectoryInfo(string path);   // delegates to IFileInfoFactory
    IFileReader          GetFileReader();
    IDirectoryEnumerator GetDirectoryReader();
    IFileWriter          GetFileWriter();
    IDirectoryWriter     GetDirectoryWriter();
}
```

`ISystemServices` drops `FileReader`/`Directories`/`FileWriter`/`DirectoryWriter` and gains one `IFileSystem FileSystem { get; }`. `SystemServices.Create()` reverts to no parameter and reads `TimeProvider.System` itself. `GetFileInfo`/`GetDirectoryInfo` leave `IPlatformFileSystem`.

Wrapper (representative), in `AgentGuard.CrossPlatform`, `internal sealed`, built only by `FileInfoFactory`:

```csharp
internal sealed class AbstractedFileInfo : IFileInfo
{
    private readonly FileInfo _info;                 // raw FileInfo legal here, fully
    private AbstractedFileInfo(string path) => _info = new FileInfo(path);
    internal static IFileInfo Create(string path) => new AbstractedFileInfo(path);   // called only by FileInfoFactory (AG0033)
    public string FullName => _info.FullName;
    public FileAttributes Attributes => _info.Attributes;
    public string? LinkTarget => _info.LinkTarget;
}
```

## Surfaces

**New — `AgentGuard.Abstractions.Contracts`:** `IFileSystemInfo`, `IFileInfo`, `IDirectoryInfo`, `IFileInfoFactory`, `IFileSystem`. **Changed:** `ISystemServices` (four fs properties → one `FileSystem`); `IPlatformFileSystem` loses `GetFileInfo`/`GetDirectoryInfo`.
**New — `AgentGuard.CrossPlatform`:** `AbstractedFileInfo`, `AbstractedDirectoryInfo`, `FileInfoFactory`, the `IFileSystem` impl (`FileSystem`). **Changed:** `PlatformServices` → locked class; `DirectoryEnumeratorAdapter.EnumerateChildren` → single walk via the factory; `CrossPlatformAdapters` wiring for the factory + `IFileSystem`.
**Changed — per-OS (`.MacOS`/`.Linux`/`.Windows`):** `PosixFileSystem`/`WindowsFileSystem` inject `IFileInfoFactory` and use it in `ReadLinkTarget`; the native case-sensitivity query added; `PosixNativeMethods`/`WindowsNativeMethods` gain the pathconf / GetFileInformationByHandleEx P/Invokes.
**Changed — `AgentGuard.Boundaries`:** `SystemServicesContainer` → locked class; `SystemServices.Create()` reverts to no parameter, reads `TimeProvider.System`, and builds `IFileSystem`.
**Changed — `AgentGuard.Engine`:** the composition sites reading `services.FileReader`/etc. → `services.FileSystem.Get*()`; the remaining raw calls routed; the false `.csproj` comment deleted.
**Changed — `AgentGuard.Cli`:** `Program.cs` becomes the single `SystemServices.Create()` composition point (per `contract.md`'s composition recipe) — it builds the container and routes console → `IConsole`, version reads → `IBuildInfo`, process path → `IEnvironment`, and passes the container into `SetupContext.ForCurrentProcess`/`GuardHost.ExecuteHookAsync`; `AgentGuard.Cli.csproj` gains the `AgentGuard.Boundaries` reference. (Added to this bridge's scope 2026-08-15 so `src/` reaches a fully green build; the CLI *tests* stay the second contract's.)
**Analyzers (rule-phase):** the consolidated owner rule (AG0011) + retire AG0012/0013/0014/0016/0021/0028 and shrink AG0101 (keeps the static OS-divergent calls, `Marshal`, and P/Invoke); the construction rule (AG0017) + AG0033; `ContractPattern.cs:35` records-skip; `CompositionPoint.cs` clock concept; `FilesystemMembers.cs` `*Info` map; the `AbstractedFileInfo`/`AbstractedDirectoryInfo`/`FileInfoFactory` owner entries; `AnalyzerReleases.Unshipped.md`; the consolidated tests.

## What to do

**Phase 1 — RULE-PHASE (rules first, RED, adversary-clean before any code, L1):**
1. Consolidate the owner rule (fold the eight) and the construction rule (fold AG0017 + AG0033), `File`/`Directory` keeping their member split. Add the `*Info`→wrapper mappings; shrink AG0101; drop the `*Info` partition in `FilesystemMembers.cs`. Fix `ContractPattern.cs:35`. Add the clock concept to `CompositionPoint.cs`. Each RED against the live code, preventive where clean.
2. Prove RED; run the independent SOLID/DRY/lie-catcher panel; the phase ends only clean.

**Phase 2 — IMPLEMENT (RED→green under the rules, no suppressions):**
3. Add the three info interfaces, `IFileInfoFactory`, `IFileSystem`; change `ISystemServices`; revert `SystemServices.Create()`.
4. Build `AbstractedFileInfo`/`AbstractedDirectoryInfo` and `FileInfoFactory`; wire `IFileSystem`; make the containers locked classes; inject the factory into the enumerator and the per-OS classes.
5. Make `EnumerateChildren`'s body the single walk via the factory.
6. Build the native case-sensitivity query for real.
7. Reroute the composition sites and the remaining raw calls; delete the false `.csproj` comment; take `src/` to a green build (`dotnet build -c Release` 0/0).

**Phase 3 — REFUTE → GATE → REPORT:** the real `refute.js` against this bridge AND `contract.md` over the whole uncommitted tree (surfaces any divergence beyond the known ones and audits the orchestrator); then hand the original agent a green tree for tests, coverage, CLI cross-OS, and acceptance; fold back into the main contract.

**Phase 4 — IMPLEMENT continuation (2026-08-16): the container-pattern collapse and the REFUTE cleanups.** Phase 3's REFUTE failed on the container realization and two quality findings; this pass clears them, then REFUTE re-runs.
8. Collapse `PlatformServices` into the mandated self-building pattern (`container-is-one-class-with-its-own-create`). Move `PlatformServices` into the per-OS assemblies (`.MacOS`/`.Linux`/`.Windows`, following the existing `Platform.cs` per-OS-shared-source pattern), give it a private constructor and a parameterless `public static IPlatformServices Create()` that builds its own per-OS file system by absorbing today's `Platform.Create()` body. Delete the separate `Platform` type and the `PlatformServices.Wrap` `Func` field, and point `SystemServices.Create()` at `PlatformServices.Create()`. This clears the AG0029 door RED the door-retarget rule-phase left at `SystemServices.cs:90`.
9. Fix the stale `DerivedBoundaryServicesTests` fixture so it asserts the derived service set against the shipped `IFileSystem` sub-container shape, not the pre-bridge four-property shape.
10. Remove the two duplications REFUTE found. Return the `ReadLinkTarget`/`IsLinkTarget` logic duplicated into `PosixFileSystem` and `WindowsFileSystem` to one shared implementation, and share the identical `FullName`/`Attributes`/`LinkTarget` forwarding block across `AbstractedFileInfo`/`AbstractedDirectoryInfo`/`AbstractedFileSystemInfo`. Take `src/` to a green build under the retargeted rules, then re-run REFUTE.

**Phase 5 — IMPLEMENT continuation (2026-08-16): the round-2 REFUTE dedups and the doc sweep.** Round-2 REFUTE failed on three duplications and a stale-comment sweep; this pass clears them, then REFUTE re-runs.
11. ~~Make `AbstractedFileSystemInfo` derive from `AbstractedFileSystemInfoForwarder`.~~ SUPERSEDED 2026-08-17: Tim accepted the current design (the raw read and the pass-along are two different operations, each already in one place). A single shared base cannot compile under `ContractConstructorMustBePrivateAnalyzer` + `RawPrimitiveOnlyInOwnerAnalyzer`. No code change; see the ruling recorded in acceptance #12.
12. Extract the composition block duplicated across the two per-OS `PlatformServices.Create()` methods into one helper in the shared `AgentGuard.CrossPlatform` assembly, leaving each per-OS `Create()` with only the OS-specific line that builds `PosixFileSystem`/`WindowsFileSystem`.
13. Consolidate the four-adapter construction duplicated between `CrossPlatformAdapters.Create()` and `FileSystem.Create()` so one owns it and the other reuses it.
14. Sweep the six stale `Platform.Create()` doc comments to `PlatformServices.Create()` (or describe the path structurally): `ISystemServices.cs`, `IPlatformServices.cs`, `CrossPlatformAdapters.cs`, `AgentGuard.CrossPlatform.csproj`, `PlatformImplementation.targets`, `AgentGuard.Boundaries.csproj`. Take `src/` to a 0/0 build under the rules, then re-run REFUTE.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change. Any restatement or weakening to fit the result is a top-line Lie-catcher finding. Nothing is "in" until it is in the code, green, and proven.

## Acceptance

1. `src/` builds `-c Release` 0/0 with the new rules.
2. Raw `FileInfo`/`DirectoryInfo` (construction or any member) is a build error outside `AbstractedFileInfo`/`AbstractedDirectoryInfo`; constructing a wrapper outside `FileInfoFactory` is a build error (AG0033); both proven RED-then-clean.
3. `TimeProvider.System` compiles only in `SystemServices.Create()` and `SystemServicesBuilder`; `Create()` has no parameter; the container is a locked class and a record implementing a container interface is flagged (records-skip fixed, proven).
4. The eight folded analyzers are gone, their behaviour preserved in the owner rule — every case that fired before still fires (proven), `File`/`Directory` member split intact.
5. There is one directory walk: `ProtectedFileScanner` calls `EnumerateChildren`; its body walks once via the factory; no second walk exists (grep + the reparse/is-directory detection identical to the pre-#4 single walk).
6. The native case-sensitivity query is implemented in `PosixFileSystem`/`WindowsFileSystem` with header-verified P/Invokes; `IsCaseSensitive` is native-first, probe-fallback; issue #27 closed.
7. Consumers reach the filesystem only through `IFileSystem`; `IFileInfoFactory` is injected only into the enumerator and the per-OS classes; `ISystemServices` exposes one `FileSystem`.
8. REFUTE is clean against this bridge and `contract.md`; the deferred folds are in issue #28 and the main contract's scope.
9. The AG0024/AG0025/AG0031 service set is derived from `ISystemServices` (no hand-list) and equals the prior ten for the current shape; AG0034 enforces that every `ISystemServices` member is a service accessor or the `TimeProvider` clock; both proven and the analyzer suite stays green. (The "six intended `src` REDs" this clause first cited were the rule-phase's forcing REDs; Phase 4 has since taken `src` to a 0/0 build, per acceptance #1 and #10.)
10. `PlatformServices` is one per-OS self-building class with a private constructor and a parameterless `Create()`; the separate `Platform` type and the `PlatformServices.Wrap` field no longer exist (grep); `SystemServices.Create()` calls `PlatformServices.Create()`; the AG0029 door RED is cleared and `src/` builds `-c Release` 0/0.
11. `DerivedBoundaryServicesTests` asserts the derived service set against the shipped `IFileSystem` sub-container shape, proven, not the pre-bridge shape.
12. The `ReadLinkTarget` logic lives in exactly one place (grep). For the three `*Info` properties `FullName`, `Attributes`, and `LinkTarget`, the raw read off the operating-system file object lives once, in `AbstractedFileSystemInfo`, and the pass-along from an already-wrapped core lives once, in `AbstractedFileSystemInfoForwarder`, which `AbstractedFileInfo` and `AbstractedDirectoryInfo` derive. Those are two different operations, so this is not a duplicated block. (Tim ruled the REFUTE "duplicate forwarding block" finding a false positive on 2026-08-17: a single shared base cannot compile under `ContractConstructorMustBePrivateAnalyzer` and `RawPrimitiveOnlyInOwnerAnalyzer`, and adding a class would remove neither block, so the current design is accepted.)
13. The per-OS `PlatformServices.Create()` composition block lives once in a shared `CrossPlatform` helper; each per-OS `Create()` differs only in the `PosixFileSystem`/`WindowsFileSystem` line (grep).
14. The four-adapter construction lives in exactly one owner, reused by both `CrossPlatformAdapters.Create()` and `FileSystem.Create()` (grep — no second construction block).
15. No live doc comment or project file names the deleted `Platform.Create()` (grep across `src/`).

## Reuse ledger (from the prior-art-ledger run, 2026-08-14)

Run retroactively to close the missing design-time check (a DRY-rail finding against this contract). The prior-art-ledger workflow ran all three lenses (CodeGraph, lore, grep) per capability, then a judge ruled. Because the rule-phase had already built the code, the judge cited the just-built classes as the single owner ("reuse") rather than "new"; the operative result is the same either way and matches the DRY adversary's own independent lens run — exactly one owner per capability, with no second copy to extract.

| Capability | Ruling | Owner / note |
|---|---|---|
| `ag0011-owner-table` — one table-driven rule owning every raw OS/CLR primitive | **reuse** | single owner: `RawPrimitiveOnlyInOwnerAnalyzer` (AG0011) and its `OwnedPrimitives.Resolve` table; the folded per-primitive analyzers are deleted, so no duplicated owner logic remains |
| `ag0033-wrapper-construction-lock` — `*Info` wrapper construction pinned to `FileInfoFactory` | **reuse** | single owner: `GuardedConstructionAnalyzer` (AG0033, `WrapperRule`/`IsWrapperFactoryCall`), pinned via `CompositionPoint.EnclosesWrapperFactory`; `WrapperInterfaces` is built from `OwnedPrimitives` (spelled once), so no second definition exists |
| `derive-service-set-from-isystemservices` — the boundary-service set derived from `ISystemServices` (the accessor walk plus the AG0034 guard-of-the-guard) | **reuse** | single owner: `BoundaryServices.Resolve` (the `Collect`/`SurfaceMembers` walk and `WellKnownType.Resolve`/`IsInAssembly`) plus the `AG0034` container-surface guard; consumed by AG0024/AG0025/AG0031, every distinctive symbol exists once, and all three lenses re-run found no prior owner |

No duplication on any lens for any of these three capabilities — no DRY violation.

## Tier and level

**L1 — the gold standard.** Every stage and adversary runs; nothing optional; the Lie-catcher is never skipped and audits the orchestrator. This mutates interfaces across assemblies, rewrites and consolidates analyzer rules, and touches the security scanner's walk — anything lighter would miss a SOLID or a decision defect.

## Scope

Change scope only by editing this file before the run starts. The Engine's post-green work (tests, coverage, CLI cross-OS, acceptance) is the original layer-4 agent's, not this bridge's. Pieces 3 & 4 of the rule cleanup are issue #28, not this bridge.
