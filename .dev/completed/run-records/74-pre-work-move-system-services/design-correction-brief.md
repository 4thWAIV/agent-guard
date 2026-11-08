# DESIGN correction brief — settled requirements

Tim set this direction after reading the first revision. It is the authority for this round, together with `contract.md`. Earlier panel proposals in `design-output.json`, `design-verdict.json`, `design-revision-output.json` and `design-revision-verdict.json` are inputs, not approvals. Where a proposal conflicts with a requirement below, correct the proposal.

## Settled requirements

These are decided. Design to them; do not re-open them.

**Shared resolution.** The reusable symbol and type resolution mechanics live in a dedicated helper. Both `AttributeIdentity` and `WrittenNameScanner` use it. Attribute-specific matching and fallback policy stay in `AttributeIdentity`. The last round put the recovery inside `WrittenNameScanner` with `AttributeIdentity` calling in; that is corrected to a dedicated third owner.

**Documentation.** XML documentation references are excluded from access enforcement, recognized by their syntax context. The existing documentation links are preserved. The last round proposed rewriting three production doc comments from `<see cref="…"/>` to `<c>…</c>`; that is corrected — the comments stay as they are and the rules do not report them.

**Identity and reuse.** Reuse the existing assembly, namespace, type and method identity helpers, and `CompositionPoint` for the shared factory-location check.

**The three lenses.** `MemberUseScanner` is retained. Declaration scanning is extracted into `DeclaredTypeScanner`. Explicit-name scanning is shared through `WrittenNameScanner`.

**Mutations run in isolated copies.** Never in the shared working tree.

## What this round must produce

One consolidated design, not a diff against earlier rounds.

The actual C# interface of every helper, with what it owns and what existing code it reuses or extracts, named by file and line. Present each file as a consequence of a responsibility, never as a filename asking for approval on its own.

The cref exclusion: which syntax context is the discriminator, proof it excludes documentation references and nothing else, and a plain statement of what the exclusion costs.

Tests proving a documentation reference is allowed while the same reference in application code is still reported.

Tests rejecting an identically named privileged caller in the wrong namespace or the wrong assembly — a `SystemServices.Create()` carrying the right names but declared elsewhere gets no privileged access.

Direct pointer-type coverage. `TypeTree.Any` already walks `IPointerTypeSymbol` at `TypeTree.cs:42`, and a pointer type symbol can be constructed from a compilation without compiling unsafe source, so this needs no change to `AnalyzerRunner` and no new expected-compiler-error exception. Confirm the route and write the test rather than recording a gap.

Compiler-error validation inside the analyzer-source test model, so every structural check runs only against a valid compilation.

## Architecture tests, and their demonstrated limits

Design tests that detect three things: duplicated scanning, omitted scanner registrations, and calling the shared permission check while ignoring its result.

Demonstrate what each check actually catches with concrete negative fixtures, including these three.

An analyzer that calls the shared factory-location check, ignores its answer, and uses a copied check instead. Transitive reachability alone passes this fixture, because the analyzer does reach the owner. The check has to be stronger than reachability.

An analyzer that duplicates only part of declaration scanning.

An analyzer that omits a required scanner registration.

Each fixture must fail the check intended to protect that responsibility. Other tests failing as well is acceptable.

State for each check what it catches and what it does not, so a later reader does not mistake its reach. Distinguish prohibited duplication from unrelated analysis that legitimately uses the same Roslyn APIs.

## Scope discipline

Keep the enforcement focused on these responsibilities.

Any additional framework, any project-wide restriction on Roslyn API use, and any change to approved behavior comes back as an explicit proposal with its scope explained, never as an assumed requirement. The last round proposed two project-wide checks — one requiring every `ContainingSymbol` read and every `GetEnclosingSymbol` call anywhere in the analyzer project to flow into `CompositionPoint` or `OwnerClass`, and one restricting which Roslyn `Register*` methods the consuming analyzers may call directly. Either may be proposed again, but only as an explicit proposal that states its scope and separates prohibited duplication from unrelated legitimate use.

## Boundaries

This authorizes the design correction only. No source file, test file, analyzer, project file, or workflow script in the repository is edited. Nothing is staged, committed, or pushed. Probe with throwaway code outside the repository.

Implementation follows Tim's approval of the result.

## Measurements established in earlier rounds

Confirm any a proposal depends on; do not re-derive the whole set.

`SemanticModel.GetEnclosingSymbol` distinguishes a call written directly in `Create()` from one nested in a lambda and one nested in a local function, returning the ordinary method, the anonymous function, and the local function respectively. `ContainingSymbol` returns the ordinary method in all three cases on both the operation context and the syntax-node context, so a caller check built on it permits what Acceptance 12 requires reported.

The analyzer driver visits documentation-comment crefs and they resolve to real symbols. A cref name node's parent is `NameMemberCref` or `QualifiedCref`, and its ancestor chain terminates at `SingleLineDocumentationCommentTrivia` rather than reaching the method declaration. At a cref position `GetEnclosingSymbol` yields the containing type.

The three live documentation references that the exclusion must preserve are `src/AgentGuard.Cli/Program.cs:17` and `:53`, each `<see cref="SystemServices.Create"/>` and each resolving as two nodes, and `src/AgentGuard.Boundaries/SystemServices.cs:79`, `<see cref="CrossPlatformAdapters"/>` inside `Create()`'s own doc comment, which travels with the relocated file.

An attribute applied with a required constructor argument omitted yields no symbol from `GetSymbolInfo` while `GetTypeInfo` still yields the type. The member-use lens fails open in that case and in a call with a wrong argument count, producing no operation at all, while the written-name lens still resolves the type.

A generic type written with arguments is one `GenericName` node; its identifier is a token, not a child `IdentifierName`.

`RegisterSymbolAction(SymbolKind.Method)` never fires for a local function, and there is no `SymbolKind.Event` registration, so a local-function parameter, a lambda parameter, and an event's type are covered only by the written-name lens.

Compiling the analyzer project's own sources without a synthesized global-usings tree produces 156 compiler errors, and the ownership queries then report confident wrong answers.

A `DiagnosticAnalyzer` cannot be declared in `AgentGuard.Analyzers.Tests`; the analyzer-design rules fire as six errors under the repository's warnings-as-errors setting.

Exactly one declaring type in the analyzer project registers `SymbolKind.Field`, `Property` and `Method` together. Four other types register two of the three. `OperationKind.VariableDeclarator` has one registration. `SyntaxKind.IdentifierName` and `GenericName` have none. `ContainingSymbol` has seven reads, all already flowing into `CompositionPoint` or `OwnerClass`. `GetEnclosingSymbol` has none. `TypeTree.Any` has two unrelated callers.

`SystemServices.Create()` declares a local typed `CrossPlatformAdapters` and names `PlatformServices` only inside a qualified invocation whose local is typed `IPlatformServices` from Abstractions.

Fixture members carrying internal types must themselves be internal, or the compilation emits CS0050, CS0051 or CS0060 and `AnalyzerRunner` throws.
