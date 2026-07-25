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
built in its own repository and will be imported into the C# proteus project later. It must govern
TypeScript, Python, C#, and Rust out of the gate, each with a fixture project for testing. The guiding
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
  Instances: **NoChangeVerifier** now; **CSharpProjectVerifier**, **NodeProjectVerifier**,
  **PythonProjectVerifier** later.
- **Provider** — a language plugin that bundles a set of Rules, a Baseline, and the Verifier(s) those Rules
  use (and later that language's Grants). Instances: **CSharpProvider**, **NodeProvider**, **PythonProvider**.
- **Baseline** — a Provider's opt-in recommended default set of its own Rules and settings, applied as a
  template over your ruleset when you enable it, the way a lint "recommended" set works.

**Data the Pipeline carries**
- **ToolCall** — the normalized, host-agnostic tool call: tool name, tool input, ToolCallId.
- **ToolCallId** — the per-call correlation id (the host's `tool_use_id`) that ties a Pre to its Post.
- **Environment** — the call's surroundings: project root, event name, session.
- **Verdict** — a Guard's judgment: allow, deny with a reason, or warn with a message.
- **Effect** — a privileged side effect a Guard requests and only the Engine executes, such as restoring
  bytes, so every protected-path write stays in one audited place.

**The artifact channel**
- **Context** — the per-ToolCall store, persisted between the separate Pre and Post processes, that Capture
  writes and Postcheck reads. Addressed by the triple (ToolCallId, Guard, kind). The Engine writes it after
  Pre, loads it before Post, and deletes it after Post.
- **Artifact** — a typed, self-describing record inside the Context; the File Guard's Artifact is the
  pre-image byte snapshot.

**A Guard's three behaviors**
- **Precheck** — the Pre-phase verify: produce a Verdict. For the File Guard this is the Coverage question.
- **Capture** — the Pre-phase prepare: write the Artifacts the Post phase will need into the Context.
- **Postcheck** — the Post-phase verify: produce a Verdict plus Effects. For the File Guard this is the
  Conformance question.

**Authorization**
- **Grant** — a signed capability token authorizing a Guard to permit something it would otherwise block.
  **File Grant** is the File Guard's grant; language-specific Grants for other Guards (for example lint) come
  later, as in 4thWAIV.
- **Grant Scope** — what a Grant authorizes: **All** in v1 (any change to the covered path) or **Part** later
  (a language-specific envelope). "All" is just the first Scope.
- **Coverage** — the Precheck, all-or-none question a Grant answers: does any Grant cover this path.
- **Conformance** — the Postcheck, change-level question a Verifier answers: is the landed change inside the
  Grant's envelope.

**The protected set**
- **Protected Set** — the union of all active Rules' matchers that the Engine matches a write against.
- **Sealed** — built-in, unremovable Rules that no Grant ever unlocks: the Grant store and the Context store.
- **System** — built-in, unremovable Rules that a Grant can unlock: the guard's shipped code and runtime
  wiring.
- **Provider** and **Project** — the configurable Rule sources (a Provider's bundle; the repo's own entries).

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

### Pre phase — two responsibilities, enforced apart
- **Precheck** answers Coverage: does any Grant cover this path, all-or-none. If the path is protected and no
  Grant covers it, deny. Precheck returns a Verdict only.
- **Capture** writes the pre-image Artifact into the Context. It runs only for a ToolCall that is not being
  denied.

The split is enforced by architecture, not developer discipline, on two mechanisms:
1. **Capability.** Precheck is handed only read-only inputs and can return only a Verdict; Capture is the only
   method given a writable Context handle. The ability to produce an Artifact does not exist inside Precheck.
2. **Engine-owned sequence.** The Engine calls every Guard's Precheck, aggregates, and only if the aggregate
   is not a deny does it then call Capture on each Guard. A denied call never reaches anyone's Capture.

### Post phase — one responsibility
- **Postcheck** answers Conformance: is the change that actually landed inside the Grant's envelope, judged by
  the Verifier. It returns a Verdict plus Effects. Per drifted path: an **All** Grant covers it and it is
  allowed with no Verifier needed; a **Part** Grant sends it to its Verifier; and no Grant at all means the
  default **NoChangeVerifier** applies, so any drift is reverted. The restore is an **Effect** the Engine
  executes, keeping the privileged disk write in one audited place.

## 7. Two verify points (AGREED)
We verify in two places, but with two different judges. **Precheck** verifies Coverage using the **Grant**;
**Postcheck** verifies Conformance using a **Verifier**. The thing that spans both phases is the Grant — it is
consulted at Precheck for coverage and again at Postcheck to select which envelope its scope allows. The
**Verifier exists only at Postcheck**, because before the write there is no change to inspect and we will not
try to predict what a script would do.

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
2. **System** — built-in, unremovable, a Grant can unlock: the guard's own shipped code and its runtime
   wiring. Config can never delete these Rules, which is what stops the AI disabling the guard by shrinking a
   config file.
3. **Provider** — opt-in per language: enabling a Provider adds its Rules and its Baseline defaults.
4. **Project** — project-specific: the repo's own hand-added protected paths and overrides.

The **Protected Set** is the union of all active Rules' matchers. Sealed and System are always present as the
floor; Provider and Project are the configurable layer on top.

## 10. The Context (AGREED)
The **Context** is a per-ToolCall store, persisted to disk because Pre and Post are separate processes, keyed
by (ToolCallId, Guard, kind). **Capture** writes it, **Postcheck** reads it, and the Engine deletes it after
Post, sweeping orphans by TTL. Guards never touch the store directly; the Engine mediates it, so the store's
own Sealed protection lives in one place. A record in the Context is an **Artifact**; the File Guard's Artifact
is the pre-image byte snapshot, and a future Guard can emit a different Artifact (an AST, a precomputed diff, a
schema fingerprint) with no change to the Engine.

## 11. What this level deliberately excludes
The Grant CLI (mint, extend, add, revoke, list, show, verify), the signed-exemption / lint gate, the
per-language analyzers such as a Roslyn analyzer, the invocation-block, key storage, and the contents of the
per-language fixtures. Each is a separate level with its own document.

## 12. Open questions (OPEN — not yet decided)
1. **Plug mechanism.** Compiled-in interfaces (Providers, Verifiers, Rules are classes registered at build
   time, shipped as part of the protected System set) versus external plugin assemblies discovered at runtime.
   Recommendation: compiled-in for v1, because a runtime plugin loader is itself an attack surface a lazy AI
   could use to register a no-op Verifier or an empty Provider.
2. **Authorized commands.** Keep a sanctioned command-prefix allowance as a second Coverage source at Precheck
   (as 4thWAIV did for `bun add` and setup commands) versus routing everything through a Grant.
   Recommendation: keep it, so the guard's own setup command can write the runtime wiring and routine package
   commands are not gated behind a signed Grant.
3. **Run-record and spec location.** A tracked `run-records/<date>-<slug>/` at the repo root, as the workflow
   skill mandates, versus keeping the spec, glossary, and contracts under the untracked `./.dev/`.
   Recommendation: keep them under `./.dev/` for now and decide on publishing later.

Minor and non-blocking: the name **Artifact** is kept unless changed; key storage is parked as its own
discussion, needed before the Grant layer but not before the File Guard spec.

## 13. Provenance
Grounded in the 4thWAIV reference docs copied to `./.dev/reference/4thWAIV-agent-governance/` — principally
`dispatcher.md`, `policy-module.md`, `protected-files-block.md`, and `drift-detect-and-revert.md` — and in the
contract and workflow conventions at `./.dev/reference/workflow-with-adversaries/`. The full prior decision log
is `./.dev/DECISIONS.md`.
