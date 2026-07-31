# Guarding Repos Written in Any Language

You do **not** port this governance system into the language of the repo you are protecting. The system is written in TypeScript/JavaScript and it stays that way. It hooks the **agent runtime** (Claude Code, Codex) — the process that reads and writes files — not the code being guarded. A C#, Rust, Go, Python, or polyglot repository is protected by the exact same TS engine that protects a TS repo. The guarded repo's source is data to the engine: file paths and shell commands, nothing more.

This file explains which of the five guarantees are language-blind (four of them), the one dependency the system imposes on the guarded repo (a JS runtime for the hooks), and how to extend the one language-coupled guarantee (the lint gate) to a non-JS linter.

---

## The engine is a sidecar, not a rewrite

Everything lives under `agent-tools/`:

- `agent-tools/hooks/pre_tool_use/` and `agent-tools/hooks/post_tool_use/` — the runtime hooks the agent invokes before and after every tool call.
- `agent-tools/lib/` — the shared decision logic (`paths.ts`, `protection-config.ts`, `bypass.ts`, and friends).
- `agent-tools/config/` — the JSON policy files.

The agent runtime is configured (via `.claude/settings.json` / `.codex/hooks.json`) to call these hooks. When the agent tries to Edit a file, Write a file, or run a Bash command, the PreToolUse hook runs first and can **block** the call; after an allowed call, the PostToolUse hook runs and can **revert** it. None of that logic inspects the *contents* of the guarded repo in a language-aware way. It reasons about **paths** and **shell commands**.

That is why the four path-and-command guarantees cost zero language-specific work.

---

## The four language-blind guarantees

### 1. Protected files

The policy is a list of picomatch globs. The static portion is `agent-tools/config/protected-paths.json`; the in-source, JSON-removal-resistant portion lives in `agent-tools/lib/protection-config.ts`. The matching engine is `isProtectedPath` in `agent-tools/lib/paths.ts`:

```ts
export function isProtectedPath(
  filePath: string,
  projectDir: string,
  protectedGlobs: readonly string[],
): boolean
```

It normalizes any incoming path — absolute, project-relative, or `./`-prefixed — to a project-relative POSIX path (`normalizeToProjectRelative`), rejects `..` escapes, and tests it against the glob set. There is no reference to any programming language anywhere in the function. A glob `src/**/*.cs` protects C# just as `turbo.json` protects a build config.

To guard a different repo's sensitive files you author the globs and stop. Point them at that repo's build config, its CI pipeline files, its secrets manifests, its lockfiles — whatever must not drift. The globs in `protected-paths.json` today are directory-oriented for the engine itself (`agent-tools/hooks/**`, `agent-tools/lib/**`, `agent-tools/config/**`, `agent-tools/scripts/**`, `agent-tools/security/**`) plus individual config and rule files; you replace that catalog with the target repo's equivalents.

### 2. Write detection through shell commands

Blocking `Edit`/`Write` tool calls is not enough — an agent can also mutate a file by running a Bash command. `extractWriteTargets` in `agent-tools/lib/paths.ts` tokenizes a shell command with `shell-quote` (never regex) and walks the token stream to compute every path the command will **write**:

```ts
export function extractWriteTargets(
  bashCommand: string,
  projectDir: string,
): ExtractWriteTargetsResult   // { ok: true; targets } | { ok: false; reason }
```

It knows redirections (`>`, `>>`, `&>`, `2>`, …), `tee`/`rm`/`mkdir`/`touch`/`truncate` (all positional args are targets), `cp`/`mv`/`ln`/`install` (last positional is the destination), `chmod`/`chown` families, `sed -i`, `awk -i inplace`, `dd of=`, inline-script bodies (`python -c`, `node -e`, `bun -e`, `ruby -e`, …), `git config core.hooksPath`, and composite entry-points (`bash -c '…'` re-parses the inner command). On any tokenizer ambiguity — an unterminated quote, a parse failure — it returns `{ ok: false }` and the caller **default-denies**.

Shell commands are language-agnostic. `dotnet build`, `cargo test`, `go run`, and `npm run build` all reach the same tokenizer. If a command writes to a protected path, it is blocked no matter what toolchain produced it.

### 3. Drift detection + revert

Before an allowed write, the PreToolUse side captures the current bytes of any protected file the call could touch and stores them as **base64 content** (not hashes) in an in-repo Tier-1 directory, `.protected-snapshots/`. (It is in-repo and Tier-1 protected precisely so the agent cannot poison the restore source; the hook process writes it directly via `fs`, which no tool-call gate intercepts.) After the call, the PostToolUse drift hook (`agent-tools/hooks/post_tool_use/`) compares the post-call bytes against the snapshot. If a protected file changed without authorization, the hook **reverts it to its pre-call bytes** — and if the call *created* a protected file, the hook **deletes** it. This is active restoration, not detect-and-warn.

Byte comparison and byte restoration are language-blind. A drifted `Program.cs` is reverted the same way a drifted `turbo.json` is.

### 4. Signed-bypass authorization

A human authorizes an intentional edit to a protected path by minting a signed bypass token. The token dir, `.elves/bypass-tokens/`, is Tier-1: the AI can never write it, so it cannot forge its own authorization. The two tiers are declared in `agent-tools/lib/protection-config.ts`:

- `TIER1_NEVER_BYPASS_GLOBS` — `.elves/bypass-tokens/**` and `.protected-snapshots/**`. Never writable by a tool call, even with a valid token. Checked *before* bypass evaluation.
- `HARDCODED_PROTECTED_GLOBS` — protected even if someone strips `protected-paths.json`, but a valid token *does* authorize the write.

`effectiveProtectedGlobs(cfg)` unions Tier-1, the hardcoded set, and the dynamic JSON/workspace-discovered set at decision time, so an emptied config can never disable the in-source lists.

Token minting and verification are crypto over path strings. Nothing about it depends on the language of the file being unlocked.

---

## The one dependency the system imposes: a JS runtime

Because the guard engine is a TS sidecar, **the guarded repo must have `bun` (or Node) available wherever the hooks run** — on the developer's machine and in CI. This is the single, unavoidable coupling. A pure-Rust or pure-C# shop that has never installed Node must add a JS runtime to its dev image and its CI runners so the hooks can execute. That is the whole cost of the four language-blind guarantees: install a runtime, point the config globs at the sensitive files, wire the hooks into the agent runtime config.

There is no second TS entry point to reconcile, no per-language reimplementation, and no byte-identity problem across languages, because there is exactly **one** signer and **one** engine.

---

## The only language-coupled guarantee: the lint gate

The fifth guarantee — enforced, non-suppressible lint — is the one place the guarded language leaks in, because linting is inherently language-specific. ESLint cannot lint C# or Rust. The design decouples the *crypto* from the *linter* so you swap only the small language-bound piece.

**Keep verbatim, in TS, for every target language:**

- The exemption **signing** and **verification** crypto: `eslint-plugin-fourth-waiv/lib/exemption-signer.ts` and `eslint-plugin-fourth-waiv/lib/exemption-verifier.ts`.
- The **mint CLI** that a human uses to sign an approved exemption.
- The anchor/registry machinery (`eslint-plugin-fourth-waiv/lib/anchor-parser.ts`, `eslint-plugin-fourth-waiv/lib/registry-loader.ts`).

These are the same signed-token trust model as the bypass guarantee. There is **one TS signer**, so a signed exemption is byte-identical regardless of which linter consumes it — no cross-language byte-identity problem.

**Swap per language — two small pieces:**

1. **The linter.** ESLint → Roslyn analyzers for C#, `clippy` for Rust, `golangci-lint` for Go, etc.
2. **A suppression scanner.** Every linter has its own inline disable directive — `#pragma warning disable` in C#, `#[allow(...)]` in Rust, `//nolint` in Go, `// eslint-disable` in JS. Write a small scanner that detects **that language's** disable directives and, for each one, calls the **same** `exemption-verifier.ts` to confirm the suppression carries a valid human signature. An unsigned suppression fails the gate exactly as it does in the JS pipeline.

**Author fresh per language:** the rule catalog itself. The rules that a C# repo needs are not the rules a TS repo needs, so the analyzer rule set is written new for each language. Only the *governance around* the rules — signing, verification, minting, the suppression check — is reused.

**Re-author, do NOT keep verbatim:** the build-to-scan coupling backstop. In the TS pipeline this is `scripts/validate-turbo-lint-coupling.js` (see `lint-gate.md §6`), which asserts every `<pkg>#build` turbo task depends on its `#lint`+`#check`. It is turbo/npm-specific — it parses `turbo.json` — so it has **no meaning** for MSBuild or Cargo and must be rewritten for the target build tool. Without an analog the fifth guarantee is structurally bypassable: `dotnet build` can compile without running the Roslyn analyzer, and `cargo build` can succeed without `clippy` or the suppression scanner — the exact hole this coupling closes. The rule is *not* "keep the script"; it is "bind the scan to the build so the build cannot green-light unscanned code."

### The suppression-tag convention per language

The JS scanner resolves each disable against a signed `(record, anchor)` pair carried in ESLint's description slot: `// eslint-disable-next-line <rule> -- fw-exempt record=<id> anchor=<id>` (`lint-gate.md` line ~151). C# `#pragma warning disable CS0168` and Rust `#[allow(dead_code)]` have **no inline description slot**, so "call the same `exemption-verifier.ts`" is underspecified until you define where the `(record, anchor)` identity rides. The convention: the per-language scanner requires a **tag comment on the line immediately above** each suppression directive, and resolves it through the unchanged registry-loader + `exemption-verifier.ts`. A suppression whose adjacent tag is missing, malformed, or does not verify **fails the gate** exactly as an unsigned `eslint-disable` does.

- **C#:**
  ```csharp
  // fw-exempt record=A1B2C3 anchor=D4E5F6
  #pragma warning disable CS0168
  ```
- **Rust:**
  ```rust
  // fw-exempt record=A1B2C3 anchor=D4E5F6
  #[allow(dead_code)]
  ```

The tag string (`fw-exempt record=<6-char> anchor=<6-char>`) and the verifier it resolves against are identical across languages — that is the "one signer, one trust core" property. Only the scanner that *finds* the directive and its adjacent tag is per-language.

### The lint-gate porting recipe

1. Stand up the target language's linter in CI and dev.
2. Author the language's rule catalog (fresh — this is the real per-language work).
3. Reuse `exemption-signer.ts` / `exemption-verifier.ts` and the mint CLI **unchanged**.
4. Write a suppression scanner that finds the language's disable-directives **plus the required adjacent `// fw-exempt record=… anchor=…` tag** and routes each through `exemption-verifier.ts`.
5. Fail the build on any suppression that does not verify.
6. **Re-author the build-to-scan coupling** for the target build system so the build cannot skip the scan — e.g. bind the Roslyn analyzer + suppression scan to the C# build via a required MSBuild target (and `TreatWarningsAsErrors`), and gate `cargo build` on `clippy` + the scanner in CI/pre-commit. `validate-turbo-lint-coupling.js` is turbo-specific and is **rewritten**, not kept.

The four path-and-command guarantees need none of this. Only the lint gate does, and even the lint gate keeps its trust core in one place.
