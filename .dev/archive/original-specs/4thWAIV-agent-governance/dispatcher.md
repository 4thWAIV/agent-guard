# The Engine Core — `types.ts` + `dispatcher.ts`

Build this **before any hook**. The dispatcher is the ONE engine every runtime
spawns; each hook is a plain function that only runs because the dispatcher
discovers and invokes it. The later build-order steps that construct the guards
(the PreToolUse block, the PostToolUse drift net, the invocation block, and the
lint gate) have nothing to execute in until this exists. `architecture.md`
sketches the engine
as a mental model; this file is the forward-construction spec — the exact
argv/stdin contract, the normalization, the discovery walk, and the aggregation
rule you must reproduce.

Two files, both **COPY VERBATIM** in `file-manifest.md` — but a docs-only builder
who is reconstructing them (not copying 4thWAIV's tree) needs the full contract,
so it is spelled out here. Nothing in either file encodes repo layout; they
operate on the host payload and the hook directory only.

- `agent-tools/lib/types.ts` — the hook contract + the token/capability types.
- `agent-tools/lib/dispatcher.ts` — parse → normalize → discover → run → aggregate.

---

## 1. `types.ts` — the single contract both runtimes and every hook share

`types.ts` declares every shared interface. It has **no runtime code and no
imports** — it is pure type declarations, which is why the dispatcher, both
hooks, the bypass library, and the mint CLI can all import from it without a
dependency cycle. Reproduce these exactly; the field names are load-bearing
(the normalization in §2 targets them, and the token shape is what the signer
canonicalizes).

```ts
export type HookEventName = "PreToolUse" | "PostToolUse";

export interface HookContext {
  /** Absolute path to the project root (resolved from the host payload's cwd). */
  projectDir: string;
  /** Which event the dispatcher is servicing. */
  hookEventName: HookEventName;
  /** session_id from the host payload; "" when the host omits it. */
  sessionId: string;
  /** cwd reported by the host payload; may differ from projectDir. */
  cwd: string;
}

export interface HookInput {
  /** Tool the host is about to invoke / just invoked (e.g. "Edit", "Bash", "apply_patch"). */
  toolName: string;
  /** Raw tool arguments; shape varies by tool (file_path, command, …). */
  toolInput: Record<string, unknown>;
  /** Correlates PreToolUse and PostToolUse for the same call. Optional. */
  toolUseId?: string;
}

export type HookOutput =
  | { decision: "allow" }
  | { decision: "deny"; reason: string }
  | { decision: "allow_with_warning"; warning: string };

export type Hook = (input: HookInput, ctx: HookContext) => Promise<HookOutput>;
```

That three-value `HookOutput` **is** the entire decision protocol. The dispatcher
understands nothing about protected paths, tokens, or drift — it only runs
`Hook`s and combines their `HookOutput`s.

The same file also owns the bypass-token contract (consumed by
`signing-and-bypass.md` and the guards, declared here so there is one copy):

```ts
export type BypassArgs = "*" | Record<string, unknown>;

export interface BypassCapability {
  guard: string;        // must equal the guard name a hook passes to bypassAllows
  args: BypassArgs;     // "*" wildcard, or a guard-specific JSON object
}

export interface TokenPayload {
  id: string;           // randomUUID
  issuedAt: string;     // ISO 8601
  expiresAt: string;    // ISO 8601 — single expiry per token
  reason: string;
  capabilities: BypassCapability[];
}

export interface Token {
  payload: TokenPayload;
  signature: string;    // base64 ed25519 over the canonicalized payload
}

export type VerifyResult =
  | { status: "valid"; token: Token }
  | { status: "expired"; token: Token }
  | { status: "invalid-signature"; reason: string }
  | { status: "malformed"; reason: string };
```

### The host payload — the ONE place snake_case lives

The host CLIs (Claude Code and Codex) both emit **snake_case** keys. `types.ts`
declares that raw shape; the dispatcher normalizes it to the camelCase
`HookInput`/`HookContext` above so no hook ever sees a host-CLI field name:

```ts
export interface HostPayload {
  tool_name?: string;
  tool_input?: Record<string, unknown>;
  tool_use_id?: string;
  session_id?: string;
  cwd?: string;
  hook_event_name?: string;
}
```

Every field is optional because the dispatcher must tolerate a malformed or
partial payload without throwing (it fills defaults — see §2). This
snake→camel mapping is the whole reason both runtimes can feed the same hooks.

---

## 2. `dispatcher.ts` — the mechanical, stateless engine

The dispatcher is spawned once per tool call by each runtime as
`bun agent-tools/lib/dispatcher.ts <event_type>` with the host payload on stdin.
Its job is fixed and side-effect-free apart from stdin/stdout/exit. Split it into
a pure `runDispatcher()` core (unit-testable) and a thin `cliEntry()` wrapper.

### Step 1 — event type from argv

`argv[2]` is the snake_case event name; map it to the canonical `HookEventName`.
An unknown value is a wiring error → **exit 2**.

```ts
const VALID_EVENT_TYPES = new Map<string, HookEventName>([
  ["pre_tool_use", "PreToolUse"],
  ["post_tool_use", "PostToolUse"],
]);
```

The snake_case arg doubles as the **subdirectory name** under
`agent-tools/hooks/` (see §Discover). A missing/empty `argv[2]` in `cliEntry`
prints usage and exits 2.

### Step 2 — payload from stdin (`readHostPayload`)

Read stdin to EOF, `trim()`, then:

- **Empty string** → return `null`. The `cliEntry` wrapper treats `null` as "the
  host didn't actually invoke a tool" and **exits 0** (allow). This is the common
  case for events the host fires speculatively.
- **Parses to a non-object** (e.g. `JSON.parse` yields a number/string/array-less
  primitive, or `parsed === null`) → **reject** → `cliEntry` writes a parse-error
  line to stderr and **exits 2**.
- **Unparseable JSON** → reject → **exit 2**.
- Otherwise resolve the parsed object as `HostPayload`.

```ts
export async function readHostPayload(): Promise<HostPayload | null> {
  const chunks: Buffer[] = [];
  return await new Promise((resolve, reject) => {
    process.stdin.on("data", (c: Buffer) => chunks.push(c));
    process.stdin.on("end", () => {
      const raw = Buffer.concat(chunks).toString("utf8").trim();
      if (raw === "") return resolve(null);
      try {
        const parsed: unknown = JSON.parse(raw);
        if (typeof parsed !== "object" || parsed === null) {
          return reject(new Error("stdin JSON is not an object"));
        }
        resolve(parsed as HostPayload);
      } catch (err) {
        reject(err instanceof Error ? err : new Error(String(err)));
      }
    });
    process.stdin.on("error", (err: Error) => reject(err));
  });
}
```

### Step 3 — normalize (snake_case → the camelCase hook contract)

This is the exact mapping. Every field defaults so a partial payload never
throws:

```ts
const ctx: HookContext = {
  projectDir: opts.projectDir,                 // = payload.cwd ?? defaultProjectDir()
  hookEventName: eventName,                     // from VALID_EVENT_TYPES
  sessionId: opts.payload.session_id ?? "",
  cwd: opts.payload.cwd ?? opts.projectDir,
};
const input: HookInput = {
  toolName: opts.payload.tool_name ?? "",
  toolInput: opts.payload.tool_input ?? {},
  ...(opts.payload.tool_use_id !== undefined
    ? { toolUseId: opts.payload.tool_use_id }
    : {}),
};
```

- `tool_name`   → `HookInput.toolName`   (default `""`)
- `tool_input`  → `HookInput.toolInput`  (default `{}`)
- `tool_use_id` → `HookInput.toolUseId`  (omitted entirely when absent — do NOT
  set it to `""`; the drift hook keys on its presence)
- `cwd`         → `HookContext.projectDir` (at the CLI boundary) **and**
  `HookContext.cwd`
- `session_id`  → `HookContext.sessionId` (default `""`)

`projectDir` is resolved once, at the CLI boundary, as `payload.cwd ??
defaultProjectDir()`, and passed into `runDispatcher`. `defaultProjectDir()`
resolves one level above `agent-tools/` via `fileURLToPath(import.meta.url)`.

### Step 4 — discover hooks (`discoverHookFiles`)

List `agent-tools/hooks/<event_type>/`, keep `*.ts` that are **not**
`*.test.ts` / `*.d.ts`, **sort** for determinism, and return absolute paths.
A missing/empty directory returns `[]` → the dispatcher **allows (exit 0)**.
Dropping a new `.ts` file into that directory registers a new guard — there is
no central list to edit.

```ts
async function discoverHookFiles(hooksDir: string, eventTypeArg: string): Promise<string[]> {
  const dir = path.join(hooksDir, eventTypeArg);
  let entries: import("node:fs").Dirent[];
  try {
    entries = await fs.readdir(dir, { withFileTypes: true });
  } catch {
    return [];
  }
  return entries
    .filter((e) => e.isFile())
    .filter((e) => e.name.endsWith(".ts") && !e.name.endsWith(".test.ts") && !e.name.endsWith(".d.ts"))
    .map((e) => path.join(dir, e.name))
    .sort();
}
```

`defaultHooksDir()` resolves `agent-tools/hooks` relative to this module
(`lib/dispatcher.ts` → `..` → `hooks`).

### Step 5 — import + invoke, default-deny on any failure

Each file is dynamic-imported (in parallel) and its **default export** is invoked
as a `Hook`. Every failure mode is converted to a `deny` so a broken guard fails
**closed**, not open:

- **Import throws** (`importHookModule`) → `deny` with the load error.
- **No function default export** → `deny` ("module does not default-export a Hook function").
- **Hook returns a non-`HookOutput` shape** (validated by `isHookOutput`) → `deny`.
- **Hook throws** → `deny` with the thrown message.

```ts
async function importHookModule(filePath: string): Promise<LoadedHookModule> {
  try {
    const mod = await import(pathToFileURL(filePath).href);
    return { filePath, hook: extractDefaultHook(mod), loadError: null };
  } catch (err) {
    return { filePath, hook: null,
      loadError: `failed to load hook (${err instanceof Error ? err.message : String(err)})` };
  }
}
```

`isHookOutput` admits exactly three shapes: `{decision:"allow"}`,
`{decision:"deny", reason:string}`, `{decision:"allow_with_warning",
warning:string}`. Anything else is a deny.

### Step 6 — aggregate

Collect every outcome, then:

1. **Warnings first.** For each `allow_with_warning`, push
   `WARN [<basename>] <warning>` to stderr. Warnings never change the exit code.
2. **Any `deny` → exit 2.** Push a `BLOCKED by agent-tools <EventName> hook(s):`
   header, then one `  • <basename>: <reason>` line per denying hook, and return
   exit **2**.
3. **Otherwise exit 0.**

```ts
if (denies.length > 0) {
  stderrLines.push(
    `BLOCKED by agent-tools ${eventName} hook(s):`,
    ...denies.map((d) => `  • ${path.basename(d.file)}: ${d.reason}`),
  );
  return { exitCode: 2, stderrLines };
}
return { exitCode: 0, stderrLines };
```

The exit codes are the contract both runtimes act on: **exit 2 = block the tool
call and surface stderr to the agent**; exit 0 = allow (warnings are advisory).
On PostToolUse, exit 2 reports the deny after the fact — and because the drift
hook already reverted the bytes, the block is backed by an actual undo.

### The CLI wrapper

`cliEntry()` glues stdin/argv/exit to the pure core, and guards against running
when imported by tests:

```ts
const invokedDirectly = fileURLToPath(import.meta.url) === path.resolve(process.argv[1] ?? "");
if (invokedDirectly) void cliEntry();
```

`cliEntry` reads argv[2], calls `readHostPayload()` (exit 2 on reject, exit 0 on
`null`), runs `runDispatcher({ eventTypeArg, payload, hooksDir:
defaultHooksDir(), projectDir: payload.cwd ?? defaultProjectDir() })`, writes
every `stderrLines` entry, and `process.exit`s the returned code.

---

## 3. Why the engine is built before the hooks

The build order puts this file **before** the four hooks. The dispatcher has no
compile-time dependency on any hook (it discovers them at runtime by directory
walk), so it compiles and unit-tests standalone against a fixture hooks
directory. Building it first means each hook you write afterward has a real
engine to run inside, and the "default-deny on failure" behavior is already
proven before a real guard depends on it.

Bypass is **not** the dispatcher's concern — each hook calls `bypassAllows()`
itself before it denies (see `signing-and-bypass.md`). The dispatcher only runs
hooks and aggregates their outputs.

---

## 4. How to test it

Drive `runDispatcher()` directly with a fixture `hooksDir` and a synthetic
`HostPayload`:

- **Unknown event arg** → exit 2, stderr names the known events.
- **Empty hooks dir / missing dir** → exit 0.
- **All-allow hooks** → exit 0, no stderr.
- **One deny** → exit 2, `BLOCKED …` header + the bullet with that hook's reason.
- **One `allow_with_warning`** → exit 0, `WARN […] …` line.
- **Default-deny matrix** — a hook file that (a) throws on import, (b) has no
  default export, (c) returns a bad shape, (d) throws at call time → each yields
  a deny.
- **Normalization** — assert `tool_use_id` absent ⇒ `toolUseId` omitted; `cwd`
  absent ⇒ `projectDir` falls back to `defaultProjectDir()`.
- **`readHostPayload`** — empty stdin ⇒ `null`; non-object JSON ⇒ reject;
  bad JSON ⇒ reject.

Reference tests: `agent-tools/lib/dispatcher.test.ts`, `…/types.test.ts`.
