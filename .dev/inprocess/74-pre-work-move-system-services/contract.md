# Move SystemServices into Engine

## Decisions

### Approved

Tim approved the L2 exception for the following scope, presented as written:

- Move `SystemServices` into Engine, leaving all four adapters in Boundaries.
- Update assembly references, factory access, and the CLI/test-builder callers.
- Retarget the three affected analyzer rules and test that prohibited access remains prohibited.
- Preserve the existing `ISystemServices` interface and runtime behavior.
- Run the opening and closing build/tests and L2 reviews.
- Make no static-class conversions, new containers, commits, or pushes.

Tim approved the relocation itself, presented as written:

- Move `SystemServices` to `src/AgentGuard.Engine/SystemServices.cs`, in namespace `AgentGuard.Engine`, preserving its internal sealed shape, private constructor, and static `Create()`. Engine may reference Boundaries and receive internal access to the four existing boundary factories. The four adapters stay in Boundaries.
- Keep the move as preparatory work within the existing static-class-evaluation issue and its current folder. Do not create another issue, duplicate the folder, or expand this contract to cover the static-class conversions.

Tim approved the boundary-access restriction, presented as written:

- Keep this contract limited to relocating the existing construction code.
- The new boundary-access rule permits only `EnvironmentAdapter.Create()`, `ConsoleAdapter.Create()`, `Ed25519SignatureService.Create()`, and `BuildInfoReader.Create()`, and only from `AgentGuard.Engine.SystemServices.Create()`. Match the actual method identities, including their declaring assembly and type. Returning a service interface is not sufficient permission. A future factory requires an explicit rule change.
- AG0023 and AG0029 permit Engine's existing platform-construction calls only from that same `SystemServices.Create()` method. No access is granted to another method in `SystemServices` or to another Engine class. The existing restrictions outside Engine are preserved.
- A shared analyzer helper may implement these checks, and must not broaden the permitted calls.

Tim approved the two test assertion changes, presented as written:

- Change the two named tests to expect the appropriate diagnostic and rename them to describe the prohibited Engine calls. Keep their prohibited-call inputs intact. Add separate positive tests proving that the permitted factory calls from the exact `SystemServices.Create()` method remain accepted. This approval covers those two assertion changes only; the agreed analyzer design is unchanged.

Tim approved the inbound grant restriction, presented as written:

- An additional rule restricts the new CLI and TestHelpers grants to `SystemServices.Create()` only. Access to public Engine APIs is preserved. The rule covers internal type references and member access, with positive and negative analyzer tests. The existing `AgentGuard.Tests` grant is unchanged.

Tim approved the reference and grant moves, presented as written:

- Engine imports the existing `PlatformImplementation.targets`; Boundaries stops importing it. The existing platform-selection behavior is preserved.
- Engine references Boundaries. The CLI drops its direct Boundaries reference, and TestHelpers references Engine instead of Boundaries.
- Boundaries replaces its CLI and TestHelpers internal-access grants with a grant to Engine.
- CrossPlatform and the three per-OS projects replace their Boundaries grant with an Engine grant.
- Engine adds grants to the CLI and TestHelpers, constrained by the approved analyzer rule.

Tim approved the analyzer support files and the method-level check, presented as written:

- The three analyzer support files are `EngineAssembly.cs`, `BoundaryAdapterFactories.cs`, and `OneDoorRule.cs`. The boundary-factory list must contain exactly the four approved method identities. Shared checking code must preserve each rule's specific restrictions.
- The method-level check in `CompositionPoint` identifies only the static `Create()` method on `AgentGuard.Engine.SystemServices` in the Engine assembly, and is used for the factory-access rules. AG0015 retains its existing type-level check, updated for the relocated class.

Tim approved AG0041 and the fourth support file, presented as written:

- AG0041 and extracting `DeclaredTypeScanner.cs` are approved, preserving AG0006's behavior and existing tests.
- Before the contract locks, establish how the rule covers internal type references beyond declarations, including `typeof`, and ensure the `SystemServices` exception permits only its use for the approved factory call. Do not narrow the approved restriction to what the existing scanners happen to cover.
- The claim that fail-closed requires two diagnostics is wrong. Any diagnostic-count requirement needs its own justification.

### Proposed — awaiting Tim's approval

- **One assertion added to the shared analyzer test runner, `analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs`.** `RunAnalyzerAsync` returns only `GetAnalyzerDiagnosticsAsync()`, so a compiler error in a fixture never surfaces and an `Assert.Empty` accept case passes whether the analyzer stayed quiet or the compiler rejected the code first. Assert that the compilation produced no compiler errors before returning the analyzer diagnostics. This strengthens every analyzer test that uses the runner, and it can turn an existing test red if any current fixture has a latent compile error; whether one does is unknown until it runs, and Acceptance 18 makes that an explicit check. The alternative is to leave the runner alone and duplicate its two-compilation setup inside the AG0041 test file.

## Rules to add

**AG0040 — Engine may call Boundaries only through the four permitted adapter factories, and only from `SystemServices.Create()`.** Gated to the `AgentGuard.Engine` compilation. A call into the `AgentGuard.Boundaries` assembly is a build error unless both of these hold: the called member is one of the four permitted method identities, and the call site is directly inside `AgentGuard.Engine.SystemServices.Create()`. The four permitted identities are the static method named `Create`, declared in assembly `AgentGuard.Boundaries`, on each of the types `EnvironmentAdapter`, `ConsoleAdapter`, `Ed25519SignatureService`, and `BuildInfoReader` in namespace `AgentGuard.Boundaries`. Each identity is matched on assembly, namespace, type name, method name, and staticness together. A method's return type grants nothing. A fifth factory in Boundaries is rejected until this rule is changed to name it. The message names every condition that failed — the called member when it is not one of the permitted identities, the call site when it is not inside `AgentGuard.Engine.SystemServices.Create()`, or both when both failed — naming the specific member and the required site.

**AG0041 — the CLI and TestHelpers may reach only the approved `SystemServices.Create()` call among Engine internals.** Gated to the `guard` and `AgentGuard.TestHelpers` compilations. Inside those two, reaching any internal type or internal member declared in `AgentGuard.Engine` is a build error, in every position the language allows, with one exception.

The one exception is the `SystemServices` type named as the receiver of an invocation of its static `Create` method. `SystemServices` in any other position is reported: as a declared type, in `typeof` or `nameof`, in a cast or a pattern, as a generic argument, in a base list, or through a using alias. Any member of `SystemServices` other than `Create` is reported.

Coverage is established by two complementary lenses, not by a list of syntax positions:

- Every name the source writes, resolved through the semantic model to the symbol it binds to. This catches an internal Engine type wherever its name appears — `typeof`, `nameof`, a cast, an `is` or `case` pattern, a generic type argument, a generic constraint, a base or interface list, an attribute argument, a using alias, an array or pointer element, and every declaration position — and catches an internal Engine member wherever it is named.
- The types a symbol or operation carries without naming them: the declared type of a field, a property, a method return, a method parameter, and a local including an inferred `var`, and the return type of an invoked member. This catches an internal Engine type reached through a public member rather than written out.

Each lens tests every type reachable through generic arguments, array elements, and pointer targets, so an internal Engine type nested inside another type is caught.

Every public Engine API stays reachable and is never reported. The `AgentGuard.Tests` compilation is not gated, so its existing grant is untouched.

The contract sets no required diagnostic count. The two lenses are separate Roslyn registrations and each reports what it finds; no cross-lens deduplication is built, because deduplication is machinery this contract does not ask for. A single statement can therefore produce more than one diagnostic, which is a fact the tests must accommodate rather than a requirement they must prove.

**AG0023 — retarget and add the caller constraint, keeping the existing restriction outside Engine.** Keep the AG0023 identifier and keep `CrossPlatformAdapters` as the one door. Keep the existing gate on the `AgentGuard.Boundaries` compilation with its existing door-only behavior, so nothing outside Engine is loosened. Add a gate on the `AgentGuard.Engine` compilation which additionally requires the call site to be directly inside `AgentGuard.Engine.SystemServices.Create()`. Rewrite the descriptor title, message, and description so they name the calling assembly and every condition that failed, on the same terms as AG0040, so a call that is the door but sits outside `SystemServices.Create()` reports that it is outside `SystemServices.Create()` rather than reporting that it is not the door. Rename the analyzer class and its file to `OneDoorIntoCrossPlatformAnalyzer`.

**AG0029 — retarget and add the caller constraint, keeping the existing restriction outside Engine.** Keep the AG0029 identifier and keep `PlatformServices.Create()` as the one door. Apply the same two gates, the same caller requirement, and the same descriptor rewrite as AG0023. Rename the analyzer class and its file to `OneDoorIntoPerOsAnalyzer`.

**The caller test takes the narrowest reading.** "Directly inside `AgentGuard.Engine.SystemServices.Create()`" means the call site's own containing method symbol is that method, matched on assembly, namespace, type name, method name, and staticness together. A call written inside a lambda or a local function nested in `Create()`'s body is reported, not permitted. `Create()`'s body is entirely top-level statements today and this contract forbids changing its shape, so nothing legitimate is refused. Widening this later takes an explicit rule change, on the same footing as naming a fifth factory.

**AG0015 — retarget the construction site.** `CompositionPoint` resolves the clock construction site as `SystemServices` in `AgentGuard.Engine` rather than in `AgentGuard.Boundaries`. The test `SystemServicesBuilder` stays the second construction site. AG0015 keeps the type-level test; the method-level test is only for the one-door rules.

AG0011 and AG0020 keep their existing anchor on the `AgentGuard.Boundaries` assembly, because the four adapters that own the raw environment, console, signature, build-info, and temp-path primitives stay there.

AG0017 needs no change. It identifies the container factory by its `ISystemServices` return type from any assembly, and its two legal callers are unchanged.

## The standard / what we're building

`SystemServices` is built in `AgentGuard.Engine`, in namespace `AgentGuard.Engine`, internal and sealed, with a private constructor and a static `Create()` that assembles the same eight services in the same order from the same factories. `EnvironmentAdapter`, `ConsoleAdapter`, `Ed25519SignatureService`, and `BuildInfoReader` stay in `AgentGuard.Boundaries`. The CLI and the test builder reach the relocated factory. `SystemServices.Create()` is the only method in Engine that may call into Boundaries, CrossPlatform, or a per-OS assembly, and the only Engine internal the CLI and the test builder may reach. No service is added to or removed from the container, and no static class is converted.

## Success definition

ALL criteria met AND no errors in the system as a result of the change.

The relocation preserves behavior, the service surface, adapter ownership, platform selection, and enforcement against unauthorized construction and primitive access. The only Engine calls permitted into Boundaries, CrossPlatform, and the per-OS assemblies are the named doors from `SystemServices.Create()`, proved by analyzer tests that reject every other caller and every unnamed factory. The only Engine internal the CLI and TestHelpers may reach is `SystemServices.Create()`, with every public Engine API still reachable and the `AgentGuard.Tests` grant untouched.

## Surfaces

- `src/AgentGuard.Boundaries/SystemServices.cs` moves to `src/AgentGuard.Engine/SystemServices.cs`.
- `src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj` — drop the `PlatformImplementation.targets` import, replace the two internals grants with `AgentGuard.Engine`.
- `src/AgentGuard.Engine/AgentGuard.Engine.csproj` — add the Boundaries project reference, add the `PlatformImplementation.targets` import, add internals grants to `guard` and `AgentGuard.TestHelpers`.
- `src/AgentGuard.Cli/AgentGuard.Cli.csproj` — drop the direct Boundaries project reference.
- `src/AgentGuard.CrossPlatform/AgentGuard.CrossPlatform.csproj` and the three `src/AgentGuard.CrossPlatform.{MacOS,Linux,Windows}/*.csproj` — retarget the `AgentGuard.Boundaries` internals grant to `AgentGuard.Engine`.
- `tests/AgentGuard.TestHelpers/AgentGuard.TestHelpers.csproj` — retarget the project reference from Boundaries to Engine.
- `src/AgentGuard.Cli/Program.cs` and `tests/AgentGuard.TestHelpers/SystemServicesBuilder.cs` — the `using AgentGuard.Boundaries` becomes `using AgentGuard.Engine`.
- `analyzers/AgentGuard.Analyzers/CompositionPoint.cs` — the construction-site identity moves to Engine, and the method-level caller test is added.
- `analyzers/AgentGuard.Analyzers/BoundariesToCrossPlatformOneDoorAnalyzer.cs` and `analyzers/AgentGuard.Analyzers/BoundariesToPerOsOneDoorAnalyzer.cs` — the Engine gate, the caller test, the descriptor text, and the rename to `OneDoorIntoCrossPlatformAnalyzer.cs` and `OneDoorIntoPerOsAnalyzer.cs`.
- `analyzers/AgentGuard.Analyzers/EngineAssembly.cs`, `analyzers/AgentGuard.Analyzers/BoundaryAdapterFactories.cs`, `analyzers/AgentGuard.Analyzers/OneDoorRule.cs`, `analyzers/AgentGuard.Analyzers/DeclaredTypeScanner.cs`, `analyzers/AgentGuard.Analyzers/EngineToBoundariesOneDoorAnalyzer.cs`, and `analyzers/AgentGuard.Analyzers/EngineInternalsOneDoorAnalyzer.cs` — new files.
- `analyzers/AgentGuard.Analyzers/ContractConcreteTypeMustNotBeReferencedAnalyzer.cs` — route its four declared-type registrations through `DeclaredTypeScanner`.
- `analyzers/AgentGuard.Analyzers/AnalyzerReleases.Unshipped.md` — the AG0040 and AG0041 rows.
- `analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs` — the no-compiler-error assertion.
- `analyzers/AgentGuard.Analyzers.Tests/BoundariesToCrossPlatformOneDoorAnalyzerTests.cs` and `analyzers/AgentGuard.Analyzers.Tests/BoundariesToPerOsOneDoorAnalyzerTests.cs`, renamed to match their analyzers, `analyzers/AgentGuard.Analyzers.Tests/GuardedConstructionAnalyzerTests.cs`, `analyzers/AgentGuard.Analyzers.Tests/TimeMustUseTimeProviderAnalyzerTests.cs`, and two new files, `analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs` and `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs`.
- `tests/AgentGuard.Tests/BoundariesSystemServicesWiringTests.cs`, `BoundariesConsoleAdapterTests.cs`, `BoundariesEnvironmentAdapterTests.cs`, `BoundariesBuildInfoTests.cs`, and `BoundariesEd25519SignatureServiceTests.cs` — comments only, where they name the assembly the container lives in.
- Comments on changed ownership and access in the listed source and project files. Historical work records stay unchanged.

## Reuse ledger

AG0040 introduces one capability: permitting a named set of method identities to be called only from one named method.

- `reuse analyzers/AgentGuard.Analyzers/WellKnownType.cs:66` — `IsInAssembly` already matches a type on namespace, name, and compiled assembly together. Every one of the four permitted identities and the permitted caller is matched through it, so a same-named type in another namespace or assembly cannot pose as a door.
- `reuse analyzers/AgentGuard.Analyzers/MemberUseScanner.cs:61` — `RegisterForAssembly` already gates a member-use scan to one source assembly. AG0023 and AG0029 call it twice, once for `AgentGuard.Boundaries` and once for `AgentGuard.Engine`; AG0040 calls it once. It is used unchanged.
- `reuse analyzers/AgentGuard.Analyzers/PlatformFactory.cs:41` — `Is(IMethodSymbol)` is the existing shape for a door identity matched as static plus method name plus type identity. `BoundaryAdapterFactories` follows it, and AG0029 keeps using `PlatformFactory` for its own door.
- `extract analyzers/AgentGuard.Analyzers/BoundariesToCrossPlatformOneDoorAnalyzer.cs:69` and `analyzers/AgentGuard.Analyzers/BoundariesToPerOsOneDoorAnalyzer.cs:61` — the two existing `Inspect` bodies are the same target-filter, door-test, report sequence. Extract it into `OneDoorRule` with the caller test added, and route all three rules through it.
- `extract analyzers/AgentGuard.Analyzers/CompositionPoint.cs:94` — `EnclosedBy` already walks a symbol's enclosing types against a predicate. The method-level caller test is added to this same file so the composition-point identity has one owner.
- `new analyzers/AgentGuard.Analyzers/EngineAssembly.cs` — no constant for the `AgentGuard.Engine` assembly name exists; `grep -rn "AgentGuard\.Engine" analyzers/AgentGuard.Analyzers/*.cs` returns only a documentation comment in `PresenceContracts.cs`.
AG0041 introduces one further capability: rejecting a reference to any internal type or member of one assembly from a named consumer, except one permitted symbol.

- `reuse analyzers/AgentGuard.Analyzers/MemberUseScanner.cs:43` — `Register` already surfaces every member use. AG0041 uses it for the member-access half, unchanged.
- `reuse analyzers/AgentGuard.Analyzers/TypeTree.cs:25` — `Any` already walks a declared type through its generic arguments, array elements, and pointer targets. AG0041 passes its own leaf test into it for the declared-type half.
- `extract analyzers/AgentGuard.Analyzers/ContractConcreteTypeMustNotBeReferencedAnalyzer.cs:49-52` — the four declared-type registrations (field, property, method, local) live only inside AG0006. Extract them into `DeclaredTypeScanner` and route both AG0006 and AG0041 through it, leaving AG0006's diagnostics and tests unchanged.
- `new analyzers/AgentGuard.Analyzers/EngineInternalsOneDoorAnalyzer.cs` — no analyzer resolves name nodes generally; `grep -rn "SyntaxKind.IdentifierName\|SyntaxKind.GenericName\|SyntaxKind.QualifiedName\|SyntaxKind.TypeOfExpression\|SyntaxKind.NameOf" analyzers/AgentGuard.Analyzers/*.cs` returns nothing. Seven analyzers register `RegisterSyntaxNodeAction` for specific kinds, and AG0007 (`ContractConcreteTypeMustNotBeCastToAnalyzer.cs:49-52`) is the closest precedent, covering casts, `as`, and the two pattern forms for a different type set. Follow its `Check(context, TypeSyntax)` shape. The lens has one consumer, so it lives inside the rule rather than in a shared owner, and is extracted when a second rule needs it.

- `new analyzers/AgentGuard.Analyzers/BoundaryAdapterFactories.cs` — no analyzer matches any of the four adapter types as an identity today; `grep -rn "EnvironmentAdapter\|ConsoleAdapter\|Ed25519SignatureService\|BuildInfoReader" analyzers/AgentGuard.Analyzers/*.cs` returns only three prose mentions of `EnvironmentAdapter` in `TempRootBackDoorAnalyzer.cs`, in documentation comments and a diagnostic description.

## What to do

1. Run `make build` and `make test` and retain the exact commands and output before changing any implementation or test.
2. Write the analyzer tests for AG0040, AG0041, and the retargeted AG0015, AG0023, and AG0029, including every rejection case in Acceptance, and observe their expected failures before changing the analyzer implementations.
3. Add `EngineAssembly.cs`, `BoundaryAdapterFactories.cs`, and `OneDoorRule.cs`; add the method-level caller test to `CompositionPoint.cs` and retarget its construction-site identity to Engine; route AG0023 and AG0029 through `OneDoorRule` with both source gates, the caller test, the descriptor rewrite, and the renames; add `EngineToBoundariesOneDoorAnalyzer.cs`; add the AG0040 row to `AnalyzerReleases.Unshipped.md`.
4. Add `DeclaredTypeScanner.cs` and route AG0006's four declared-type registrations through it without changing AG0006's diagnostics or its tests. Add `EngineInternalsOneDoorAnalyzer.cs` carrying both the written-name lens and the carried-type lens, and add the AG0041 row to `AnalyzerReleases.Unshipped.md`.
5. Move `SystemServices.cs` to `src/AgentGuard.Engine/`, change its namespace to `AgentGuard.Engine`, and leave its shape, service instances, and construction order exactly as they are.
6. Apply every project reference, platform-targets import, and internals grant change listed in Surfaces, and change the two `using AgentGuard.Boundaries` statements in `Program.cs` and `SystemServicesBuilder.cs`.
7. Run the acceptance checks and the L2 READINESS, Prove-It, DRY, and Lie-catcher reviews. Keep their outputs in this folder.

## What the agent MAY do

Change the construction, project, analyzer, test, and directly affected comment surfaces listed above. Use the existing factories and test infrastructure.

## What the agent MUST NOT do

Do not convert static classes, add service interfaces or container members, move the four adapters, change application behavior, suppress diagnostics, weaken or delete tests, stage, commit, push, or open a PR. Do not reverse any test assertion other than the two named in Acceptance 7. Do not permit any call beyond the four named factory identities and the two existing platform doors, and do not permit any caller other than `AgentGuard.Engine.SystemServices.Create()`. Do not loosen AG0023 or AG0029 where they apply outside Engine. Do not permit the CLI or TestHelpers to reach any Engine internal other than the approved `SystemServices.Create()` invocation, and do not gate the `AgentGuard.Tests` compilation. Do not narrow AG0041's coverage to the positions an existing scanner already handles; a position the language allows and the rule misses is a defect, not a boundary. Do not change AG0006's diagnostics or its tests. Do not create a second work folder or a second issue. Stop and report any required scope change. Ask before work estimated over ten minutes, with the estimate.

## Acceptance

1. `make build` and `make test` pass before and after the implementation; retain exact commands and output.
2. `analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs` proves AG0040 accepts each of `EnvironmentAdapter.Create()`, `ConsoleAdapter.Create()`, `Ed25519SignatureService.Create()`, and `BuildInfoReader.Create()` when called from `SystemServices.Create()` in an `AgentGuard.Engine` compilation.
3. The same test file proves AG0040 reports each of these: one of the four factory calls made from another method on `SystemServices`; one of the four made from a different Engine class; a call to a fifth Boundaries factory that returns a service interface but is not one of the four named identities; a call to a `Create` method on a type with one of the four names declared in a different namespace; and a call to a `Create` method on a type with one of the four names declared in an assembly other than `AgentGuard.Boundaries`.
4. The same test file proves AG0040 reports a non-factory Boundaries member called from `SystemServices.Create()`, and reports nothing at all for any of these calls made from a compilation that is not `AgentGuard.Engine`.
5. The AG0023 and AG0029 tests prove their door is accepted from `SystemServices.Create()` in an `AgentGuard.Engine` compilation, and reported when called from another method on `SystemServices`, from another Engine class, and when the door type is a same-named decoy in another namespace or assembly. Each test asserts the reported message names every condition that failed: the call site alone when the call is the door but the site is wrong, the member alone when the site is right but the call is not the door, and both when both failed. No test asserts a message that says a call to the door is not the door.
6. The three existing tests that exercise an `AgentGuard.Boundaries` compilation — `CallIntoAdapterFactory_FromBoundaries_IsNotReported`, `CallIntoOtherCrossPlatformType_FromBoundaries_IsReported`, and `CallOtherPerOsMember_FromBoundaries_IsReported`, together with `CallPlatformServicesCreate_FromBoundaries_IsNotReported` and `CallOldPlatformDoor_FromBoundaries_IsReported` — still pass with their current expectations, proving the restriction outside Engine was not loosened.
7. `CallIntoOtherCrossPlatformType_FromNonBoundariesAssembly_IsNotReported` and `CallOtherPerOsMember_FromNonBoundariesAssembly_IsNotReported` each build an `AgentGuard.Engine` compilation and today assert no diagnostic. Each keeps its prohibited-call input byte for byte, is renamed to describe the prohibited Engine call it makes, and asserts the diagnostic instead. Beside each, a new positive test makes the same rule's permitted door call from the exact `AgentGuard.Engine.SystemServices.Create()` method and asserts no diagnostic. These are the only two assertions either rule's tests may reverse.
8. `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs` proves AG0041 accepts an invocation of `SystemServices.Create()` and accepts use of a public Engine type and a public Engine member, in both a `guard` and an `AgentGuard.TestHelpers` compilation. Its fake Engine source declares every type and member the rule governs as `internal` and carries `[assembly: InternalsVisibleTo("guard")]` and `[assembly: InternalsVisibleTo("AgentGuard.TestHelpers")]`, so the accepted call is genuinely accessible and the assertion turns on the analyzer rather than on an inaccessibility error.
9. The same test file proves AG0041 reports each of these in both compilations. Through the written-name lens: an internal Engine type in `typeof`, in `nameof`, in a cast, in an `is` pattern, as a generic type argument, in a base list, and through a using alias; and an internal Engine member named anywhere other than the approved call. Through the carried-type lens: an internal Engine type as a field type, a property type, a method return type, a method parameter type, a local variable type, the inferred type of a `var` local, and a type argument nested inside a generic or array type. It proves nothing is reported in an `AgentGuard.Tests` compilation, so that grant is unchanged.
10. The same test file proves the `SystemServices` exception is no wider than the approved call: `SystemServices` is reported in `typeof`, in `nameof`, as a declared type, in a cast, in a pattern, as a generic argument, in a base list, and through a using alias, and a member of `SystemServices` other than `Create` is reported. Only the invocation of `SystemServices.Create()` is accepted.
11. Every AG0041 test asserts the diagnostics it actually expects by count and message. No AG0041 test uses `Assert.Single` on an input that can trip both lenses.
12. AG0040, AG0023, and AG0029 each report their own permitted door call when it is written inside a lambda nested in `Create()`'s body, and again when it is written inside a local function nested in `Create()`'s body.
13. AG0006's behavior is unchanged after the `DeclaredTypeScanner` extraction: `analyzers/AgentGuard.Analyzers.Tests/ContractConcreteTypeMustNotBeReferencedAnalyzerTests.cs` passes with every existing test and assertion unmodified.
14. The AG0015 test proves a direct `TimeProvider.System` read is accepted inside `SystemServices` in an `AgentGuard.Engine` compilation and reported elsewhere in Engine.
15. The existing wiring tests and the four boundary adapter test classes pass unchanged in behavior. `src/AgentGuard.Abstractions/Contracts/ISystemServices.cs` and all four boundary adapter implementations have no behavioral changes, established by `git diff` review.
16. For each of `osx-arm64`, `linux-x64`, and `win-x64`, run `dotnet msbuild src/AgentGuard.Engine/AgentGuard.Engine.csproj -getItem:ProjectReference -p:AgentGuardPlatformRid=<rid>`. Each result selects exactly the matching per-OS project and exactly one of them. This checks project selection, not native execution on another OS.
17. `dotnet msbuild src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj -getItem:ProjectReference` returns `AgentGuard.Abstractions` and nothing else.
18. Every analyzer test in `analyzers/AgentGuard.Analyzers.Tests` passes after the `AnalyzerRunner` assertion is added. Any fixture the assertion newly fails is fixed so it compiles; no fixture is exempted from the assertion and the assertion is not weakened.
19. `git diff --check` passes. Review `git diff` and `git status --short` against every contract boundary. Report any unavailable cross-OS execution explicitly; do not claim it ran.

## Level

L2, with Tim's explicit exception for this structural preparatory move; retain the required independent L2 reviews.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path.
