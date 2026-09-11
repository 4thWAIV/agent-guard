> The live GitHub issue supersedes this file. Read https://github.com/4thWAIV/agent-guard/issues/75 for the authoritative text.

# 75 — The agent return path — an agent starts a long-running action and receives the result when it finishes

## Why

Everything an agent can ask the guard to do today finishes inside the call that asked for it. Anything that needs the person at the desktop does not: approving a contract, a presence check, signing. Those take as long as a human takes, and there is no way for the answer to reach the agent afterwards. The agent either blocks on a call that cannot return in time, or it never learns what the user decided.

This is the missing return path. Without it, every human-in-the-loop capability the product is built around — approval, presence, signing — has no way to report its outcome to the agent that asked for it.

## What exists today

The guard reaches an agent through exactly two hook events, `PreToolUse` and `PostToolUse`, wired into `.claude/settings.json` as `hook pre` and `hook post --host claude-code` (`src/AgentGuard.Engine/Setup/HookCommand.cs`). Both are pull, blocking, and agent-initiated: the agent is about to use a tool, the guard answers within that call. Nothing in the repo can originate a message toward an agent.

The per-host seam is already built and already names both agents. `IHostAdapter` (`src/AgentGuard.Abstractions/Contracts/IHostAdapter.cs`) states "One implementation exists per supported host (Claude Code, Codex)", and today carries only the hook direction — `Read` normalizes a payload, `Render` writes the host's exit-code protocol.

Each host offers a different delivery mechanism, and they are not the same shape:

- **Claude** has Monitor, which turns each stdout line of a background process into a notification in the session. It is a subscription the agent arms itself. Default deadline 300s, maximum 3600s, or `persistent: true` for the life of the session.
- **Codex** has `codex queue --thread <THREAD> --message <TEXT>`, which injects a message into an existing session. It is a push — the guard runs a command and the message lands. Thread ids resolve from `~/.codex/session_index.jsonl`, which is line-delimited `{id, thread_name, updated_at}`.

## The work

An agent makes a request that will take a long time — usually because it needs the user at the desktop. The request is fire and forget: the CLI call returns immediately with a correlation id and does not wait. The result arrives later, by whichever route the agent's host supports.

**Claude** starts a `listen` process and keeps it running as a persistent Monitor, armed when the skill loads rather than per request. The response arrives as a line on that stream, matched to the request by its correlation id.

**Codex** needs no listener. When the action completes, the server calls `codex queue` against the requesting session's thread.

The CLI verb for the listening process has not been selected. `listen` is a placeholder used throughout this document and is not a choice. `monitor` is the other candidate raised so far, and it collides with the name of Claude's own Monitor tool, which will make it ambiguous to talk about.

This needs:

1. **Every protocol message carries its session id.** Both agents send their session or thread id on every message to the guard, not only on the opening request. The server keeps an index of session id to where a response for it goes, so a result reaches the session that asked for it. With several Claude monitors running at once, that id is the only thing telling them apart.
2. **A correlation id.** The request returns one and the response carries it back. The session id says where a response goes; the correlation id says which request it answers. Both are needed, and one does not substitute for the other.
3. **Every path ends in exactly one response.** Approved, rejected, dismissed, and no graphical session available all produce a result. If only approval reports back, a rejection leaves the agent waiting forever.
4. **A deadline longer than Monitor's default.** A person reading a contract will exceed five minutes. The CLI owns the deadline; the Monitor runs persistent.
5. **Fail loud on restart.** If the server restarts with a request outstanding, the listening process exits with an error rather than hanging. The request is lost — acceptable — but the agent finds out.

A session id an agent sends is a reply-to address, not a claim about who it is. It routes a response; it authorizes nothing. The server establishes the user from peer credentials as it does for any other client, and delivers only to a session belonging to that user.

## Done when

An agent requests an action that needs the user, gets a correlation id back immediately, and receives the outcome when the user finishes — on Claude through a persistent `listen` monitor armed at skill load, and on Codex through `codex queue`. With more than one session connected at once, each result reaches the session that asked for it and no other. Rejection, dismissal, and no-graphical-session each produce a result rather than silence, and a server restart during a pending request ends it with an error rather than a hang.
