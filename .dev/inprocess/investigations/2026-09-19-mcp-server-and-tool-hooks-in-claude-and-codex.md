# Investigation — MCP server options and tool-hook firing in Claude Code and Codex

Date: 2026-09-19
Versions the facts apply to: Claude Code 2.1.274, Codex CLI 0.154.0, MCP C# SDK 2.2.0.

Two questions were investigated. First, what the options are for exposing an executable as an MCP server that both Claude Code and Codex can consume, covering both the current state and a future long-running service. Second, whether pre- and post-tool hooks fire when either tool spawns a sub-agent.

## Part 1 — MCP server options

### Transports each client accepts

Claude Code accepts `stdio`, `http` (streamable HTTP, also accepting the alias `streamable-http`), `sse` (deprecated) and `ws` (WebSocket).

Codex 0.154.0 accepts `stdio` and `streamable_http` only. It has no SSE and no WebSocket transport. The `experimental_use_rmcp_client` flag no longer exists; the rmcp client is the only client and nothing about HTTP transport is gated.

The transports common to both are therefore **stdio** and **streamable HTTP**.

### Server features each client consumes

| Feature | Claude Code | Codex 0.154.0 |
| --- | --- | --- |
| tools | yes | yes |
| resources | yes | yes |
| prompts (as slash commands) | yes | no |
| elicitation | no | yes |
| sampling | no | no |
| roots | no | no |
| logging | — | yes |
| pagination | — | yes |

A server intended to behave identically in both is a tools-and-resources server. Prompts are ignored by Codex; elicitation is ignored by Claude Code.

### Protocol version

Codex requests protocol revision `2025-06-18` by default. Its `2026-07-28` mode is double-gated behind the `mcp_2026_07_28` feature flag (under development, currently false) and a `CODEX_MCP_PROTOCOL_VERSION=2026-07-28` environment marker on the server. Codex skips `2025-11-25` entirely.

Claude Code negotiates between a v1 runtime (MCP TypeScript SDK 1.x) and a v2 runtime (SDK 2.0, protocol revision `2026-07-28`), controlled by `MCP_SDK_GENERATION` and `MCP_PROTOCOL_NEGOTIATION`.

Leaving `McpServerOptions.ProtocolVersion` null makes the server answer whatever revision the client requests, which satisfies both. Pinning `2026-07-28` makes the server reject Codex's default handshake.

### Options before a long-running service exists

**Stdio.** The client spawns the executable as a child process and owns its lifetime, one process per client session. Nothing needs to be running in advance and nothing needs tearing down. The same command line is configured in both clients. Three constraints apply: stdout carries JSON-RPC only, all logging goes to stderr, and the `initialize` handshake must complete inside Codex's `startup_timeout_sec`, which defaults to 10 seconds.

**Streamable HTTP, launched outside the client.** Supported by both, but neither client will start the listener. Something external must run it and keep it alive; the client config carries only a URL.

### Options once a long-running service exists

**Streamable HTTP served by the service.** Both clients point at one URL and share one process, so any state or cache lives in the service rather than per session.

**Stdio front end proxying to the service.** The executable keeps the stdio contract the clients already understand and forwards to the service over its own IPC. The client configuration is then identical before and after the service exists.

**Both transports from one binary, selected at startup by argument or environment.** The C# SDK has no shipped sample for this. The maintainers' stated intent on the open issue is two entry points sharing a single registration extension method, chosen at startup — either/or, not both live at once. Registering both would not collide at the DI level, since the stdio path registers a singleton transport, a single-session hosted service and a singleton server while the HTTP path registers a handler, a session manager and a server per session, but that configuration is untested and undocumented.

### Configuration shapes

There is no shared configuration format. Claude Code reads JSON, Codex reads TOML, and the server itself is identical either way.

Claude Code stdio, in `.mcp.json` at the repo root or under `mcpServers` in `~/.claude.json`:

```json
{
  "mcpServers": {
    "agentguard": {
      "type": "stdio",
      "command": "/usr/local/bin/agentguard",
      "args": ["mcp"],
      "env": { "AGENTGUARD_LOG": "${AGENTGUARD_LOG:-warn}" },
      "timeout": 600000
    }
  }
}
```

Codex stdio, in `~/.codex/config.toml` or a trusted project's `.codex/config.toml`:

```toml
[mcp_servers.agentguard]
command = "/usr/local/bin/agentguard"
args = ["mcp"]
startup_timeout_sec = 10
tool_timeout_sec = 60

[mcp_servers.agentguard.env]
AGENTGUARD_LOG = "warn"
```

Claude Code HTTP:

```json
{
  "mcpServers": {
    "agentguard": {
      "type": "http",
      "url": "http://127.0.0.1:3001/mcp",
      "headers": { "Authorization": "Bearer ${AGENTGUARD_TOKEN}" }
    }
  }
}
```

Codex HTTP:

```toml
[mcp_servers.agentguard]
url = "http://127.0.0.1:3001/mcp"
bearer_token_env_var = "AGENTGUARD_TOKEN"
http_headers = { "X-AgentGuard-Client" = "codex" }
```

CLI equivalents are `claude mcp add --scope project agentguard -- /usr/local/bin/agentguard mcp` and `codex mcp add agentguard -- /usr/local/bin/agentguard mcp`, and for HTTP `codex mcp add agentguard --url http://127.0.0.1:3001/mcp --bearer-token-env-var AGENTGUARD_TOKEN`.

Claude Code scopes are local (`~/.claude.json` under the project path key), project (`.mcp.json` in the repo) and user (top-level `mcpServers` in `~/.claude.json`), with the highest scope overriding lower ones outright rather than merging fields.

Codex plugins ship an `mcpServers` JSON file in the same shape Claude Code uses, so distributing as a Codex plugin unifies the two formats. The installed GitHub plugin is a live example:

```json
{ "mcpServers": { "github": { "type": "http", "url": "https://api.githubcopilot.com/mcp/", "bearer_token_env_var": "GITHUB_PAT_TOKEN" } } }
```

### Authentication

Both clients support a static bearer token, arbitrary static headers, a local helper command that prints a JSON object of headers, and OAuth. Claude Code names the helper `headersHelper` and re-runs it automatically on a 401 or 403, with a 10-second timeout. Codex names it `http_headers_helper` and also offers `bearer_token_env_var` and `env_http_headers`.

Claude Code reads known credential variables such as `ANTHROPIC_API_KEY` and `NPM_TOKEN` as empty inside a remote `url` or `headers`, so a custom variable name must be used.

### Codex-only controls with no Claude Code counterpart

Per server: `enabled`, `required`, `enabled_tools`, `disabled_tools`, `supports_parallel_tool_calls`, `omit_tools_from`, `environment_id`. Per tool: `[mcp_servers.<name>.tools.<tool>]` with `approval_mode` of `auto`, `prompt`, `writes` or `approve`, and an `output_token_limit`.

Codex renames MCP tools to `mcp__<server>__<tool>`, capped at 128 bytes with a sha1 suffix beyond that.

### Implementing the server in .NET

The official SDK is at `github.com/modelcontextprotocol/csharp-sdk`, Apache-2.0, under the Linux Foundation rather than Microsoft, currently version 2.2.0. Packages: `ModelContextProtocol.Core` (client and low-level server, no hosting dependency), `ModelContextProtocol` (adds DI and `Microsoft.Extensions.Hosting.Abstractions`), `ModelContextProtocol.AspNetCore` (HTTP), plus `.Extensions.Apps` and `.Extensions.Tasks`.

Target frameworks are `net10.0;net9.0;net8.0` for all three, with `netstandard2.0` additionally on `.Core` and `ModelContextProtocol`.

Stdio without any hosting dependency:

```csharp
await using McpServer server = McpServer.Create(
    new StdioServerTransport("agentguard"),
    new McpServerOptions
    {
        ToolCollection = [McpServerTool.Create((string arg) => $"Echo: {arg}", new() { Name = "Echo" })]
    });
await server.RunAsync();
```

With hosting, the logging line is what protects stdio framing:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<MyTools>();
await builder.Build().RunAsync();
```

HTTP:

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<MyTools>();
app.MapMcp("/mcp");
```

`MapMcp` defaults to the root path. `HttpServerSessionMode` replaced the 1.x boolean `Stateless` property and now defaults to stateless. `McpClientFactory` and `McpServerFactory` were removed in favour of `McpClient.CreateAsync` and `McpServer.Create`, and `SseClientTransport` became `HttpClientTransport`.

Two constraints matter for a single-file self-contained publish. Use the generic `WithTools<T>()`; the non-generic `WithTools` and `WithToolsFromAssembly` carry `[RequiresUnreferencedCode]` because they scan the assembly. And SDK issue 1846 is open: a tool taking an array parameter throws a `JsonTypeInfo` `NotSupportedException` under Native AOT, with the fix PR unmerged and the workaround being to supply `List<object>` metadata. That defect is AOT-specific and does not apply to a single-file publish that is not AOT-compiled.

The SDK sets `IsAotCompatible` on all packages except the `netstandard2.0` target, and its CI publishes and runs a real native binary as a gate.

## Part 2 — do tool hooks fire on an agent call

The answer is yes in both tools, at both levels. The call that spawns the sub-agent fires `PreToolUse` and `PostToolUse` like any other tool call, and the tool calls the sub-agent makes internally fire the same hooks again. Both tools expose `agent_id` and `agent_type` in the hook input to tell the two apart, and both omit those keys entirely on a main-thread call.

### Claude Code

The spawn tool is named `Agent`, renamed from `Task` in version 2.1.63, with `Task` still accepted as an alias. A `PreToolUse` matcher of `"Agent"` catches the delegation itself.

From the hooks reference: "Hooks from settings files, managed policy settings, and plugins also run inside subagents. When a subagent calls a tool, tool events such as `PreToolUse` and `PostToolUse` fire the same configured hooks as in the main conversation, and the input carries the `agent_id` and `agent_type` common input fields that identify the subagent."

`agent_id` is documented as "Present only when the hook fires inside a subagent call. Use this to distinguish subagent hook calls from main-thread calls." `agent_type` is the agent name, such as `Explore` or a custom agent's name.

Common input fields on every event: `session_id`, `prompt_id`, `transcript_path`, `cwd`, `scratchpad_dir`, `permission_mode`, `hook_event_name`.

`SubagentStart` and `SubagentStop` also exist and match on agent type. An agent's own markdown frontmatter can define hooks that run only while that agent is active.

MCP tool calls fire hooks, matched as `mcp__server__tool` with regex support, and plugin-bundled servers carry the plugin name as `mcp__plugin_<plugin>_<server>__<tool>`.

### Codex 0.154.0

Sub-agents are the `multi_agent` feature, which is stable and enabled. The tool surface is `spawn_agent`, `assign_agent_task`, `send_message`, `wait_agent`, `close_agent`, `followup_task`, `interrupt_agent` and `list_agents`. `spawn_agent` carries `Agent` as a matcher alias, but stdin always reports `tool_name: "spawn_agent"`.

A spawned sub-agent is a full session that builds its own hook set from the same configuration layers, which is why its internal tool calls fire everything again. `agent_type` is the role name from `config.agent_roles`, defaulting to `default`.

Twelve hook events exist: `PreToolUse`, `PermissionRequest`, `PostToolUse`, `PreCompact`, `PostCompact`, `SessionStart`, `SessionEnd`, `UserPromptSubmit`, `SubagentStart`, `SubagentStop`, `Stop`, `Interrupt`. `SessionEnd` is root-only and does not fire for sub-agents. Non-ThreadSpawn sub-agents used for review, compaction and memory consolidation get no start or stop hooks.

`PreToolUse` stdin carries `cwd`, `hook_event_name`, `model`, `permission_mode`, `session_id`, `tool_input`, `tool_name`, `tool_use_id`, `transcript_path` and `turn_id`, with `agent_id` and `agent_type` optional. `PostToolUse` adds `tool_response`. For a sub-agent hook, `session_id` is the parent session's id.

Codex's published documentation omits `agent_id` and `agent_type` from its payload tables, so a documentation-only reading concludes that a hook cannot distinguish a sub-agent. The shipped binary's embedded schemas and the source at tag `rust-v0.154.0` both carry the fields, and an in-repo test asserts the parent gets none and the child gets its role name.

The matcher is not plain regex. An omitted matcher, an empty string or `*` matches everything. A matcher composed only of `[A-Za-z0-9_|]` is an exact literal with `|` alternation, so `Bash` does not match `BashOutput` and `mcp__memory` does not match `mcp__memory__create_entities`. Anything else is treated as an unanchored regex, so `^Bash` does match `BashOutput`. The matcher is ignored entirely for `UserPromptSubmit`, `Stop` and `Interrupt`; it applies to `agent_type` for the sub-agent events, to `source` for `SessionStart`, and to `trigger` for the compaction events.

Blocking semantics: exit 0 passes, exit 2 with a reason written to stderr blocks, and every other exit code is recorded as failed without blocking. Exit 2 with empty stderr does not block. Only synchronous hooks can block — a hook declared `async = true` that exits 2 simply fails. The JSON form is `{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"..."}}`, with the legacy `{"decision":"block","reason":"..."}` also accepted. A `permissionDecision` of `ask` and a legacy `decision` of `approve` are parsed but unsupported, and the tool call proceeds.

Hosted tools such as `WebSearch` are not hooked at all. Codex's own documentation states: "Some specialized tool paths can opt out of the default hook path. Treat tool hooks as a useful guardrail, not a complete enforcement boundary."

Hook configuration is additive across layers rather than overriding. All of `<CODEX_HOME>/hooks.json`, `<CODEX_HOME>/config.toml`, `<project>/.codex/hooks.json`, `<project>/.codex/config.toml`, plugin sources and managed sources contribute, and every matching hook runs.

Hooks are no longer behind a feature flag; `codex features list` reports `hooks stable true`. The `codex_hooks` key is a deprecated alias that still works.

### The Codex hook trust mechanism

`[hooks.state]` entries are keyed as `{key_source}:{event}:{group_index}:{handler_index}`. The `trusted_hash` is not taken over the key or the file text. It is a SHA-256 over a normalized identity — the event name in snake_case, the matcher, and one normalized handler — serialized as recursively key-sorted canonical JSON. Editing the command, matcher or timeout invalidates the hash and silently stops the hook until it is re-trusted through `/hooks`. The states are Trusted, Modified, Untrusted and Managed. `--dangerously-bypass-hook-trust` skips the check but still honours `enabled = false`.

## Defects found in the fourth-waiv-ai Codex hook configuration

`/Users/timothystockstill/code/macos/4thWAIV/fourth-waiv-ai/.codex/config.toml` declares a `PreToolUse` hook with `matcher = "apply_patch|Edit|Write|NotebookEdit"`.

`NotebookEdit` is dead: no Codex tool or alias by that name exists. The comment above it is also incorrect. There is no Edit/Write to apply_patch normalization; `apply_patch` advertises `Write` and `Edit` as matcher aliases, and stdin always reports `tool_name: "apply_patch"`. The hook still fires, because the matcher is in exact-alternation mode and `apply_patch` matches literally.

That hook covers file-editing tools only. Shell-mediated writes such as `sed -i`, `rm`, output redirection and `git checkout` bypass it entirely.

## Evidence and gaps

The Claude Code facts were taken from the hooks reference and sub-agents pages at code.claude.com and quoted directly.

The Codex facts were taken from the shipped 0.154.0 binary's embedded JSON schemas, the source at tag `rust-v0.154.0`, the CLI's own help output on this machine, and the documentation at learn.chatgpt.com. An attempt to capture live hook payloads from a running Codex session under an isolated home did not complete, because `codex exec` hung with no model reachable in that environment. The trust-hash algorithm was confirmed by reproducing a stored hash from this machine byte for byte; the payload field claims rest on the binary schema and the source, which agree with each other.

Not established: which Codex release removed `codex mcp-server`; whether Codex feeds third-party MCP resources into model context or only surfaces them for Codex Apps; how Codex resolves conflicting decisions among several matching `PreToolUse` hooks, which is documented only for `PermissionRequest`, `Stop` and `SubagentStop`; and whether registering both MCP transports on one .NET host actually works.

## Part 3 — how each tool handles workflows

### Claude Code

Claude Code has a first-class scripted workflow primitive. A workflow is a JavaScript file that opens with an exported `meta` literal giving `name`, `description` and `phases`, and then orchestrates agents through `agent()`, `parallel()`, `pipeline()` and `phase()`. The script decides what runs, in what order, and what happens to each result; the model does not.

Scripts are passed inline, or by `scriptPath`, or by `name` for a saved workflow in `.claude/workflows/`. Every invocation persists its script under the session directory and returns that path, so a run can be edited and re-invoked against the same path. A prior run can be resumed with `resumeFromRunId`, and completed `agent()` calls with unchanged prompt and options return their cached results rather than re-running. Each `agent()` call can carry a `label`, a `phase` and a `schema`, so results come back structured rather than as prose. Workflows run in the background and report completion as a task notification.

This repository's five workflow scripts in `.agents/workflows/` are exactly this kind of script.

Alongside that, Claude Code has agents defined in `.claude/agents/*.md`, user-defined slash commands, skills, plugins and hooks.

### Codex 0.154.0

Codex has no scripted workflow primitive. There is no workflow file format, no directory convention, no CLI verb and no configuration key that lets a script sequence phases, fan out to sub-agents and collect structured results inside a session. A sweep of the CLI subcommands, the source at tag `rust-v0.154.0` and the documentation found nothing of the kind. The matches that look promising are unrelated: `tools/orchestrator.rs` is the tool-call approval and sandbox sequence, `agent-graph-store` persists the parent and child agent tree without executing it, and `templates/agents/orchestrator.md` is a system-prompt template that asks the model to parallelise.

Sub-agent orchestration in Codex is model-driven. The active tool family is `multi_agent_v1`, namespace `multi_agent_v1`, with `spawn_agent`, `send_input`, `resume_agent`, `wait_agent` and `close_agent`. A V2 family exists under namespace `collaboration` with `spawn_agent`, `send_message`, `followup_task`, `wait_agent`, `interrupt_agent` and `list_agents`, but `multi_agent_v2` is currently false.

`config.agent_roles` declares what an agent is, never when it runs or what runs next. Roles are declared inline under `[agents.<name>]` or as `.toml` files auto-discovered in `~/.codex/agents/` and `<project>/.codex/agents/`, and a role may only narrow the child: developer instructions, model, reasoning effort, verbosity, personality, service tier, plus feature and skill disables. There is no ordering, dependency, phase or routing key anywhere in the schema. The official documentation states that Codex itself handles orchestration across agents, including spawning, routing follow-ups, waiting and closing.

Codex has no user-defined slash commands. The slash commands are a compiled-in enum. A plugin's `commands/*.md` files are converted at install time into skills, capped at 4000 bytes, so they become model-selected rather than deterministic entry points. A plugin's `agents/*.md` files are plain prompt text that nothing in the loader registers as an agent. There is no `~/.codex/prompts/` mechanism.

Hooks are the only deterministic in-session seam, and they cannot spawn or sequence agents. The `prompt` and `agent` hook handler types are declared in the schema but explicitly unimplemented, logging "agent hooks are not supported yet".

### The deterministic route on Codex

The only way to get deterministic control flow is a script outside the session driving `codex exec`. In `--json` mode stdout is JSONL, one event per line, with event types `thread.started`, `turn.started`, `turn.completed`, `turn.failed`, `item.started`, `item.updated`, `item.completed` and `error`, and item kinds including `agent_message`, `command_execution`, `file_change`, `mcp_tool_call` and `collab_tool_call`. `--output-schema` constrains the final result and `-o` writes the last message to a file, so a script can branch on a structured result. `codex exec resume --last` and `codex exec fork <id>` continue a thread. The official TypeScript and Python SDKs wrap this same CLI and expose no sub-agent control.

Fan-out under that model means launching several `codex exec` processes in parallel and joining on their exit. Each is a separate process with its own thread, so they share no context and cannot message one another.

### Consequence for the Codex workflow twins backlog item

`.dev/backlog/10-codex-workflow-twins/issue.md` states the scope as running "the same fan-out steps through Codex's own subagents, driven by the same step-spec the Claude scripts use."

Codex's in-session sub-agents cannot be driven that way. Nothing outside the model chooses which sub-agent runs next, so a step-spec cannot sequence them. A Codex twin that preserves the determinism of the Claude scripts has to be an external driver around `codex exec`, which yields independent processes with no shared context, rather than in-session sub-agents. That is a different mechanism from the one the backlog item describes, and the choice belongs to the human.
