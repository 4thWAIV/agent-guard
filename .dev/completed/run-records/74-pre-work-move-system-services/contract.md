# Move SystemServices into Engine

## Decisions

### Approved

Current execution scope: complete the SystemServices relocation and its approved access restrictions. Verification uses the existing analyzer test runner, behavioral regression tests, direct helper tests where needed, and the existing L1 reviewers. This scope correction supersedes the earlier request to build architecture-policing tests: analyzer-source compilation, ownership-query infrastructure, call-graph/control-dependency analysis, registration-policing tests, grant-list synchronization checks, and a new suppression-ban analyzer are not deliverables of this relocation. Existing suppression review and approval requirements remain in force. Historical panel outputs are evidence, not additional requirements.

The relocation runs at L1. This replaces the earlier level exception without changing the approved relocation scope.

The approved scope is:

- Move `SystemServices` into Engine, leaving all four adapters in Boundaries.
- Update assembly references, factory access, and the CLI/test-builder callers.
- Retarget the three affected analyzer rules and test that prohibited access remains prohibited.
- Preserve the existing `ISystemServices` interface and runtime behavior.
- Run the opening and closing build/tests and the full L1 workflow and reviews.
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

Tim approved the shared analyzer fixture source, presented as written:

- Add `SharedAnalyzerSources.cs` to the contract's Surfaces so the four analyzer test classes share one fake Engine fixture.

Tim approved the wiring test rename, presented as written:

- Rename `BoundariesSystemServicesWiringTests`, both file and class, to `EngineSystemServicesWiringTests`. This is a mechanical consequence of moving the container, not a separate architecture decision.

Tim approved extending the two one-door rules, presented as written:

- Extend AG0023 and AG0029 to catch prohibited type references as well as member access in Engine. Preserve their existing Boundaries behavior.

Tim approved the split between the two analyzer helpers, presented as written:

- Retain the contracted `DeclaredTypeScanner` for checking declared types, and design the shared helper for explicit type references. Present the proposed new file and its responsibilities for approval.

Tim approved the shared resolution helper, presented as written:

- Put shared symbol and type resolution mechanics in a dedicated helper used by `AttributeIdentity` and `WrittenNameScanner`. Keep attribute-specific matching and fallback policy in `AttributeIdentity`.

Tim approved the documentation exclusion, presented as written:

- Exclude XML documentation references from access enforcement by their syntax context. Preserve the existing documentation links.

Tim approved the identity reuse and its decoy tests, presented as written:

- Reuse the existing assembly, namespace, type and method identity helpers and `CompositionPoint`. Include tests rejecting an identically named privileged caller in the wrong namespace or assembly.

Tim approved the three lenses, presented as written:

- Retain `MemberUseScanner`, extract declaration scanning into `DeclaredTypeScanner`, and share explicit-name scanning through `WrittenNameScanner`.

Direct pointer-type coverage remains part of testing the existing `TypeTree` traversal. Reuse `AnalyzerRunner` for behavioral fixtures and direct helper tests. Mutation tests, when needed to prove those tests, run in isolated copies.

Tim approved the scope discipline for this work, presented as written:

- Keep the enforcement focused on these responsibilities. Bring any additional framework, project-wide restriction or change to approved behavior back as an explicit proposal.

Tim approved the test assembly's compiler visibility of the relocated container, presented as written:

- Approve the main test assembly gaining compiler visibility of `SystemServices` through Engine's existing internal-access grant. AG0017 continues to prohibit direct factory calls; tests continue to construct the container through `SystemServicesBuilder`.
- Keep the existing grant and AG0041's approved scope. Add a regression test proving that a direct `SystemServices.Create()` call from an `AgentGuard.Tests` compilation reports AG0017. Apply the existing suppression-review and approval requirements.

Tim approved the documentation-exclusion discriminator, presented as written:

- Use a `CrefSyntax` ancestor check to exclude documentation references, preserving the existing documentation links.

Tim accepted RULE-PHASE and approved the ARCHITECTURE and TDD warning override, presented as written:

- Accept RULE-PHASE and proceed to ARCHITECTURE under the existing L1 contract.
- Keep the work focused on relocating `SystemServices`, updating its references and grants, and completing the approved tests. Use the completed rule work and existing design. Leave the separately identified cleanup outside this delivery.
- For ARCHITECTURE and TDD intermediate builds only, AG0015 may remain an enabled warning while `SystemServices` remains in Boundaries. Final verification must run without that override.
- Report the signature diff and architecture review results, then stop for approval before TDD. If no further signature changes are needed, report and review that result rather than inventing additional interfaces.

Tim approved the direct-dispatch exception for the TDD round, presented as written:

- Direct dispatch replaces the file's requirement to use the TDD workflow script for this round only.
- Regression tests of helpers already implemented during RULE-PHASE may pass immediately. Prove their assertions exercise the required behavior.
- Tests for behavior still awaiting IMPLEMENT must expose that missing behavior. Never manufacture RED by breaking working code or weakening expectations.
- This is a run-specific exception to the conflicting workflow and test-rail instructions. It changes neither the relocation scope nor its acceptance requirements.

IMPLEMENT handoff decision:

- For this relocation, IMPLEMENT accepts passing regression tests for helpers already implemented during RULE-PHASE under the approved TDD exception. This takes precedence over the `implement.js` instruction that every coverage entry must be red; those entries remain truthfully `red: false`. Historical failed review verdicts remain preserved, but a resolved finding is not an outstanding IMPLEMENT blocker merely because the handoff retains that verdict. Unresolved findings remain outstanding, and IMPLEMENT must clear the relocation's production build failure and satisfy the contract's final verification without warning overrides.

### Recorded conversation authorizations

These entries retain authorizations already given; they do not relabel failed reviews or claim that a later delivery received an earlier review. Dates and times below are UTC.

- On 2026-09-16 at 04:17:55, Tim instructed: "Accept the reviewed ARCHITECTURE no-signature result and proceed to TDD using:" followed by `.dev/inprocess/74-pre-work-move-system-services/tdd-worker-instructions.md`. The accepted result requires no new application signatures. The pending-acceptance text in `architecture-result.md` predates this authorization and remains historical.
- On 2026-09-16 at 16:04:23, Tim instructed: "Perform an L3 correction directly. Preserve the delivered tests’ behavior and independently reported cases." That instruction authorized the shared-constant and evidence-report correction, affected and full-suite test runs, and targeted DRY and Lie-catcher review. It also stated: "Retain the existing test-quality result where its reviewed behavior remains unchanged."
- On 2026-09-16 at 17:20:16, Tim instructed: "Complete one correction and review of the SystemServices relocation’s TDD delivery report." The instruction limited edits to `tdd-result.md` and required DRY and Lie-catcher review of that correction.
- On 2026-09-16 at 18:43:32, Tim instructed: "Resume the SystemServices relocation by correcting the missing tests." That instruction authorized the Acceptance 25 wrong-namespace caller tests and diagnostic-location assertions, with test-quality, DRY and Lie-catcher review.
- On 2026-09-16 at 19:27:55, Tim instructed: "Run one L2 correction of the three DRY violations, followed by one independent DRY-only review. This is the review scope authorized for this correction." That exception applies to the factory-call, string-comparison and wrong-namespace extractions only. `dry-fix-review-dry.md` reviews those changes; the earlier test-quality and Lie-catcher reviews remain reviews of the Acceptance 25 delivery. The full L1 implementation review remains required.
- Tim explicitly authorized the checkpoint commit and subsequent staging baseline. Codex performed the checkpoint commit `a12e58b` and staged the baseline at Tim's direction. Preserve that index during implementation and review. The prohibition on agents staging or committing without authorization does not invalidate these explicitly requested operations and grants no permission for further staging, commits or pushes.

Approval provenance: the first two dated instructions are user messages in Claude session `c96ea1f7-6b0a-4cef-b29f-2fca152f05fe`; the remaining three are user messages in session `7e128584-a478-42e9-a0ef-5669bb1f47c2`. They are recorded here after the fact, not presented as entries that existed before dispatch. The staging and checkpoint instructions were given directly to Codex in this task's conversation.

## Rules to add

The rules below enforce application access after relocation. Reuse and review establish the shared implementation; this section adds no rules about how analyzer implementations themselves are written. XML documentation references are excluded by syntax context, and the existing documentation links remain unchanged.

**The documentation exclusion is a `CrefSyntax` ancestor check**, owned by `WrittenNameScanner` and applied to the written-name lens alone. A name node with a `CrefSyntax` ancestor is a documentation reference and is not reported. The two alternatives are rejected for cause: `IsPartOfStructuredTrivia` is also true for a preprocessor-directive name such as `#if GUARDED` or `#pragma warning disable CA1822`, so it would exempt a construct this contract does not name; and a parent-kind test misses a generic cref such as `<see cref="Cache{Guarded}"/>`, whose node sits under a type-argument list, and misses an operator's cref parameter list. `MemberUseScanner` and `DeclaredTypeScanner` need no exclusion, because a cref produces no operation and declares no type. The exclusion means a documentation comment inside a gated compilation may name a guarded type or member freely; that is the accepted cost of preserving the existing documentation links unchanged.

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

**AG0023 and AG0029 catch a type reference inside Engine, not only a member access.** On the Engine gate only, each rule additionally reports a reference to a type declared in the assembly it guards — the core `AgentGuard.CrossPlatform` assembly for AG0023, a per-OS implementation assembly for AG0029 — unless the referenced type is that rule's own door type and the reference sits directly inside `AgentGuard.Engine.SystemServices.Create()`. AG0023's door type is `CrossPlatformAdapters`; AG0029's is `PlatformServices`.

Coverage is established by the same two lenses AG0041 uses, applied to each rule's own type set rather than to Engine internals. The first lens resolves every name the source writes through the semantic model to the symbol it binds to, so a guarded type is caught in `typeof`, `nameof`, a cast, an `is` or `case` pattern, a generic type argument, a generic constraint, a base or interface list, an attribute argument, a using alias, an array or pointer element, and every declaration position. The second lens reads the types a symbol or operation carries without naming them: the declared type of a field, a property, a method return, a method parameter, and a local including an inferred `var`, and the return type of an invoked member. Each lens tests every type reachable through generic arguments, array elements, and pointer targets.

The two lenses now have three consumers — AG0041, AG0023, and AG0029 — so neither may live inside AG0041. They are owned by two files, one per lens. The carried-type lens is the contracted `DeclaredTypeScanner`, extracted from AG0006's four declared-type registrations and consumed by AG0006, AG0041, AG0023's Engine gate, and AG0029's Engine gate. The written-name lens is a separate shared helper, because no analyzer resolves a general written type name today. `WrittenNameScanner` owns that half and delegates shared resolution to `SymbolResolution`. `AttributeIdentity` uses the same resolver while retaining attribute-specific matching and fallback.

`SystemServices.Create()` as it stands declares a local typed `CrossPlatformAdapters` and names `PlatformServices` on a qualified call, so both are door-type references inside `Create()` and both stay accepted. `IPlatformServices` is declared in `AgentGuard.Abstractions`, so it is in neither rule's type set.

The Boundaries gate of each rule is unchanged. It keeps its existing member-access-only behavior and its existing door-only test, so nothing outside Engine is loosened and no existing Boundaries expectation moves.

**The caller test takes the narrowest reading.** "Directly inside `AgentGuard.Engine.SystemServices.Create()`" means the call site's own containing method symbol is that method, matched on assembly, namespace, type name, method name, and staticness together. A call written inside a lambda or a local function nested in `Create()`'s body is reported, not permitted. `Create()`'s body is entirely top-level statements today and this contract forbids changing its shape, so nothing legitimate is refused. Widening this later takes an explicit rule change, on the same footing as naming a fifth factory.

**AG0015 — retarget the construction site.** `CompositionPoint` resolves the clock construction site as `SystemServices` in `AgentGuard.Engine` rather than in `AgentGuard.Boundaries`. The test `SystemServicesBuilder` stays the second construction site. AG0015 keeps the type-level test; the method-level test is only for the one-door rules.

AG0011 and AG0020 keep their existing anchor on the `AgentGuard.Boundaries` assembly, because the four adapters that own the raw environment, console, signature, build-info, and temp-path primitives stay there.

AG0017 needs no change. It identifies the container factory by its `ISystemServices` return type from any assembly, and its two legal callers are unchanged.

## The standard / what we're building

`SystemServices` is built in `AgentGuard.Engine`, in namespace `AgentGuard.Engine`, internal and sealed, with a private constructor and a static `Create()` that assembles the same eight services in the same order from the same factories. `EnvironmentAdapter`, `ConsoleAdapter`, `Ed25519SignatureService`, and `BuildInfoReader` stay in `AgentGuard.Boundaries`. The CLI and the test builder reach the relocated factory. `SystemServices.Create()` is the only method in Engine that may call into Boundaries, CrossPlatform, or a per-OS assembly, and the only Engine internal the CLI and the test builder may reach. No service is added to or removed from the container, and no static class is converted.

The existing `AnalyzerRunner` compiler-error validation and its regression tests are completed prerequisites, not implementation work in this run. Preserve their behavior: compiler errors must match the declared identifiers and occurrence counts, the default expectation is none, and referenced compilations must compile without errors. Reuse that runner for the new analyzer fixtures.

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
- `analyzers/AgentGuard.Analyzers/SymbolResolution.cs` and `analyzers/AgentGuard.Analyzers/WrittenNameScanner.cs` — shared resolution and explicit-name scanning, including the documentation exclusion.
- `analyzers/AgentGuard.Analyzers/AttributeIdentity.cs` — extract shared resolution into `SymbolResolution`, preserving attribute matching and fallback behavior.
- `analyzers/AgentGuard.Analyzers/PlatformFactory.cs` — expose the existing factory type-identity check for reuse by type-reference scanning.
- `analyzers/AgentGuard.Analyzers/TypeTree.cs` — share diagnostic type-name formatting through `Describe(ITypeSymbol?)` for the approved access rules.
- `analyzers/AgentGuard.Analyzers/CrossPlatformBoundary.cs` — share the core-assembly check and foreign-assembly namespace-decoy check through `IsCoreAssembly` and `IsSharedNamespaceDecoy`, preserving the genuine core and per-OS factory permissions.
- `analyzers/AgentGuard.Analyzers.Tests/SymbolResolutionTests.cs`, `analyzers/AgentGuard.Analyzers.Tests/WrittenNameScannerTests.cs`, `analyzers/AgentGuard.Analyzers.Tests/DeclaredTypeScannerTests.cs`, and `analyzers/AgentGuard.Analyzers.Tests/TypeTreeTests.cs` — direct tests where the existing behavioral fixtures do not already prove the shared helper's responsibility; use the existing runner, without a second compilation framework.
- `analyzers/AgentGuard.Analyzers/ContractConcreteTypeMustNotBeReferencedAnalyzer.cs` — route its four declared-type registrations through `DeclaredTypeScanner`.
- `analyzers/AgentGuard.Analyzers/AnalyzerReleases.Unshipped.md` — the AG0040 and AG0041 rows.
- `analyzers/AgentGuard.Analyzers.Tests/BoundariesToCrossPlatformOneDoorAnalyzerTests.cs` and `analyzers/AgentGuard.Analyzers.Tests/BoundariesToPerOsOneDoorAnalyzerTests.cs`, renamed to match their analyzers, `analyzers/AgentGuard.Analyzers.Tests/GuardedConstructionAnalyzerTests.cs`, `analyzers/AgentGuard.Analyzers.Tests/TimeMustUseTimeProviderAnalyzerTests.cs`, and two new files, `analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs` and `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs`.
- `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs` — the fake `AgentGuard.Engine` fixture source, spelled once and consumed by the four analyzer test classes that need it.
- `tests/AgentGuard.Tests/BoundariesSystemServicesWiringTests.cs` — renamed to `EngineSystemServicesWiringTests.cs`, with the class renamed to match, plus the comments that name the assembly the container lives in.
- `tests/AgentGuard.Tests/BoundariesConsoleAdapterTests.cs`, `BoundariesEnvironmentAdapterTests.cs`, `BoundariesBuildInfoTests.cs`, and `BoundariesEd25519SignatureServiceTests.cs` — comments only, where they name the assembly the container lives in. Their names keep the `Boundaries` prefix, because the four adapters they exercise stay in `AgentGuard.Boundaries`.
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
- `new analyzers/AgentGuard.Analyzers/EngineInternalsOneDoorAnalyzer.cs` — no analyzer resolves name nodes generally; `grep -rn "SyntaxKind.IdentifierName\|SyntaxKind.GenericName\|SyntaxKind.QualifiedName\|SyntaxKind.TypeOfExpression\|SyntaxKind.NameOf" analyzers/AgentGuard.Analyzers/*.cs` returns nothing. Seven analyzers register `RegisterSyntaxNodeAction` for specific kinds, and AG0007 (`ContractConcreteTypeMustNotBeCastToAnalyzer.cs:49-52`) is the closest precedent, covering casts, `as`, and the two pattern forms for a different type set. Follow its `Check(context, TypeSyntax)` shape. The written-name lens has three consumers — AG0041, AG0023's Engine gate, and AG0029's Engine gate — so it is its own shared helper from the start rather than living inside AG0041. It is a separate file from `DeclaredTypeScanner`, which keeps the carried-type half: the two halves resolve different things from different inputs, one walking syntax to resolve a written name and one reading an already-declared type off a symbol. Use `WrittenNameScanner` for registration and `SymbolResolution` for shared resolution; reuse the completed design evidence for their implementation.
- `reuse analyzers/AgentGuard.Analyzers/TypeTree.cs:25` for the AG0023 and AG0029 type-reference halves — `Any` walks a referenced type through its generic arguments, array elements, and pointer targets for those two rules on the same terms as for AG0041, with each rule passing its own leaf test.
- `reuse` the extracted `DeclaredTypeScanner` for the AG0023 and AG0029 carried-type halves — the same four declared-type registrations serve all three rules, so no second declared-type registration set is written.

- `new analyzers/AgentGuard.Analyzers/BoundaryAdapterFactories.cs` — no analyzer matches any of the four adapter types as an identity today; `grep -rn "EnvironmentAdapter\|ConsoleAdapter\|Ed25519SignatureService\|BuildInfoReader" analyzers/AgentGuard.Analyzers/*.cs` returns only three prose mentions of `EnvironmentAdapter` in `TempRootBackDoorAnalyzer.cs`, in documentation comments and a diagnostic description.

- `extract analyzers/AgentGuard.Analyzers/AttributeIdentity.cs:60-72` — shared symbol/type resolution lives in `SymbolResolution`; both `AttributeIdentity` and `WrittenNameScanner` call it. Preserve attribute-specific matching and fallback in `AttributeIdentity`.
- `reuse analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs` — behavioral fixtures and direct helper tests use its existing compilation and diagnostic-validation paths. There is no analyzer-source model to build for this relocation.
- `extract analyzers/AgentGuard.Analyzers/TypeTree.cs` — `Describe(ITypeSymbol?)` owns the repeated type-description formatting used by `EngineInternalsOneDoorAnalyzer` and `OneDoorRule`.
- `extract analyzers/AgentGuard.Analyzers/CrossPlatformBoundary.cs` — `IsCoreAssembly` owns the core-assembly comparison; `IsSharedNamespaceDecoy` composes the existing namespace and assembly identity checks once for the Engine gates of AG0023 and AG0029. Each rule still applies its own permitted-factory identity check and rejects an imitation of its own factory.

The two extraction entries above reconcile completed RULE-PHASE corrections with this ledger. They do not claim that a separate prior-art-ledger tool run occurred before those corrections, and do not change the approved access restrictions.

## What to do

GROUND and DESIGN have run. Continue at CONTRACT using their recorded evidence and this reduced execution scope. Reconcile only the retained relocation requirements, complete the required hidden-decision check and opening build/test bracket, then bring the contract checkpoint for approval to enter RULE-PHASE. Do not launch another GROUND or DESIGN panel to reconsider the discarded analyzer-analysis machinery.

Before implementation, reconcile this contract with the live code and recorded approvals. Report any required contract adjustment or unresolved decision for approval; do not resolve it by weakening an acceptance check or expanding scope. Do not repeat completed design stages without authorization.

Use the L1 sequence and role boundaries defined by `rails-run-a-workflow`: GROUND, DESIGN, CONTRACT, RULE-PHASE, ARCHITECTURE, TDD, IMPLEMENT, READINESS, REFUTE, GATE, REPORT. Use the actual completed GROUND and DESIGN stage records; older preparation records do not replace missing stage results. Preserve historical records rather than relabeling them as L1 outputs.

1. GROUND: prepare the relocation-scoped `conversation.md` brief for Tim's approval before launching. Ground the current assembly references, service construction, analyzer ownership, and test infrastructure. Run the stage's prior-art ledger for the rule capabilities. Do not broaden the brief to the static-class conversions or the completed runner repair.
2. DESIGN and CONTRACT: assess the existing approved design against the grounded code, complete the required SOLID and DRY design reviews, resolve the hidden-decision scan, and amend this contract for Tim's approval where necessary. Run `make build` and `make test` and retain their exact commands and output before the first mutating stage.
3. RULE-PHASE: implement the shared `SymbolResolution` and `WrittenNameScanner` responsibilities and the `AttributeIdentity`/`PlatformFactory` reuse edits as part of the approved access rules. The rule author writes the approved analyzer rules and their compliant/violating fixtures. Add `EngineAssembly.cs`, `BoundaryAdapterFactories.cs`, and `OneDoorRule.cs`; add the method-level caller test and retarget the construction-site identity; update and rename AG0023 and AG0029; add AG0040 and AG0041 and their release rows. Extract `DeclaredTypeScanner` while preserving AG0006's diagnostics and existing tests. Retain the stage's independent SOLID, DRY, and Lie-catcher reviews. Record production impact without changing the production feature to clear it in this stage.
4. ARCHITECTURE: write only the approved application signatures, or record the contract-backed no-signature result if applicable, with the required independent panel. Obtain Tim's approval of the signature diff before TDD. If intermediate builds require selected diagnostic warnings, obtain approval of the exact IDs and builds; no warning override is preapproved by this contract.
5. TDD: a separate test author completes the relocation acceptance tests and records RED-first proof, with the required test-quality, DRY, and Lie-catcher reviews. Reuse RULE-PHASE's analyzer fixtures rather than duplicating them.
6. IMPLEMENT: a separate worker moves `SystemServices.cs` to `src/AgentGuard.Engine/`, changes its namespace, and preserves its shape, service instances, and construction order. Apply the approved project references, platform-target import, internal-access grants, and caller changes. The implementation worker does not edit the analyzer rules or acceptance tests.
7. READINESS and REFUTE: run the required readiness check and all five independent L1 reviewers: Prove-It, SOLID, DRY, Laziness-auditor, and Lie-catcher. Keep exact commands, complete output, immediately captured exit codes, and stage/reviewer results in this folder. Identify the tested snapshot, including uncommitted and untracked changes.
8. Keep the shared checkout unchanged throughout every review. Each reviewer identifies the snapshot reviewed and runs destructive mutation tests only in its own isolated copy. Give these instructions directly to reviewers; do not modify workflow scripts.
9. Stop and report every stage's review result before proceeding. Any further fix round requires Tim's approval after Codex reviews the findings and proposed corrections. Do not run an automatic IMPLEMENT/REFUTE loop. After final review, wait for authorization to perform GATE and REPORT; use the normal closing build/test commands without warning overrides. No stage authorizes committing or beginning another task.

## What the agent MAY do

Change the construction, project, analyzer, test, and directly affected comment surfaces listed above. Use the existing factories and test infrastructure.

## What the agent MUST NOT do

Do not redo the completed analyzer-runner repair, change `AnalyzerRunner.cs` or `AnalyzerRunnerTests.cs`, or expand this relocation into repairs of unrelated fixtures. Stop and report if a required change reaches those files. Do not change appd specifications, close issues, merge the existing PR, remove other worktrees, or modify memory or workflow files under this contract.

Do not convert static classes, add service interfaces or container members, move the four adapters, change application behavior, suppress diagnostics, weaken or delete tests, stage, commit, push, or open a PR. Do not reverse any test assertion other than the two named in Acceptance 7. Do not permit any call beyond the four named factory identities and the two existing platform doors, and do not permit any caller other than `AgentGuard.Engine.SystemServices.Create()`. Do not loosen AG0023 or AG0029 where they apply outside Engine. Do not permit the CLI or TestHelpers to reach any Engine internal other than the approved `SystemServices.Create()` invocation, and do not gate the `AgentGuard.Tests` compilation. Do not narrow AG0041's coverage, or the AG0023 and AG0029 Engine type-reference coverage, to the positions an existing scanner already handles; a position the language allows and the rule misses is a defect, not a boundary. Do not add the type-reference coverage to either rule's Boundaries gate, and do not write a second implementation of either lens. Do not change AG0006's diagnostics or its tests. Do not create a second work folder or a second issue. Stop and report any required scope change. Ask before work estimated over ten minutes, with the estimate.

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
17. `dotnet msbuild src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj -getItem:ProjectReference` returns exactly two project references: `AgentGuard.Abstractions` and `AgentGuard.Analyzers`. The analyzer reference has `OutputItemType=Analyzer` and `ReferenceOutputAssembly=false`.
18. Every analyzer test in `analyzers/AgentGuard.Analyzers.Tests` passes using the existing compiler-error validation. `AnalyzerRunner.cs` and `AnalyzerRunnerTests.cs` are unchanged from this run's opening baseline. New relocation fixtures compile under the default empty compiler-error expectation; no new expected-compiler-error exception is introduced. Report any failure requiring work outside this relocation contract rather than starting another runner or unrelated-fixture repair.
19. `git diff --check` passes. Review `git diff` and `git status --short` against every contract boundary. Report any unavailable cross-OS execution explicitly; do not claim it ran.
20. The AG0023 tests prove the Engine gate reports a reference to a CrossPlatform-core type that is not the door, made from anywhere in an `AgentGuard.Engine` compilation, through both lenses. Through the written-name lens: in `typeof`, in `nameof`, in a cast, in an `is` pattern, as a generic type argument, in a base list, and through a using alias. Through the carried-type lens: as a field type, a property type, a method return type, a method parameter type, a local variable type, the inferred type of a `var` local, and a type argument nested inside a generic or array type. The AG0029 tests prove the same for a per-OS type that is not the door.
21. The AG0023 tests prove the Engine gate accepts a reference to `CrossPlatformAdapters` written inside `AgentGuard.Engine.SystemServices.Create()` — including as the declared type of a local, the form `Create()` uses today — and reports the same reference written in any other method of the Engine compilation. The AG0029 tests prove the same for `PlatformServices`. Both prove a reference to `IPlatformServices` is never reported, because it is declared in `AgentGuard.Abstractions` and is in neither rule's type set.
22. The AG0023 and AG0029 tests prove the Boundaries gate is unchanged by the type-reference extension: a reference to a guarded type that is not a member access, made from an `AgentGuard.Boundaries` compilation, is not reported by either rule.
23. The two lenses have exactly one owner in the analyzer source, consumed by AG0041, AG0023, and AG0029. No second implementation of either lens exists, established by reading every registration in `analyzers/AgentGuard.Analyzers` during the existing DRY review, not by building an analyzer-analysis framework.
24. Behavioral tests prove documentation references to governed types and members are allowed while the corresponding application-code references remain restricted. Assert the fixture contains a documentation-reference node so the test cannot pass by omitting documentation parsing. Existing production documentation links remain unchanged.
25. Prove the privileged caller's full identity: wrong-namespace callers are rejected by the access analyzers; direct tests of `CompositionPoint` reject a same-named method in the wrong assembly. Existing non-Engine tests continue to prove the assembly gate. Tests assert the intended diagnostics and source locations, accommodating multiple scanner reports without weakening an existing expectation.
26. Test generic guarded names and shared type resolution. The existing malformed-attribute tests prove that extracting resolution preserves recovery when symbol resolution fails but type information remains available; keep their inputs, assertions, and compiler-error expectations unchanged. Reuse these tests rather than introduce another malformed-fixture exception.
27. Direct tests of `TypeTree.Any` exercise pointer targets using compiler-created type symbols and the existing compilation helper. This proves traversal, not unsafe-source compilation. No runner change is needed.
28. A regression test proves that a direct `SystemServices.Create()` call written in an `AgentGuard.Tests` compilation reports AG0017. The fixture's `SystemServices` is declared in namespace and assembly `AgentGuard.Engine` and its `Create()` is reachable, so the assertion turns on the analyzer rather than on an inaccessibility error. This pins the one wall that remains against the main test assembly after the relocation gives it compiler visibility of the container.
29. The documentation exclusion is a `CrefSyntax` ancestor check. A direct test proves it is true for a cref naming a guarded type, including a generic cref whose node sits under a type-argument list and an operator's cref parameter list, and false for the same identifier written in code and for a preprocessor-directive name.

## Level

L1, as Tim directed for the SystemServices relocation; all required stages and independent reviews apply.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path.
