# Agent-Guard — Design & Planning: the Guard Engine and the File Guard

## Status and scope
This is a living design record, not a contract: it carries rationale, which a contract must not.
Every item below is marked **AGREED** or **OPEN**. Open items are decisions we have not made yet, and
must not be read as settled.

This document covers exactly one level of the system: the **Guard Engine** (the single hook entry, the
pipeline, and the stores) and the **File Guard** (write-block plus drift-restore). It deliberately excludes
the Grant CLI internals, the signed-exemption / lint gate, the per-language analyzers, the invocation-block,
key storage, and the contents of the language fixtures. Those are other levels and get their own documents.

## 1. What we are building (context)
Agent-guard is a standalone, multi-language, open-source agent-governance tool whose one job is to stop a
**lazy** AI — not a malicious attacker — from cheating the golden build by disabling analysis. It is being
built in its own repository and will be imported into the C# proteus project later. It must eventually govern
four languages — C#, TypeScript, Python, and Rust — each with a fixture project for testing; the first release
covers three (C#, TypeScript, Rust), with Python following. The guiding
principle is that the AI can never authorize its own exception; only a human holding the signing key can.

## 2. Locked decisions that shape this level (AGREED)
- **Engine runtime is C#/.NET.** The rationale is the threat model, not speed: a C# engine is harder for a
  lazy AI, which is most fluent in TypeScript, to quietly subvert. The 600-second hook timeout makes startup
  latency irrelevant, so speed was never the real reason. The 4thWAIV TypeScript engine is a behavioral
  reference to port from, not a tree to copy.
- **Both host runtimes are targets:** Claude Code and Codex.
- **Config-driven with a hardcoded floor:** the only thing hardcoded in source is the guard's own shipped
  code; every consuming-project path and all runtime wiring live in configuration.
- **Threat model:** a lazy cheater. Make the easy cheats harder, iterate on where the AI actually goes, and
  do not over-engineer for extreme measures.

## 3. Glossary (AGREED)
The agreed vocabulary for this level. Names in **bold** are load-bearing.

**Runtime and orchestration**
- **Engine** — the whole shipped guard runtime: the pipeline, the registry, and the stores.
- **`guard` CLI in hook mode** — the single executable and primary entry point; the host wires its hook to
  `guard hook <event>`. There is no separate "entry" concept.
- **Pipeline** — for one event, runs the registered Guards in order and aggregates their Verdicts into one
  block-or-allow exit decision.
- **Guard Registry** — where Guards are registered so the Pipeline discovers them by lookup; adding a Guard
  is adding a module, not editing a central switch.

**The pluggable units, at four levels**
- **Guard** — a pipeline mechanism (block, snapshot, restore). Few exist. The **File Guard** is the one for
  protected files.
- **Rule** — a per-path policy the File Guard applies: a path matcher, the Grant kind it requires, and the
  Verifier that judges a change to it. Many exist; they are data the one File Guard runs.
- **Verifier** — a reusable change-judgment strategy a Rule names for its Postcheck. Shared across Rules.
  Instances: **NoChangeVerifier** now; the per-language project Verifiers later — **CSharpProjectVerifier**,
  **NodeProjectVerifier**, **RustProjectVerifier** for the starting three, and **PythonProjectVerifier** after.
- **Provider** — a language plugin that bundles a set of Rules, a Baseline, and the Verifier(s) those Rules
  use (and later that language's Grants). Compiled in for v1; runtime plugin discovery is a later concern.
  Instances: **CSharpProvider**, **NodeProvider**, **RustProvider** ship to start; **PythonProvider** follows.
- **Baseline** — a Provider's opt-in recommended default set of its own Rules and settings, applied as a
  template over your ruleset when you enable it, the way a lint "recommended" set works.

**Data the Pipeline carries**
- **ToolCall** — the normalized, host-agnostic tool call: tool name, tool input, ToolCallId.
- **ToolCallId** — the per-call correlation id (the host's `tool_use_id`) that ties a Pre to its Post.
- **Environment** — the call's surroundings: project root, event name, session.
- **Verdict** — a Guard's judgment: allow, deny with a reason, or warn with a message.
- **Effect** — a privileged side effect a Guard requests and only the Engine executes, such as restoring
  bytes, so every protected-path write stays in one audited place.

**The Context**
- **Context** — the per-ToolCall store, persisted between the separate Pre and Post processes, that Capture
  writes and Postcheck reads. Addressed by the triple (ToolCallId, Guard, kind). The Engine writes it after
  Pre, loads it before Post, and deletes it after Post. For the File Guard it holds the **pre-image snapshot**;
  because the store is keyed by kind, a future Guard can hold a different kind of data with no change to the
  Engine.

**A Guard's three behaviors**
- **Precheck** — the Pre-phase verify: produce a Verdict. For the File Guard this is the core-system block —
  deny a write that touches the Sealed or System set, the only things Post cannot undo.
- **Capture** — the Pre-phase prepare: write into the Context what the Post phase will need — for the File
  Guard, the pre-image snapshot.
- **Postcheck** — the Post-phase verify: produce a Verdict plus Effects. For the File Guard this is the full
  drift-and-revert over the configurable protected files, answering Coverage and then Conformance from the
  bytes that landed.

**Authorization**
- **Grant** — a signed capability token authorizing a Guard to permit something it would otherwise block.
  **File Grant** is the File Guard's grant; language-specific Grants for other Guards (for example lint) come
  later, as in 4thWAIV.
- **Grant Scope** — what a Grant authorizes: **All** in v1 (any change to the covered path) or **Part** later
  (a language-specific envelope). "All" is just the first Scope.
- **Coverage** — the all-or-none question of whether a change is authorized at all: does a Grant cover this
  path, or does an approved command account for it. Answered at Postcheck for the configurable protected files,
  and at Precheck only for the System set (a Grant in v1; the approved-command allowance is deferred).
- **Conformance** — the Postcheck, change-level question a Verifier answers: is the landed change inside the
  Grant's envelope.

**The protected set**
- **Protected Set** — the union of all active Rules' matchers that the Engine matches a write against.
- **Sealed** — built-in, unremovable Rules that no Grant ever unlocks: the Grant store and the Context store.
- **System** — built-in, unremovable Rules that a Grant can unlock: the guard's shipped code and runtime
  wiring.
- **Provider** and **Project** — the configurable Rule sources (a Provider's bundle; the repo's own entries);
  together they are the configurable protected files that Postcheck governs, as opposed to the Sealed and System
  floor that Precheck blocks.

## 4. The Engine and the Pipeline (AGREED)
The host invokes the `guard` CLI in hook mode on both `PreToolUse` and `PostToolUse`, passing the event name
on argv and the tool-call payload on stdin. The Pipeline parses and normalizes that payload into a **ToolCall**
and an **Environment**, runs the registered Guards for the event in order, aggregates their Verdicts, and maps
the result to the host's exit code: any single deny blocks the call, and warnings are advisory. The Engine
itself understands only the three Verdict shapes and knows nothing about paths, Grants, or snapshots.

Pre and Post are **separate process invocations** of the same binary, so any state that must cross from Pre to
Post is persisted in the **Context**, keyed by ToolCallId; nothing is held in memory between the two.

## 5. The Guard / Rule / Verifier / Provider model (AGREED)
There are four distinct things at four levels, and keeping them apart is what makes the system extensible
without duplicating logic:
- A **Guard** is the mechanism. The single **File Guard** owns the block/snapshot/restore machinery, so that
  logic exists exactly once.
- A **Rule** is a per-path policy the File Guard applies: matcher, required Grant, Verifier. Rules are data,
  not new mechanisms.
- A **Verifier** is a shared change-judgment strategy a Rule points at; many Rules share one Verifier (dozens
  point at NoChangeVerifier; C# project files point at CSharpProjectVerifier).
- A **Provider** is a language bundle that ships many Rules, a Baseline, and the Verifier(s) those Rules use.

The chain is: a Provider ships many Rules; each Rule names one shared Verifier; the one File Guard applies all
active Rules and runs their Verifiers.

## 6. The File Guard across the two phases (AGREED)
The division of labor is deliberately lopsided. **Precheck blocks only what Post cannot undo**; everything else
is left for Post to catch from the bytes that landed. Post can revert any change it can both see and that does
not disable Post itself, so the only writes that must be stopped before they land are the ones that fail one of
those two tests: the Context store, which is excluded from the drift comparison so a write into it never appears
as drift; and the runtime wiring and the guard's own installed code, a change to which would stop Post running
correctly on the next call. The Grant store is sealed alongside the Context store so no forged token can be
slipped in ahead of the check. Those, and only those, are Precheck's business.

### Pre phase — two responsibilities, enforced apart
- **Precheck** is the core-system block. It denies a write that touches the **Sealed** set (the Grant store and
  the Context store) unconditionally, and one that touches the **System** set (the guard's shipped code and the
  specific runtime-wiring files) unless a Grant covers it — the approved-command allowance that would also let
  the guard's own setup command through is deferred from v1. It does not ask the
  broad Coverage question for ordinary protected files, and it does not try to find every write target in an
  arbitrary command: because no legitimate tool call ever writes the core system, Precheck is conservative and
  denies on any reference to those few fixed paths. Precheck returns a Verdict only.
- **Capture** writes the pre-image snapshot into the Context for every configurable protected path a change
  could touch. It runs only for a ToolCall that is not being denied.

The split is enforced by architecture, not developer discipline, on two mechanisms:
1. **Capability.** Precheck is handed only read-only inputs and can return only a Verdict; Capture is the only
   method given a writable Context handle, and the Engine never hands a Guard the store itself, so a Precheck
   has nothing to write the Context with. Because Guards are compiled into the Engine assembly this rests on the
   Engine's wiring, not an assembly boundary — enough for the lazy threat; a separate Guards assembly is the
   lever only if the threat model ever widens to a malicious Guard author.
2. **Engine-owned sequence.** The Engine calls every Guard's Precheck, aggregates, and only if the aggregate
   is not a deny does it then call Capture on each Guard. A denied call never reaches anyone's Capture.

### What Precheck matches on — and what it does not block
Four points govern the core-system match, all decided:
- **Specific named paths, never whole host directories.** The System set is matched on the exact files and
  directories the guard names — `.claude/settings.json`, `.codex/config.toml`, the Grant store dir, the Context
  store dir, and the guard's installed binary — each canonicalized (`.`/`..` and symlinks resolved) before the
  check. It is never the whole `.claude` or `.codex` tree: the AI works inside those legitimately, and blocking
  the directory would break normal operation.
- **Structured edits are exact; Bash is best-effort.** An Edit/Write call carries its target as a field, so
  Precheck canonicalizes that path and matches it exactly — airtight. A Bash command carries its target as free
  text, so Precheck can only match a reference to a named path in the command string. That is deliberately
  best-effort: it catches the store dirs cleanly because their names are distinctive, but for a two-segment
  wiring path like `.claude/settings.json` it misses a split such as `cd .claude && sed -i settings.json`, and
  we do not widen to `.claude` because that would over-block. The primary defense for the wiring is therefore
  the exact match on structured edits — the lazy AI's natural route — with the Bash reference-match as a cheap
  extra layer and the `cd`-split left as an accepted residual.
- **The snapshot store gets an independent state check (v1).** The snapshot store is the source Post restores
  from, and Post cannot see writes into it, so Precheck does not lean on the best-effort command match there. On
  every call Precheck independently inspects the snapshot directory's actual state — not the command string —
  and denies, treating it as Sealed, if a snapshot for the in-flight `tool_use_id` is already present or the
  directory is otherwise anomalous. That catches a poisoned snapshot however the write reached the store. The
  tightest corner — a call overwriting its own snapshot inside the same command — is already hard because the AI
  cannot name its own live `tool_use_id`; fully closing even that is the one piece held in reserve: Capture
  records a hash of the snapshot it wrote and Post verifies the snapshot still matches before trusting it.
- **Reads are never blocked.** The File Guard protects write-integrity, not read-confidentiality. Nothing in the
  core set is a secret whose disclosure helps a cheat: the snapshots are prior public bytes, and a Grant is
  verified against the one change it authorizes — bound, not a bearer secret — so reading the Grant store is
  inert. The one genuinely read-sensitive asset, the private signing key, is deliberately not among these files
  (it lives in the external secret store, never in the repo). So Precheck blocks only writes, and the deny
  message for a blocked write may tell the AI that reading the path with the Read tool is allowed.

### Post phase — the full drift-and-revert
- **Postcheck** compares the pre-image snapshot against what is now on disk to find every drifted protected
  path, then decides each one from complete data — it holds both the bytes and the command that produced them.
  Per drifted path it answers **Coverage** first: an **All** Grant covers it, or (later) an approved command
  accounts for it, and it is allowed; otherwise it answers **Conformance** with the path's Verifier — a **Part**
  Grant sends the change to its envelope check, and no Grant at all means the default **NoChangeVerifier**, so
  any drift is reverted. The restore is an **Effect** the Engine executes, keeping the privileged disk write in
  one audited place.

The narrowing this buys, stated plainly: Post reverts an unauthorized change to an ordinary protected file
after it lands rather than blocking it first, so a single shell command that both changes such a file and uses
it within that same command runs with the change before Post reverts. Against a lazy AI that is out of scope,
and the golden build a human or CI runs is separate from the AI's own command; Precheck can be widened later if
that changes.

## 7. Two verify points (AGREED)
We verify in two places with two different jobs. **Precheck** is the narrow core-system block described above:
it consults a Grant or the setup command only for the System set, and denies Sealed outright. **Postcheck** is
where the real judging happens for ordinary protected files — it answers Coverage (a Grant, or later an approved
command) and then Conformance (a Verifier) from the change that actually landed. The **Verifier exists only at
Postcheck**, because before the write there is no change to inspect and we will not try to predict what a script
would do. The Grant is still the thing that can span both phases — consulted at Precheck for the System set and
at Postcheck to select which envelope its scope allows — but ordinary protected files are a Postcheck matter
only.

## 8. Grants and scope (AGREED)
A **Grant** is a signed capability token; the **File Grant** is the one the File Guard consults. Its **Scope**
is **All** in v1, meaning any change to the covered path, with **Part** reserved for a later language-specific
envelope; "All" is simply the first Scope implementation. Later, other Guards such as a lint guard will have
their own language-specific Grants, exactly as 4thWAIV did.

## 9. The protected set (AGREED)
For any write target the File Guard must answer three things — is it protected, what Grant does a change need,
and how is a change judged — and all three come from the active **Rules**. Rules arrive from four sources, in
precedence, and the top two are the anti-cheat floor:
1. **Sealed** — built-in, unremovable, no Grant ever unlocks: the Grant store and the Context store. A change
   is never allowed.
2. **System** — built-in, unremovable, a Grant can unlock: the guard's own shipped code and installed binary,
   and the specific runtime-wiring files (`.claude/settings.json`, `.codex/config.toml`) — the named files, not
   the whole `.claude`/`.codex` tree. Config can never delete these Rules, which is what stops the AI disabling
   the guard by shrinking a config file.
3. **Provider** — opt-in per language: enabling a Provider adds its Rules and its Baseline defaults.
4. **Project** — project-specific: the repo's own hand-added protected paths and overrides.

The **Protected Set** is the union of all active Rules' matchers. Sealed and System are always present as the
floor; Provider and Project are the configurable layer on top. The floor — Sealed and System — is what Precheck
blocks before a write lands; the configurable layer above it is what Postcheck catches and reverts after the
fact.

## 10. The Context (AGREED)
The **Context** is a per-ToolCall store, persisted to disk because Pre and Post are separate processes, keyed
by (ToolCallId, Guard, kind). **Capture** writes it, **Postcheck** reads it, and the Engine deletes it after
Post, sweeping orphans by TTL. Guards never touch the store directly; the Engine mediates it, so the store's
own Sealed protection lives in one place. What the File Guard stores in the Context is the **pre-image byte
snapshot**; because the store is keyed by kind, a future Guard can store a different kind of data (an AST, a
precomputed diff, a schema fingerprint) with no change to the Engine.

## 11. What this level deliberately excludes
The Grant CLI (mint, extend, add, revoke, list, show, verify), the signed-exemption / lint gate, the
per-language analyzers such as a Roslyn analyzer, the invocation-block, key storage, and the contents of the
per-language fixtures. Each is a separate level with its own document.

## 12. Open questions (OPEN — not yet decided)
1. **Plug mechanism.** DECIDED 2026-07-26: **compiled-in for v1** — Providers, Verifiers, and Rules are classes
   registered at build time, shipped as part of the protected System set. A runtime plugin loader is itself an
   attack surface a lazy AI could use to register a no-op Verifier or an empty Provider, so runtime
   extensibility is deferred to a later concern.
2. **Authorized commands.** Keep a sanctioned command-prefix allowance as a second Coverage source versus
   routing everything through a Grant. With the phase split above it lands in two places: the guard's own setup
   command is recognized at Precheck so it can write the runtime wiring, while routine package commands such as
   `dotnet add` are recognized at Postcheck, where the command and the drift are both in hand.
   Recommendation: keep it, so neither is gated behind a signed Grant.
3. **Run-record and spec location.** A tracked `run-records/<date>-<slug>/` at the repo root, as the workflow
   skill mandates, versus keeping the spec, glossary, and contracts under the untracked `./.dev/`.
   Recommendation: keep them under `./.dev/` for now and decide on publishing later.

Minor and non-blocking: key storage is parked as its own discussion, needed before the Grant layer but not
before the File Guard spec.

## 13. Provenance
Grounded in the 4thWAIV reference docs copied to `./.dev/reference/4thWAIV-agent-governance/` — principally
`dispatcher.md`, `policy-module.md`, `protected-files-block.md`, and `drift-detect-and-revert.md` — and in the
contract and workflow conventions now in the `rails-run-a-workflow` and `rails-write-a-contract` skills (`./.agents/skills/`). The full prior decision log
is `./.dev/DECISIONS.md`.
