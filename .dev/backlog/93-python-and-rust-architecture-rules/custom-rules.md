# The standard: how an architecture rule is written in each language

Written 26 October 2026. One answer per language, to be set as the standard.

**Python: a pylint checker. Rust: a dylint lint, written as a `LateLintPass`.**

Both are the mirror of what the 57 Roslyn analyzers already do in C# and what an ESLint plugin does
in TypeScript: a rule is a class, with an id, a message and a description, that walks a resolved
syntax tree and reports at the site of the violation, failing the build.

## What our existing rules need, read out of them

Three rules, read in full, set the bar every mechanism has to clear.

**AG0011, `RawPrimitiveOnlyInOwnerAnalyzer`.** A raw OS or CLR primitive is allowed only in the class
that implements its owning interface *and* compiles into the owner assembly — its own summary says
the conjunction applies "so a class cannot self-grant by declaring the interface in the wrong
assembly".

**AG0001, `NoClassInAbstractionsAnalyzer`.** A public plain class may not be declared in an
`.Abstractions` namespace or any namespace nested under one.

**AG0025, `OneOwnerPerInterfaceAnalyzer`.** At most one named type per compilation may implement a
given owner interface. It carries `WellKnownDiagnosticTags.CompilationEnd`, because the second
implementer is only knowable once every type has been seen.

From those, four capabilities:

1. **Resolve a name to its definition.** AG0011 has to know a call really is `System.IO.File.ReadAllText`,
   not a method that happens to share the name. A mechanism that matches text can be defeated by an
   alias, and laundering a call past a rule is the exact behaviour this project exists to catch.
2. **Know where a declaration sits.** AG0011 needs the assembly, AG0001 needs the namespace.
3. **Inspect a declaration's shape.** Its kind, its base list, its members and its body.
4. **Accumulate across the whole program and report at the end.** AG0025 cannot be answered one file
   at a time.

A mechanism missing any one of these cannot carry our rules.

## Python: a pylint checker

Pylint is the only Python linter that takes a custom rule **and** runs on astroid, which does
inference. That is the whole argument.

| capability | how pylint answers it |
|---|---|
| resolve a name | astroid inference: a call's target is inferred to its definition and `qname()` gives the fully qualified name, so an aliased import still resolves to `os.environ` |
| where it sits | `node.root().name` is the module's dotted path; its first segment is the package — the Python forms of namespace and assembly |
| shape | `ClassDef.ancestors()` resolves the base list through aliases; the body is astroid nodes, so a body-shape rule is a walk |
| whole program | `BaseChecker` has `open()` and `close()`; `close()` runs after the whole set of modules, which is the mirror of `CompilationEnd` |

**The shape of a rule.** Subclass `BaseChecker` from `pylint.checkers`. Give it a `name`, and a
`msgs` dictionary mapping a message id to a tuple of the displayed message, the symbol and the
description — the same three things a `DiagnosticDescriptor` carries. Define `visit_<node>` and
`leave_<node>` methods for the node kinds the rule cares about, and call
`self.add_message(symbol, node=node)` to report at the site. A module-level `register(linter)`
function registers it.

**Ids.** Custom checkers take 51 to 99 as the first two digits of the id, reserved for them, so ours
are `W51xx` with symbols that read in English — `raw-primitive-outside-owner`, not a number.

**Tests.** `pylint.testutils.CheckerTestCase` with `CHECKER_CLASS` set, `astroid.extract_node` to
build the source under test, and `assertAddsMessages` with a `MessageTest` or `assertNoMessages`.
That is the same red-first shape the Roslyn analyzer tests already use.

**How it runs.** `pylint --load-plugins=agentguard_rules`, failing the build, beside `ruff check`
for the standard rule set.

## Rust: a dylint lint, as a LateLintPass

Dylint runs lints we write as a dynamic library loaded by `cargo dylint`, without forking clippy.
The lint is written as a `LateLintPass`, which runs **after type checking** — that is the whole
argument, because an `EarlyLintPass` sees syntax only and fails capability 1.

| capability | how a LateLintPass answers it |
|---|---|
| resolve a name | `LateContext` carries `TyCtxt`, so a call resolves to a `DefId` and `def_path_str` gives its fully qualified path |
| where it sits | the crate name is the assembly; the module path off the `DefId` is the namespace |
| shape | the HIR gives the item, its body and its signature; trait implementations are answerable through the type context |
| whole program | `check_crate_post` on `LateLintPass`, the mirror of `CompilationEnd` |

**The shape of a rule.** `cargo dylint new <name>` generates a library that loads unchanged.
`declare_late_lint!` names the lint, its default level and its description, and the pass implements
the `check_*` methods it needs. Lints are listed in `workspace.metadata.dylint.libraries` in
`Cargo.toml`, or in `dylint.toml`, and per-library settings go in `dylint.toml` keyed by name.

**Tests.** The `ui_test` function dylint provides, which compiles a fixture and compares the
diagnostics against an expected file.

**How it runs.** `cargo dylint --all` beside `cargo clippy -- -D warnings`. CI caches the cargo
directories, `target/`, `~/.dylint_drivers/` and `~/.rustup/toolchains/`, which dylint's own
documentation lists, because it builds the lint library with the target's toolchain.

**The cost, stated plainly.** Rules are written against compiler internals that carry no stability
guarantee, so a toolchain bump can break a lint. That is the price of capability 1 in Rust and there
is no mechanism that gives it without paying.

## Why each of the alternatives fails

| rejected | why |
|---|---|
| ruff | takes no custom rules at all; its own FAQ names that as its main limitation against flake8 |
| flake8 plugin | the standard library `ast`, with no inference. An aliased import defeats it, which is laundering — the thing we build against |
| import-linter | imports only. It cannot say "this call is allowed inside the one class that implements `IEnvironment`". Good at layering, not a custom-rule mechanism |
| clippy | runs a fixed set and will not load a rule of ours |
| `clippy.toml` disallowed-methods | a flat ban with no exemption. AG0011 is "banned everywhere **except** the one owner", and the configuration has no way to say the exception |
| cargo-pup | a builder DSL over the same rustc internals, at 0.1.8 from one vendor. Its vocabulary is boundaries, so it cannot express the body-shape rules — no control flow, no state, no async inside a native-ops implementer |
| EarlyLintPass, ast-grep, semgrep | syntax only. They fail capability 1 |

Two of those are still worth running for what they are good at — ruff as Python's standard rule set,
clippy as Rust's — but neither carries an architecture rule of ours.

## How our existing rules map

| kind of rule | example | the Python rule | the Rust rule |
|---|---|---|---|
| a primitive banned outside its one owner | AG0011 | infer the call target, compare its `qname()` against the owned table, allow only inside the class implementing the owning Protocol, in the owning package | resolve the `DefId`, compare `def_path_str` against the owned table, allow only in the impl of the owning trait, in the owning crate |
| a declaration kind banned in a place | AG0001 | `visit_classdef`, check the module's dotted name | `check_item`, check the module path |
| exactly one implementer | AG0025 | accumulate in `visit_classdef`, report in `close()` | accumulate, report in `check_crate_post` |
| a contract's shape | AG0003, AG0004, AG0005 | `visit_classdef` over the Protocol and its members | `check_item` over the trait and its items |
| body shape: no control flow, no state, no async | AG0106, AG0115, AG0116 | `visit_functiondef` and walk the body nodes | `check_fn` over the HIR body |
| construction only through a factory | AG0017 | `visit_call`, infer the target class | `check_expr` on a call |
| an interop signature rule | AG0102, AG0103, AG0104 | does not arise; Python has no P/Invoke | `check_foreign_item` |

## What it costs

The first rule in each language carries the scaffolding: the plugin package, the owned table, the
test harness and the CI step. Two days for Python. Three for Rust, and the extra day is the pinned
toolchain and the CI caching, not the rule.

After that a rule is one class and its tests, and the per-rule cost is roughly half a day in either
language. Rules are ported as a guarded project needs them, not all 57 at once.

## The standard, stated

One rule is one class, in the language's own plugin mechanism, carrying one id, one symbol that
reads in English, one message and one description. The table of owned primitives lives once per
language, beside the rules. Every rule has a test that fails before it passes. The build fails on a
violation, and the only way round it is a suppression, which is visible and greppable in all four
languages.
