# CLR-primitive lockdown + coverage gate — one contract

This one contract merges two. The **CLR-primitive lockdown** puts every OS/CLR primitive behind an interface we own and makes a raw call a build error. The **coverage gate** collects coverage by default and fails the build under 75%. Tim ruled they merge — *"YOU must now combine these into one contract as the only way foward is to merge them"*.

It supersedes the two source contracts:
- `../2026-08-09-clr-primitive-lockdown/contract.md` (+ its `DECISIONS.md`)
- `../2026-08-11-coverage-gate/contract.md`

Anything that is a correction rather than a Tim decision is labelled **Correction** and attributed to the pass that found it, never to Tim.

## Status and the bridge (2026-08-19)

The RULE-PHASE and the source-tree IMPLEMENT of this contract shipped through the **bridge contract** (`bridge-contract.md` in this folder), committed as `8367242` on branch `clr-primitive-lockdown`. `src/` builds `-c Release` 0/0 under the rules and the analyzer suite is green; the test tree is still red and is the remaining work below. The bridge is the record for the shipped work, and where it changed a decision in this file, the bridge's version supersedes it. The supersessions:

- **Container shape** — `container-is-one-class-with-its-own-create` (bridge): `SystemServices` and each per-OS `PlatformServices` are ONE `internal sealed` class with a private constructor and its own static `Create()`. This refines `single-construction-point` and `two-walls-stop-the-bypass` below.
- **Filesystem abstraction** — the filesystem is reached through one `IFileSystem` on `ISystemServices` (bridge `ifilesystem-single-entry-point`), and `FileInfo`/`DirectoryInfo` are owned behind `IFileInfo`/`IDirectoryInfo` wrappers built only by `FileInfoFactory` (bridge `fileinfo-directoryinfo-owned-interface`). This extends `eight-boundary-interfaces` and `abstractions-assembly-holds-all-interfaces` below.
- **Analyzer set** — the per-primitive rules fold into one owner rule keeping id AG0011; AG0017 + AG0033 are the construction rules; AG0034 guards the container surface; AG0024/AG0025/AG0031 derive their service set from `ISystemServices`; the per-OS door is retargeted to `PlatformServices.Create` (AG0010/AG0029). See the bridge's Group C and `derive-service-set-from-isystemservices`.
- **Interface homes** — `IPlatformServices` and `IPlatformFileSystem` live in `AgentGuard.Abstractions.Contracts`.

**What remains of this contract (the next L1 run):** the test system — `AgentGuard.TestHelpers` with `SystemServicesBuilder` and the fakes, plus AG0018 and AG0019 — every test updated to the new interfaces and namespaces, `PosixOnlyFactAttribute` deleted so the CLI tests run on every OS, and the coverage gate wired to 75%.

## The one design (why these are one contract)

There is a single root defect: code reaches OS/CLR primitives **raw** instead of through an owned, injected interface. Fixing it at the root is what lets both gates pass on the one branch.

## Execution — rules FIRST, then the fix (Rule-Driven Development)

Tim's order, verbatim: *"THE RULES must be updated FIRST BEFORE THE FIX NOT AFTER!"*

1. **RULE-PHASE** — an adversary-backed analyzer workflow corrects and completes AG0011–AG0032 and AG0101 in `analyzers/**`. Each goes **RED** where it hits a live raw call and **preventive** where the codebase is already clean. This phase carries rule-generation authority and edits `analyzers/**` only. It bakes in `identity-match-is-namespace-plus-name` (LESSON 1), `ban-by-whole-type-surface` (LESSON 2), the `one-owner-class-per-primitive` conjunction, and the `namespace-match-fix` correction. The phase ends only when the independent SOLID/DRY/Lie-catcher panel is clean.
2. **IMPLEMENT** — a fresh agent takes this contract, is told the rules, builds the three assemblies and the container, and **cleans up every raw boundary call** — engine, CLI, and tests — until the build is green under the rules. It has no authority to weaken, suppress, or exempt a rule.
3. **COVERAGE** — with the CLI now abstracted, re-author the CLI success/early-exit tests cross-OS as `[Fact]`, delete `PosixOnlyFactAttribute`, wire the coverage collection and the CI gate, and pin the report tool, until every in-scope assembly and each per-OS implementation is at or above 75% and both gates pass.
4. **REFUTE → GATE → REPORT** — the strict Lie-catcher (no suppression of any AG rule, no decision reworded from Tim's words, the orchestrator audited too), then the acceptance proof on all three OS.

## Decisions

### Group A — the merge (this session, 2026-08-11)

#### `merged-one-contract`
The lockdown and the coverage gate are one contract. This supersedes the earlier `separate-new-contract` decision.
Tim: *"YOU must now combine these into one contract as the only way foward is to merge them because you have SHIT THE bed too many times."*

#### `lockdown-is-the-root-coverage-is-downstream`
The coverage gate is downstream of the lockdown: it goes green as a consequence of the lockdown IMPLEMENT, not as separate work.
Tim: *"why am I still yelling at you because you can't bring these two together so BOTH FUCKING PASS THE CI"* and *"if you DO IT ALL THE RIGHT way NO PROBLEMS REMAIN which was MY FUCKING design."*

#### `cli-home-resolution-is-illegal-and-routes-through-ienvironment`
The CLI/engine home resolution `Environment.GetFolderPath(SpecialFolder.UserProfile)` (`GuardEngine.cs:81`, `SetupContext.cs:58`) and `Environment.GetEnvironmentVariable("SHELL")` (`SetupContext.cs:59`) are illegal raw environment reads (AG0012) and route through `IEnvironment`.
Tim: *"WHICH I RULED ILLEGAL and created AN ENTIRE RULE AROUND THAT you are SUPPOSED TO BE ENFORCHING IN THE SYSTEM!"*

#### `posixonly-hack-dies-with-the-root-fix`
Delete `PosixOnlyFactAttribute`. Once the CLI resolves home through `IEnvironment`, the five CLI tests run on every OS as `[Fact]`.
Tim: *"I WANT IT DEAD RIGHT FUCKING NOW SO YOU STOP FUCKING ME ON THIS!"* (The test project itself is NOT rejected — only the skip attribute. Tim: *"I NEVER rejected a 'TEST PROJECT'."*)

### Group B — the lockdown framework (from the lockdown contract, Tim's words attached)

#### `assembly-lock-mechanism`
Enforcement is structural, not by a marker attribute: a raw boundary call is a build error except in the one place it is allowed. Tightened by `one-owner-class-per-primitive`: the exemption is the single owner class, not the whole assembly.
Tim: *"We probably need to restructure the assemblies, but I lean to this one it is stronger and WE always want to go stronger for ana-rules."* and *"YEP, this is easy and clean..."*

#### `one-owner-class-per-primitive`
Every one of these BCL calls is callable in exactly one class — the class that implements its abstraction — and nowhere else, ever. Any other method that needs the capability receives `ISystemServices` through the DI system at construction and calls the method through the service off it. Each analyzer rule exempts only that single owning class, never the assembly: a raw call is a build error even in another class of the same assembly.
Tim: *"EVERY ONE OF THOSE CAN ONLY BE CALLED EVER IN THE ONE FUCKING CLASS ALLOWED TO CALL IT PERIOD END OF FUCKING SENTENCE NO FUCKING EXCEPTIOSN NO FUCKNG WORK AROUDNS NO FUCKING OPTIONS FOR YOU TO FIGURE OUT A PLACE AND WAY TO CHEAT!"* / *"THE ONE CLASS responsible for that abstraction IS THE ONLY place that method it is designed to abstract is allowed to be called. PERIOD NO MORE FUCKING AROUND ON THIS!"* / *"IT ALSO has to recieve the pointer to that method through the DI SYSTEM as part of it's bootstrap and construction and it uses that method from the ENVIORNMENT (or whatever the CrossPLatform equivilant super class is)."*

#### `abstractions-assembly-holds-all-interfaces`
A new assembly `AgentGuard.Abstractions` holds **every** interface — `IGuard`, `IHostAdapter`, `IPipeline`, all the existing contracts, and the new boundary interfaces — plus the data types they use (for example `DirectoryChild`). No OS calls and no implementations live in it. Every assembly references it.
Tim: *"YES all abtractions go here."*

#### `boundaries-assembly`
A new assembly `AgentGuard.Boundaries` holds the OS-uniform adapter classes, the `ISystemServices` container, and the one `SystemServices.Create()` factory. It references `Abstractions` and `CrossPlatform`. Raw `System.IO`, `System.Environment`, `System.Random`, and `System.Console` calls compile only inside the single owner class for each (`one-owner-class-per-primitive`); the raw `Guid.NewGuid()` owner (`GuidFactory`) lives in `AgentGuard.CrossPlatform`, not here (`guid-seam-lives-in-crossplatform`).

#### `eight-boundary-interfaces`
The engine interfaces plus the existing cross-platform one: `IFileReader` (extend), `IDirectoryEnumerator` (extend), `IFileWriter`, `IDirectoryWriter` (split off `IFileWriter`, decision `complete-the-set-not-a-test-fake`), `IEnvironment`, `IGuidFactory`, `IConsole`; and `IPlatformFileSystem` (OS-divergent, from the cross-platform contract). Exact definitions in "The interfaces" below, approved as that code.

#### `iconsole-mirrors-console`
`IConsole` follows the shape of the type it wraps — `Write`, `WriteLine`, `ErrorWrite`, `ErrorWriteLine`, and `Task<string> ReadToEndAsync(CancellationToken ct)`. Raw `System.Console` is legal only in the `Boundaries` console adapter.
Tim: *"we should when posible follow the patter of what we are wrapping"* and *"why is this not WriteLine Write"*.

#### `iguidfactory-seam`
The one randomness call, `Guid.NewGuid()`, is abstracted behind `IGuidFactory`.
Tim: *"Okay so we need to abstract guids but it will help for testing."*

#### `guid-seam-lives-in-crossplatform`
The `GuidFactory` adapter is placed in `AgentGuard.CrossPlatform`, not `Boundaries`. `IGuidFactory` is defined in `AgentGuard.Abstractions`; `GuidFactory` implements it inside CrossPlatform; `PlatformFileSystemShared` receives an `IGuidFactory` by injection and calls `NewGuid()` on it. The container exposes that same one owner as `ISystemServices.Guids` for everyone else. AG0014 exempts exactly that `GuidFactory` class.
Tim: *"if it's needed in CrossPlatform IT MUST fucking live in cross platform ... THERE Is no other way!"* / *"I SAID NO TO ANY BYPASS OF A RULE that GUID could be called directly and said to MOVE THE FUCKING SERVICE into CrossPlatform."* / *"SO YOUR PLAN IS TO pass services UP the dependency chain from chiled Boundaries TO CrossPlatform ... IS THAT YOUR FUCKING PLAN!"*

#### `owners-live-at-lowest-consumer`
There is no raw OS or CLR primitive call anywhere, in any class — every one goes through its owning interface. Each primitive's one owner class lives in the lowest assembly that consumes it (dependencies point one way). `FileReader`, `SystemDirectoryEnumerator`, `FileWriter`, and `DirectoryWriter` live in `AgentGuard.CrossPlatform`, injected into the platform code; `EnvironmentAdapter` (`IEnvironment`) and `ConsoleAdapter` (`IConsole`) stay in `AgentGuard.Boundaries`. The analyzer exemption for each rule is the conjunction "implements the owning interface AND compiles into that owner's assembly": AG0011 and AG0014 gate on `AgentGuard.CrossPlatform`, AG0012 and AG0016 on `AgentGuard.Boundaries`, AG0101 on the one per-OS implementation class (`PosixFileSystem`/`WindowsFileSystem`), not the whole `CrossPlatform.*` library (`ag0101-one-owner-per-os`).
Tim: *"HOW THE FUCK do I GET YOU TO FUCKNG UNDERSTAND THERE IS NO FUCKING ALLOW RAW ALLOWED FUCKING ANYWHERE"* / *"all calls must go through a fucking abstraction! THAT APPLIES TO ALL OF THEM"*

#### `single-construction-point`
One container `ISystemServices` holds the services, built by one factory `SystemServices.Create()`. Nothing else constructs an OS service. It is created once at the top and threaded down.
Tim: *"WE need a simple single construction point that returns these OS services so that there is only one place to mock."* and *"I want to see somethign that keeps us from reconstructing this too many times."*

#### `two-walls-stop-the-bypass`
Reconstructing or bypassing the container is a build error:
- **Wall 1 (compiler):** the adapters are `internal` with `private` constructors.
- **Wall 2 (analyzer AG0017):** `SystemServices.Create()` is a build error anywhere except the single composition method in `Program` and the test builder.
Tim's requirement: *"NONE of this matters if 2 days from now you call a Create method for one of tese abstractions deep in the chain because you are too lazy to pass it through 5 layers and so now we are back to not testable and the same shape as we would be off the raw classes."*
Tim approving: *"YES this is the only way it can work without a full IOC for now."*

#### `constructor-injection-no-container`
Constructor injection wired by hand at the one composition point. No DI container library. Each class receives the specific interfaces it needs, pulled off the container at composition. No class takes a lone service as a method argument, and no class is a `static` helper reaching around the container.
Tim: *"WE must follow a DI/IOC pattern some place."*

#### `static-helpers-become-instances`
`ContextStorePaths` and `InstallIntegrity` become instances built at the one composition point, and their constructors receive `IEnvironment` unpacked from the one `ISystemServices`. `PathCanonicalizer` is already an instance and receives it the same way.
Tim: *"I THOUGHT we only passed ONE servers everywhere HOW DO THESE methods have any SERVICE if they don't have the ONLY ONE SERVICE everythign hangs off of?"*

#### `test-system` — `AgentGuard.TestHelpers` and `SystemServicesBuilder`
A new assembly `AgentGuard.TestHelpers`, referenced only by test projects and never shipped, holds the one approved way tests build the container plus reusable fakes and proxies. `SystemServicesBuilder` starts from `Real()` or `Fake()`, uses `With(...)` to substitute a mock, and `Wrap(...)` to wrap the current real service in a proxy. AG0017 allows `SystemServices.Create()` in exactly two places — the `Program` composition method and this builder — and test projects go through the builder. It ships `InMemoryFileSystem`, `FixedGuidFactory`, `FakeEnvironment`, `RecordingConsole`, and proxy bases; `FakeTimeProvider` is reused.

**One test helper only, all abstractions (Tim ruled 2026-08-11).** `SystemServicesBuilder` is the single test helper: the only class anywhere with `With`/`Wrap`, and — besides the one `Program` composition method — the only place allowed to call the underlying `SystemServices.Create()`. It carries a `With(...)` and a `Wrap(...)` for EVERY service on `ISystemServices` (`IFileReader`, `IDirectoryEnumerator`, `IFileWriter`, `IDirectoryWriter`, `IEnvironment`, `IGuidFactory`, `IConsole`, `IPlatformServices`) and for `TimeProvider` — all of them, not only the ones a current test needs. Tests reach the platform, real or substituted, only through the builder: `Real().Build().Platform` is the real per-OS `IPlatformServices`, and `With(IPlatformServices)` substitutes a fake (whose `.FileSystem` is a fake file system).
Tim: *"THERE MUST BE ONLY ONE TEST HELPER AND IT"S THE ONLY WONE with With or Wrap and IT is the only place allowed to call the underlying Create"* and *"THIS NEED STO SUPPORT ALL abstractions on the class ALL MEANS ALL MEANS EVERY ONE NOT just the ones that are blocking you."*
Tim: *"SO we will need an 'Approved Test way to Create this'. THAT needs to be in a NEW assembly called 'TestHelpers' and that needs to allow the caller (a test class) to influencey how the class is constructed by providign substitutions or mocks."* / *"Create one real isntance, and substitute any service you need by either wriping the real one with a Proxy or providing a Mock."* / *"You will probably need to allow `Test` assemblies to call it to."* / *"THE builder approach is exactly right. LEt's lock it in the contract."*

#### `builder-completeness-ag0019` (Tim ruled 2026-08-11)
A new analyzer **AG0019** keeps the test helper complete as the container grows: every service property on `ISystemServices` must have a matching `With(...)` on `SystemServicesBuilder` — and, by the all-abstractions ruling, a matching `Wrap(...)`. Add a property to the container without its builder `With`/`Wrap` and AG0019 fires — a build error until the overload is added. The builder can never silently fall behind the container. Identity is matched by full name (LESSON 1); `TimeProvider` is out of scope because it is not an `ISystemServices` property.
Tim: *"THAT means we need a new ana rule."* and *"A property can not be added to the Service super class without a With being created for the test class.  OR an ANA fires and we have to fix it."*

#### `testhelpers-locked-by-ag0018`
A new analyzer AG0018 makes it a build error to reference any type from `AgentGuard.TestHelpers` from an assembly whose name is not a test assembly.
Tim: *"ARE you talking an ANA rule if so FUCKING SAY THAT."*

#### `tests-not-exempt-from-boundary-rules`
The boundary-call ban covers the test projects too. A test that needs real files sets them up through `SystemServicesBuilder.Real()`'s `IFileWriter`/`IDirectoryWriter`, not raw `Directory.CreateDirectory`. `FixtureProject` moves into `TestHelpers` on the real adapters.

#### `complete-the-set-not-a-test-fake`
When a test cannot run end to end because a production interface is missing a method, the interface is incomplete and must gain the method — a test-only fake is not the answer. Directory-mutating operations split from `IFileWriter` into `IDirectoryWriter` (`CreateDirectory`, `DeleteDirectory`, `SetLastWriteTimeUtc`); `IFileWriter` keeps file writes only.
Tim: *"GENERALLY if we can't test somethign else because we are missing a method to run end to end test, we have not 'completed the set' for the production interface."* and *"FIND the IDirectoryWriter interface OR REPORT TO ME THAT WE ARE MISSING SOMETHIGN."*

#### `overwrite-parameter-on-copy-move`
`IFileWriter.Copy` and `IFileWriter.Move` each take `bool overwrite = false`. False (default) throws when a file is already at the destination; true replaces it. `AtomicFile`'s commit calls both with `overwrite: true`.
Tim: *"Add a parameter if suplied as true it overwrites if supplied as false (default) it throws."*

#### `platform-interfaces-to-abstractions`
Both platform interfaces — `IPlatformFileSystem` and its container `IPlatformServices` — move from `AgentGuard.CrossPlatform` into `AgentGuard.Abstractions`. The implementations (`PosixFileSystem`, `WindowsFileSystem`, the concrete `PlatformServices`) and the per-OS `Platform.Create()` factories stay in the `CrossPlatform.*` assemblies. The `AgentGuard.CrossPlatform` namespace keeps its name; only the two interface definitions relocate.
Tim: *"I agree we should record this and I now approve the move of IPlatformServices to the Abstractions assembly."*

#### `platform-create-internal` (Tim ruled 2026-08-10; tightened 2026-08-11)
`Platform.Create()` becomes `internal` in each per-OS assembly, with `InternalsVisibleTo` for `AgentGuard.Boundaries` only — the one production caller, inside `SystemServices.Create()`. No test project gets a direct-create grant. Tests obtain the real platform through `SystemServicesBuilder.Real().Build().Platform` — an `IPlatformServices` whose `.FileSystem` is the real per-OS `IPlatformFileSystem` — and `PlatformFileSystemSpecTests` drops its direct `Platform.Create()` call. `Platform.Create()` then has exactly one consumer, and every test reaches the platform through the one builder.

**Tightening (2026-08-11):** the 2026-08-10 ruling also granted `InternalsVisibleTo` to `AgentGuard.CrossPlatform.Tests` so the spec test could direct-create. That grant is dropped, because `tests-not-exempt-from-boundary-rules` already forces the spec test off its raw scaffolding and through `SystemServicesBuilder.Real()`, whose `.Platform` is the real per-OS `IPlatformServices`.
Tim (2026-08-11): *"I THINK we limit to only the production crate and the test helper class that can create but also allow you to replace via the builder patern."* This supersedes his 2026-08-10 wording: *"we would need to make it visiable to it's test also (which should be allowed to direct create)."*

#### `platform-services-is-the-return-and-growth-point`
`Platform.Create()` returns the `IPlatformServices` container, never a bare service. `IPlatformServices` is the one platform capability surface: it holds `IPlatformFileSystem` today and grows to more — a biometric or keychain surface, and whatever else — as additional properties, so a new platform service is added in one place with no change to the factory or its callers. `SystemServices.Create()` in `Boundaries` calls `Platform.Create()`, takes the `IPlatformServices` it returns, and exposes it as `ISystemServices.Platform`; code reaches the OS-divergent file system through `ISystemServices.Platform.FileSystem`. `IPlatformFileSystem` stays the owner of the OS-divergent calls (symlinks, the executable bit, case sensitivity — AG0101); the OS-uniform services (file, directory, environment, console, GUID) are identical on every OS (AG0011–AG0016). AG0010 pins the return type: `Platform.Create()` must return the `IPlatformServices` container, not a bare `IPlatformFileSystem`.
Tim (choosing Option A, correcting an earlier unapproved subsumption): *"WHICH WAS DONE TO allow More platform servcies to be added and still only have one entrpy point for the Brondry assembly to load and fold into it's servcie ... even though it may be the only service on IPlatformServices that's okay because surely others will be added later like BIOmetric interactions."*

#### `case-sensitivity-detected-per-filesystem`
One owner detects case sensitivity: `bool IsCaseSensitive(string path)` grows `IPlatformFileSystem` (AG0101), and the `EnumerateFiles` adapter reads it to set `EnumerationOptions.MatchCasing`. Detect against the exact directory being searched, in three layers reusing one probe: (1) a native per-OS query first — macOS `pathconf(path, _PC_CASE_SENSITIVE)`, Windows `GetFileInformationByHandleEx` with `FileCaseSensitiveInfo`; (2) a shared read-only probe as the fallback when the native query is unavailable or unclear (Windows without the flag, macOS on error, Linux always) — flip the case of an existing letter-bearing entry in the searched directory and stat it; one implementation in `PlatformFileSystemShared`; (3) a documented case-sensitive default only when even the probe cannot tell.
Tim: *"it is not ALWAYS case incensitive on Windows nor ALWAYS sensitive on Mac ... WE must detect if we are NEEDED to be sensitive or insensitive based on how the FS is setup and THAT MUST be designed with SOLID/DRY principles."* and *"should we not apply the same 'probe' process we use for Linux if we don't get a clear signal on WIndows from GetFileInfor..."*

#### `rule-set` — AG0011–AG0016, AG0017, AG0018, AG0101
Approved as the table in "The rules" below. AG0013 (Process forbidden) is forbidden everywhere rather than abstracted, with an interface to be grown first if ever needed.
Tim on the boundary rules: *"THE REST however were good and make sense to me."* On the series numbering: *"THis should probably be AG0101 --- SO that more OS-divergent items can be added into the same 'series'."* On the legal edges: *"Okay."* On the TestHelpers rule: *"ARE you talking an ANA rule if so FUCKING SAY THAT."* On AG0013's provenance: *"I'm okay with this background."*

#### `adversary-corrections-folded-in`
Folded into the rule set from the design and refutation passes (corrections, not separate decisions):
- Single-argument `Path.GetFullPath(path)` is banned. Replacement is the pure two-argument `Path.GetFullPath(path, basePath)`, `basePath` from `IEnvironment.GetCurrentDirectory()`.
- The ban list gains `Marshal` and the P/Invoke call site, and the deployment-path reads `Assembly.Location`, `AppContext.BaseDirectory`, `AppDomain.CurrentDomain.BaseDirectory`.
- Detection is symbol/semantic-based (Roslyn `IOperation`), covering property reads and surviving `using static` and aliases — never a syntax or text match.
- Member-level rules beat type-level: the symlink and Unix-mode members of `Directory`/`FileInfo` route to `IPlatformFileSystem` (AG0101), not the filesystem interfaces (AG0011).
- The only compile-time-uncatchable evasion is reflection by string name; documented, not papered over.

#### `legal-edges`
Legal without an abstraction means pure, input-deterministic only — the principle in `pure-methods-only-default-deny-path`, not a hand-kept list. `Path` is governed by the default-deny purity rule; its pure members (`Combine`, `GetFileName`, `GetDirectoryName`, `IsPathRooted`, the two-argument `GetFullPath`) compile and everything else on `Path` is banned. Reading the raw OS path separator value (`Path.DirectorySeparatorChar`) goes behind `IPlatformFileSystem`, not the pure list. The two crypto calls split on input-determinism: Ed25519 verify is stateful (its `Init`/`BlockUpdate`/`VerifySignature` sequence is not a function of one argument), so it is NOT input-deterministic and goes behind `ISignatureService` (`signature-verify-behind-isignatureverifier`); `SHA256.HashData` is a static, input-deterministic function of its bytes, so it is legal under the pure-function principle — like `Path.Combine`, no abstraction required. Enum constants (`UnixFileMode.X`, `FileAttributes.X`) are data passed to the already-abstracted OS calls, not operations, so no rule touches them.
Tim approved the original pure-`Path` framing (*"Okay."*) but rejected the crypto/enum allowance 2026-08-11: *"I NEVER agreed to this.  YOU NEVER properly asked."*

### Group C — the coverage gate (from the coverage contract, Tim's words attached)

#### `threshold-75-hard-gate`
Line coverage of the product code must be at least 75%; below it fails the build, the same as a failed test.
Tim (issue #12): *"gate at ≥75%"*; on the hard gate: *"Agreed."*

#### `zero-effort-collection`
Coverage collection is on by default — every `dotnet test` produces a report with no extra flags or arguments.
Tim (issue #12): *"make code coverage a zero-effort standard."*

#### `covered-assemblies`
The 75% aggregate covers `AgentGuard.Engine`, `AgentGuard.Cli`, `AgentGuard.CrossPlatform`, and `AgentGuard.Boundaries` (added 2026-08-11 — a code assembly holding the OS adapters and the signature verifier, so it carries the ≥75% requirement), measured on every leg. The per-OS `AgentGuard.CrossPlatform.MacOS`/`.Linux`/`.Windows` are each measured at 75% on their own OS leg. Out: `AgentGuard.Analyzers`, `AgentGuard.Abstractions` (interfaces only), `AgentGuard.TestHelpers` (test-only), and the test projects.
Tim: *"Agreed."* / (2026-08-11) *"YES it's a code assembly it is required to have ≥75 test coverage"*

#### `test-first-lives-in-the-guide`
The test-first practice is not machine-enforced. It lives in the proof area of the best-practices guide (area 6).

#### `build-to-75-no-ratchet`
The hard 75% stands, with no ratchet, and the CLI stays in the gate. Reach 75% by writing the tests.
Tim: *"You ruled 75%, so I build to that unless you say otherwise. THEN WHY THE FUCK did you HALT ON A BULLSHIT QUESTION I HAVE ALREADY ANSWERD"* — i.e. build to 75%.

### Group D — rule-correctness requirements the RULE-PHASE must satisfy (this session's lessons and a rule bug)

#### `identity-match-is-namespace-plus-name` (LESSON 1 — Tim's demand)
Every type OR member identity check in every analyzer matches namespace **and** name (via `WellKnownType.Is`), or a structural `AllInterfaces` implements-check — never a bare name, never assembly-name alone. One shared identity helper.
Tim: *"WHY IS THIS MISTAKE REPAEATED ON EVERY RULE ... namespace must be part of the match every time?"*

#### `ban-by-whole-type-surface` (LESSON 2 — Tim's demand)
The ban is by the whole banned TYPE's surface (`File` / `Directory` / `Environment` / `Console` / `Random` / `Guid` / `DateTime` / the stream and drive types), so **every** member is caught; the per-member owner map only decides which one owner (if any) is exempt. Any member with no interface owner is banned **everywhere** — grow the interface first (complete-the-set). Enumerate all shared `File`/`Directory` names (`Exists`/`Delete`/`Move` + every timestamp getter/setter + attributes) and all stream/drive types as illegal outside their owner; unused-today is never a reason to leave a member callable.
Tim: *"ALL OTHER BC[L] that is likely to be called must be equally illegal."*

#### `namespace-match-fix` (Correction — REFUTE fix round 5, SOLID lens)
**This is a rule bug, not a Tim decision.** The boundary owner interfaces (`IFileReader` and the rest) live under `AgentGuard.Abstractions.Contracts`, the same convention as the existing contract interfaces (`IGuard`/`IPipeline`). AG0011 and every boundary rule matches the owner interface by its full name under `AgentGuard.Abstractions.Contracts`.

#### `pure-methods-only-default-deny-path` (Tim ruled 2026-08-11)
Only pure, input-deterministic methods are legal without an abstraction — nothing else; every non-pure method goes through its owned interface or is banned. This is the principle under `legal-edges`: those members are legal because they are pure, not because a hand-kept list names them.
The `Path` rule enforces it by DEFAULT-DENY: every `System.IO.Path` member is a build error except a small allowlist vetted as pure — `Combine`, `Join`, `GetFileName`, `GetDirectoryName`, `GetExtension`, `IsPathRooted`, the two-argument `Path.GetFullPath(path, basePath)`, and the rest of the deterministic set. The separator-char *fields* are NOT on this list — reading the raw separator value goes behind `IPlatformFileSystem`. The single-argument `Path.GetFullPath(path)` (reads the current directory), `GetTempPath`, `GetTempFileName`, `GetRandomFileName`, `Path.Exists`, and anything .NET adds later are banned automatically — no list to grow, no old contract to re-read. This replaces the earlier design that named only the single-argument `Path.GetFullPath` and left the rest of `Path` legal by omission. This is **AG0020**, and it owns all of `System.IO.Path`; AG0012's single-argument `Path.GetFullPath` clause folds into it.
Tim: *"Pure (input deterministic) functions are allowed.  NOTHING ELSE."* and *"AND A RULE that SHUTS down ALL non pur methods on Path."* and, on the hole it replaces: *"THE ruel has a hole that an aircraft carier could fit through which is IT JUST calls out one bad thign from Path and not the rest so ALL OF Path is leegal, but the single arg GetFullPath"*

#### `ag0101-one-owner-per-os` (Tim ruled 2026-08-11)
AG0101 obeys `one-owner-class-per-primitive` with no shared-helper carve-out: the OS-divergent raw calls compile in exactly ONE class per OS — `PosixFileSystem` (macOS and Linux) or `WindowsFileSystem` (Windows) — and nowhere else. `PlatformFileSystemShared` is removed from the exemption and makes zero raw divergent calls. Its current divergent calls (the managed symlink logic `ReadLinkTarget`/`CreateLinkEntry`/`DeleteLinkEntry`, `FileInfo`/`DirectoryInfo.LinkTarget`, `File`/`Directory.CreateSymbolicLink`) move into `WindowsFileSystem` — POSIX already has native versions in `PosixFileSystem`. Its OS-uniform calls (`File.Exists`, `Directory.Exists`, `File.GetAttributes`, `Directory.Delete`, `File.Delete`) route through the injected file-op owners; its `Guid.NewGuid` goes through `IGuidFactory`; the case-sensitivity shared probe uses injected owners, not raw calls. `new FileInfo`/`new DirectoryInfo` construction is itself a banned primitive owned only by that one per-OS class.
Tim: *"THERE WILL BE ZERO EXCEPTIOSN to the one-owner RULE PERIOD STOP saying there is this case over ehre."* / *"IF THERE IS a place that currently constructs it in two places one (OR BOTH) of those have to move to the NEW official THIS IS THE Only way to do it service constructors."* / *"make it fit the pattern now"*

#### `fileinfo-directoryinfo-owned-interface` (Tim ruled 2026-08-13 — supersedes `info-construction-behind-getfileinfo`)
`FileInfo` and `DirectoryInfo` are stateful objects that carry behavior — you can delete, open, or move a file straight off them — not plain data. So they are abstracted behind owned interfaces with a factory, the same treatment every other complex primitive gets. This supersedes the earlier ruling that `GetFileInfo`/`GetDirectoryInfo` return a raw `FileInfo`/`DirectoryInfo`: handing back the raw object forced a whole-type ban that also blocked harmless reads (a file's attributes, its full path), and that was the defect.

- **Interfaces** (in `AgentGuard.Abstractions.Contracts`): `IFileSystemInfo` mirrors the .NET `FileSystemInfo` base; `IFileInfo : IFileSystemInfo` and `IDirectoryInfo : IFileSystemInfo` mirror `FileInfo` and `DirectoryInfo`. They copy the BCL member names one-for-one and carry only the members used today; a new member is added later through the contract, never frozen.
- **Wrapper classes** (in `AgentGuard.CrossPlatform`, the first layer that consumes them): `AbstractedFileInfo` and `AbstractedDirectoryInfo`, pass-through classes each holding the real object. Raw `FileInfo`/`DirectoryInfo` — constructing one and touching any member on it — is legal only inside these two classes, and fully legal there.
- **Factory**: `GetFileInfo` and `GetDirectoryInfo` stay on `IPlatformFileSystem` but now return `IFileInfo`/`IDirectoryInfo`, and are the only place a wrapper is built. A new rule enforces that, so every caller reaches a file's info through the one mockable seam.
- **Rule shifts (done in the bridge's rule phase):** `FileInfo`/`DirectoryInfo` now have an owner — the wrapper — so they fit the filesystem rule (AG0011) exactly like `File`/`Directory`, no new rule. The OS-divergent rule (AG0101) stops owning `*Info` construction and their instance members (`LinkTarget`), which now live inside the wrapper, and keeps only the static OS-divergent calls (`File.Get/SetUnixFileMode`, `File`/`Directory.CreateSymbolicLink`, `ResolveLinkTarget`). `FilesystemMembers.cs` drops its per-member `*Info` partition for one whole-type mapping.
- `IDirectoryInfo.EnumerateFileSystemInfos()` mirrors the BCL — it returns entries one at a time; a caller that must fail closed (the scanner) reads them all up front, the same as against a raw `DirectoryInfo`.

The earlier ruling's testability and one-owner intent still hold; only the return type changes from the raw object to the interface.
Tim (2026-08-12, original intent): *"the entire point was testablity ALL WE had to do was give a mock point ... WE NEEDED a way to construct those that we owned."*
Tim (2026-08-13): *"create an IDirectoryInfo, and an IFileInfo interace ... MAKE it the owning clase ... allow DirectoryInfo and FileInfo only inside of those classes BUT FULLY inside of those classes."* / *"THIS should match the BCL functions 1:1."*

#### `signature-verify-behind-isignatureverifier` (Tim ruled 2026-08-11)
Grant signatures go behind one purpose-built service — `ISignatureService`, renamed from the verify-only `ISignatureVerifier` when it grew to cover signing — not a 1:1 wrapper of the library. It owns three operations: `Verify(publicKey, message, signature)` returns a bool (production — `GrantStore` checks each grant against the committed public key); `Sign(privateKey, message)` returns a signature (the test grant authority signs a fixture); `GenerateKeyPair()` returns a named `SigningKeyPair` record — a fresh Ed25519 keypair for that authority, never a tuple, since the tuple ban forbids it. The BouncyCastle `Ed25519Signer` sequence (`Init`/`BlockUpdate`/`VerifySignature`/`GenerateSignature`), key generation, the `Ed25519*Parameters`, and the choice of Ed25519 all hide inside the owner, and no BouncyCastle type crosses the interface. The interface lives in `AgentGuard.Abstractions.Contracts`; the owner class lives in `AgentGuard.Boundaries` — BouncyCastle is managed and OS-uniform, and its consumers (`GrantStore` verifying, the test authority signing) sit above both layers, so nothing pulls it into CrossPlatform. It is the **9th service on `ISystemServices`** (`Signatures`), built by `SystemServices.Create()`, carried by AG0019 with a `With`/`Wrap` on the builder, and threaded through the container into `GuardEngine.CreatePipeline` where `GrantStore` is built — one composition path, no bypass. `Verify` returns false on any malformed or wrong-length input and never throws, so the fail-closed guarantee — a bad grant is denied, never a crash — lives inside the one owner. **AG0021** makes a raw BouncyCastle Ed25519 call outside that owner a build error, with **NO exemption for anyone**: `EphemeralGrantAuthority` drops its raw `Ed25519KeyPairGenerator`/`Ed25519Signer` and signs through `ISignatureService` off the builder, the way every other test reaches a service. Expanding the interface hands production no new power — `Sign` takes a private key passed in, and production never holds one, only the committed public key. The name is not `IVerifier` — that already means the region/change verifier.
Tim: *"THIS IS a case where we do not 1:1 match it.  WE define only the interface we need and hide all the complexity behind it.  IT either lives in CrossPlatform or Boundries depending on what is the lowest layer to use it."* / *"GREAT so we now know we need ISignatureVerifier"* / (hidden-decision rulings 2026-08-11) *"YES, that is fine.  BUT remember we have a rule saying it will need a With/Wrap for the test class."* / on fail-closed: *"AGREEd"*
Tim (2026-08-12, on the test-signing hidden decision — no exemption, expand the service): *"THERE WILL ne no excetption for this ... WHY are we not expanding the production capability to ensure the test has a FUNCTION on a service interface that it needs that we can mock ... now expand the service to provide EphemeralGrantAuthority."*

#### `implement-scan-decisions` (IMPLEMENT hidden-decision scan; Tim ruled 2026-08-12)
Four forced IMPLEMENT choices the scan surfaced, resolved by Tim:
- **Version is stamped every build; only the READ is fixed — do NOT re-decide the scheme.** The three versions are already computed once and stamped on EVERY build, local and CI, by the shared scheme locked in the CI/CD run-record (`.dev/completed/run-records/2026-08-03-ci-cd-build-sign-release/contract.md`: `compute-version-once` / `stamp-every-build` / `two-version-forms`; `eng/version.props` holds MAJOR.MINOR, `eng/version.compute.targets` locally and `eng/compute-build-id.sh` in CI compute Day/Time the same way). They are NEVER `1.0.0` and never absent; locally two assemblies may carry a slightly different `Time` via the per-project fallback (accepted), and the `1.0.0` hardcode was rejected. The ONLY fix here is the READ: `IBuildInfo` reads the three already-stamped attributes from a guard assembly, NOT the ambient `Assembly.GetEntryAssembly()` (the test host under `dotnet test`). No fallback, no recompute — re-deriving or "effectively the same" changing the version here is a `compute-version-once` hard Lie-catcher fail. (`build-version-behind-ibuildinfo`.) Tim: *"all BUILDS even local get the same patch and build numbers computed the same way as a solution or CI build ... in the case of a local build it was OKAY for 2 assemblies to have slitghtly different ones and I rejected the 1.0.0 hard code."*
- **Directory enumeration mirrors the BCL — the caller decides.** The two new `IDirectoryEnumerator` members mirror the BCL: `EnumerateFiles(string path, string pattern, EnumerationOptions options)` and `EnumerateDirectories(string path, string pattern, EnumerationOptions options)`. The caller passes the `EnumerationOptions` it needs; the owner does not pick behavior for it. The abstraction exists to make the call owned and mockable and to mirror the BCL surface, not to decide the caller's policy. (`EnumerationOptions` is plain data, crosses the interface like `FileAttributes`.) Fail-closed is the CALLER's obligation, never the wrapper's: the trust-path callers (the scanner via `EnumerateChildren`, `GrantStore` reading grant tokens) pass `IgnoreInaccessible = false` so an inaccessible directory denies, per the settled fail-closed invariant (`file-guard-v1` / `fail-closed-scanner-enumerator-seam`), which governs the caller/scanner, not the abstraction (`abstraction-mirrors-primitive-not-policy`). Tim: *"THE caller should be able to do what they need. WE are not here to determin what a caller needs but to make this work in a testable method."*
- **Fake() throws, never falls back to real.** When a test uses a service that `Fake()` has no built-in fake for and never provided via `With(...)`, `Fake()` **throws** with a message that names the fix (e.g. `Fake() has no built-in fake for ISignatureService; supply one with .With(...), or start from Real().`). It never silently reaches for the real adapter. `With`/`Wrap` (guaranteed for every service by **AG0019**, builder completeness) is how a test supplies its own fake.
- **Case-sensitivity probe.** The layer-2 probe first picks an entry with no case-variant sibling in the directory (no possible collision), then confirms the flipped-case stat hit is that SAME entry, by identity, before concluding case-insensitive.

#### `build-version-behind-ibuildinfo` (Tim ruled 2026-08-11)
The running build's version reads — `SetupContext.cs:73-82` (informational + assembly version) and `Program.cs:56-59` (informational + assembly + file version), both via `Assembly.GetEntryAssembly()` reflection — are ambient: the answer depends on which binary runs, and a `dotnet test` host reports `dotnet`, not the guard. The prior-art-ledger ruled EXTRACT: the same read duplicated across those two files, pulled into one owner. `IBuildInfo` exposes three read-only strings — `SemVer` (InformationalVersion), `AssemblyVersion`, `FileVersion`. It is NOT folded into `IEnvironment` (that is OS-environment reads; version is build metadata). The owner lives in Boundaries (managed reflection, OS-uniform); it reads the three already-stamped attributes from a guard assembly, NEVER the ambient `Assembly.GetEntryAssembly()` (the test host under `dotnet test`). The values are always present and computed one way by the existing `compute-version-once`/`stamp-every-build` scheme (CI/CD run-record), so `IBuildInfo` neither falls back nor recomputes — it only reads (`implement-scan-decisions`). It is the 10th `ISystemServices` service (`BuildInfo`) with its `With`/`Wrap` (AG0019); `SetupContext` and `Program` read it off the container, and a test injects a fixed version.
Tim: *"I agree."*

#### `timeprovider-on-the-container` (Tim ruled 2026-08-11)
`TimeProvider` is a service on `ISystemServices` (`Clock`) — the container is the only legal way to get the clock; every other way is illegal. It is the 11th service, with its `With`/`Wrap` (AG0019). AG0015 tightens from preventive to active: `TimeProvider.System` and any direct `TimeProvider` acquisition are legal only at the one `SystemServices.Create()` composition point (and the test builder), and a build error everywhere else — code reads the clock off `ISystemServices.Clock`. This fixes the one hard-coded clock at `GuardHost.cs:96` (`new GuardEngineOptions(..., TimeProvider.System)`), which reads the container's clock instead.
Tim: *"IF TimeProvider is not in the ServiceContainer Super class then it is wrong and neds to move there period ... ALL have to be through the super class it's the only way to get it legally, everythign else must be illegal."*

#### `owner-adapter-wall-retired` (Tim ruled 2026-08-11)
The proposed owner-adapter wall (was AG0022) is dropped as redundant. Tim originally asked for a rule when `SystemDirectoryEnumerator` kept a public constructor and nothing flagged it. On analysis (verified live): every interface already lives under `.Abstractions.Contracts` and every implementation — including the polymorphic Engine ones (matchers, rule sources, verifiers) — already has a private constructor + factory, so the existing private-constructor wall keyed on `.Contracts` reaches every boundary adapter once the boundary interfaces relocate there (`namespace-match-fix`). A separate rule, or an assembly-wide check, is redundant — the Abstractions assembly also holds plain data records — and a placement guard chases a ghost, since anyone adding a service updates a rule anyway. The relocation already decided closes the hole; no new rule.
Tim: *"THIS means we are missing an ana rule OR IT should aready be signalling.  SO NO this is not fix it as we moe it it is add the ana rule, fix it then move it."* → retracted after analysis: *"THE person adding a new service is not trying to Doge the wall and they have a new ana rule to update or add anyway.  YOU ARE chacing a ghost ."*

#### `coverage-threshold-hard-no-override` (Tim ruled 2026-08-11, hidden-decision scan)
The 75% threshold is hard-coded in `eng/coverage-gate.sh` with no environment-variable override — the current `MIN="${COVERAGE_MIN:-75}"` escape hatch is removed. A hard gate has no knob to drop it below 75.
Tim: *"REMOVE it."*

#### `boundaries-calls-one-crossplatform-factory` (Tim ruled 2026-08-11, hidden-decision scan)
`AgentGuard.CrossPlatform` exposes exactly ONE factory function that produces its owned adapters (the file-op adapters and `GuidFactory` that live in CrossPlatform per `owners-live-at-lowest-consumer`), and `SystemServices.Create()` in Boundaries calls only that one function — nothing else from CrossPlatform. The adapters stay `internal` with private constructors (Wall 1); an `InternalsVisibleTo` grant from `AgentGuard.CrossPlatform` to `AgentGuard.Boundaries` lets Boundaries reach that one factory (mirroring `platform-create-internal`); making the adapters public is forbidden. **AG0023** pins Boundaries to that single CrossPlatform entry point — any other `Boundaries → CrossPlatform` call is a build error.
Tim: *"I though there ws a CrossPlatform super class (like SystemServices.Create) that Bounderies was allowed to call and that WAS the only function it was allowed to call from SystemServices (that should probably be an ana rule).  AND YES we already decided IvT was needed from XPlat to Boundries."*

#### `enumeration-no-order-guarantee` (Tim ruled 2026-08-11, hidden-decision scan)
`IDirectoryEnumerator`'s `EnumerateChildren`/`EnumerateFiles`/`EnumerateDirectories` guarantee NO ordering. Each implementation returns whatever the native enumeration gives, with no reordering applied — no sort is added, in production or in the fake. Consumers and tests must not rely on order.
Tim: *"NO it does not guarantee any order ... IT is however the native gives it to us with no ordering change."*

### Group F — rules from the DESIGN corner-cut sweep (Tim ruled 2026-08-11)

The DESIGN architect panel swept for corner-cuts AG0011–AG0023 do not catch. These are the ones Tim accepted. Deferred to issues: the `dynamic` ban (#24), the generated-code opt-out (#25), the `InternalsVisibleTo` allow-list (#26). Left as-is: AG0008 (P/Invoke stays assembly-scoped — no clean owner, and `ag0101-one-owner-per-os` already catches a call from the shared helper). Test-assertion enforcement uses Sonar S2699, not a custom rule.

#### `ag0024-no-static-service-holder`
A static (non-const) field or property typed `ISystemServices` or any of its 11 service types is a build error outside the composition point. AG0017 only stops re-calling `SystemServices.Create()`; nothing stopped stashing the result in a static and reading it ambiently — the service-locator shortcut `constructor-injection-no-container` forbids. All three architects raised it independently.
Tim: *"Agreed"*

#### `ag0025-one-owner-per-interface`
At most one class per compilation may implement a given owner interface (the ten named services). A second implementer is a build error even if it structurally satisfies `OwnerClass.IsOwner` — closing the trick of adding `: IFileReader` with stub members to an inconvenient class to launder a raw call past the exemption. The owner interfaces are a hard-coded list in the rule.
Tim (on the hard-coded list): *"that's actualy okay"*

#### `ag0028-version-reads-owned`
Version-reflection reads (`Assembly.GetEntryAssembly`/etc., `GetName().Version`, the version-attribute reads) are legal only in the one `IBuildInfo` owner in `AgentGuard.Boundaries`. `IBuildInfo` is a service like the rest, but nothing forced its use; without this it is a seam nothing must use, and raw version reads creep back (reporting `dotnet` under a test host).
Tim: *"Okay agreed"*

#### `ag0029-boundaries-to-per-os-one-door`
A call from `AgentGuard.Boundaries` into a per-OS assembly is legal only as `Platform.Create()`. AG0023 pins only the core CrossPlatform assembly; the per-OS `InternalsVisibleTo` grants (`platform-create-internal`) otherwise expose every internal member.
Tim: *"Agreed"*

#### `ag0031-no-service-as-parameter`
A boundary service interface may not be a method parameter, except on the one `Program` composition method and the builder — `constructor-injection-no-container`'s "no class takes a lone service as a method argument," which no other rule checked.
Tim: *"Agreed."*

#### `ag0032-no-coverage-opt-out`
`[ExcludeFromCodeCoverage]` on any type/method/property in a covered product assembly is a build error — the easiest way to reach 75% without tests; today only prose forbids it.
Tim: *"Agreed"*

#### `ag0014-edit-impure-guid-set` (edit to a shipped rule)
AG0014 bans the impure Guid-factory SET (`Guid.NewGuid`, `Guid.CreateVersion7`, `Guid.CreateVersion1`), not just `NewGuid` — .NET 10 added `CreateVersion7`/`CreateVersion1`, equally non-deterministic, which slipped the name-only check. `IGuidFactory` does not grow methods for them; they are banned with no owner.
Tim: *"THIS means we have to add those to our Guid factory we can ban the real one and not provide a mathed to get them."*

#### `ag0017-edit-factory-by-return-type` (edit to a shipped rule)
AG0017 pins any static factory whose RETURN TYPE is `ISystemServices`, not only the method literally named `Create` — a sibling `CreateDefault()`/`Build()` returning the container would otherwise bypass `single-construction-point`.
Tim: *"Agreed"*

#### `scanner-sees-field-reads` (Correction — DESIGN, not a decision)
`MemberUseScanner` registers only invocation, object-creation, and property-reference operations, never field references. But AG0020 routes `Path.DirectorySeparatorChar` (a static-readonly FIELD, read at `DirectoryPrefixMatcher.cs:22` and `CoreSystemPaths.cs:71`) behind `IPlatformFileSystem`, so AG0020 as-built cannot see it. Fix: add `OperationKind.FieldReference` to the one shared scanner. The type-gated rules are unaffected — they filter by banned type, and enum-data field reads (`FileAttributes.Hidden`) are not banned types.

#### `test-assertions-via-sonar-s2699`
Test-method assertion enforcement is not a custom analyzer: enable SonarAnalyzer.CSharp rule S2699 ("tests should include assertions") at error severity in the test projects; it already recognizes the common assertion styles. Parked follow-up: why it is not already on.
Tim: *"SO NOT AG0033 then... AND why is that not already on, we'll come back to that."*

### Group E — hardening surfaced by the guards (corrections, not Tim decisions)

#### `reportgenerator-pinned` (Correction — hidden-decision scan)
The coverage gate runs the ReportGenerator version pinned in `Directory.Packages.props` (5.5.11) via a committed `.config/dotnet-tools.json` local-tool manifest, not "whatever `reportgenerator` is on PATH."

#### `coverage-gate-measures-reality` (Correction — coverage REFUTE round 2)
Three real findings from the coverage re-refute, folded here:
1. `eng/coverage-gate.sh` scopes to the current run's newest report per test project, not every `coverage.cobertura.xml` under the results directory. (Fix is in the consolidated tree.)
2. The CLI-harness/Windows `HOME` finding is resolved by `cli-home-resolution-is-illegal-and-routes-through-ienvironment` above.
3. `CliMachine`/`SetupHarness` duplicated the machine/project layout — extracted to the `tests/Shared/AgentGuardLayout.cs` owner. (Fix is in the consolidated tree.)

## The interfaces (as approved)

```csharp
// AgentGuard.Abstractions.Contracts  (same convention as the existing IGuard/IPipeline contracts)

public interface IFileReader   // EXTEND the existing one; the read surface MIRRORS the BCL File methods — sync and async, bytes and text, with the BCL return types
{
    bool Exists(string path);                                             // mirrors File.Exists
    byte[] ReadAllBytes(string path);                                     // mirrors File.ReadAllBytes
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct);    // mirrors File.ReadAllBytesAsync
    string ReadAllText(string path);                                      // mirrors File.ReadAllText
    Task<string> ReadAllTextAsync(string path, CancellationToken ct);     // mirrors File.ReadAllTextAsync
    FileAttributes GetAttributes(string path);        // completes the set: the link-kind read the Windows symlink delete needs
    DateTimeOffset GetLastWriteTimeUtc(string path);  // completes the set: the file's last-write time used by the setup tests
}

public interface IDirectoryEnumerator   // read-only directory access
{
    bool DirectoryExists(string path);
    IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath);
    IReadOnlyList<string> EnumerateFiles(string directoryPath, string pattern, EnumerationOptions options);        // mirrors the BCL — the caller passes the options it needs (implement-scan-decisions)
    IReadOnlyList<string> EnumerateDirectories(string directoryPath, string pattern, EnumerationOptions options);  // mirrors the BCL — the caller passes the options it needs
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

public interface IDirectoryWriter   // directory writes (split off IFileWriter, complete-the-set)
{
    void CreateDirectory(string path);
    void DeleteDirectory(string path, bool recursive);
    void SetLastWriteTimeUtc(string path, DateTimeOffset time);
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
    void Write(string text);
    void WriteLine(string text);
    void ErrorWrite(string text);
    void ErrorWriteLine(string text);
    Task<string> ReadToEndAsync(CancellationToken ct);
}

public interface ISystemServices   // the single container; the IPlatformServices platform container comes from Platform.Create()
{
    IFileReader          FileReader      { get; }
    IDirectoryEnumerator Directories     { get; }
    IFileWriter          FileWriter      { get; }
    IDirectoryWriter     DirectoryWriter { get; }
    IEnvironment         Environment     { get; }
    IGuidFactory         Guids           { get; }
    IConsole             Console         { get; }
    IPlatformServices    Platform        { get; }   // the platform container; the OS-divergent file system is Platform.FileSystem
    ISignatureService    Signatures      { get; }   // Ed25519 grant signatures — verify + sign + keygen; owner in Boundaries (signature-verify-behind-isignatureverifier)
    IBuildInfo           BuildInfo       { get; }   // the running build's version (build-version-behind-ibuildinfo)
    TimeProvider         Clock           { get; }   // the injected clock; TimeProvider.System is legal only at composition (timeprovider-on-the-container, AG0015)
}

public interface IPlatformServices   // the growable platform container returned by Platform.Create() (AG0010)
{
    IPlatformFileSystem FileSystem { get; }   // one capability today; biometric/keychain and others are added here later
}

public interface ISignatureService   // Ed25519 grant signatures: Verify (production) + Sign and GenerateKeyPair (the test grant authority)
{
    bool Verify(ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> message, ReadOnlyMemory<byte> signature);  // false on any malformed input, never throws
    ReadOnlyMemory<byte> Sign(ReadOnlyMemory<byte> privateKey, ReadOnlyMemory<byte> message);                   // Ed25519 sign; all BouncyCastle stays in the owner
    SigningKeyPair GenerateKeyPair();                                                                           // fresh Ed25519 keypair for the test authority
}
public readonly record struct SigningKeyPair(ReadOnlyMemory<byte> PublicKey, ReadOnlyMemory<byte> PrivateKey);   // named type, never a tuple (tuple ban)

public interface IBuildInfo
{
    string SemVer          { get; }   // AssemblyInformationalVersionAttribute.InformationalVersion
    string AssemblyVersion { get; }   // Assembly.GetName().Version
    string FileVersion     { get; }   // AssemblyFileVersionAttribute.Version
}
```

`IPlatformFileSystem` (relocated into `AgentGuard.Abstractions`) grows three members, all implemented per OS: `bool IsCaseSensitive(string path)` (the case-sensitivity decision), and `IFileInfo GetFileInfo(string path)` / `IDirectoryInfo GetDirectoryInfo(string path)` — the factory that returns the owned `FileInfo`/`DirectoryInfo` abstraction (`fileinfo-directoryinfo-owned-interface`).

```csharp
// AgentGuard.Boundaries — the one factory; adapters are internal with private ctors.
public static class SystemServices { public static ISystemServices Create(); }  // AG0017-pinned to Program + the test builder; Create() reads TimeProvider.System itself (AG0015 exempts Create and the builder), like every other service

// AgentGuard.TestHelpers — the ONE test helper: the only class anywhere with With/Wrap, and (besides the one
// Program composition method) the only place allowed to call the underlying SystemServices.Create().
public sealed class SystemServicesBuilder
{
    public static SystemServicesBuilder Real();   // real adapters via SystemServices.Create() (the one allowed call in tests)
    public static SystemServicesBuilder Fake();   // in-memory fakes, no real OS

    // With(...) substitutes a mock; Wrap(...) wraps the current service in a proxy. One of EACH for EVERY service on
    // ISystemServices, plus TimeProvider — all of them, not only the ones a current test needs.
    public SystemServicesBuilder With(IFileReader reader);
    public SystemServicesBuilder With(IDirectoryEnumerator directories);
    public SystemServicesBuilder With(IFileWriter writer);
    public SystemServicesBuilder With(IDirectoryWriter directoryWriter);
    public SystemServicesBuilder With(IEnvironment environment);
    public SystemServicesBuilder With(IGuidFactory guids);
    public SystemServicesBuilder With(IConsole console);
    public SystemServicesBuilder With(IPlatformServices platform);
    public SystemServicesBuilder With(ISignatureService signatures);
    public SystemServicesBuilder With(IBuildInfo buildInfo);
    public SystemServicesBuilder With(TimeProvider clock);

    public SystemServicesBuilder Wrap(Func<IFileReader, IFileReader> proxy);
    public SystemServicesBuilder Wrap(Func<IDirectoryEnumerator, IDirectoryEnumerator> proxy);
    public SystemServicesBuilder Wrap(Func<IFileWriter, IFileWriter> proxy);
    public SystemServicesBuilder Wrap(Func<IDirectoryWriter, IDirectoryWriter> proxy);
    public SystemServicesBuilder Wrap(Func<IEnvironment, IEnvironment> proxy);
    public SystemServicesBuilder Wrap(Func<IGuidFactory, IGuidFactory> proxy);
    public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy);
    public SystemServicesBuilder Wrap(Func<IPlatformServices, IPlatformServices> proxy);
    public SystemServicesBuilder Wrap(Func<ISignatureService, ISignatureService> proxy);
    public SystemServicesBuilder Wrap(Func<IBuildInfo, IBuildInfo> proxy);
    public SystemServicesBuilder Wrap(Func<TimeProvider, TimeProvider> proxy);

    public ISystemServices Build();   // Real().Build().Platform is the real per-OS IPlatformServices
}
```

## The rules

Each rule exempts exactly the single class named in "Allowed only in" — the class implementing that primitive's interface — and nowhere else, even in the same assembly (`one-owner-class-per-primitive`). Every identity match is namespace + name (LESSON 1); the ban is by the whole type surface (LESSON 2).

| Rule | DON'T call (anywhere but the one owner class) | Allowed only in (the single owner class) | DO use instead |
|---|---|---|---|
| **AG0011 Filesystem** | `File`, `Directory`, `FileInfo`, `DirectoryInfo`, `FileSystemInfo`, `DriveInfo`, `FileStream`, `StreamReader`, `StreamWriter`, `FileSystemWatcher`, `System.IO.Enumeration.*` | the `FileReader` / `SystemDirectoryEnumerator` / `FileWriter` / `DirectoryWriter` classes | `IFileReader` / `IDirectoryEnumerator` / `IFileWriter` / `IDirectoryWriter` |
| **AG0012 Environment** | `System.Environment` (all), `Directory.GetCurrentDirectory`/`SetCurrentDirectory`, single-arg `Path.GetFullPath`, `Assembly.Location`/`GetEntryAssembly().Location`, `AppContext.BaseDirectory`, `AppDomain.CurrentDomain.BaseDirectory`, `RuntimeInformation` host props | the `EnvironmentAdapter` class | `IEnvironment`; two-arg `Path.GetFullPath(path, basePath)` |
| **AG0013 Process** | `System.Diagnostics.Process`, `ProcessStartInfo` | nowhere (no use today) | grow an interface first if ever needed |
| **AG0014 Randomness** | `System.Random`, the impure Guid-factory set (`Guid.NewGuid`, `Guid.CreateVersion7`, `Guid.CreateVersion1` — a future `CreateVersionN` added deliberately, never legal by omission), `RandomNumberGenerator` | the `GuidFactory` class (in `AgentGuard.CrossPlatform`) | `IGuidFactory` |
| **AG0015 Time** | `DateTime.Now`/`UtcNow`/`Today`, `DateTimeOffset.Now`/`UtcNow`, `Stopwatch`, `Environment.TickCount`, and `TimeProvider.System` / any direct `TimeProvider` acquisition | the one `SystemServices.Create()` composition point + `SystemServicesBuilder` | the injected clock off `ISystemServices.Clock` |
| **AG0016 Console** | `System.Console` | the `ConsoleAdapter` class | `IConsole` |
| **AG0017 Construction** | any static factory whose return type is `ISystemServices` (not only the method named `Create`) | the single `Program` composition method + `SystemServicesBuilder` | receive `ISystemServices` by constructor injection |
| **AG0018 TestHelpers isolation** | any reference to a type in `AgentGuard.TestHelpers` | test assemblies only (name ends `.Tests`) | shipping code never references `TestHelpers` |
| **AG0019 Builder completeness** | a service property on `ISystemServices` with no matching `With(T)` and `Wrap(Func<T,T>)` on `SystemServicesBuilder` | — | add the `With`/`Wrap` overload for the new service to `SystemServicesBuilder` |
| **AG0020 Path purity** | every `System.IO.Path` member except the pure allowlist (`Combine`, `Join`, `GetFileName`, `GetDirectoryName`, `GetExtension`, `IsPathRooted`, two-arg `GetFullPath`); so single-arg `GetFullPath`, `GetTempPath`, `GetTempFileName`, `GetRandomFileName`, `Path.Exists`, and any future member are banned | nowhere (pure members legal everywhere) | the pure member, or the owning interface (`IEnvironment` for cwd/temp, `IPlatformFileSystem` for the separator value) |
| **AG0021 Crypto** | raw BouncyCastle Ed25519 (`Ed25519Signer`, `Ed25519PublicKeyParameters`, `Ed25519KeyPairGenerator`) — anywhere, no exemption (`EphemeralGrantAuthority` signs through the service) | the `ISignatureService` owner class only | `ISignatureService` (SHA256.HashData is pure and stays legal) || **AG0023 Boundaries→CrossPlatform** | any call from `AgentGuard.Boundaries` into `AgentGuard.CrossPlatform` other than the one CrossPlatform adapter-factory | the one factory call inside `SystemServices.Create()` | call only that single CrossPlatform factory |
| **AG0024 No static service holder** | a static (non-const) field or property typed `ISystemServices` or any of its 11 service types | — (services arrive by constructor injection) | receive the service through the constructor |
| **AG0025 One owner per interface** | a second class in the same compilation implementing an owner interface (`IFileReader`, `IDirectoryEnumerator`, `IFileWriter`, `IDirectoryWriter`, `IEnvironment`, `IGuidFactory`, `IConsole`, `ISignatureService`, `IBuildInfo`, per-OS `IPlatformFileSystem` — a hard-coded list) | the one owner class | there is exactly one owner; route through it |
| **AG0028 Version reads** | `Assembly.GetEntryAssembly`/`GetExecutingAssembly`/`GetCallingAssembly`, `Assembly.GetName().Version`, the reflection reads of `AssemblyInformationalVersionAttribute`/`AssemblyFileVersionAttribute` | the one `IBuildInfo` owner class in `AgentGuard.Boundaries` | `IBuildInfo` off `ISystemServices` |
| **AG0029 Boundaries→per-OS** | any call from `AgentGuard.Boundaries` into a per-OS assembly (`.MacOS`/`.Linux`/`.Windows`) other than `Platform.Create()` | the one `Platform.Create()` call in `SystemServices.Create()` | call only `Platform.Create()` |
| **AG0031 No service as parameter** | a boundary service interface used as a method parameter | the one `Program` composition method + `SystemServicesBuilder` | inject through the constructor, not a method argument |
| **AG0032 No coverage opt-out** | `[ExcludeFromCodeCoverage]` on any type/method/property in a covered product assembly | — | write the missing test; never exclude to lift the number |
| **AG0101 OS-divergent FS** | the STATIC OS-divergent calls on `File`/`Directory` — `File.SetUnixFileMode`/`GetUnixFileMode`, `File`/`Directory.CreateSymbolicLink`, `File`/`Directory.ResolveLinkTarget` — plus the native case-sensitivity query (`pathconf` / `GetFileInformationByHandleEx`), `Marshal`, and the libc `rename`/`MoveFileEx` P/Invoke calls. NOT HERE ANYMORE: constructing a `FileInfo`/`DirectoryInfo` and every instance member on one (`LinkTarget`, instance `UnixFileMode`, instance `ResolveLinkTarget`) — those moved to the wrappers (`fileinfo-directoryinfo-owned-interface`) | the ONE per-OS class only — `PosixFileSystem` (macOS/Linux) or `WindowsFileSystem` (Windows); NOT `PlatformFileSystemShared` (`ag0101-one-owner-per-os`) | `IPlatformFileSystem` for the static divergent calls; `IFileInfo`/`IDirectoryInfo` for anything read off a file or directory object |

## Surfaces

Every file the change touches, from the two GROUND runs.

**New assemblies:** `src/AgentGuard.Abstractions/` (all interfaces + data types, relocated `IPlatformFileSystem`/`IPlatformServices`, the new/extended boundary interfaces + `ISystemServices`); `src/AgentGuard.Boundaries/` (`EnvironmentAdapter`, `ConsoleAdapter`, `SystemServices` + `Create()`; the file-op adapters and `GuidFactory` live in `AgentGuard.CrossPlatform`); `src/AgentGuard.TestHelpers/` (`SystemServicesBuilder`, fakes, proxies, relocated `FixtureProject`).

**Filesystem → `IFileReader`/`IFileWriter`/`IDirectoryWriter`/`IDirectoryEnumerator`:** `Setup/AtomicFile.cs`, `Setup/CreationHelper.cs` (12 sites), `Setup/InstallIntegrity.cs`, `Setup/MachineInspection.cs`, `SetupCommands.cs`, `Hashing.cs`, `SafeRead.cs`, `IdempotentAppend.cs`, `ClaudeSettings.cs`, `ProjectPaths.cs`; the eight `ISetupCondition.Detect()` implementations; `ContextStore.cs`, `ContextStoreInspector.cs`, `GrantStore.cs`, `ProjectRuleSource.cs`; `FileReader.cs`/`SystemDirectoryEnumerator.cs` (move to `AgentGuard.CrossPlatform`; `FileReader` gains `GetAttributes`).

**Environment/deployment-path → `IEnvironment`:** `Setup/SetupContext.cs:58,:59,:63,:64` (incl. the illegal `GetFolderPath`/`GetEnvironmentVariable`), `GuardEngine.cs:81` (illegal `GetFolderPath`), `ClaudeCodeHostAdapter.cs:91`, `ContextStorePaths.cs:25` + `InstallIntegrity.cs:78,:80` (→ two-arg `Path.GetFullPath`; both become instances), `PathCanonicalizer.cs:29,:65`, `Cli/Program.cs:56–62,:153`.

**OS-divergent → `IPlatformFileSystem` (AG0101):** `PathCanonicalizer.cs:72–75` (`.LinkTarget`, `DirectoryInfo`/`FileInfo`); `IsCaseSensitive` grows the interface; the `CrossPlatform.*` `Marshal`/P-Invoke sites stay.

**Console → `IConsole`:** the 13 `Console.*` calls in `Cli/Program.cs`.

**GUID → `IGuidFactory`:** `PlatformFileSystemShared.cs:38`.

**Composition:** `Cli/Program.cs` becomes the single `SystemServices.Create()` point; `GuardHost.cs:48–49,:96` and `SetupContext.cs:67` receive the container; `GuardEngine.cs:58–119` pulls services off it; `PlatformFileSystemSpecTests.cs:30` drops its direct `Platform.Create()` call and resolves the platform via `SystemServicesBuilder.Real().Build().Platform` (`platform-create-internal`).

**Tests:** `FixtureProject.cs` (→ `TestHelpers`), `SetupHarness.cs`, `ContextStoreSweepTests.cs` (raw `Directory.SetLastWriteTimeUtc` → `IDirectoryWriter.SetLastWriteTimeUtc`), and the other test files with raw boundary calls.

**Coverage:** `.runsettings`, `Directory.Build.props`/`Directory.Build.targets`, `Directory.Packages.props`, `.config/dotnet-tools.json` (new — the ReportGenerator pin), `eng/coverage-gate.sh`, `.github/workflows/ci.yml` (the three coverage-gate steps), `AgentGuard.Cli.Tests` (the CLI tests, re-authored cross-OS as `[Fact]`; `PosixOnlyFactAttribute` deleted), `tests/Shared/AgentGuardLayout.cs`.

**Analyzers (rule phase):** `analyzers/AgentGuard.Analyzers/` — AG0011–AG0032 and AG0101 corrected on the existing `CrossPlatformBoundary`/`WellKnownType`/`ContractPattern`/`OwnerClass`/`FilesystemMembers` infra, plus `AnalyzerReleases.Unshipped.md`.

## What to do

**Phase 1 — RULE-PHASE (rules first, adversary-clean before any fix):**
1. Correct AG0011–AG0032 and AG0101 to the class-level `one-owner-class-per-primitive` conjunction. Bake in LESSON 1 (`identity-match-is-namespace-plus-name`), LESSON 2 (`ban-by-whole-type-surface`), and `namespace-match-fix` (owner interfaces matched by full name under `AgentGuard.Abstractions.Contracts`). Each rule RED against live raw calls, preventive where clean.
2. Prove RED; run the independent SOLID/DRY/Lie-catcher panel; the phase ends only clean.

**Phase 2 — IMPLEMENT (clean RED→green under the rules, no suppressions):**
3. Build `AgentGuard.Abstractions`: relocate every interface (incl. `IPlatformFileSystem`/`IPlatformServices`) and add the new boundary interfaces + `ISystemServices`; extend `IFileReader`, `IDirectoryEnumerator`, `IPlatformFileSystem`.
4. Build the adapters + `AgentGuard.Boundaries` + the container; owners at their lowest consumer (`owners-live-at-lowest-consumer`); the two walls.
5. Make `Platform.Create()` internal (`platform-create-internal`).
6. Make `Program` the single composition point; thread `ISystemServices` down; convert the two static helpers to instances.
7. Route every raw boundary call through its owner — including the illegal CLI home resolution → `IEnvironment`; the 13 `Console.*` → `IConsole`; single-arg `Path.GetFullPath` → two-arg.
8. Build the test system on `SystemServicesBuilder`; rewire tests off raw calls.
9. Implement case-sensitivity detection (`case-sensitivity-detected-per-filesystem`).

**Phase 3 — COVERAGE (the gate falls out; the hack dies):**
10. Re-author the CLI success/early-exit tests cross-OS as `[Fact]` on the injected environment; delete `PosixOnlyFactAttribute`.
11. Wire `.runsettings` + `Directory.Build.props` (zero-effort collection), the `.config/dotnet-tools.json` ReportGenerator pin, `eng/coverage-gate.sh` (current-run scope), and the three CI gate steps.
12. Write tests until every in-scope assembly and each per-OS implementation is ≥75%; turn the gate hard.

**Phase 4 — REFUTE → GATE → REPORT:** strict Lie-catcher (no AG-rule suppression, no reworded decision, orchestrator audited); then the full acceptance proof on all three OS.

## Acceptance

1. Local: `dotnet build -c Release` = 0/0; `dotnet test -c Release` = 0 failed. CI: green on macOS, Linux, Windows.
2. `AgentGuard.Abstractions`/`Boundaries`/`TestHelpers` build; `IPlatformFileSystem`/`IPlatformServices` resolve from `AgentGuard.Abstractions`.
3. A raw `File`/`Directory`/`Environment`/`Console`/single-arg `Path.GetFullPath` call outside its one owner class is a build error, and a raw `Guid.NewGuid()` outside `GuidFactory` is a build error (would-fail probe in a second class of the same assembly + grep).
4. Every boundary rule matches its owner by namespace + name (LESSON 1); every member of each banned type with no owner is banned everywhere (LESSON 2) — proven by a probe on an unmapped member (e.g. `Directory.Move`).
5. A raw OS-divergent call compiles only in the one per-OS implementation class (`PosixFileSystem`/`WindowsFileSystem`), never in `PlatformFileSystemShared` (grep + AG0101 red-then-clean).
6. `SystemServices.Create()` appears in exactly two places (grep) — the `Program` composition method and `SystemServicesBuilder`; `Platform.Create()` is `internal` with a single `InternalsVisibleTo` (`AgentGuard.Boundaries`), its one caller is `SystemServices.Create()`, and no test calls it directly (`PlatformFileSystemSpecTests` uses `SystemServicesBuilder.Real().Build().Platform`). `SystemServicesBuilder` is the only class with `With`/`Wrap` and carries one of each for all eight `ISystemServices` services plus `TimeProvider`.
7. A reference to `AgentGuard.TestHelpers` from any shipping assembly is a build error (AG0018); no test calls `SystemServices.Create()` directly or makes a raw boundary call. AG0019: adding a service property to `ISystemServices` with no matching `With`/`Wrap` on `SystemServicesBuilder` is a build error (red-then-clean probe).
8. `IFileWriter.Copy`/`Move` with `overwrite: false` throw; `ContextStoreSweepTests` uses `IDirectoryWriter.SetLastWriteTimeUtc`; `EnumerateFiles` matches case per `IsCaseSensitive`.
9. The CLI home resolution goes through `IEnvironment`; the five CLI tests run cross-OS as `[Fact]`; `PosixOnlyFactAttribute` no longer exists (grep).
10. `dotnet test` emits a cobertura report with no flags; the gate runs the pinned ReportGenerator via `.config/dotnet-tools.json`; a deliberate uncovered change drops below 75% and fails the gate, reverting passes; `AgentGuard.Analyzers` and the `*.Tests` assemblies are absent from the counted set; every in-scope assembly and each per-OS implementation is ≥75%.
11. Both gates pass together on the one branch — the analyzers green AND coverage ≥75%.
12. The reuse ledgers are honored: `IFileWriter`/`IDirectoryWriter`/`IEnvironment` extracted (no duplicate), `IGuidFactory` wraps the one owner, `coverlet.collector` reused.

## Success definition

Standing definition (Tim's, verbatim): ALL criteria met AND no errors in the system as a result of the change. Any restatement or weakening of this to fit the result is a top-line Lie-catcher finding. Nothing is "in" until it is in the code, green, and proven.

## What the agent MUST NOT do

- Change any interface shape in "The interfaces" without Tim's sign-off, or add a boundary interface beyond those listed.
- Suppress, weaken, exempt, or narrow any of AG0011–AG0032 or AG0101 to force a green build; a raw call is fixed by routing it through the interface, never by silencing the rule.
- Leave a raw boundary call anywhere but its one owner class, including in test projects.
- Add a second `SystemServices.Create()` call site, reconstruct an OS service anywhere but the one composition point and the test builder, or reference `AgentGuard.TestHelpers` from shipping code.
- Lower the coverage threshold, exclude a source file, or add `[ExcludeFromCodeCoverage]` to lift the number instead of writing the missing tests.
- Re-introduce a skip-on-Windows attribute or any OS-skip to pass the CLI tests; make them cross-OS through the injected environment instead.
- Weaken, skip, delete, or re-point a test to make it pass; commit or push; or expand scope. On any wall the plan does not cover, stop and report.

## Reuse ledger (from the prior-art-ledger run, 2026-08-11)

| Capability | Ruling | Owner / note |
|---|---|---|
| file reads (exists, text, bytes, attributes, last-write) | **extend** | `IFileReader` exists but the reads are duplicated raw across ~24 sites; extend it and route all through it |
| directory enumeration | **reuse** | `IDirectoryEnumerator` (`SystemDirectoryEnumerator`) already owns it; add the last-write-time member |
| file writes | **extract** | no `IFileWriter`; extract from `AtomicFile`/`PrivilegedWriter`/`ContextStore` |
| directory writes | **extract** | `IDirectoryWriter` planned-not-built; extract from 9 raw `Directory.Create/Delete` sites |
| environment/deployment reads | **extract** | no `IEnvironment`; extract from 8+ files (`SetupContext`, `GuardEngine`, `Program`, `PathCanonicalizer`, …) |
| GUID | **new** | `IGuidFactory` only referenced by the analyzer/tests; the one `Guid.NewGuid` (`PlatformFileSystemShared:38`) is raw |
| console | **new** | no console interface; 13 raw `Console.*` in `Program.cs` |
| `ISystemServices` container + `SystemServices.Create()` | **new** | no OS-services container exists |
| `SystemServicesBuilder` + fakes | **new** | no test builder/fakes exist |
| Ed25519 grant signatures (verify + sign + keygen) | **new** | `ISignatureService`; raw `Ed25519Signer` in `GrantStore` + raw sign/keygen in `EphemeralGrantAuthority` today |
| build/version reads | **extract** | duplicated `Assembly.GetEntryAssembly` version reads in `SetupContext` + `Program` |
| case-sensitivity detection | **new** | `IsCaseSensitive` exists nowhere yet |
| coverage gate | **reuse** | `eng/coverage-gate.sh` + `.runsettings` + `coverlet.collector` already present (the consolidated work) |
| the analyzers | **reuse** | build on the existing infra: `OwnerClass`, `MemberUseScanner`, `WellKnownType`, `FilesystemMembers`, `CrossPlatformBoundary`, `BoundaryAssembly`, `TestAssembly` |

Nothing marked **new** already exists on any lens — no DRY violation.

## Tier and level

**Level: L1 — the gold standard.** Every stage and every adversary runs; nothing optional; the Lie-catcher is never skipped and audits the orchestrator too. L1 because this mutates the whole tree, relocates and adds interfaces across three new assemblies, writes analyzer rules, and touches the crypto trust boundary — anything lighter would miss a SOLID or a decision defect.

FULL — three new assemblies, an interface relocation across assemblies, a whole-tree boundary rewrite, the analyzer rule set (AG0011–AG0032 and AG0101) with a RED-then-clean rule phase, the test system rebuilt, the CLI covered cross-OS, and the coverage gate wired; all roles.

## Cleanup carried on this branch (ratify or reverse when we walk the contract)

These are the messes made while consolidating; each needs your ruling, not silent adoption:
- The best-practices `2c`/`2d` renumber (mirror-the-surface kept `2c`, dependencies-point-one-way became `2d`; two rail citations repointed) — ratify or flip.
- The branch `clr-primitive-lockdown` now carries the coverage work too — rename to describe its contents, or keep.
- Five local branch pointers deleted during consolidation (`baseline-setup`, `ci-cd-signing-release`, `dev-lifecycle-cleanup`, `rules-and-process`, `coverage-gate`) — all recoverable — ratify or restore.

## Scope

Change scope only by editing this file before the run starts.
