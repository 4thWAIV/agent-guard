# Brief — move SystemServices into Engine

This brief describes the SystemServices relocation. `contract.md` is the execution authority; completed GROUND and DESIGN records supply evidence. Continue at the CONTRACT checkpoint, then the remaining L1 stages. The current scope is the move, its reference/grant changes, the approved application-access rules, shared implementation needed by those rules, and tests using the existing runner. The proposed analyzer-analysis framework and suppression-ban rule are removed from this work. Earlier panel proposals do not extend the contract.

## What this work is

`SystemServices` is the one concrete `ISystemServices` container and the one composition factory that builds it. It lives today in `AgentGuard.Boundaries` and is the only place in that assembly that reaches into `AgentGuard.CrossPlatform` and a per-OS implementation assembly.

This work moves that one class into `AgentGuard.Engine`, moves the project references and internal-access grants that follow it, and retargets or adds the analyzer rules that keep the resulting access narrow. The four boundary adapters stay in `AgentGuard.Boundaries`. No service is added or removed, no static class is converted, and no application behavior changes.

This is preparatory work inside the static-class-evaluation issue, GitHub issue #74. The static-class conversions themselves are later work and are not in this run.

## Decisions already recorded

The full approved wording is in `contract.md` under Decisions. In summary, Tim approved:

- The relocation itself — `SystemServices` moves to `src/AgentGuard.Engine/SystemServices.cs` in namespace `AgentGuard.Engine`, keeping its internal sealed shape, its private constructor, and its static `Create()`.
- The reference and grant moves — Engine imports `PlatformImplementation.targets` and references Boundaries, the CLI drops its direct Boundaries reference, TestHelpers references Engine instead of Boundaries, Boundaries grants internal access to Engine instead of to the CLI and TestHelpers, the CrossPlatform and per-OS projects grant to Engine instead of Boundaries, and Engine grants to the CLI and TestHelpers.
- The boundary-access restriction — AG0040 permits exactly four adapter factories, and only from `AgentGuard.Engine.SystemServices.Create()`.
- The inbound grant restriction — AG0041 permits the CLI and TestHelpers to reach exactly one Engine internal, the `SystemServices.Create()` invocation.
- The retargeting of AG0023 and AG0029 with a caller constraint added on the Engine side and the existing restriction outside Engine preserved.
- The four analyzer support files, the method-level caller check in `CompositionPoint`, and the `DeclaredTypeScanner` extraction.
- The two named test assertion reversals, and no others.
- `SharedAnalyzerSources.cs` joining the Surfaces, so the four analyzer test classes share one fake Engine fixture.
- Renaming `BoundariesSystemServicesWiringTests`, both file and class, to `EngineSystemServicesWiringTests`, as a mechanical consequence of moving the container rather than a separate architecture decision.
- Extending AG0023 and AG0029 to catch prohibited type references as well as member access in Engine, with their existing Boundaries behavior preserved.

The relocation runs at L1. Every stage and all five independent adversaries apply.

## Ground truth established from the live tree

These facts come from reading the working tree at this commit. Explorers should confirm them rather than assume them, and report any that no longer hold.

`src/AgentGuard.Boundaries` holds five source files: `SystemServices.cs` and the four adapters `EnvironmentAdapter.cs`, `ConsoleAdapter.cs`, `Ed25519SignatureService.cs`, and `BuildInfoReader.cs`. `SystemServices.cs` is the only one that names a `AgentGuard.CrossPlatform` type, so once it leaves, Boundaries needs no CrossPlatform reference at all.

Each of the four adapter factories is declared `internal static`, which is why Engine needs internal access to Boundaries after the move. `SystemServices.Create()` is declared `public static` on an internal sealed class, so its effective visibility is already internal.

`SystemServices.Create()` has two callers: `src/AgentGuard.Cli/Program.cs:62` and `tests/AgentGuard.TestHelpers/SystemServicesBuilder.cs`, which calls it at line 77 in `Real()` and again at line 311 to read the real temp root once. Both files reach it through `using AgentGuard.Boundaries`, and both need only that using changed.

`AgentGuard.Engine` declares no type named `SystemServices`, `EnvironmentAdapter`, `ConsoleAdapter`, `Ed25519SignatureService`, `BuildInfoReader`, `CrossPlatformAdapters`, `PlatformServices`, `FileSystem`, `GuidFactory`, or `FileInfoFactory`, so the move introduces no name collision. Engine already declares types in namespace `AgentGuard.Engine` at its root, so the relocated file fits the existing layout.

`AgentGuard.Engine` declares no type implementing `ISystemServices`, `IFileSystem`, or `IPlatformServices`, so the relocated container becomes the only container implementer in the Engine compilation and AG0022 stays satisfied.

`tests/AgentGuard.Tests` reaches the Boundaries adapters only through the container that `SystemServicesBuilder` builds. Its five `Boundaries*Tests.cs` files name the internal adapter types in comments only, never in code, so no test loses access when the Boundaries grants move.

`eng/signing.props` stamps the public key onto every `InternalsVisibleTo` item with a blanket update, so the new grants need no signing change.

`eng/coverage-gate.sh` gates one aggregate covering `AgentGuard.Engine`, `guard`, `AgentGuard.CrossPlatform`, and `AgentGuard.Boundaries` together, and its counted-set self-check compares that list against the `<Include>` set in `.runsettings`. Moving lines from Boundaries to Engine leaves both the aggregate number and the counted set unchanged.

`MemberUseScanner.RegisterForAssembly` returns without registering when the compilation is not the named assembly, so AG0023 and AG0029 can each call it twice — once for Boundaries and once for Engine — and at most one registration takes effect per compilation.

`AnalyzerRunner.RunWithReferenceAsync` compiles one subject assembly against one reference assembly, each with a caller-supplied assembly name. That is the shape the new AG0040 and AG0041 tests need, so no runner change is required.

`CallIntoOtherCrossPlatformType_FromNonBoundariesAssembly_IsNotReported` and `CallOtherPerOsMember_FromNonBoundariesAssembly_IsNotReported` already build their subject compilation as `AgentGuard.Engine` and already place the prohibited call inside an ordinary class method rather than inside `Create()`. Reversing their assertion needs no change to their input.

`grep -rn "AgentGuard\.Engine" analyzers/AgentGuard.Analyzers/*.cs` returns one documentation comment in `PresenceContracts.cs` and nothing else, so there is no existing constant for the Engine assembly name.

The analyzer class names `BoundariesToCrossPlatformOneDoorAnalyzer` and `BoundariesToPerOsOneDoorAnalyzer` appear only in their own source files, their two test files, and the Notes column of `AnalyzerReleases.Unshipped.md`. Renaming them touches those places and no others.

Nothing in the repository runs `dotnet pack`, so the `PackageId` on `AgentGuard.Engine` has no consequence for the new references.

## Consequences of the approved design that explorers should not reopen

Once Boundaries stops referencing CrossPlatform, the Boundaries half of AG0023 and AG0029 can no longer fire against production code, because a Boundaries file can no longer name a CrossPlatform or per-OS type. The contract keeps those gates deliberately, so that re-adding the reference does not silently re-open the door. Their existing tests build synthetic Boundaries compilations and continue to prove the restriction.

`tests/AgentGuard.Tests` already holds an internal-access grant to Engine and is deliberately not gated by AG0041. After the move it can therefore name the relocated `SystemServices` type, which it could not do while the type was in Boundaries. AG0017 still reports any call to `SystemServices.Create()` from there, because AG0017 pins that call to the `Program` composition method and the test `SystemServicesBuilder` in every compilation.

Engine's compilation gains a transitive view of BouncyCastle through the Boundaries package reference. AG0011 owns the raw BouncyCastle types to the Boundaries assembly, so an Engine file that used one would be reported.

## Current checkpoint

IMPLEMENT has returned its result. READINESS identified missing scope and authorization records; REFUTE was launched and then stopped. `contract.md`, Decisions / Recorded conversation authorizations, now retains the existing architecture acceptance, correction-round permissions, DRY-only review permission, and checkpoint/staging authorization. The contract's Surfaces and Reuse ledger now include the two shared analyzer owners omitted from those lists. Historical stage and reviewer outputs retain their original status; they are not current approval records. No final implementation-review verdict is claimed here.

## Areas covered by the completed GROUND stage

Four areas, one explorer each.

**container-and-composition.** The exact current shape of `SystemServices`, the services it assembles and in what order, both call sites, the wiring and adapter tests that cover it, and everything that would have to change for the type to compile and behave identically in `AgentGuard.Engine`. Establish whether any behavior, not only any reference, depends on the container's declaring assembly.

**project-graph-and-grants.** The current project references and internal-access grants across Abstractions, Boundaries, CrossPlatform, the three per-OS projects, Engine, the CLI, TestHelpers, and the three test projects. Establish how `PlatformImplementation.targets` selects a per-OS implementation, how `AgentGuardPlatformRid` is threaded to each importer, and what each consumer of Engine inherits once Engine imports that file. Establish the effect on the self-contained cross-RID publish, on the signing transform, and on the coverage counted set.

**analyzer-fence.** Every rule anchored on the Boundaries assembly, on the `SystemServices` type, or on `CompositionPoint`, and what each one reports before and after the relocation. Establish which rules the contract's list covers and whether any rule outside that list changes behavior when the container's declaring assembly changes. Establish what `MemberUseScanner` does and does not observe, so the coverage of each retargeted rule is stated in terms of what it can actually see. Establish every existing registration that resolves a written type name or reads a carried type, including the declared-type registrations inside AG0006 and the cast and pattern coverage inside AG0007, so DESIGN can name one owner for the two lenses that AG0041, AG0023, and AG0029 now share.

**analyzer-test-infrastructure.** The current fixture and helper structure in `analyzers/AgentGuard.Analyzers.Tests`, including `AnalyzerRunner`, its compiler-error validation, and `SharedAnalyzerSources`. Establish where a fake Engine compilation fixture belongs so that the four test classes needing one share a single owner, and establish that the existing runner supports every compilation shape the new acceptance checks require without being changed.

## New capabilities for the prior-art ledger

Three, taken from the contract's Rules to add.

**call-site-restricted-factory-set** — permitting a named set of method identities to be called only from one named method.

**internal-surface-one-door** — rejecting a reference to any internal type or member of one assembly from a named consumer, except one permitted symbol.

**type-reference-lens** — resolving every written type name and every carried type an operation declares, so a rule can judge a type reference that is not a member access. AG0041 needs it, and AG0023 and AG0029 now need it too, so the ledger rules where it lives rather than assuming a new file.

## Out of scope

The static-class conversions, including the `ISystemServices` parameters that Engine methods take today, are the later work of issue #74 and are not touched here.

The completed analyzer-runner repair is a prerequisite, not work in this run. `AnalyzerRunner.cs` and `AnalyzerRunnerTests.cs` stay unchanged, and unrelated fixture repairs are out of scope. If a required change reaches those files, the run stops and reports.

No adapter moves out of Boundaries, no service interface or container member is added, no diagnostic is suppressed, and no test is weakened or deleted. Nothing is staged, committed, or pushed, and no pull request is opened or merged under this contract.
