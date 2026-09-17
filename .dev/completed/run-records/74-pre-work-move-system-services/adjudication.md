# Adjudication of the hidden-decision scan records

## Overruled — the two-diagnostic record

The third scan's second `autoResolved` record resolved AG0041's double-report question by citing best-practices guide principles 5a and area 5 ("fail closed ... never silently continue"). Tim rejected that reasoning: one diagnostic already fails the build, so fail-closed does not require two, and any diagnostic-count requirement needs its own justification.

The contract now sets no required diagnostic count. It states the factual position instead: the written-name lens and the carried-type lens are separate Roslyn registrations, each reports what it finds, and no cross-lens deduplication is built because deduplication is machinery this contract does not ask for. A single statement can therefore produce more than one diagnostic.

The test consequence stands on its own evidence and is unaffected by the overruled principle: `Assert.Single` is used across 48 analyzer test files, including `analyzers/AgentGuard.Analyzers.Tests/BoundariesToCrossPlatformOneDoorAnalyzerTests.cs:62`, so an AG0041 test whose input trips both lenses must assert by count and message instead. Acceptance 11 carries that.

## Accepted

The third scan's first `autoResolved` record — that the literal descriptor prose for AG0040, AG0041, AG0023, and AG0029 is the implementer's to write under principle 5b — is accepted. The contract fixes what each message must contain and Acceptance 5 pins it.

The second scan's `autoResolved` record — rewriting the AG0023 and AG0029 descriptor text for the Engine gate under principle 5b — is accepted and is in the contract.

The second scan's finding on the inbound grants was brought to Tim, who approved AG0041 in response.

## Fourth scan — both records accepted, one raised to Tim

The record on the caller test's meaning is accepted. "Directly inside `AgentGuard.Engine.SystemServices.Create()`" now means the call site's own containing method symbol is that method; a call inside a lambda or a local function nested in `Create()`'s body is reported. Cited principle 3a, least privilege and narrowest workable scope. The narrowest reading refuses nothing legitimate: `SystemServices.Create()`'s body is entirely top-level statements at `src/AgentGuard.Boundaries/SystemServices.cs:86-107` and this contract forbids changing its shape. Acceptance 12 pins the lambda and local-function rejections.

The record on the test harness is accepted in part and raised in part. The half that is local to the new test file — the fake Engine source declaring its types and members `internal` and carrying the two `InternalsVisibleTo` attributes — is folded into Acceptance 8, because without it the accept case would pass whether the analyzer stayed quiet or the compiler rejected the reference with an inaccessibility error. The half that changes `analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs` is shared test infrastructure outside the contract's Surfaces, so it went to Tim rather than being auto-resolved. `RunAnalyzerAsync` at `AnalyzerRunner.cs:126-134` returns only `GetAnalyzerDiagnosticsAsync()`, and no existing fixture carries an `InternalsVisibleTo` attribute.
