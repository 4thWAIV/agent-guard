---
name: rails-read-me
description: Read first — the repository facts, locations, build and shipping rules, protected areas, and routes to the skills that own the working rules.
---

# How to work in this codebase

## What this repo is

agent-guard is a tool whose sole job is to stop a lazy AI from disabling or cheating the guard and the golden build. The adversary is the AI taking the easy path and papering over it — not a malicious attacker. It is not a hardened security product; the bar is: **make cheating more work than doing the right thing.**

It has two layers: the **guard product** (protects a project's files and config, installed through the CLI, cross-platform, with signed releases) and the **rails** (the analyzer fence, the workflow, and these skills that keep the AI honest while building it).

## The code and how to build it

A C#/.NET solution — `AgentGuard.sln`, with the SDK pinned in `global.json` (currently .NET SDK `10.0.400`, roll-forward disabled — that exact SDK must be installed or the build fails to resolve one). The product is in `src/`: the guard `Engine`, the `Cli`, the `Abstractions` contracts, the `Boundaries`, and the `CrossPlatform` base with its per-OS `.Linux`/`.MacOS`/`.Windows` libraries. Tests are in `tests/`, the analyzer fence in `analyzers/`, the build/sign/coverage/release scripts in `eng/`, and CI in `.github/workflows/`. Build and test with `make build` and `make test` (equivalently `dotnet build` / `dotnet test`); the analyzers run as build errors.

The **`gate`** referenced throughout is the required CI status check — green only when build, tests, coverage (`eng/coverage-gate.sh`, a 75% floor), and the analyzers all pass on macOS, Linux, and Windows, defined in `.github/workflows/ci.yml`. Locally, `make build && make test` runs build, tests, and the analyzers; the coverage floor and the cross-OS legs are enforced by CI, so run the local pass green before you push.

## Skill routing

Load the owner instead of copying its rules here:

- `rails-decisions` owns the decision boundary, approval procedure, and decision violations. The Lie-catcher rules against it.
- `rails-run-a-workflow` owns level selection, stage execution, role authority, retries, the gate, provenance, and reporting.
- `rails-write-a-contract` owns the complete contract contents and section order.
- `rails-explorer` owns live-code exploration and the evidence required to establish ground truth.
- `rails-solid-code` owns structural design rules, including the rule against language escape hatches.
- `rails-dry-code` owns duplication and prior-art requirements.
- `rails-real-work` owns completeness, shortcuts, hacks, stale evidence, and report quality.
- `rails-test-code` owns test completeness, RED-first behavior, assertions, integration coverage, test tampering, and pinned-oracle rules.
- `rails-real-work` owns the communication standard that binds every worker and adversary: lead with the verdict, answer the literal question, plain vocabulary and concrete values, problems before successes.

## How work runs

**Substantive work** is anything that mutates state or behavior, ships or changes code, claims a fix, adds a capability, or changes a validator / gate / process — as opposed to a trivial doc edit, a read, or an implementation detail inside an already-approved unit — where "already-approved" covers only the exact item that got a yes, not an inflated reading of it. When in doubt, treat it as substantive.

**Substantive work runs through the staged process in `rails-run-a-workflow`, and that document is the ONLY definition of it.** This file deliberately does not list the stages, name the roles, or describe what any stage does.

**Before your first substantive change you must read `rails-run-a-workflow` in full**, and the `rails-write-a-contract` contract guide it depends on. It names every other rail and skill the process uses, and every script that runs a stage — go to it for those, never to this file.

One rule here rather than in the process, because it applies to every deferral whether or not a run is in flight:

- **File deferred work as a GitHub issue the moment it's deferred** — via the `gh` CLI (`gh issue create`) — with the real requirements in the body. A deferral recorded only as a note or memory is untracked and won't happen. Filing an issue also creates its work folder, `.dev/backlog/<issue-number>-<slug>/`.

## Where things live

See `.dev/README.md`. Work moves **backlog → inprocess → completed**. `reference/` holds living docs (this process, decisions, and the **best-practices guide** at `.dev/reference/best-practices-guide.md` — the layered constitution the rails delegate to, which a decision is checked against before it ever reaches the human); `archive/` holds historical / non-workflow material.

**Derive the current state, never read it from a status document.** There is no checked-in index of what exists and what is done. List the parts from the source: the rails are `ls .agents/skills/`, the workflow scripts are `ls .agents/workflows/`, shipped work is `ls .dev/completed/run-records/`, work in flight is `ls .dev/inprocess/`, and the analyzer rules are `analyzers/AgentGuard.Analyzers/AnalyzerReleases.*.md`. What is deferred, planned, or undecided lives in the GitHub issues (`gh issue list`) — that is the state tracker.

**The roadmap lives in two places, with two jobs.** `.dev/backlog/` holds the design and plan documents — the long-form thinking about a piece of work. The GitHub issues (`gh issue list`) hold the tracked work items, their order, and what is still open. Read the backlog document to understand what a piece of work is; read the issues to find what is next.

**One folder per work item, created once and never duplicated.** Filing a GitHub issue creates its work folder at `.dev/backlog/<issue-number>-<slug>/`. The folder travels with the work through `backlog`, `inprocess` and `completed`, and is kept for good in whichever of the three the work currently sits in. It holds `issue.md` with the issue body, which the live GitHub issue always supersedes, plus any design document, `conversation.md` — the summary of what the human decided in conversation — and, once the work starts, the contract and the run records. A run never creates a second folder. `rails-run-a-workflow` owns when the folder moves, what a run keeps inside it, and how work the human starts without an issue gets its folder.

**The rails skills are the checked-in source at `.agents/skills/rails-*`** (`.claude/skills` symlinks to it); the workflow scripts are `.agents/workflows/*.js` (`.claude/workflows` symlinks to it too).

## Shipping and branch protection

Work integrates on `dev` and is cut to `main` only at a milestone worth releasing — never just because `dev` is green and ahead. PRs into `dev`/`main` require one approving review and a green `gate` status check; **repo admins bypass both**, so a maintainer can direct-push a trivial docs or bookkeeping change, but real code always goes through the PR and the gate. **File the run-record inside the shipping PR, before merge** — the `inprocess/`→`completed/` move rides the same PR, and REPORT sweeps the whole `inprocess/` tree for any straggler whose work already shipped (detail in `rails-run-a-workflow`, Provenance).

## Protected areas

`analyzers/**` (the enforcement rules) would normally sit **behind the guard we are building**, and will as soon as the guard supports both *enforcing* them **and** *allowing selective, controlled changes*. Until then a change is **decision-gated** — brought to the human first (new rules via `rails-run-a-workflow`'s RULE-PHASE).

`src/AgentGuard.Abstractions/**` is **not** locked or frozen. Adding or changing a contract interface needs the human's sign-off (like any interface — a design element that outlives its function) and is held to the contract-pattern analyzers, but interfaces are **developed properly when that is the right design, not avoided.**


## Rule-Driven Development

Architecture is fenced by analyzer rules written **before** the code they govern, so code is born compliant; the only way to break the architecture afterward is to **suppress** a rule — a visible, greppable act the Lie-catcher catches. The full operational process — the rule phase, the implementation phase, the revoked rule-writing authority, the cleanup law, and the separation of powers — lives in **`rails-run-a-workflow`**.

*Not yet automated:* the workflow machinery — the rule-generation token grants and the paired analyzer/implementation workflows — is future work (it rides on the guard's minting/grant system). Until it exists, the two phases are approximated manually within a contract.

## Repository tools

A `.codegraph/` index, when present, lets CodeGraph return source and call paths. Don't create an index unprompted. The searchable code trees are `src`, `tests`, `analyzers`, and `eng`. `rails-explorer` owns how to establish ground truth, and `rails-dry-code` owns the CodeGraph and grep lenses used by the prior-art ledger.

## Claude and Codex memory stores

Claude Code's repository-specific memory store for this checkout is `~/.claude/projects/-Users-timothystockstill-code-macos-4thWAIV-agent-guard/memory/`.

Codex's memory store is shared at `~/.codex/memories/`. Only memory entries related to agent-guard are in scope when this skill is loaded.

When either memory store contains an entry matching a loaded rail, make the entry carry the rail's current wording while retaining the entry's existing incident context. Bring stale wording up to date and add missing content. Update, never delete.
