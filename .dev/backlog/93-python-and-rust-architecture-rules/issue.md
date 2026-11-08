The architecture rules this repository enforces in C# through 57 Roslyn analyzers, and in TypeScript through ESLint, have no equivalent in Python or Rust. This issue records the mechanism settled on 26 October 2026 for each, so it can be built when a guarded project needs it.

**Python is a pylint checker. Rust is a dylint lint, written as a `LateLintPass`.**

A rule is a class in all four languages, carrying an id, a symbol, a message and a description, walking a resolved syntax tree, reporting at the site of the violation and failing the build.

## What our existing rules demand

Three of ours set the bar.

- **AG0011** (`RawPrimitiveOnlyInOwnerAnalyzer`) allows a raw OS or CLR primitive only in the class that implements its owning interface **and** compiles into the owner assembly. Its own summary states the conjunction applies so a class cannot self-grant by declaring the interface in the wrong assembly.
- **AG0001** (`NoClassInAbstractionsAnalyzer`) forbids a public plain class in an `.Abstractions` namespace or any namespace nested under one.
- **AG0025** (`OneOwnerPerInterfaceAnalyzer`) allows at most one named type per compilation to implement an owner interface, and carries `WellKnownDiagnosticTags.CompilationEnd`.

Between them they require four capabilities of any mechanism:

1. **Resolve a name to its definition.** AG0011 must know a call really is `System.IO.File.ReadAllText` and not a method that shares the name. A mechanism matching text is defeated by an alias, and laundering a call past a rule is the behaviour this project exists to catch.
2. **Know where a declaration sits.** AG0011 needs the assembly; AG0001 needs the namespace.
3. **Inspect a declaration's shape** — kind, base list, members and body.
4. **Accumulate across the whole program and report at the end.** AG0025 cannot be answered one file at a time.

A mechanism missing any one of these cannot carry these rules.

## Python: a pylint checker

Pylint is the only Python linter that takes a custom rule and runs on astroid, which does inference.

| capability | how pylint answers it |
|---|---|
| resolve a name | astroid inference; a call's target is inferred to its definition and `qname()` gives the fully qualified name, so an aliased import still resolves |
| where it sits | `node.root().name` is the module's dotted path, its first segment the package |
| shape | `ClassDef.ancestors()` resolves the base list through aliases; the body is astroid nodes |
| whole program | `BaseChecker` has `open()` and `close()`; `close()` runs after the whole set of modules, mirroring `CompilationEnd` |

A rule subclasses `BaseChecker` from `pylint.checkers` with a `name` and a `msgs` dictionary mapping an id to the displayed message, the symbol and the description — the same three things a `DiagnosticDescriptor` carries. It defines `visit_<node>` and `leave_<node>` methods and calls `self.add_message(symbol, node=node)`. A module-level `register(linter)` function registers it.

Custom checkers take 51 to 99 as the first two digits of the message id, reserved for them, so ours are `W51xx` with symbols that read in English. Tests use `pylint.testutils.CheckerTestCase` with `assertAddsMessages` and `assertNoMessages`, mirroring the Roslyn analyzer tests. It runs as `pylint --load-plugins=<package>`, beside `ruff check` for the standard rule set.

## Rust: a dylint lint, as a LateLintPass

Dylint runs lints written as a dynamic library loaded by `cargo dylint`, without forking clippy. A `LateLintPass` runs after type checking; an `EarlyLintPass` sees syntax only and fails capability 1.

| capability | how a LateLintPass answers it |
|---|---|
| resolve a name | `LateContext` carries `TyCtxt`, so a call resolves to a `DefId` and `def_path_str` gives its fully qualified path |
| where it sits | the crate name is the assembly; the module path off the `DefId` is the namespace |
| shape | the HIR gives the item, its body and its signature; trait implementations are answerable through the type context |
| whole program | `check_crate_post` on `LateLintPass`, mirroring `CompilationEnd` |

`cargo dylint new <name>` generates a library that loads unchanged; `declare_late_lint!` names the lint with its level and description; libraries are listed in `workspace.metadata.dylint.libraries` in `Cargo.toml` or in `dylint.toml`, with per-library settings in `dylint.toml` keyed by name. Tests use the `ui_test` function dylint provides. It runs as `cargo dylint --all`, beside `cargo clippy -- -D warnings`.

## Why the alternatives were rejected

| rejected | why |
|---|---|
| ruff | takes no custom rules; its FAQ names that as its main limitation against flake8 |
| flake8 plugin | the standard library `ast`, no inference; an aliased import defeats it |
| import-linter | reasons about imports only; cannot say a call is allowed inside the one class implementing a given Protocol |
| clippy | runs a fixed set and will not load a custom rule |
| `clippy.toml` disallowed-methods | a flat ban with no exemption; AG0011 is "banned everywhere except the one owner", which the configuration cannot express |
| cargo-pup | a builder DSL over the same rustc internals, version 0.1.8; its vocabulary is boundaries, so it cannot express the body-shape rules AG0106, AG0115 and AG0116 |
| EarlyLintPass, ast-grep, semgrep | syntax only; they cannot resolve a name to its definition |

Ruff and clippy are still the right standard rule set for each language, and import-linter is still the right tool for Python layering contracts. None of the three carries an architecture rule of ours.

## How the existing rules map

| kind of rule | example | Python | Rust |
|---|---|---|---|
| a primitive banned outside its one owner | AG0011 | infer the call target, compare `qname()` against the owned table, allow only inside the class implementing the owning Protocol, in the owning package | resolve the `DefId`, compare `def_path_str` against the owned table, allow only in the impl of the owning trait, in the owning crate |
| a declaration kind banned in a place | AG0001 | `visit_classdef`, check the module's dotted name | `check_item`, check the module path |
| exactly one implementer | AG0025 | accumulate in `visit_classdef`, report in `close()` | accumulate, report in `check_crate_post` |
| a contract's shape | AG0003–AG0005 | `visit_classdef` over the Protocol and its members | `check_item` over the trait and its items |
| body shape: no control flow, state or async | AG0106, AG0115, AG0116 | `visit_functiondef`, walk the body nodes | `check_fn` over the HIR body |
| construction only through a factory | AG0017 | `visit_call`, infer the target class | `check_expr` on a call |
| an interop signature rule | AG0102–AG0104 | does not arise; Python has no P/Invoke | `check_foreign_item` |

## The standard

One rule is one class, in the language's own plugin mechanism, carrying one id, one symbol that reads in English, one message and one description. The table of owned primitives lives once per language, beside the rules, as `OwnedPrimitives` does in C#. Every rule has a test that fails before it passes. The build fails on a violation, and the only way round it is a suppression, which is visible and greppable in all four languages.

## Estimate

The first rule in each language carries the scaffolding — the plugin package, the owned table, the test harness and the CI step. Two days for Python; three for Rust, where the extra day is the pinned toolchain and the CI caching rather than the rule. After that a rule is one class and its tests, roughly half a day in either language. Rules are ported as a guarded project needs them, not all 57 at once.

## Costs and what is not established

- Dylint rules are written against compiler internals that carry no stability guarantee, so a toolchain bump can break a lint. Dylint builds the lint library with the target's toolchain, so CI must cache the cargo directories, `target/`, `~/.dylint_drivers/` and `~/.rustup/toolchains/`.
- Pylint's documentation describes `close()` as running after the whole set of modules, which is what the whole-program rules need, but no worked example of reporting a message from inside `close()` was found. That gets proven by the first Python rule rather than assumed.

## Scope

This issue is the rule-enforcement mechanism only. It is not the architecture language, which is issue #82's road and its step issues. Nothing here is built until a guarded project has Python or Rust source.

The full write-up, with sources, is in this issue's work folder: `custom-rules.md` (the standard and the mapping) and `linters.md` (what each language's standard linter can carry by configuration).

