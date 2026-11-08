# The linting system we adopt for each language

Rewritten 26 October 2026. Tim's instruction was to come back with the correct normal custom linting
system for each language, to be incorporated into the project, using what already exists rather than
building anything. This is that report.

The pattern is the same in every language and has three tiers. The standard linter brings its own
rule set for free. A large share of architecture rules are then configuration in that same linter and
need no code at all. Only what configuration cannot say is written as a custom rule.

## The table

| language | the standard linter | architecture rules by configuration | custom rule code when needed |
|---|---|---|---|
| C# | Roslyn analyzers, already in the build | — | `DiagnosticAnalyzer` classes, 57 of them already written |
| TypeScript | ESLint, already in the build | — | a local plugin registered under `plugins` in `eslint.config.js` |
| Python | **ruff** | **import-linter** contracts, and ruff's own `banned-api` | **pylint** custom checkers |
| Rust | **clippy** | **`clippy.toml`** `disallowed-methods`, `disallowed-types`, `disallowed-macros` | **dylint** lints, or **cargo-pup** for architecture |

## Python

**The standard linter is ruff.** It brings the rule sets of flake8, isort, pyupgrade, pydocstyle and
the rest in one binary, configured under `[tool.ruff]` in `pyproject.toml` and run with `ruff check`.
It cannot take custom rules: its own FAQ names the absence of custom lint rules as its main
limitation against flake8. That is not a reason to pass it over; it is the reason the next two rows
exist.

**Architecture rules, with no code written.**

*Banned imports and banned calls.* Ruff's `banned-api` rule, `TID251`, inherited from
flake8-tidy-imports, bans a module or a member of a module project-wide. It is configured with
`lint.flake8-tidy-imports.banned-api` and `lint.flake8-tidy-imports.extend-banned-api`, each entry
being the banned import and the message to print. `TID253`, `banned-module-level-imports`, bans an
import at module level only. That is the Python form of "nothing may call this directly".

*Layering and boundaries.* **import-linter**, version 2.15, BSD 2-Clause, whose summary is "Lint your
Python architecture". It reads contracts from a config section and fails when one is broken. It has
three contract types, and all three are architecture statements:

- `forbidden` — one set of modules never imports another, including by an indirect path. This is
  "the engine may not reach the boundaries".
- `independence` — a set of modules have no imports between them in either direction.
- `layers` — higher layers may import lower ones and never the reverse, including indirectly.

A forbidden contract is written like this:

```ini
[importlinter]
root_package = myapp

[importlinter:contract:no-billing-in-web]
name = Web must not import billing
type = forbidden
source_modules =
    myapp.web
forbidden_modules =
    myapp.billing
```

A layers contract names the layers in order, highest first, under a container package. Import Linter
also accepts custom contract types for what the three do not cover.

**Custom rule code, when configuration cannot say it.** A **pylint** checker. It subclasses
`BaseChecker` from `pylint.checkers`, carries a `name` and a `msgs` dictionary where each entry is a
message id, a symbol and a description, and defines `visit_` and `leave_` methods for the node kinds
it cares about, calling `self.add_message` to report. A module-level `register(linter)` function
registers it, and it is loaded with `load-plugins`. Custom checkers take message ids beginning 51 to
99 so they never collide with pylint's own. It runs on astroid, which does inference, so a checker
can reason about what a name actually refers to rather than only what the text says.

**How it is incorporated.** `pyproject.toml` carries `[tool.ruff]` and the import-linter contracts,
`.pylintrc` carries `load-plugins`, and the CI job runs `ruff check`, `lint-imports` and `pylint`.
Nothing is compiled.

## Rust

**The standard linter is clippy.** It ships with the toolchain and is run with `cargo clippy`,
failing the build with `-D warnings`. It runs a fixed rule set and will not load a rule of ours.

**Architecture rules, with no code written.** `clippy.toml` at the workspace root configures three
lints that are exactly the shape of this project's boundary rules:

- `disallowed-methods` — the `disallowed_methods` lint
- `disallowed-types` — the `disallowed_types` lint
- `disallowed-macros` — the `disallowed_macros` lint

Each entry carries `path`, the fully qualified item to disallow, and optionally `reason`,
`replacement`, and `allow-invalid` for a path that may not exist. Written out:

```toml
disallowed-methods = [
    { path = "std::env::var", reason = "goes through the environment port", replacement = "crate::ports::env::get" },
]
disallowed-types = [
    { path = "std::collections::HashMap", reason = "iteration order is not deterministic" },
]
```

That is the Rust equivalent of AG0011, the rule that keeps raw primitive calls out of everything but
the one owner, and it is configuration rather than code. Clippy searches for the file from
`CLIPPY_CONF_DIR`, then `CARGO_MANIFEST_DIR`, then the current directory, walking up to the root. The
config file is marked unstable in clippy's own documentation.

Module boundaries in Rust are also enforced by the compiler itself through `pub(crate)` and module
visibility, before any linter is involved.

**Custom rule code, when configuration cannot say it.** **dylint**, from Trail of Bits. A lint is a
dynamic library implementing `LateLintPass`, so it has the full compiler semantics. `cargo dylint new
<name>` generates one, `cargo dylint --all` runs every library listed in
`workspace.metadata.dylint.libraries` in `Cargo.toml` or in `dylint.toml`, and per-library settings
go in `dylint.toml` keyed by library name. It needs `cargo install cargo-dylint dylint-link`, and
rules are written against compiler internals that carry no stability guarantee.

**cargo-pup** is the architecture-specific alternative: version 0.1.8, from Datadog, first published
June 2025 and updated June 2026, described as a Rust architectural linting tool that integrates with
rustc to enforce architectural patterns and boundaries. It is the nearest thing Rust has to
import-linter. At 0.1.8 and about five and a half thousand downloads it is young, and that is the one
judgement call in this report.

**How it is incorporated.** `clippy.toml` at the workspace root, `cargo clippy -- -D warnings` in CI,
and `cargo dylint --all` beside it when a custom lint exists. Dylint builds the lint library with the
target's toolchain, so CI caches `~/.cargo`, `target/`, `~/.dylint_drivers/` and `~/.rustup/toolchains/`,
which the dylint documentation lists. That caching is the real cost of dylint, and it is the reason
to reach for configuration first and cargo-pup second.

## C# and TypeScript, for completeness

C# is Roslyn analyzers, which this repository already runs as build errors on three operating
systems, with rule ids `AG0001` to `AG0041`, `AG0101` to `AG0116` and `AGS5443`. TypeScript is
type-aware ESLint through `eslint.config.js`; a custom rule there is a local plugin module exporting
a `rules` object, registered under `plugins` in the flat config. Neither changes.

## The one thing this leaves open

Each language now has its own place to say an architecture rule: analyzer classes in C#, an ESLint
plugin in TypeScript, import-linter contracts and ruff bans in Python, `clippy.toml` entries in Rust.
Those four places hold the same statements written four ways. Whether the architecture document
emits them, or a person keeps the four in step by hand, is the next decision, and it is not part of
this report.

## Sources

Ruff's FAQ and its `banned-api` rule page, Import Linter's contract-type documentation and its PyPI
release, pylint's custom-checker how-to, clippy's configuration and lint-configuration pages, the
dylint repository, and the cargo-pup entry on crates.io.
