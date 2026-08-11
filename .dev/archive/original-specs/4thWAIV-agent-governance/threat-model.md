# Threat Model: The One Invariant

Read this first. Everything else in this system — the hooks, the token format, the
signing CLI, the drift snapshots, the lint exemptions — exists to serve a single
invariant. If you understand the invariant, every design decision downstream
follows from it.

## The invariant

> **The AI can never authorize its own exception. Only the human, holding a
> private key the AI cannot reach, can.**

You are building governance for an AI coding agent that runs with **full
file-edit and full shell access** in the repository. That is the starting
condition, not a bug to fix — the agent needs that access to do useful work. The
system does not sandbox the agent's capabilities. It draws a small set of
security-critical files and commands, locks them, and makes the lock
**unliftable from inside the agent's own capability set.**

The adversary in this model is the agent itself — whether through an honest
mistake, a confused chain of reasoning, or a prompt-injection that turns it
hostile. The assumption is that the agent may at any moment try to weaken its own
guardrails: edit the hooks that enforce the locks, disable a lint rule, mint
itself a bypass token, or read the signing key. **Every one of those moves must
fail, and it must fail in a way the agent cannot undo by editing more files or
running more commands.**

## Why a cryptographic root, and not a config flag

A permission encoded as a file the agent can write is not a permission — it is a
suggestion. If "am I allowed to edit the hooks?" is answered by reading a config
value, the agent flips the config. If it is answered by checking whether a hook
is registered, the agent unregisters the hook. Any authority that lives inside
the agent's reach is an authority the agent holds.

So the authority lives outside the agent's reach. The trust root is a single
**Ed25519 keypair**:

- The **public key** is committed to the repo at
  `agent-tools/security/bypass-public-key.pem`. It can only *verify* signatures;
  possessing it grants nothing.
- The **private key** lives solely in the human's **1Password vault**
  (`op://4thWAIV_Engineering/ProtectedFilesBypassSigningKey/notesPlain`). It is
  never on disk, never in an environment variable, never committed. The signing
  CLI reads it via `op read` for exactly as long as it takes to produce one
  signature, then discards it — see the private-key handling in
  `readPrivateKey()` in `agent-tools/security/bypass.ts`, which spawns `op read`,
  captures stdout, builds a `KeyObject`, and holds it nowhere.

Because only that private key can produce a signature the committed public key
accepts, an authorization the human did not personally issue **does not exist**.
The agent can write any file it wants that *claims* to be a grant; it cannot write
one that *verifies*. `verifyTokenBlob()` in `agent-tools/lib/bypass.ts` re-hashes
the canonicalized payload and calls `node:crypto.verify("ed25519", …)`; a forged
or tampered token returns `invalid-signature` and is silently dropped by
`loadActiveTokens()`, which keeps only tokens whose `status === "valid"`.

This is why the exception mechanism is a **signed token** and not a config entry,
an allowlist file, or an environment variable. The signature is the whole point.

## The five guarantees

The invariant decomposes into five guarantees. Each is a distinct thing the agent
must not be able to do, and each exists because closing one hole without the
others would leave a way around.

### 1. The agent cannot forge an exception

Every bypass is a token carrying an Ed25519 signature over its payload. Verifying
uses only the committed public key. A hand-written token, a token with an edited
payload, or a token copied and re-timed all fail signature verification and are
discarded before they can authorize anything.

*Why it exists:* this is the cryptographic floor. Without it, "protected" means
nothing — the agent writes its own permission slip.

### 2. The agent cannot mint a real exception

Producing a valid signature requires the private key, and the private key is
reachable only by running the signing subcommands (`bypass mint/extend/add`, and
the parallel `lint:exempt mint-*`) which call `op read` against the human's
1Password vault. Two barriers keep those out of the agent's reach, and **both
must hold** — neither is sufficient alone:

- **The invocation-block guard** denies the mint verbs and the `op read` of the
  signing key. For this to be real, the guard must match a **shell-normalized**
  command, not the raw string — otherwise `bash -c "bun run bypass mint …"`,
  `FOO=1 bun run bypass mint …`, `eval '…'`, or `bash -c "op read op://…"` sail
  past a naive `^`-anchored regex. See `invocation-block.md §3`: the guard
  reuses the same `shell-quote` tokenize + `bash -c`/`eval`-recursion +
  env-assignment-strip normalizer as the protected-files write guard, so the
  wrapped forms are blocked too.
- **The 1Password session.** Even a perfectly shell-aware block is a
  belt-and-suspenders layer over the fact that `op read` needs the human's
  authenticated 1Password session. That session is **load-bearing**: during an
  agent run it must require **per-invocation authentication** (not a cached or
  biometric "don't ask again" window), or an already-unlocked session becomes an
  ambient capability the agent can spend the moment any hole in the block appears.

Do not describe this guarantee as the agent being *physically incapable* of
running the verbs. It is the composition of a shell-aware denylist **and** a
human-gated secret store; state both, and treat a cached/always-unlocked `op`
session as a break in the model.

*Why it exists:* forgery being impossible (guarantee 1) only matters if genuine
minting is also out of the agent's reach. Otherwise the agent just mints a real
one.

### 3. The lock cannot be removed by editing the config

The protected-path policy is not purely data-driven. The dynamic list in
`config/protected-paths.json` can grow the protected set, but it cannot shrink the
core: the enforcement engine's own files are protected by **in-source directory
globs** that no JSON edit can reach. `HARDCODED_PROTECTED_GLOBS` in
`agent-tools/lib/protection-config.ts` covers whole directories —
`agent-tools/hooks/**`, `agent-tools/lib/**`, `agent-tools/config/**`,
`agent-tools/scripts/**`, `agent-tools/security/**` — plus the signed-exemption
machinery in `eslint-plugin-fourth-waiv/`. `effectiveProtectedGlobs()` unions
these in-source globs with the dynamic list at decision time, so even a
test-injected empty config cannot disable them.

*Why it exists:* if the lock's membership lived only in a writable JSON file, the
agent would empty the file. Directory globs (not a cherry-picked file list) close
the "add one new file to the protected dir and it slips through" gap.

### 4. Some things cannot be bypassed at all — even with a valid token

Two paths are **Tier 1**: never writable by any tool call, even one presenting a
valid bypass token. `TIER1_NEVER_BYPASS_GLOBS` in `protection-config.ts` lists
exactly two:

- `.elves/bypass-tokens/**` — the token directory. If the agent could write here
  under a bypass, it would write its own tokens and guarantee 2 collapses.
- `.protected-snapshots/**` — the in-repo store the drift hook restores from
  (see guarantee 5). If the agent could write here, it would poison the snapshot
  so a "revert" restores *its* bytes.

Both hooks check Tier 1 *before* consulting any token, so a bypass never even gets
evaluated for these paths. The hook process itself writes both directories
directly via `fs`, which no tool-call gate touches.

*Why it exists:* the bypass mechanism is powerful. Two things must sit above it or
the mechanism eats itself — the place tokens live, and the place pre-call truth is
stored.

### 5. An unauthorized change to a protected file is reverted, not merely detected

Enforcement is two hooks working as a pair. **PreToolUse** blocks a write to a
protected path unless a valid bypass covers it. **PostToolUse** is the drift
backstop: before a covered tool runs, the pre-call bytes of the target are
snapshotted as base64 **content** into the in-repo Tier-1 directory
`.protected-snapshots/`; after the tool runs, if the file changed without
authorization, the drift hook **restores the file to its pre-call bytes** (and
deletes the file if the tool created it). It does not stop at reporting drift — it
undoes it.

*Why it exists:* a pre-write gate can be routed around by a tool the gate did not
anticipate. Detection alone leaves the bad bytes on disk. Storing the actual
pre-call content (not a hash) is what makes true reversion possible — a hash can
tell you *that* something changed, but only the content can put it back.

## How the guarantees interlock

None of the five stands alone; each closes a door the others would leave open.

- Forgery is blocked (1), so the only real tokens come from minting — which is
  out of reach (2).
- Minting is out of reach (2), but a token, once real, is powerful — so the
  places that would let the agent mint or poison are lifted above bypass entirely
  (4).
- The config can grow the locks but not shrink the engine (3), so the agent
  cannot edit its way to an empty policy.
- And because a novel tool might still slip a write past the pre-write gate, the
  post-write hook restores the original bytes from a store the agent cannot touch
  (5, protected by 4).

Walk any attack the agent might attempt and it terminates at the same wall: to
lift the exception it must produce a signature, and to produce a signature it must
hold a key that lives only with the human. That is the invariant, restated as a
dead end.

## What a re-implementer must preserve

Two different things are sometimes called "porting," and only one of them
touches this engine:

- **Guarding a repo written in another language** (C#, Rust, Go, …) requires
  **no port at all.** The engine is a JS sidecar that hooks the agent runtime,
  not the guarded code; it reasons about file paths and shell commands, which are
  language-blind. You keep the TS engine as-is. See `guarding-other-languages.md`.
- **Reimplementing the governance engine itself in another language** is optional
  — you would only do it to drop the `bun`/Node dependency — and if you do, you
  must preserve the five invariants below. The substrate is `node:crypto` Ed25519,
  a JSONL audit log, and directory-glob matching, all replaceable.

What is **not** negotiable in either case:

1. The private key never touches the agent's environment — read it just-in-time
   from a human-held secret store, use it, discard it.
2. The core protected set is expressed **in source**, not only in editable config,
   and unioned in at decision time.
3. The token/exemption is a signature the committed public key verifies — never a
   flag, allowlist entry, or env var.
4. At least the token store and the snapshot store are un-bypassable (Tier 1),
   checked before any bypass evaluation.
5. The post-write path restores original **content**, not just flags a hash
   mismatch.

Drop any one of these and the invariant leaks: the agent finds the door where it
can authorize itself.
