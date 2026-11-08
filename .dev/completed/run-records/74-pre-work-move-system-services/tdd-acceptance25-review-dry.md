# DRY review — Acceptance 25 test work, SystemServices relocation

## Verdict: FAIL

Three duplications are confirmed against the live files. `rails-dry-code` rail 5 ("Every literal string, number, or path lives in exactly one named owner and is referenced from there") and rail 6 ("Every block of logic and every function exists once") are each violated, and the rail's verdict rule is "Return FAIL when any Violation is confirmed."

The lead one is D1: the round hoisted `"EnvironmentAdapter.Create()"` into a new named constant and then left a literal copy of that same value 139 lines below it, in the one test in that class whose two sibling classes both route the equivalent value through their own constant.

Nothing in this review rests on a count, a digest, a line number or a duplication claim taken from any report or earlier review. Every one was re-derived from the live working tree and from my own runs, recorded below.

## Still open from the earlier round and not resolved here — the rail-1 ledger conflict

`rails-dry-code` rail 1 requires every new capability, "including a one-off helper", to carry a prior-art-ledger ruling in the contract's reuse ledger, and calls itself a locked law. This round added four new shared owners — `SharedAnalyzerSources.AssertSpans`, `InsideContainerFactoryInWrongNamespace`, `WrongContainerNamespace` and the four-argument `EngineCompilation` overload — and `contract.md`'s reuse ledger (lines 201 to 224) carries no row for any of them. Its only test-infrastructure row is `reuse analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs`.

The same conflict was raised by the earlier DRY review of this round (`tdd-review-dry.md:171-182`) over that round's eight helpers, referred to Tim as a process question, and is still unanswered. The test author cannot satisfy the rail as written, because the ruling has to be carried in the contract and the author is not permitted to edit the contract. I did not rest this verdict on rail 1. I ran the search the ledger exists to force, myself, on all four new owners, and the results are in "What I attacked and could not break"; no ruling would have come out wrong.

This paragraph is here so the next stage does not trip on it, not as a finding against the test author.

## D1 — the hoisted permitted-factory call still has a second spelling (rail 5)

**Location.** `analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs:90` and `:229`.

Line 90, added by this round:

```csharp
private const string PermittedFactoryCall = "EnvironmentAdapter.Create()";
```

Line 229, untouched by this round, inside `SameNamedFactoryInAnotherNamespace_IsReported`:

```csharp
            SharedAnalyzerSources.InsideContainerFactory(
                "using DecoyNamespace;", "EnvironmentAdapter.Create()"));
```

**What contradicts it.** The constant's own doc comment at `:88-89` names three call sites and claims they are all of them: "Held once because the compliant theory, the wrong-site test and the wrong-namespace caller test all make this same call and must make the same one." The mechanical literal scan over the five changed files returns `'EnvironmentAdapter.Create()' -> ['EngineToBoundariesOneDoorAnalyzerTests.cs:90', 'EngineToBoundariesOneDoorAnalyzerTests.cs:229']`. The value is spelled in two places, so the "held once" claim is false as delivered.

The two sibling classes show the pattern this file is meant to follow. `OneDoorIntoCrossPlatformAnalyzerTests.cs:86-90` declares `DoorType` / `DoorMember` / `DoorCall`, and its wrong-namespace-decoy test at `:291` passes `DoorCall` and at `:295` passes `DoorMember`. `OneDoorIntoPerOsAnalyzerTests.cs:60-64` declares the same triple, and its wrong-namespace-decoy test at `:300` and `:304` does the same. `EngineToBoundariesOneDoorAnalyzerTests.cs:229` and `:234` is the direct analogue of both, and it spells the literal in both halves.

**What the round changed, read off the base commit.** At `a12e58b` the value was spelled three times, at `:99`, `:119` and `:204`. This round replaced the first two with the constant and left the third, so the value went from three copies to one owner plus one copy. The hoist was driven by the new test needing the value, not by deduplication — which is visible in the three sibling values the round left alone: `ConsoleAdapter.Create()` at `:104`, `:157` and `:244`; `Ed25519SignatureService.Create()` at `:105` and `:185`; `BuildInfoReader.Create()` at `:106` and `:171`. One of four permitted factories got a partial owner; the other three got none.

**The fix.** Give this class the same owner triple its two sibling classes already have — a type constant, a member constant built from it, and a call constant built from that — and route `:214`, `:219`, `:229` and `:234` through them. `:229` takes the call constant and `:234` takes the member constant, exactly as `OneDoorIntoCrossPlatformAnalyzerTests.cs:291` and `:295` do. Do the same for the other three permitted factories, so the theory at `:101-107` and the tests at `:157`, `:171`, `:185` and `:244` each read one owner.

## D2 — the sorted-multiset comparison is still spelled twice, in this round's own new helper (rail 6)

**Location.** `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs:977-980`, written by this round, and `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs:305-309`.

The new helper:

```csharp
        Assert.Equal(
            expectedSpans.OrderBy(text => text, StringComparer.Ordinal),
            diagnostics.Select(diagnostic => AnalyzerRunner.SpanText(source, diagnostic))
                .OrderBy(text => text, StringComparer.Ordinal));
```

The standing copy:

```csharp
        Assert.Equal(
            expectedSubjects.Select(subject => ExpectedMessage(assemblyName, subject))
                .OrderBy(message => message, StringComparer.Ordinal),
            diagnostics.Select(diagnostic => diagnostic.Message())
                .OrderBy(message => message, StringComparer.Ordinal));
```

Same logic with renamed variables: project both sides to strings, sort each ordinally with `StringComparer.Ordinal`, compare with `Assert.Equal`. Rail 6 names exactly this — "same logic even with renamed variables".

**What contradicts the extraction being complete.** The earlier DRY review of this round recorded both copies as one finding (`tdd-review-dry.md:163-167`, F9) and prescribed the fix: "one owner taking two sequences would serve both." This round extracted one of the two copies — the one in `DeclaredTypeScannerTests` — into `AssertSpans`, and hard-wired the projection to `AnalyzerRunner.SpanText(source, diagnostic)` inside the owner. That forecloses the prescribed fix: the message copy cannot route through `AssertSpans`, because `AssertSpans` decides the projection rather than taking it. So the extraction collapsed one copy instead of the copies, which is what rail 4's Practice requires — "a capability ruled 'extract' collapses the copies into a single shared owner and calls it."

The project already has the shape that would have worked. `analyzers/AgentGuard.Analyzers/AppliedAttributeBan.cs:28-52` is the in-repo precedent: the shared body is the owner and the varying policy is passed in per caller.

**The fix.** Extract the comparison itself as the one owner — a method taking the two string sequences — then have `AssertSpans` project the span texts and call it, and have `EngineInternalsOneDoorAnalyzerTests.AssertReportsAsync` project the messages and call it. Neither caller then spells `OrderBy(... StringComparer.Ordinal)` or `Assert.Equal` again.

## D3 — the new wrong-namespace owner leaves four literal spellings of the same namespace standing (rail 5)

**Location.** `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs:145`, added by this round:

```csharp
    internal const string WrongContainerNamespace = EngineAssemblyName + ".Decoy";
```

`EngineAssemblyName` is `"AgentGuard.Engine"` (`SharedAnalyzerSources.cs:128`), so the constant is `"AgentGuard.Engine.Decoy"`. That value is spelled literally, with no reference to the new owner, at:

- `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs:115` — `namespace AgentGuard.Engine.Decoy`, inside `EngineAssemblySource`, in the same file as the new constant.
- `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs:42` — `private const string DecoyContainerType = "AgentGuard.Engine.Decoy.SystemServices";`
- `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs:44` — `private const string DecoyContainerCreate = "AgentGuard.Engine.Decoy.SystemServices.Create()";`
- `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs:253` — `"        internal static object Build() => AgentGuard.Engine.Decoy.SystemServices.Create();"`

**What contradicts it.** The new constant's own doc comment at `:139-144` claims the alignment it does not enforce: "The same spelling is what `EngineAssemblySource`'s decoy container uses, so the wrong-namespace container reads the same wherever a test declares one." `EngineAssemblySource` hard-codes the namespace at `:115`. Change `WrongContainerNamespace` and line 115 does not follow, and the stated invariant breaks silently.

**The raw-string objection does not hold.** The technique for interpolating a constant into one of these fixture sources is already in use in the files this round edited: `OneDoorIntoCrossPlatformAnalyzerTests.cs:66-73` writes `$$"""` with `{{SharedAnalyzerSources.CrossPlatformUsing}}` as the hole and ordinary single braces elsewhere in the C# body. `EngineAssemblySource` converts the same way.

**The prior ruling on this exact class.** The earlier DRY review of this round raised the same shape as a blocking finding — a new `EngineAssemblyName` owner routed into one test class while six spellings of the same literal stood in two others — and the correction round closed it. The project's own standard is that adding a shared owner obliges routing the sibling spellings to it.

**The fix.** Route all four through `WrongContainerNamespace`: make `EngineAssemblySource` an interpolated raw string whose namespace hole is the constant, and build `DecoyContainerType` and `DecoyContainerCreate` from it, with `:253` reading `DecoyContainerCreate`.

---
OPTIONAL DETAILS (do not need to read)

## Snapshot reviewed

Live working tree, `HEAD` `a12e58b305c5315bc175332066e90a2d693b1769`, branch `appd-1-process`. The five delivered files hash to the digests recorded in `tdd-acceptance25-scope-and-digests.txt`; I ran `shasum -a 256` against the live files and all five match.

```
db6c6995a18d64a6fc54e759483e9b806c534356e07572b49f3fc428ebfd97e4  SharedAnalyzerSources.cs
1942cc6cfa58ae99297f5a4b9102b3a14cb6e2d12adcccc55b2187d421ab6e3d  DeclaredTypeScannerTests.cs
e20d7570be1b80a13956ff3c2850e62b8361ab12da1c2390d333c38937955a04  EngineToBoundariesOneDoorAnalyzerTests.cs
6ec75338a3eb73c3eaae59842c99170c3b3e3fb751f23ce07e2a555cf96d0c26  OneDoorIntoCrossPlatformAnalyzerTests.cs
7139aa1d44db1d5e1ec4486218b4ece897b888911da76fcb880f7224acb81a02  OneDoorIntoPerOsAnalyzerTests.cs
```

The shared checkout was not mutated. `git status --short -- analyzers` after all my runs returns the same five modified files and nothing else. Every build and every run I did was in `scratchpad/dry-iso` (an rsync of the working tree, `.git`, `bin`, `obj`, `.dev` and `.codegraph` excluded, verified byte-identical to the checkout's `analyzers` tree with `diff -r` before use) and in `scratchpad/dry-base` (a copy of that with the five test files replaced by their `a12e58b` content).

## What I ran

**Full analyzer suite over the delivered files, my own run, in `scratchpad/dry-iso`.**

```
Passed!  - Failed:     0, Passed:   619, Skipped:     0, Total:   619, Duration: 9 s - AgentGuard.Analyzers.Tests.dll (net10.0)
EXIT=0
```

**Full analyzer suite over the base state of the five files, my own run, in `scratchpad/dry-base`.** 616 cases, exit 0.

**Per-case name comparison, base against delivered.** Both runs listed with `--logger "console;verbosity=normal"`, names sorted, timings stripped. The diff is exactly three added lines and nothing else:

```
> AgentGuard.Analyzers.Tests.EngineToBoundariesOneDoorAnalyzerTests.PermittedFactory_FromSameNamedContainerInAnotherNamespace_IsReported
> AgentGuard.Analyzers.Tests.OneDoorIntoCrossPlatformAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported
> AgentGuard.Analyzers.Tests.OneDoorIntoPerOsAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported
```

Every one of the 616 base cases is present in the delivered run under the same name. Nothing was renamed, bundled, split or lost by either extraction.

**Template equivalence for the `EngineCompilation` extraction.** I pulled both raw-string templates — the base one from `git show a12e58b:...` and the live one from disk — emulated `string.Format`'s `{{`/`}}`/`{n}` handling, and compared the emitted fixture.

Base template, after raw-string indentation stripping:

```
{0}

namespace AgentGuard.Engine
{{
    internal sealed class SystemServices
    {{
{1}
    }}

{2}
}}
```

Live template:

```
{0}

namespace {1}
{{
    internal sealed class SystemServices
    {{
{2}
    }}

{3}
}}
```

`Format(base, usings, body, other)` and `Format(live, usings, "AgentGuard.Engine", body, other)` produce byte-identical output. With `"AgentGuard.Engine.Decoy"` in the namespace position, the only line that differs is the `namespace` line.

**Both discovery lenses, run by me per rails 2 and 7.**

- grep across `analyzers`, `src`, `tests` and `eng` for every new owner name (`AssertSpans`, `InsideContainerFactoryInWrongNamespace`, `WrongContainerNamespace`, `PermittedFactoryCall`, `ContainerFactoryIn`, `EngineCompilation`), for `SpanText`, for `OrderBy(`, for `.Decoy`, for `Decoy`, for `EnvironmentAdapter`, and for `internal static object`.
- CodeGraph (`codegraph_explore`) over `AssertSpans SpanText AssertReportsAsync AssertNamesExactly InsideContainerFactoryInWrongNamespace EngineCompilation ContainerFactoryIn`. It returned the call path `InsideContainerFactoryInWrongNamespace -> ContainerFactoryIn -> EngineCompilation`, confirmed `AnalyzerRunner.SpanText` as the single span-text owner with no competitor anywhere, and surfaced `AppliedAttributeBan` as the in-repo precedent cited in D2.
- A mechanical string-literal scan over the five changed files for any value spelled in more than one place, skipping raw-string blocks and comment lines. It is what produced D1's two locations and the three unhoisted sibling factory calls.

## What I attacked and could not break

**"The `EngineCompilation` extraction changed the fixture the 616 existing tests are built on."** Refuted, twice over. The template emulation returns byte-identical output for the three-argument path, and the per-case name lists are identical but for the three added tests.

**"`AssertSpans` is a false 'new' over a primitive that already exists" (rail 3).** Refuted. `AnalyzerRunner.SpanText` (`AnalyzerRunner.cs:138-142`) is the one owner of reading a diagnostic's span text, and `AssertSpans` calls it rather than re-deriving `diagnostic.Location.SourceSpan` and `source.Substring`. grep for `Location.SourceSpan` and `source.Substring` across the test project returns only that one owner. CodeGraph returns no competing owner. The assertion body it holds is the verbatim body that was inline in `DeclaredTypeScannerTests` at the base commit, with the expected array parameterized — an extraction, not a rewrite. D2 is about the copy it did not collapse, not about this.

**"`WrongContainerNamespace` duplicates `DecoyEngineAssembly`."** Refuted. `EngineToBoundariesOneDoorAnalyzerTests.cs:94` spells the same composition, `SharedAnalyzerSources.EngineAssemblyName + ".Decoy"`, but it names a different fact: the assembly a decoy container is compiled into, used at `:297` and `:302` for the wrong-assembly direct test of `CompositionPoint`. `WrongContainerNamespace` names the namespace a container is declared in. Either can be chosen without the other, so this is two facts that happen to share a spelling, not one value spelled twice. D3 is about the four spellings that are the same fact.

**"`InsideContainerFactoryInWrongNamespace` duplicates the wrong-namespace container `EngineAssemblySource` already declares."** Refuted. `SharedAnalyzerSources.cs:115-121` declares `internal static class SystemServices` inside a source compiled as a REFERENCED assembly — it is a callee the gated consumers reach. The new builder emits `internal sealed class SystemServices` inside the compilation UNDER ANALYSIS — it is the caller whose identity is on trial. Different role, different declaration, no shared body.

**"The three new test methods are one test written three times" (rail 6).** Refuted. Each drives a different analyzer through that class's own `RunAsync`/`RunFromEngineAsync`, against that class's own `RuleId`, with its own expected span list — one span for AG0040, three for AG0023, two for AG0029. The shared parts are already in the one owner: the fixture comes from `SharedAnalyzerSources.InsideContainerFactoryInWrongNamespace` and the message check from `SharedAnalyzerSources.AssertNamesExactly`. What remains per class is the constants that differ. This matches the existing idiom of the ten or so sibling `DoorCall_*_IsReported` tests already in those two classes.

**"The per-OS test reaches for the wrong `using` constant."** Refuted. `OneDoorIntoPerOsAnalyzerTests.cs:278` passes `SharedAnalyzerSources.CrossPlatformUsing`, which is correct because that class's `PerOsSource` (`:17-38`) declares its types in `namespace AgentGuard.CrossPlatform`. Every other fixture in the class uses the same constant, so this reads through the existing owner rather than introducing a second one.

**"The four-argument `EngineCompilation` overload is a second template."** Refuted. There is one template. The three-argument overload is a one-line delegation at `:519-522`, and every one of the eight pre-existing call sites still goes through it unchanged.

**"A fixture shell, an assertion or a helper was written twice inside the round's own additions."** Refuted for all four additions. `ContainerFactoryIn` (`:1048-1052`) holds the expression-bodied factory once and both public entry points call it; `InsideContainerFactory` and `InsideContainerFactoryInWrongNamespace` each reduce to a single call with a different namespace argument.

**"The round weakened an existing expectation to make the new tests pass."** Refuted. `git diff` against `a12e58b` removes exactly two things from the test files: the `using System.Linq;` directive in `DeclaredTypeScannerTests.cs` (unused once the Linq call moved into the shared helper) and the four inline assertion lines that became the call to `AssertSpans`. Every other changed line is an addition or a literal-to-constant substitution. No assertion was deleted, loosened or narrowed.

## Confirmed duplication this round did not create

Recorded so it is not mistaken for a finding against this work, and so the next round has it.

- `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs:418` and `:1050` both spell the expression-bodied container factory: `InertContainerFactory` is `"        internal static object Create() => null!;"` and `ContainerFactoryIn`'s body builds `"        internal static object Create() => " + expression + ";"`. The first is the `null!` case of the second. This round moved the second line from `InsideContainerFactory` into `ContainerFactoryIn`; it did not create the split.
- The member-declaration prefix `"        internal static object "` is spelled at `SharedAnalyzerSources.cs:433`, `:690`, `:716`, `:745`, `:795`, `:797`, `:799`, `:800`, `:805`, `:806`, `:807`, `:809`, `:1050` and `:1059`. Pre-existing throughout.
- `"using DecoyNamespace;"` is spelled at `EngineToBoundariesOneDoorAnalyzerTests.cs:229`, `OneDoorIntoCrossPlatformAnalyzerTests.cs:291` and `OneDoorIntoPerOsAnalyzerTests.cs:300`, with no owner. Pre-existing, three copies at the base commit as well.
- `"AgentGuard.CrossPlatform.Decoy"` at `OneDoorIntoCrossPlatformAnalyzerTests.cs:311` and `OneDoorIntoPerOsAnalyzerTests.cs:85`, and `"AgentGuard.CrossPlatform.MacOS"` at `OneDoorIntoCrossPlatformAnalyzerTests.cs:331` and `OneDoorIntoPerOsAnalyzerTests.cs:68`. Both were named by the earlier DRY review of this round and both are still open.
- `"AgentGuard.Boundaries"` is spelled at `EngineToBoundariesOneDoorAnalyzerTests.cs:275` and `:320`, `OneDoorIntoCrossPlatformAnalyzerTests.cs:117`, `:125` and `:137`, and `OneDoorIntoPerOsAnalyzerTests.cs:101`, `:109`, `:140` and `:152`, with no owner, while the sibling `EngineAssemblyName` and `GuardAssemblyName` both have one in `SharedAnalyzerSources`. Pre-existing.
