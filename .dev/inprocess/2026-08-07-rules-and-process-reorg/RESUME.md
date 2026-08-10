# RESUME — rules/process reorg + all in-flight workstreams (2026-08-07)

Session handoff before compaction. Read this first on resume. Decisions in Section A are **only in chat** until now — they are the fragile part.

## A. NEW decisions this session (rules & process) — not yet in any other file

1. **Global rules file** = `~/.codex/AGENTS.md` (master), `~/.claude/CLAUDE.md` = `@~/.codex/AGENTS.md` import (proven cross-tool standard, Tim approved the import over a symlink). Same idiom at project level. Global holds only **universal, tool/language-agnostic** rules:
   - **Approve-first** — describe what and WHY before any tool call that does real work; the human approves each major update. *(Tim: "Agreed.")*
   - **SOLID/DRY** — follow; if the code makes it hard in a way the plan didn't cover, STOP and present. **SOLID and DRY each get their OWN separate file**, used by the acting agent AND as the center/checklist for the adversaries that enforce them. *(Tim: "these should be seperate files and used for both the acting agent but as the center for the advers that enforce it.")*
   - **Stop on the unexpected** — any problem not in the plan → STOP and present, don't improvise. *(Tim: "Agreed.")*
   - **CodeGraph managed block** — presence-locked, position-flexible (may move in the file, must stay). agent-guard will **inject its own managed block on install**, mirroring how CodeGraph injected its `<!-- CODEGRAPH_START -->` block. *(Tim: "mirror that when we deploy our tool, so we can inject our own rules.")*

2. **Build+test bracket is NOT a global/behavioral rule — it belongs to the CONTRACT RUNNER.** Run at a contract's **implementation start** (prove a clean baseline — no pre-existing failures, so a later unexpected failure can't be cheated off as "pre-existing") and at **implementation end** (green, because CI enforces it). **Once per contract.** NOT per work-item, NOT mid-small-change, NOT on first load, NOT for authoring a contract. *(Tim: "a build and test at the beginning and end SHOULD be in the contract runner not your rules… IT must be how contracts are worked.")*

3. **TS/`any` rule moves from global → project level** (language-specific; this repo is C#). Relocated, not deleted.

4. **Workflows do the work; the agent protects its memory/context.** Need a rule on WHEN the agent acts directly vs. assigns a process. *(Tim.)*

5. **Three process tiers (ceremony levels):**
   - **Self-action** — agent does the work directly, ≤ 5–10 min.
   - **Low ceremony** — one agent + a small set of adversaries.
   - **Normal ceremony** — the current `workflow-with-adversaries` contract process (the build+test bracket; SOLID/DRY as separate files; full adversary panel).

6. **Memories STAY.** Tim ruled it (I honor memory over skills). Do NOT remove or "de-duplicate" them. Reconcile-on-load keeps them synced to the skill. (I kept proposing to delete them — stop.)

7. **Rules organization (best-practice, researched):** global (universal) / project (repo-specific) / **skills** (procedures — auto-surfaced by description; `workflow-with-adversaries` + contract guide SHOULD BE skills, currently mis-filed in `.dev/reference/`) / reference (records/history — pointer only, e.g. `DECISIONS.md`) / memory (recall copy). **Inline** what's needed every turn (compact); **pointer** to heavy on-demand. Keep top-level files minimal — "bloat → the agent ignores the rules."

8. **Skill compaction** (15 KB `how-to-work-here`): a careful rewrite gated by a **round-trip fidelity check** (reconstruct-and-diff + LLM-judge), NOT auto-token-pruning (would drop the qualifiers — the exact failure). Prior art: LLMLingua/-2 (borrow the *verification method*, not the pruner); AGENTS.md/progressive-disclosure best-practice guides.

9. **Communication skill + shared vocabulary — TO BUILD.** Fixes the #1 problem all session: I paraphrase, coin jargon, and corrupt standard terms into private pseudo-code. Standardize how I report + define a shared vocabulary.

10. **Behavior catalog (my frequent dangerous/annoying behaviors → become rules).** First entry: *runs full build/test too often (on load, mid-small-change, before a contract exists)*. Others to encode: paraphrasing/dropping qualifiers when restating rules; distorting a decision-gate ("touch only via a contract") into a "lock"; inventing decisions/deferrals; over-producing; asking instead of doing; conflating one rule with another.

11. **Interface home DECIDED:** `IPlatformFileSystem`/`IPlatformServices` live in the `AgentGuard.CrossPlatform` assembly (so every other assembly leverages it) — **not** `Abstractions.Contracts`. *(Tim.)* And **`Abstractions/**` is NOT frozen/locked** — that was my error; the working-rules skill was corrected (`analyzers/**` is decision-gated, not a lock; Abstractions is governed by the approval rule + contract analyzers).

12. **RDD (Rule-Driven Development)** is in the skill: analyzer rules written before the code (born compliant); breaking architecture = suppressing a rule (lie-catcher catches the fingerprint). Two phases: **rule phase** (has rule-gen authority, edits `analyzers/**`) → **implementation phase** (fresh agent, tokens revoked, works within the rules or asks the human). **Rules go RED against existing violations; the contract cleans them up, and the AI does the cleanup.**

## B. The 5 workstreams — state + next step

1. **.md / skills reorg (this):** decisions in Section A; NOT executed yet. Next (pending Tim's approval of the 3-rule global): wire the global (AGENTS.md master + CLAUDE.md `@import`, TS rule → project), then the behavior catalog + the communication skill, then the 3-tier process doc + the SOLID/DRY files, then compact the 15 KB skill (fidelity-gated).

2. **Contracts / process:** define the 3 ceremony tiers; move the build+test bracket into the contract runner; SOLID + DRY as separate files (used by the actor AND as the adversary's center). NOT written yet.

3. **Analyzers to finish (for RDD/contracts):** the **interop CA rule** — `[LibraryImport]`/`[DllImport]` allowed ONLY in `AgentGuard.CrossPlatform.*`; built in the cross-platform contract's rule phase, goes RED against `NativeInterop.cs`, drives its deletion. Plus the **SOLID/DRY criteria files**. Plus the **anti-`#if` lint rule** (from `PLAN-crypto-minting-and-presence` platform decision #2). `analyzers/**` is decision-gated (not frozen).

4. **CrossPlatform work:** contract at `.dev/inprocess/2026-08-07-cross-platform-engine-and-interop/contract.md` (LOCKED, adversary-checkable). Build the 3 `AgentGuard.CrossPlatform.*` libs (POSIX authored once, `<Compile Link>`-shared mac→linux; Windows `MoveFileEx`), the 1 OS-agnostic spec test project, rewire `SymlinkOps` → `IPlatformFileSystem`, **DELETE `NativeInterop.cs`** (the cleanup), fix config-protection **CRLF**, add the "enable Developer Mode / run elevated" error. Interface lives in `AgentGuard.CrossPlatform`. Proven prototype/seed at `proto/platform-interop/` (6 spec tests green under the full analyzer gate). **This unblocks the CI/CD Windows leg.**

5. **CI/CD work:** contract at `.dev/inprocess/2026-08-03-ci-cd-build-sign-release/contract.md`. State: Pass-2 signing/release built + adversary-reviewed, **staged uncommitted on branch `ci-cd-signing-release`** (a work commit `52cb508` was pushed; **dev** branch created; **PR #7** open; **6 secrets** set). The live CI run went **RED**: the Windows test leg fails because the engine P/Invokes `libc rename` — **fixed by workstream 4**. Known fixes still owed: the Windows `$env:AG`-under-pwsh word-split (`shell: bash`), the missing mac hardened-runtime launch proof (`guard version`), the missing `ProcessArchitecture=X64` Rosetta test. Tim's Pass-2 rulings: public-key stored as its own secret (delete `snk-to-publickey.cs`), signing push-only, per-platform verify artifacts, build the local `AGENTGUARD_COSIGN` pass, Sectigo (`http://timestamp.sectigo.com`, verified) as the DigiCert timestamp fallback. **Success = green on all three OS on GitHub.**

## C. Order / dependencies
- **CrossPlatform (4) unblocks CI/CD (5)** — CI can't go green on all 3 OS until the engine runs on Windows.
- The **interop CA rule (3)** is part of the cross-platform contract's rule phase.
- The **rules/process reorg (1,2)** is orthogonal infrastructure Tim is driving now; can run in parallel.

## Hard-won behavior rules that must survive (do not regress)
- Quote rules verbatim; never paraphrase or drop qualifiers (compression = summarization = where I distort). Paste source via a tool, don't retype from memory.
- Never invent a decision, a deferral, or a scope cut; only the human's explicit yes to the exact item counts.
- `Abstractions/**` is NOT frozen; `analyzers/**` is decision-gated, not a lock.
- Don't run full build/test on load / mid-small-change / before a contract.
- Memories stay.
