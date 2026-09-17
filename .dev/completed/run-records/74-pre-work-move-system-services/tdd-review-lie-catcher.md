# Lie-catcher review — TDD round, SystemServices relocation

## Verdict

**FAIL.** Narrowly, and not on the test work.

Six confirmed findings, all in the author's *claims about its own sources*, not in the tests:
two acceptance rows assigned to a later stage without the citation the instruction file requires,
three evidence claims the named files do not carry, two numbers contradicted by the report's own
text and its own retained log, and one report-structure miss.

Everything substantive checked out. I reproduced every number independently, re-ran four of the six
mutations in my own isolated copy, walked every test name in the 29-row acceptance mapping back to
the file it names, and verified the authority and scope boundaries. I tried hard to break the two
self-reported items and could not: both are honest and correctly scoped.

Reviewed snapshot: the ten uncommitted/untracked files under `analyzers/AgentGuard.Analyzers.Tests/`
whose SHA-256 digests in `tdd-digests.txt` I verified byte-identical in the shared checkout, before
and after this review. `HEAD` = `c1c2a1875c14b1a2fb76bf1affc04b386769dccb`, unchanged.

---

## Findings

### 1. Acceptance 16 and 17 are assigned to a later stage with no contract instruction identified

`tdd-worker-instructions.md:28` — verbatim:

> Before editing, map the Acceptance section to existing tests, missing tests and checks assigned to
> IMPLEMENT or final verification. A later-stage assignment must identify the actual contract
> instruction assigning it there.

Five criteria are classified LATER. Three identify an instruction:

- Row 1 quotes the criterion's own "before and after the implementation" and names
  `contract-opening-bracket.txt`.
- Row 15 quotes the contract ("This is a mechanical consequence of moving the container, not a
  separate architecture decision") **and** the instruction ("The class move, project-reference
  changes, internal-access grant changes and production callers belong to IMPLEMENT, not this
  author").
- Row 23's verbatim requirement itself says "during the existing DRY review".

Two do not. `tdd-result.md`, acceptance table:

- Row 16: `LATER — a final-verification command over project files IMPLEMENT has not yet changed.`
- Row 17: `LATER — same.`

Neither names a contract instruction. The instruction sentence the author quoted one row earlier —
"project-reference changes ... belong to IMPLEMENT, not this author" — and contract "What to do"
step 6 ("Apply the approved project references, platform-target import, internal-access grants, and
caller changes") both cover 16 and 17 squarely. The citation was available and was not made.

**The classification is substantively TRUE** — I proved it rather than taking it on trust. In my
isolated copy:

```
dotnet msbuild src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj -getItem:ProjectReference
  → AgentGuard.Analyzers, AgentGuard.Abstractions, AgentGuard.CrossPlatform,
    AgentGuard.CrossPlatform.MacOS          (Acceptance 17 requires Abstractions and nothing else)

dotnet msbuild src/AgentGuard.Engine/AgentGuard.Engine.csproj -getItem:ProjectReference \
  -p:AgentGuardPlatformRid=osx-arm64
  → AgentGuard.Analyzers, AgentGuard.Abstractions   (no per-OS project selected at all)
```

Both genuinely require IMPLEMENT. But the guard the instruction imposes exists precisely so a
reader does not have to run a build to find that out, and on these two rows it is missing. This is
the one finding I rule as an unmet requirement.

### 2. A command described as retained was never run

`tdd-result.md`, acceptance row 15:

> This round changed nothing under `tests/` or `src/`: `git status --short -- src tests` is empty,
> retained in `tdd-diff-check-and-status.txt`.

`tdd-diff-check-and-status.txt` records exactly two commands: `git diff --check` and an **unscoped**
`git status --short`. The scoped form was never run and its output is not in the file.

```
grep -c 'src tests' .dev/inprocess/.../tdd-diff-check-and-status.txt   → 0
```

The underlying fact is true — the retained unscoped status lists no `src/` or `tests/` path
(`grep -cE '^\s*.?.? (src|tests)/'` → 0), and my own mtime sweep found nothing under `src/` or
`tests/` touched in the round's window. But the report names a command as retained that does not
exist in the evidence, which is the exact defect class this round was told to hunt.

### 3. The evidence table attributes the digests to the wrong file

`tdd-result.md`, Evidence table:

> | `tdd-final-analyzer-tests.txt` | same, plus `shasum -a 256` of the touched files | 616 passed, `EXIT=0` |

`tdd-final-analyzer-tests.txt` contains no shasum output. The digests are in `tdd-digests.txt`,
which has **no row at all** in a table whose preamble reads "Every command below was run from the
repository root, with its complete output and its immediately captured exit code retained in this
folder." The report does name `tdd-digests.txt` correctly further down, under "Delivered files", so
this is an inconsistency rather than a fabrication — the digest file exists and all ten hashes
verify.

### 4. Two numbers contradicted by their own sources

**(a)** `tdd-result.md`, "Changed files":

> `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs` — five additions and nothing
> removed or altered: `EngineAssemblyName`, `EngineUsing`, `GatedConsumer` (moved verbatim from
> `EngineInternalsOneDoorAnalyzerTests`), `RunAgainstFakeEngineAsync`,
> `AssertHasDocumentationReference`, `SemanticModelAsync`, `TypeIn`, `OnlyNode`.

"Five additions" followed by **eight** named additions.

The "nothing removed or altered" half is TRUE and I verified it:
`git diff --stat analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs` → `707 +++++++`,
zero deletions, so no pre-existing line in that file was changed by anyone in this run.

**(b)** `tdd-result.md`, Evidence table, `tdd-run1-analyzer-tests.txt` row:

> `EXIT=1` — the first compile of the new tests, rejected by five repository analyzer rules
> (CA1849, S6966, MA0004 ×2, CA1859 ×2).

The retained log carries **four** distinct rule ids in **six** diagnostics:

```
1 error CA1849   2 error CA1859   2 error MA0004   1 error S6966
```

Neither 5 rules nor 5 diagnostics. The parenthetical enumeration in the same sentence contradicts
the count in front of it.

### 5. The two items the report raises for the reader sit below the acceptance table

`rails-real-work` rail 12: "Every report leads with what is broken, unresolved, or needs the
reader's decision, in that order." Rail 5: "Anything raised so that a later stage, another agent, or
the reader does not trip on it ... goes ABOVE the fold."

`tdd-result.md` opens with "Authoring complete. The full analyzer suite is green at 616/616". The
section titled **"One placement decision to sign off"** — which by its own title asks the reader to
act — and **"One gap, reported rather than papered over"** both sit after the 29-row acceptance
mapping, at roughly 85% of the document. Raising them at all is right; placing them there is the
rail's named failure.

### 6. The isolation verification is asserted, not captured

`tdd-mutation-proofs.txt` header and footer:

> the copy was verified byte-identical to the delivered analyzers tree before and after this run
>
> ISOLATED COPY RESTORED — identical to the delivered tree

No command, no output, no exit code for either verification. The instruction requires "complete
actual command output and immediately observed exit codes". The claim is TRUE — I confirmed the
shared checkout's ten files are byte-identical to `tdd-digests.txt` after the round — but the
evidence for it was asserted rather than retained, unlike every other command in the folder.

---

## What I tried to refute and could not

### Every number, re-derived rather than read

All runs in an isolated `rsync` copy at
`…/scratchpad/iso`, verified digest-identical to the delivered tree.

| Claim | My independent result |
| --- | --- |
| 616/616 green | `Passed! - Failed: 0, Passed: 616, Total: 616` |
| 600 baseline | Reconstructed: deleting the three new files gives **602**; minus the two new `CompositionPoint` tests = **600**. The +16 delta is exactly 8+4+2+2. |
| 221 affected cases | Sum of my own per-class counts = 8+4+2+5+15+81+33+34+20+7+12 = **221** |
| Per-class counts | All eleven match exactly: `SymbolResolutionTests` 8, `TypeTreeTests` 4, `DeclaredTypeScannerTests` 2, `WrittenNameScannerTests` 5, `EngineToBoundariesOneDoorAnalyzerTests` 15, `EngineInternalsOneDoorAnalyzerTests` 81, `OneDoorIntoCrossPlatformAnalyzerTests` 33, `OneDoorIntoPerOsAnalyzerTests` 34, `GuardedConstructionAnalyzerTests` 20, `ContractConcreteTypeMustNotBeReferencedAnalyzerTests` 7, `TimeMustUseTimeProviderAnalyzerTests` 12 |
| 12 permitted-public, 52 negative | `Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics` → **64** cases, **12** carrying `expectedSubjects: []`, leaving **52**. Corroborated structurally: `PermittedPublicReaches` 6 rows × 2 = 12, `ProhibitedReaches` 14 × 2 = 28, `ProhibitedContainerReaches` 12 × 2 = 24; 12+28+24 = 64. |

### The mutation proofs — four re-run, all reproduced

Applied and reverted one at a time in my own isolated copy; the shared checkout was never touched.

| Mutation | Author's claim | My run |
| --- | --- | --- |
| M3 — add the invoked-return registration to `DeclaredTypeScanner.RegisterDeclarations` | both `DeclaredTypeScannerTests` FAIL, AG0006's seven existing tests all still PASS | `Failed: 2, Passed: 7, Total: 9` — identical, including the `["Declared", "Declared()", "Declared()", "EngineInternal"]` diff |
| M4 — drop the assembly from `CompositionPoint`'s identity conjunction | `ContainerFactoryIdentity_RejectsTheSameNamedMethodInAnotherAssembly` FAILS, the other 14 AG0040 tests still PASS | `Failed: 1, Passed: 14, Total: 15` — identical, `Expected: False / Actual: True` |
| M6 — delete tier one from `SymbolResolution.NamedType` | `NamedType_OnAnInvocation_…` FAILS (`Sample.Guarded` for `Sample.Holder`), the two malformed-attribute classes still PASS | `Failed: 1, Passed: 40, Total: 41` — identical, same expected/actual strings |
| M5 — demote the documented fixture's `///` to `//` | all 4 behavioral documentation cases FAIL at the node assertion | `Failed: 4, Passed: 3, Total: 7` — identical |

M3, M4 and M6 are the three the author highlights, and each one's "only the direct test catches it"
claim is exactly what the pass/fail split shows.

### The two self-reported items — both honest

**Acceptance 24 was not met before this round.** Confirmed against the RULE-PHASE record, not the
author's word. The superseded assertion is in `rule-phase-fix5-output.json`:

```
Assert.Contains(\"cref\", source, StringComparison.Ordinal)
```

and `AssertHasDocumentationReference` appears there **zero** times. The replacement
(`SharedAnalyzerSources.cs:871`) is a real node assertion:

```csharp
Assert.Contains(
    CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes(descendIntoTrivia: true),
    node => node is CrefSyntax);
```

Its doc comment claims "The source is parsed exactly as `AnalyzerRunner` parses it" — I checked, and
that is true: `AnalyzerRunner.cs:54`, `:90`, `:98`, `:124` all call `CSharpSyntaxTree.ParseText(source)`
with no parse options, the same call the assertion makes. And the M5 mutation leaves the literal
text `cref` in the fixture while destroying the node, so the claim that the old text assertion would
have passed there is correct by construction. Correctly scoped: strengthening an assertion to meet a
contract acceptance requirement is this author's job, it reverses no approved decision, and no
expectation anywhere was weakened.

**The uncovered `SymbolResolution.Symbol` branch.** The guard at `SymbolResolution.cs:36`:

```csharp
return bound is INamedTypeSymbol { TypeKind: TypeKind.Error } ? null : bound;
```

The author's reasoning holds. Producing an error type from `GetSymbolInfo(...).Symbol` requires a
fixture the compiler rejects, which `tdd-worker-instructions.md:38` forbids ("do not create an
invalid unresolved-name fixture as a shortcut around the compiler-error requirement") and Acceptance
18 forbids ("no new expected-compiler-error exception is introduced"). No contract criterion
requires a direct test of it — Acceptance 26 assigns the resolution-failure proof to the existing
malformed-attribute tests, and those are intact: `InteropOnlyInCrossPlatformLibrariesAnalyzerTests.cs:198`
still declares `"CS7036"`, `:222` still declares `"CS0246", "CS0246"`,
`NativeCallbackBodyMustBeGuardedAnalyzerTests.cs:429` still declares `"CS7036"`, and neither file
appears in `git status --short`. The author states it as an uncovered branch and claims nothing.
This is a gap reported, not rationalised.

### The contract

The direct-dispatch exception is recorded at `contract.md:113–118` and is **byte-identical** to the
text forwarded to me — I diffed it, and the only difference was my own file's missing trailing
newline.

Nothing else was added to the contract by this round. `contract.md`'s mtime is `2026-09-15 22:28:39`,
before the round's first evidence capture (`tdd-baseline-analyzer-tests.txt`, `STARTED
2026-09-16T04:33:51Z` = 22:33:51 local), and a full-tree `find -newermt "2026-09-15 22:30:00"`
(excluding `.git`, `bin`, `obj`, `.dev`, `.codegraph`) returns **exactly the ten test files** and
nothing else. The rest of the contract's uncommitted diff is the accumulated GROUND / DESIGN /
CONTRACT / RULE-PHASE / ARCHITECTURE record, all predating this round.

### Authority — clean

Files modified after 22:30 local, whole tree:

```
analyzers/AgentGuard.Analyzers.Tests/GuardedConstructionAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/WrittenNameScannerTests.cs
analyzers/AgentGuard.Analyzers.Tests/OneDoorIntoCrossPlatformAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/DeclaredTypeScannerTests.cs
analyzers/AgentGuard.Analyzers.Tests/OneDoorIntoPerOsAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/SymbolResolutionTests.cs
analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/TypeTreeTests.cs
analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs
```

- No production code, analyzer source, project file, grant, workflow script or contract. Every
  analyzer source file's mtime is `17:05` or earlier.
- `AnalyzerRunner.cs` (mtime 2026-09-13 14:08) and `AnalyzerRunnerTests.cs` (2026-09-12 21:00) both
  untouched; both tracked and absent from `git status --short`.
- All ten files are contract Surfaces (`contract.md:192`, `:195`, `:196`).
- Nothing staged (`git diff --cached` empty), no commit (`HEAD` still `c1c2a18`, reflog unchanged),
  no stash, branch level with `origin/appd-1-process`.
- No expected-compiler-error declaration was introduced: `grep -n '"CS[0-9]'` over all ten returns
  nothing.
- `GuardedConstructionAnalyzerTests.cs`: `25 insertions, 0 deletions` — no assertion removed or
  weakened. `SharedAnalyzerSources.cs`: `707 insertions, 0 deletions`.

### Scope — unchanged

No expansion, no contraction. Three new files (`SymbolResolutionTests.cs`,
`DeclaredTypeScannerTests.cs`, `TypeTreeTests.cs`) match the instruction's three named assignments;
the direct `CompositionPoint` check landed in `EngineToBoundariesOneDoorAnalyzerTests.cs`, which IS
a contract-listed surface (`contract.md:195`), satisfying "in a contract-listed test surface". The
class move, project references, grants and callers were left to IMPLEMENT.

### Acceptance mapping — every EXISTING claim I checked is true

Every test name cited across all 29 rows exists in the file the row names (checked by grep over
`analyzers/AgentGuard.Analyzers.Tests/`). Spot checks against the code:

- **2–4, 12**: the four permitted identities, the five rejection shapes, the non-factory member, the
  non-Engine compilation, and the lambda/local-function cases are all present in
  `EngineToBoundariesOneDoorAnalyzerTests.cs`.
- **5**: `SharedAnalyzerSources.AssertNamesExactly` (`:928`) checks the required condition is named
  **and**, through `AssertNames` (`:943`), that no unfailed condition is named in either wording —
  which is what pins "No test asserts a message that says a call to the door is not the door".
- **8**: `EngineAssemblySource` (`:69`) declares every governed type/member `internal` and carries
  `InternalsVisibleTo("guard")` and `InternalsVisibleTo("AgentGuard.TestHelpers")` (plus
  `"AgentGuard.Tests"`, which criteria 9 and 28 need).
- **9, 10**: every clause has its own theory row or its own test; `ProhibitedReaches` 14 rows,
  `ProhibitedContainerReaches` 12 rows, base-list and using-alias as separate tests, plus
  `EveryProhibitedReach_FromTestsAssembly_IsNotReported`.
- **11**: `AssertReportsAsync` asserts exact count **and** the exact message multiset; `Assert.Single`
  appears **zero** times in `EngineInternalsOneDoorAnalyzerTests.cs`.
- **20, 21**: `SharedAnalyzerSources.TypeReferencePositions` (`:736`) has exactly 14 positions —
  seven written-name, seven carried-type — and base list / using alias are separate tests.
  `DoorLocalDeclaration_InsideContainerFactory_IsNotReported` (AG0023) uses the declared-local form
  `Create()` uses today; `DoorTypeReference_InsideContainerFactory_IsNotReported` (AG0029) uses the
  qualified-call form, matching `contract.md:153`. `AbstractionsTypeReference_FromEngine_IsNotReported`
  in both classes.
- **27**: `TypeTreeTests` drives `TypeTree.Any` with `CreatePointerTypeSymbol`,
  `CreateArrayTypeSymbol` and `Construct`, and the leaf test **records** every named type offered,
  so each assertion names which types the walk reached and in what order — traversal, not a non-null
  constructed symbol.
- **29**: all five clauses have their own case in `WrittenNameScannerTests`, including
  `Assert.True(directiveName.IsPartOfStructuredTrivia())` before asserting the discriminator returns
  false for it — which is the rejected-alternative proof the contract's Rules section names.

### The intermediate build

`tdd-intermediate-build.txt` carries exactly one diagnostic — `warning AG0015` at
`src/AgentGuard.Boundaries/SystemServices.cs(88,30)` — with `1 Warning(s)`, `0 Error(s)`, `EXIT=0`.
Nothing else is masked by the `-p:WarningsNotAsErrors=AG0015` override. The claim is exact.

---

## What FAIL does and does not mean here

It does **not** mean the tests are weak, the numbers are fabricated, an acceptance requirement was
dodged, or authority was exceeded. I attacked all four and none held.

It means the report's account of its own evidence is not trustworthy at the level this project's
recurring defect demands: three claims about what a named file contains that the file does not
contain, two counts contradicted by the report's own next clause, and two later-stage assignments
missing the citation the instruction file makes mandatory. Each is individually small; together they
are the same pattern that has burned the last several rounds, and the acceptance-mapping row the
citation rule exists to protect is exactly where two of them landed.

Every one is fixable in the report alone. No test needs to change.

---

## Method

- Isolated copy: `rsync -rlptgo --delete --no-devices --no-specials` of the full working tree,
  including the uncommitted and untracked delivered files, into the session scratchpad. Every
  destructive run was performed there; the shared checkout's ten delivered files were verified
  byte-identical to `tdd-digests.txt` at the start and at the end of this review, with `HEAD`
  unchanged and nothing staged.
- Full suite re-run with `--logger "console;verbosity=detailed"` and the per-case lines counted
  directly, never read off the author's logs.
- Four mutations applied and reverted individually, each followed by a digest check.
- Baseline reconstructed by removing this round's additions rather than trusting the retained
  baseline log.
- The two `dotnet msbuild -getItem:ProjectReference` commands of Acceptance 16 and 17 run in the
  isolated copy to test the "LATER" classification rather than accept it.
