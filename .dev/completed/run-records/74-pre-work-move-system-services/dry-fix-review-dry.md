# DRY review — the three-item correction round, SystemServices relocation

## Verdict: PASS

All three ordered corrections are delivered, wired and load-bearing, and the round introduces no new duplication. Every value the round touched is written in fewer places than before or in the same number; none is written in more.

| value | places writing it, before | after |
| --- | --- | --- |
| `AgentGuard.Engine.Decoy` as a namespace | 4 | 0 (one owner, `SharedAnalyzerSources.WrongContainerNamespace`) |
| `EnvironmentAdapter.Create()` | 2 | 1 |
| `.SystemServices` | 9 | 7 |
| `.Create()` | 22 | 20 |
| `        internal static object Build() => ` | 5 | 5 |

Nothing in this review rests on a count, a digest, a line number, a mutation result or a duplication claim taken from a capture or an earlier review. The intermediate state was reconstructed independently and every number below comes from my own runs, recorded in the optional section.

## Two things the next stage must not trip on

### The earlier DRY review's D1 fix was deliberately narrowed, and the remainder is still standing

`tdd-acceptance25-review-dry.md` prescribed for D1: "Give this class the same owner triple its two sibling classes already have — a type constant, a member constant built from it, and a call constant built from that", plus the same treatment for the other three permitted factories. The forwarded correction ordered only the one item: "route the remaining `EnvironmentAdapter.Create()` copy through `PermittedFactoryCall`." That one item is done and correct. The rest of D1 was not ordered and is not done:

- `analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs:235` still writes `SharedAnalyzerSources.CalledMemberAccusation("EnvironmentAdapter.Create")` as a literal. It is the same identity as `PermittedFactoryCall` at `:91` minus the parentheses, and it does not derive from it, so changing `PermittedFactoryCall` leaves `:235` stale. Both sibling classes derive the two from one owner: `OneDoorIntoCrossPlatformAnalyzerTests.cs:88-90` and `OneDoorIntoPerOsAnalyzerTests.cs:62-64` each declare `DoorType` / `DoorMember = DoorType + ".Create"` / `DoorCall = DoorMember + "()"`, and their wrong-namespace tests pass `DoorCall` and `DoorMember` from that chain.
- The other three permitted factories are still unhoisted: `ConsoleAdapter.Create()` at `:105`, `:158` and `:245`; `Ed25519SignatureService.Create()` at `:106` and `:186`; `BuildInfoReader.Create()` at `:107` and `:172`.

Both are inherited — all of these literals stand unchanged from the base commit `a12e58b` — and both are outside the scope this round was given. Neither is a finding against this correction. They are named here so the next stage does not read D1 as closed.

### The reuse-ledger conflict is unchanged

`rails-dry-code` rail 1 requires every new capability, "including a one-off helper", to carry a prior-art-ledger ruling in the contract's reuse ledger. This round added one new shared owner, `SharedAnalyzerSources.AssertSameStrings`, and `contract.md`'s reuse ledger (lines 201-224) carries no row for it; its only test-infrastructure row remains `reuse analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs`.

The same conflict was raised by the two earlier DRY reviews of this work, referred to Tim as a process question, and is still unanswered: the test author cannot satisfy the rail as written, because the ruling has to live in the contract and the author is not permitted to edit the contract. I did not rest this verdict on rail 1. I ran both discovery lenses myself over the one new owner and the ruling would have come out `extract`, which is what was built — the evidence is below.

---
OPTIONAL DETAILS (do not need to read)

## Correction 1 — the factory-call extraction is complete and wired

`analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs:230` now passes `PermittedFactoryCall` instead of the literal. The value `"EnvironmentAdapter.Create()"` is written in exactly one place in the whole test project, `:91`.

The comment at `:88-90` names four call sites and they are the four that exist. Every use of the constant in the file: `:104` (the `PermittedFactoryCalls` theory data, driving `PermittedFactory_FromContainerFactory_IsNotReported` at `:112`), `:124` (`PermittedFactory_FromAnotherContainerMethod_IsReported`, the wrong-site test), `:142` and `:147` (`PermittedFactory_FromSameNamedContainerInAnotherNamespace_IsReported`, the wrong-namespace caller test), `:230` (`SameNamedFactoryInAnotherNamespace_IsReported`, the wrong-namespace factory test). No fifth site.

**Mutation M1, and it discriminates.** Changing `PermittedFactoryCall` to `"GuidFactoryAdapter.Create()"`:

- against the live tree, `SameNamedFactoryInAnotherNamespace_IsReported` fails — `System.InvalidOperationException : The test fixture compiled into 'AgentGuard.Engine' did not produce the compiler errors this test declares`, `CS0103: The name 'GuidFactoryAdapter' does not exist in the current context`;
- against the reconstructed pre-correction tree, the same mutation leaves that same test passing, because it held its own literal.

That pair is the proof the extraction changed something real rather than moving text around.

## Correction 2 — one owner for the comparison, multiplicity and ordinal both preserved

`SharedAnalyzerSources.AssertSameStrings(IEnumerable<string> expected, IEnumerable<string> actual)` at `SharedAnalyzerSources.cs:998-1003` holds the comparison once:

```csharp
Assert.Equal(
    expected.OrderBy(text => text, StringComparer.Ordinal),
    actual.OrderBy(text => text, StringComparer.Ordinal));
```

Both callers reach it and neither re-spells the sort or the assertion: `AssertSpans` at `:975-981` projects the actual side through `AnalyzerRunner.SpanText`, and `EngineInternalsOneDoorAnalyzerTests.AssertReportsAsync` at `:306-308` projects the expected side through its own `ExpectedMessage` and the actual side through `Message()`. Argument order is expected-then-actual on both paths, so xUnit's Expected and Actual labels stay correct; the M2a output below shows them the right way round.

CodeGraph returns exactly two callers for `AssertSameStrings` and four for `AssertSpans`, and no competing owner of a two-sequence string comparison anywhere in the repository. grep for `SequenceEqual`, `Assert.Equivalent` and `OrderBy(... Ordinal)` across `analyzers`, `src`, `tests` and `eng` returns no second copy of this logic.

**Mutations, each chosen so it can actually discriminate.**

- M2a, caller-side multiplicity: drop one duplicate from `OneDoorIntoCrossPlatformAnalyzerTests.cs:278`, `AssertSpans(source, DoorCall, DoorCall, DoorType)` to `AssertSpans(source, DoorCall, DoorType)`. Fails inside `AssertSameStrings`: `Expected: ["CrossPlatformAdapters", "CrossPlatformAdapters.Create()"]`, `Actual: ["CrossPlatformAdapters", "CrossPlatformAdapters.Create()", "CrossPlatformAdapters.Create()"]`. On the `AssertSpans` path there is no separate count assertion, so this helper alone is what pins the count.
- M2d, the control for M2a, and the one that proves the multiset semantics is load-bearing rather than incidental: apply M2a AND replace both sides inside `AssertSameStrings` with `.Distinct(StringComparer.Ordinal).OrderBy(...)`. The test passes. Set equality would indeed weaken the assertion, exactly as the correction's own doc comment claims. Note that `.Distinct()` applied symmetrically WITHOUT M2a leaves the suite green and proves nothing — the discrimination comes from pairing the symmetric helper change with the asymmetric caller change.
- M2b, message-side multiplicity: drop one of the two `SharedAnalyzerSources.EngineInternalTypeName` subjects from the `Field` row of `ProhibitedReaches` in `EngineInternalsOneDoorAnalyzerTests.cs`. Both its cases fail, `Expected: 1 / Actual: 2`. On this path the count is pinned twice — by `Assert.Equal(expectedSubjects.Length, diagnostics.Length)` at `:303`, which fires first, and by the multiset compare behind it. Both survived the extraction.
- M2c, ordinal equality and ordinal sort: change one expected span at `OneDoorIntoCrossPlatformAnalyzerTests.cs:278` from `DoorType` to `"crossPlatformAdapters"`. It fails, so the comparison is case-sensitive end to end. The failure output also shows the sort is ordinal rather than case-insensitive or culture-aware: `Expected: ["CrossPlatformAdapters.Create()", "CrossPlatformAdapters.Create()", "crossPlatformAdapters"]` places the lowercase entry last, which is ordinal ordering.

No assertion was lost. Executable `Assert.` calls across the three changed files go from 35 to 34, and the one difference is the `Assert.Equal` that moved out of `AssertReportsAsync` into the shared owner it now calls; `Assert.Equal(expectedSubjects.Length, diagnostics.Length)` and `Assert.All(diagnostics, ...)` are untouched. `[Fact]`, `[Theory]` and `[MemberData]` counts are unchanged in every file.

## Correction 3 — one owner for the wrong namespace, generated fixture text byte-identical, assembly identities left alone

`SharedAnalyzerSources.EngineAssemblySource` at `:69` became a `$$"""` raw string whose decoy `namespace` at `:115` is the hole `{{WrongContainerNamespace}}`. `EngineInternalsOneDoorAnalyzerTests.cs:44` builds `DecoyContainerType` from `SharedAnalyzerSources.WrongContainerNamespace`, `:46` builds `DecoyContainerCreate` from `DecoyContainerType`, and `:254` builds the consumer body from `DecoyContainerCreate`. The namespace is written in one place.

**Fixture text identity, proved in C# rather than by emulation.** I extracted the raw-string block for `EngineAssemblySource` verbatim from the pre-correction file, re-emitted it in the isolated copy as a second constant at the same indentation, and asserted the two constants equal inside the test assembly:

```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```

The pre-correction block is also byte-identical to the base commit's, so the Acceptance 25 round had not touched it either. The two changed lines are only the `$$` prefix and the namespace hole.

**The probe discriminates.** Under mutation M3 below, the same probe fails at the namespace and nowhere else:

```
Assert.Equal() Failure: Strings differ
                               ↓ (pos 964)
Expected: ···"ntGuard.Engine.Decoy\n{\n    internal stati"···
Actual:   ···"ntGuard.Engine.Decoy2\n{\n    internal stat"···
```

**Mutation M3, and its pre-correction counterpart.**

- Live tree, change `WrongContainerNamespace` to `EngineAssemblyName + ".Decoy2"`: `SameNamedContainerInTheWrongNamespace_FromGatedConsumer_IsReported` passes for both gated assemblies, and the FULL suite passes 619 of 619. The fixture declaration, the derived expected subjects and the caller-side wrong-namespace fixture all moved together.
- Pre-correction tree, change only the hard-coded `namespace AgentGuard.Engine.Decoy` inside `EngineAssemblySource`: the same test fails for both assemblies with `CS0234: The type or namespace name 'Decoy' does not exist in the namespace 'AgentGuard.Engine'`, because the expected-subject literals did not follow.
- Pre-correction tree, change only `WrongContainerNamespace`: the same test passes while the constant and the declaration have silently diverged. That is precisely the invariant the old doc comment at `:139-144` claimed and did not enforce, and it is what the correction closed.

**The two assembly identities stayed independent.** `TimeMustUseTimeProviderAnalyzerTests.cs` was not touched by either round — it hashes to `d8876818` at the base commit and to `d8876818` live — and its `"AgentGuard.Engine.Decoy"` at `:166` is the `assemblyName` argument to `AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, assemblyName)`, an assembly and not a namespace. `EngineToBoundariesOneDoorAnalyzerTests.cs:95`, `DecoyEngineAssembly = SharedAnalyzerSources.EngineAssemblyName + ".Decoy"`, is unchanged from the base commit and is used at `:298` and `:303` as a compiled assembly name. Neither was routed through `WrongContainerNamespace`. The M3 full-suite run above is the direct proof: renaming the namespace owner leaves all 619 cases green, including both wrong-assembly tests, so the namespace owner does not reach into either assembly identity.

## No new duplication

The occurrence counts in the verdict table were produced by scanning every write of each value — inside larger literals as well as standalone — across the seven in-scope test files, on the reconstructed pre-correction tree and on the live tree.

Two candidate findings did not survive checking, and I am recording them so they are not re-raised:

- `"        internal static object Build() => "` now appears as a standalone literal at `EngineInternalsOneDoorAnalyzerTests.cs:231` and `:254` where before the correction only `:231` spelled it standalone. It is not a new duplication. The same shell text is also written at `:147` and `:158` as part of whole literals and at `SharedAnalyzerSources.cs:746`; the number of places writing it is 5 before and 5 after. The correction changed one site's spelling form, not the number of sites.
- `".SystemServices"` now appears as a standalone fragment at `EngineInternalsOneDoorAnalyzerTests.cs:44` and `EngineToBoundariesOneDoorAnalyzerTests.cs:84`, where before only `:84` spelled it standalone. Also not a new duplication: counting every write of `.SystemServices` gives 9 before and 7 after, because `DecoyContainerCreate` is now derived from `DecoyContainerType` instead of respelling the whole name.

The lesson for anyone re-running this check: counting standalone string literals measures spelling FORM and will report a false positive every time a whole literal is replaced by a composition. Count the places that write the value.

## What I attacked and could not break

- **"The `$$"""` conversion changed the generated fixture."** Refuted by the in-assembly equality probe above, and the probe is shown to discriminate by M3.
- **"The extraction into `AssertSameStrings` weakened an assertion."** Refuted by M2a, M2b and M2c, which each fail, and by the assertion-count comparison. The comparison is the same four lines of logic with the same comparer, the same argument order and the same `Assert.Equal` overload.
- **"`AnalyzerRunner.ThrowOnUnexpectedCompilerErrors` is a third copy of the same multiset comparison"** (`AnalyzerRunner.cs:193-273`). Refuted. It compares two string multisets, but through a `Dictionary<string,int>` tally producing a per-identifier breakdown in an `InvalidOperationException` — a fixture-validity precondition, not a test assertion, with a different mechanism and a different failure mode. It also predates both rounds: `AnalyzerRunner.cs` is unchanged since the base commit.
- **"`AssertSameStrings` is a false 'new' over an owner that already exists" (rail 3).** Refuted by both lenses. grep across `analyzers`, `src`, `tests` and `eng` for `SequenceEqual`, `Assert.Equivalent` and `OrderBy(... StringComparer.Ordinal)` returns no second owner; CodeGraph over `AssertSameStrings AssertSpans AssertReportsAsync SpanText ThrowOnUnexpectedCompilerErrors Tally` returns the call path `AssertSpans -> AssertSameStrings` and `AssertReportsAsync -> AssertSameStrings` and no competitor. The correct ruling is `extract`, and what was built is an extract: both copies collapsed into one owner that both callers call.
- **"`WrongContainerNamespace` and `DecoyEngineAssembly` are one value spelled twice."** Refuted, and this correction did not make it worse. They name different facts — the namespace a container is declared in, and the assembly a container is compiled into — that happen to share a spelling. The M3 full-suite run demonstrates the independence directly.
- **"The correction changed an expectation or lost a scenario."** Refuted. 619 cases before, 619 after, and the two sorted case-name lists are identical: no case added, removed, renamed, bundled or split. The intermediate-to-live diff over the whole `analyzers` tree touches three files and nothing else; `git status` shows no change under `src`, `tests`, `eng` or `analyzers/AgentGuard.Analyzers`.
- **"The round restored `tdd-result.md` or wrote a replacement report."** Refuted. `git status --porcelain` still carries ` D .dev/inprocess/74-pre-work-move-system-services/tdd-result.md`, and no replacement narrative file exists in the folder.

## How the before state was reconstructed

The base commit is `a12e58b305c5315bc175332066e90a2d693b1769` and the working tree carries two uncommitted rounds on it. I separated them without trusting any capture's word:

1. `EngineInternalsOneDoorAnalyzerTests.cs` hashes to `85ad3248` at `a12e58b` and was not in the Acceptance 25 change list, so its pre-correction state is the base state, read straight from git.
2. For the other two files I reverse-applied the round diff recorded in `dry-fix-scope-and-digests.txt` onto a copy of the live tree. The result hashes to `db6c6995` for `SharedAnalyzerSources.cs` and `e20d7570` for `EngineToBoundariesOneDoorAnalyzerTests.cs` — the digests recorded before this round ran, and independently quoted by `tdd-acceptance25-review-dry.md`. A diff that reverse-applies to exactly those hashes cannot be hiding a hunk, so the round's scope is established rather than asserted.
3. `diff -r` between that reconstructed tree and the live `analyzers` tree returns those three files and nothing else.

## Runs

All of them in isolated rsync copies under the session scratchpad, with `analyzers/*/bin` and `analyzers/*/obj` absent before each build so no stale analyzer DLL could be read. SDK `10.0.400` from `/usr/local/bin/dotnet`, matching the `global.json` pin.

```
live tree, full analyzer suite
  Total tests: 619   Passed: 619   Failed: 0   Skipped: 0

live tree, affected classes only
  (DeclaredTypeScanner, EngineToBoundariesOneDoor, OneDoorIntoCrossPlatform,
   OneDoorIntoPerOs, EngineInternalsOneDoor, TimeMustUseTimeProvider)
  Passed!  - Failed: 0, Passed: 180, Skipped: 0, Total: 180

reconstructed pre-correction tree, full analyzer suite
  Total tests: 619   Passed: 619   Failed: 0   Skipped: 0

case-name lists, pre-correction against live
  identical, 619 names each
```

Per-class case counts on the live run: `EngineInternalsOneDoorAnalyzerTests` 81, `OneDoorIntoPerOsAnalyzerTests` 35, `OneDoorIntoCrossPlatformAnalyzerTests` 34, `EngineToBoundariesOneDoorAnalyzerTests` 16, `TimeMustUseTimeProviderAnalyzerTests` 12, `DeclaredTypeScannerTests` 2.

The shared checkout was not mutated. `git status --porcelain` after all my runs shows the same six modified test files, the same deletion of `tdd-result.md` and the same untracked captures it showed before I started, plus this review file.
