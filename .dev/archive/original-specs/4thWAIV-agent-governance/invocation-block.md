# The invocation-block hook

**Topic:** how a Bash command is matched against a blocked-pattern list, the block/allow decision the hook returns, how the authorized-prefix allow-list *does not* interact with the block, and how a signed bypass token overrides a block.

**Files this doc describes (code is truth — read these):**
- `agent-tools/hooks/pre_tool_use/invocation-block.ts` — the hook
- `agent-tools/config/invocation-blocked.json` — the blocked-pattern list
- `agent-tools/config/authorized-commands.json` — the write-authorization prefix list (a *different* guard's config; described here only to draw the boundary)

---

## 1. What this guard does

This is a PreToolUse hook. It inspects only `Bash` tool calls and denies any whose `command` string matches one of a set of JavaScript regular expressions. Its job: stop the agent from running the privileged CLI subcommands that mint/extend/revoke bypass and lint-exempt tokens, from reading the ed25519 private signing key out of the secret store, and from executing the human operator's one-shot script. These are things only the human operator runs; the agent cannot.

The guard name it registers under (for bypass tokens) is:

```ts
const GUARD_NAME = "invocation_block:bash";
```

exported at the bottom of the file as `export const guardName = GUARD_NAME;`.

---

## 2. The blocked-pattern config

`agent-tools/config/invocation-blocked.json` is a single object with one key:

```json
{ "patterns": string[] }
```

Each string is a JavaScript regex source. The current list blocks:

- Bypass-token mutation: `^bun run bypass (mint|extend|add|revoke)\b`, plus the two direct-invocation forms of the same script (`bun agent-tools/security/bypass.ts …` and `agent-tools/security/bypass.ts …`).
- Reading the private signing key from the secret store: `^op read .*ProtectedFilesBypassSigningKey.*`.
- Lint-exempt token mutation: `^bun run lint:exempt (mint-current|extend|shrink|revoke|repair)\b`, `mint-config`, and `mint-infrastructure-globs`, each with its two direct `eslint-plugin-fourth-waiv/cli/lint-exempt.ts` forms.
- Running the operator's one-shot script: `^(bash|sh|zsh|env)\s+(.*\s)?(\./)?\.tmp/user_execute\.sh\b` and the bare `^(\./)?\.tmp/user_execute\.sh\b`.

`parseConfig` (in the hook) hard-validates the shape: it throws if `patterns` is missing, not an array, or contains a non-string. A throw propagates out of the hook, and the dispatcher converts any thrown hook into a deny (default-deny on error). So a malformed config fails closed, never open.

### Config loading and caching

`loadConfig` reads the JSON once and memoizes a compiled form for the life of the dispatcher process:

```ts
cachedConfig = {
  patterns: parsed.patterns.map((p) => new RegExp(p)),
  raw: parsed.patterns,
};
```

`patterns` is the compiled `RegExp[]`; `raw` keeps the original strings so the deny message can name the exact pattern that matched. `_resetConfigCacheForTests` and `_setConfigForTests` exist only so tests can swap patterns without disk IO.

---

## 3. The matching algorithm — shell-aware, not raw-string

**The mint verbs are `^`-anchored regexes. A raw `re.test(rawCommand)` match is
not enough to keep them out of the agent's reach**, and building it that way
re-opens the self-minting hole guarantee 2 is supposed to close. If the hook
tests only the unnormalized command line, every one of these slips past a
`^bun run bypass` / `^op read` anchor and is ALLOWED:

```bash
bash -c "bun run bypass mint --reason=x --capability='*'"   # anchor sees "bash", not "bun"
FOO=1 bun run bypass mint …                                  # anchor sees "FOO=1", not "bun"
eval 'bun run bypass mint …'                                 # anchor sees "eval"
bash -c "op read op://…/ProtectedFilesBypassSigningKey/…"    # reads the private key out-of-band
```

The last one is the sharpest: `op read` wrapped in `bash -c` reads the signing
key using whatever 1Password session is already unlocked — the exact session the
mint CLI assumes — so a prompt-injected agent could sign its own wildcard token.
The protected-files guard does **not** catch these either, because `op read`
writes no file. So this guard must normalize the command exactly the way
`protected-files.ts` normalizes Bash write targets: **reuse the same shell-aware
front end (`shell-quote` tokenize + `validateQuoteBalance`) from `paths.ts`**,
then run each normalized command segment through the regex list.

The required pipeline (mirrors `extractWriteTargets` in `agent-tools/lib/paths.ts`,
so build one shell-aware normalizer and both guards call it):

1. **Tool filter.** `toolName !== "Bash"` → allow.
2. **Command extraction.** `input.toolInput["command"]` must be a non-empty
   string, else allow.
3. **Quote-balance gate → default-deny.** Run `validateQuoteBalance` first
   (`shell-quote.parse()` silently truncates on an unterminated quote rather than
   throwing). Any imbalance, or a tokenizer exception, returns `deny` — never a
   silent allow. A command whose shape the normalizer cannot resolve is treated
   as blocked, not waved through.
4. **Tokenize + segment.** Split the token stream at pipeline/separator operators
   (`|`, `&&`, `||`, `;`, `&`, `(`, …) into command segments.
5. **Normalize each segment.** For every segment:
   - **Strip leading `FOO=bar` env-assignments** and re-parse the remainder as a
     fresh segment (so `FOO=1 bun run bypass mint` normalizes to `bun run bypass
     mint`).
   - **Recurse into shell entry-points** — `bash -c '…'`, `sh -c '…'`,
     `zsh -c '…'`, and `eval '…'` — re-parsing the inner command body as its own
     command set (so the anchors test the real inner command, not the `bash`/`eval`
     wrapper).
   - Rebuild the normalized segment command string from its tokens.
6. **Pattern scan per normalized segment.** Test every compiled pattern against
   **each** normalized segment string (not just the raw line). `^`-anchoring now
   binds to the start of the real command, so `^bun run bypass` and `^op read`
   fire on the unwrapped, de-env-assigned form.
7. **First match decides.** On the first pattern that matches any normalized
   segment, check for a bypass (section 5); absent a bypass, return `deny`. No
   match on any segment → allow.

Patterns are still compiled with **no flags** (case-sensitive, single-line). The
change is *what they are tested against*: the normalized segments, not the raw
string. Default-deny on tokenizer failure is mandatory — an agent that cannot get
its command parsed cleanly does not get it run.

> Reproduction note: the shipped `agent-tools/hooks/pre_tool_use/invocation-block.ts`
> historically matched the raw command line only. A faithful clone that must make
> the mint verbs genuinely unreachable builds the shell-aware normalizer above;
> do not copy the raw-string form. It shares `validateQuoteBalance` + the
> segment/env-assignment/`bash -c`-recursion logic with `paths.ts` — one
> normalizer, two guards.

---

## 4. The decision contract

The hook returns a `HookOutput` discriminated union:

```ts
| { decision: "allow" }
| { decision: "deny"; reason: string }
| { decision: "allow_with_warning"; warning: string }
```

The dispatcher maps these to a process exit code across all hooks that ran for the event:

- `allow` → contributes nothing; if every hook allows, exit **0**.
- `allow_with_warning` → a `WARN` line on stderr, exit stays **0**.
- `deny` → stderr gets a `BLOCKED by agent-tools PreToolUse hook(s):` header with one bullet per denying hook, and the dispatcher exits **2**. Exit 2 is the "block the tool call" convention.

Any single deny across the whole dispatch wins → exit 2.

The deny reason for this guard is built by `blockedReason`:

```
BLOCKED: invocation matches an invocation-block pattern.
  command: <the command>
  pattern: <the raw regex string that matched>
See agent-tools/config/invocation-blocked.json. The human runs these subcommands;
if you need to run something the pattern blocks, ask the human to mint a bypass token
for guard "invocation_block:bash".
```

The agent sees the command, the exact pattern that matched, and the remediation.

---

## 5. Signed bypass override

Before returning `deny`, the hook consults `bypassAllows` from `agent-tools/lib/bypass.ts`:

```ts
const bp = await bypassAllows({
  guardName: GUARD_NAME,                            // "invocation_block:bash"
  argsCheck: (args) => argsCoversCommand(args, cmd),
  projectDir: ctx.projectDir,
  toolName: "Bash",
  command: cmd,
});
if (bp.matched) {
  return {
    decision: "allow_with_warning",
    warning: `bypass token ${bp.tokenId.slice(0, 8)} authorized Bash invocation (pattern: ${pattern})`,
  };
}
return { decision: "deny", reason: blockedReason(cmd, pattern) };
```

`bypassAllows` loads every active token from `.elves/bypass-tokens/*.token`, verifies each against the committed ed25519 public key, drops any that are expired or fail signature verification, and walks the surviving tokens' capabilities. It only considers a capability whose `guard` equals this hook's guard name (`invocation_block:bash`); for such a capability it calls the hook's own `argsCheck`. First match wins: it appends a `bypass_used` entry to the bypass audit log and returns `{ matched: true, tokenId, capability }`.

The private signing key never enters this path — only the mint/extend/add CLI reads it, and that read is itself invocation-blocked. That is the loop that prevents the agent from minting its own bypass.

### Args vocabulary for `invocation_block:bash`

The hook's `argsCheck` decides whether a token capability's `args` covers the specific command:

```ts
function argsCoversCommand(args: BypassArgs, cmd: string): boolean {
  if (args === "*") return true;                    // any blocked invocation
  const regexArg = args["regex"];
  if (typeof regexArg === "string") {
    try { return new RegExp(regexArg).test(cmd); }  // command matches this regex
    catch { return false; }
  }
  const commandArg = args["command"];
  if (typeof commandArg === "string") return cmd === commandArg;  // exact equality
  return false;
}
```

So a capability of `{ guard: "invocation_block:bash", args: "*" }` authorizes any otherwise-blocked invocation; `{ regex: "…" }` narrows to commands matching that regex; `{ command: "…" }` requires exact string equality.

On a bypass match the hook returns `allow_with_warning`, not `allow` — the exit code stays 0 but a `WARN` line names the token id and the blocked pattern, and the audit log records `bypass_used`. This is the only way a blocked invocation is permitted.

---

## 6. The authorized-prefix list is a different guard — it does NOT un-block

`agent-tools/config/authorized-commands.json` is `{ "prefixes": string[] }`. It belongs to the **protected-paths write guard** (`protected-files.ts`), not to this hook. Its purpose is to let certain Bash commands write to otherwise-protected file paths — e.g. `bun add <pkg>` rewrites `package.json` (a protected path), so `bun add` is on the prefix list.

This hook never reads `authorized-commands.json`. Being on the authorized-prefix list buys write permission from the protected-paths guard and buys **nothing** against this invocation block. Both hooks run in the same PreToolUse dispatch, and any deny wins.

Worked example — `bun run lint:exempt mint-current`:

- Protected-paths guard: `bun run lint:exempt` is an authorized write prefix → that hook returns `allow`.
- Invocation-block guard: `^bun run lint:exempt (mint-current|…)\b` matches → this hook returns `deny`.
- Dispatcher: one deny present → **exit 2**. The command is blocked.

The allow-list cannot override the block-list. They enforce different concerns and the stricter outcome wins.

Both config files live under `agent-tools/config/**`, which is itself a protected directory, so the agent cannot edit either JSON to add itself to a prefix list or remove a blocked pattern — those writes are denied by the protected-paths guard.

---

## 7. Concrete blocked-vs-allowed examples

**Blocked (exit 2):**

- `bun run bypass mint -- --reason=x` → matches `^bun run bypass (mint|extend|add|revoke)\b`.
- `op read "op://…/ProtectedFilesBypassSigningKey/notesPlain"` → matches `^op read .*ProtectedFilesBypassSigningKey.*`.
- `bun run lint:exempt mint-current` → matches the lint:exempt mutation pattern (blocked even though the same prefix authorizes protected-path writes).
- `bash ./.tmp/user_execute.sh` and `./.tmp/user_execute.sh` → match the user-execute patterns.
- `bash -c "bun run bypass mint …"`, `FOO=1 bun run bypass mint …`, `eval 'bun run bypass mint …'` → the normalizer unwraps the `bash -c`/`eval` body and strips the leading env-assignment, so the pattern matches the **normalized** segment. (A raw-string match would miss all three — see §3.)
- `bash -c "op read op://…/ProtectedFilesBypassSigningKey/…"` → `^op read …` matches the unwrapped inner command; the private-key read is blocked even inside a `bash -c`.

**Allowed — no pattern matches (exit 0):**

- `bun run bypass list`, `bun run bypass verify` — `list`/`verify` are not in the `(mint|extend|add|revoke)` alternation.
- `ls -la`, `echo hi`, `bun run check` — no pattern matches.
- Any non-Bash tool call (Edit, Write, MCP) — allowed by this guard outright.

**Allowed via bypass (exit 0 + WARN + audit):**

- `bun run bypass mint …` with an active `.elves/bypass-tokens/*.token` whose signature verifies, is unexpired, and carries `{ guard: "invocation_block:bash", args: "*" }` (or a matching `{ regex }` / `{ command }`) → `allow_with_warning`, a `WARN … bypass token <id8> authorized Bash invocation` line, and a `bypass_used` audit entry.

---

## 8. How to test it

Follow the pattern in `invocation-block.test.ts`. Use `_setConfigForTests` to inject a known pattern set (avoids disk IO and coupling to the shipped config), or `_resetConfigCacheForTests` to force a reload. Cover:

- Non-Bash tool call → `allow`.
- Bash with empty/missing command → `allow`.
- Command matching a blocked pattern, no bypass present → `deny`, with `reason` containing the command and the raw pattern.
- Command not matching any pattern → `allow`.
- Command matching a blocked pattern **and** a valid bypass token scoped to `invocation_block:bash` present → `allow_with_warning`, warning naming the token id prefix.
- Args-vocabulary unit tests on `argsCoversCommand`: `"*"` covers everything; `{ regex }` covers matching commands only; `{ command }` requires exact equality; anything else returns `false`.
