# DESIGN revision brief — reuse the existing analyzer infrastructure, and keep it honest

Tim approved this direction after reading the first DESIGN result. This brief states what the revision must settle. It supersedes nothing in `contract.md`; every decision recorded there is already approved and is not this round's to revisit. The first round's result is preserved in `design-output.json` and `design-verdict.json`.

## What this round exists to decide

The first round answered one question: who owns the written-name lens. It proposed `WrittenNameScanner`, and Tim approved the direction rather than the detail. This round settles the whole shared-scanner design and the enforcement that keeps it from drifting.

## Reuse the existing code

Design around these responsibilities.

`MemberUseScanner` is retained as it stands, for calls, construction, and member access.

`DeclaredTypeScanner` is the extraction of the declaration scanning that lives today inside `ContractConcreteTypeMustNotBeReferencedAnalyzer`. That analyzer and the new consumers are all routed through it.

`WrittenNameScanner` handles explicit source-name references.

Compare the proposed symbol resolution in `WrittenNameScanner` against `AttributeIdentity`, including the case where constructor resolution fails but type information remains available. `AttributeIdentity.ResolveAttributeType` resolves in two tiers — `GetSymbolInfo` to the attribute's constructor and then its containing type, then `GetTypeInfo` when the constructor does not bind, which is what happens when a required constructor argument is omitted and overload resolution fails while the type still binds. Reuse or extract the common mechanics. The attribute-specific fallback policy stays with its owning rules, AG0008 and AG0105.

Access policy stays in the consuming analyzers. Reuse the existing owners for type identity, nested-type traversal, factory identity, and caller-location checks.

For each helper, state what it owns, what existing code it reuses or extracts, and its actual C# interface.

Resolve implementation details through the existing rails. Bring Tim the choices that change architecture, coverage, or authority.

## Design tests for behavior and ownership

Include direct scanner tests covering the required reference forms and the unresolved-symbol cases.

Include consuming-rule tests proving both the rejection of forbidden references and the acceptance of the permitted factory calls.

Design architecture tests that verify the relevant analyzers use the shared scanners and the caller-location checks, and that Engine receives the extended checks while Boundaries retains its existing behavior.

Replace the first round's proposed absence-of-`"Create"` literal test with a check of actual `CompositionPoint` use. Define precisely how the ownership checks distinguish duplicated scanning from unrelated analysis. Use parsed C# and symbol resolution where the distinction requires them.

Give the architecture checks their own positive and negative fixtures, demonstrating that they reject duplicated or bypassed implementations.

Plan isolated mutation checks that remove required scanning, bypass a caller restriction, and introduce duplicate scanning. Identify which test must fail for each mutation.

## What the revision returns

The proposed shared interfaces and ownership. The architecture restrictions the tests will enforce. Any remaining decisions Tim must make. The design-review findings.

## Boundaries

This authorizes design work only. No source file, test file, analyzer, project file, or workflow script is edited in this round. Nothing is staged, committed, or pushed.

The round carries a two-hour ceiling. If it will be exceeded, the work stops and reports where it stands rather than continuing.

## Established facts this round builds on rather than re-deriving

These were established from the live tree in GROUND and in the first DESIGN round. Confirm any that a proposal depends on; do not re-derive the whole set.

No analyzer resolves a general written type name today. A grep for `SyntaxKind.IdentifierName`, `GenericName`, `QualifiedName`, `TypeOfExpression`, or `NameOf` across the analyzer project returns nothing. `ContractConcreteTypeMustNotBeCastToAnalyzer` (AG0007) covers only cast, `as`, and the two pattern forms, through four hand-picked registrations.

`ContractConcreteTypeMustNotBeReferencedAnalyzer` (AG0006) holds the declaration scanning in four registrations: field, property, method covering both return type and parameters, and a `VariableDeclarator` operation action for locals. That `VariableDeclarator` registration is the only one in the project.

`TypeTree.Any` already serves two unrelated callers, `ContractPattern.cs:50` and `ReturnTypesMustNotBeTuplesAnalyzer.cs:131`, so it is a shared primitive and no rule may claim exclusive use of it.

`OwnerClass.InAssembly` at `OwnerClass.cs:107` is the existing owner of the compilation-assembly-name comparison. `MemberUseScanner.cs:67` already holds a second inline copy and is out of scope to fix here.

`Directory.Build.props:84` wires the project's own analyzers only where `AgentGuardIsAnalyzerProject` is not true, and `AgentGuard.Analyzers.csproj` sets it true, so a Roslyn analyzer written to police the analyzer project would never run. The enforcement mechanism is therefore a test, not an analyzer.

`RepositoryFiles.FindRepositoryRoot()` in the analyzer test project backs the existing structural tests `OsSpecificFileNotCrossCompiledTests` and `EvidenceUploadsCannotRegressTests`, each carrying a planted-regression fixture. Neither parses C# or resolves symbols; both read project XML and file names. Compiling the analyzer project's own sources in a test and resolving symbols over them is new infrastructure, and it works: reading the sources from the repository root, parsing them, and building a compilation whose references are the Roslyn assemblies already loaded in the test process resolves symbols correctly.

Roslyn is pinned at `Microsoft.CodeAnalysis.CSharp` 4.14.0 in `Directory.Packages.props:23`.

`SystemServices.Create()` declares a local typed `CrossPlatformAdapters` and names `PlatformServices` only inside a qualified invocation, whose local is typed `IPlatformServices` from Abstractions. So `CrossPlatformAdapters` is both a written name and a carried type, while `PlatformServices` is a written name only.

A `using static AgentGuard.Engine.SystemServices;` directive's own `SystemServices` leaf resolves to the named type with a `QualifiedName` parent rather than as an invocation receiver, so that directive is reported and the bare `Create()` form cannot occur in rule-clean code.

`AgentGuard.Engine` contains no reference to any guarded type today, so the new Engine gates have no production impact beyond the relocated `SystemServices.cs`, whose two references sit inside `Create()`.
