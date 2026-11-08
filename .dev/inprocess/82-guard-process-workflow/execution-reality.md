# Execution reality — what can actually call what

Facts, not design. Everything the workflow model asserts has to be derivable from this page. Sourced from the Claude Code Workflow tool's own reference and from the shipped scripts in `.agents/workflows/`.

## The three places code runs

| Place | Can call Guard? | Can spawn agents? | Notes |
|---|---|---|---|
| **Main thread** — the session the human is in | Yes, CLI or MCP | Yes, via the Workflow tool or a single Agent call | The only place the human is present |
| **Workflow script** — the JS the Workflow tool runs | **No** | Yes, `agent()` | Sandboxed: no filesystem, no Node API, no module import |
| **Subagent** — spawned by `agent()` | Yes, CLI or MCP | No | Has tools; reaches session MCP tools through ToolSearch |

## What the workflow script can and cannot do

Can: `agent(prompt, opts)`, `parallel(thunks)`, `pipeline(items, ...stages)`, `phase(title)`, `log(message)`, read `args`, read `budget`, call `workflow(name, args)` one level deep, and return a value to the main thread.

Cannot: touch the filesystem, run a process, import a module, call Guard, stream an agent's output, or use `Date.now()`, `Math.random()` or argless `new Date()`.

`agent()` returns the agent's final text, or a schema-validated object. One value, at completion. There is no pipe.

## What this forces

**The script is a router, not a writer.** It decides who runs next from returned values. Every document write happens on the main thread or inside a subagent.

**A subagent that produces a result writes it through Guard itself.** The alternative — returning it to the script and having the main thread write it later — puts the acting thread between a reviewer's output and Guard.

**The script briefs an agent with paths, not content.** It cannot read a document, so a prompt carries the path and the agent reads it. This is already how the shipped scripts work: `contractPath`, `rulePhaseResultPath`, `stageOutputPaths`.

**Guard cannot observe the agent lifecycle.** `agent()` is internal to the runtime and fires no hook. No launch, no death, no duration reaches Guard. Guard knows an agent existed only because a result arrived.

**Signing is main-thread only.** A gate needs the human's presence, and the human is only in the main thread.

## What that breaks in the current drafts

`AgentRunStatus` of `Running`, `Interrupted` and `Failed` have no source. Guard sees a result or no result.

`AgentRun.StartedAt` and `EndedAt` have no source. The comment says "The execution mechanism records the timestamps, not the agent," and that mechanism does not call Guard.

A `guard agent dispatch` operation cannot exist. It would sit beside the real launch rather than in it, so the script could launch three reviewers and register one.

A missing reviewer therefore cannot be detected by comparing dispatched against returned. It is detected by comparing **required by the level** — which Guard wrote at the Scope gate and which sits inside a signed checkpoint — against **submitted**. That is stronger: it fails whether the agent died, was never launched, or was launched and dropped.

## The cross-tool consequence

Codex has no scripted workflow primitive at all: no file format, no CLI verb, no config key, and sub-agent orchestration there is model-driven through `spawn_agent` and `wait_agent`. Its only deterministic control flow is a shell script around `codex exec`.

So a design that depends on the Workflow script cannot run on Codex. The backbone that works in both is **main thread plus Guard plus subagents that call Guard**. Claude Code's Workflow tool is then an accelerator for the fan-out stages, not the spine.
