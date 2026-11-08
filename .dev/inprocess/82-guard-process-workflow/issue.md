# Guard process workflow

Issue: https://github.com/4thWAIV/agent-guard/issues/82

## Purpose

Define a structured, tool-managed workflow for AgentGuard that preserves human intent, original agent results, and execution evidence without LLM reconstruction or silent alteration.

## Starting vision

- Workflow documents have strict structures. Eventually, all writes go through AgentGuard.
- Documents move through one structured, dynamically configurable workflow.
- An LLM uses a tool to alter results. Every alteration is recorded, key alterations require user approval, and the system can detect subsequent alteration.
- Eventually, the server signs protected content and associates it with workflow and agent-session GUIDs across Claude and Codex.
- Start by defining the important parts of the conversation, its schema, and its file structure. The intended structured representation is YAML.
- The conversation carries the user's intent through the process. Investigation scope, areas, and capabilities belong in that representation rather than separately rewritten workflow arguments.
- Guard and workflow phases append evidence through tools or create separate tool-managed artifacts linked from the conversation.
- At the appropriate workflow point, a tool extracts the relevant conversation sections into the contract upon request.
- AgentGuard participates in stage transitions and enforces stage-dependent modification rules. Changing protected content can require a presence check and user approval.
- Tool help describes both patch-style edits and Claude-style edits.
- Map the inputs and outputs across the workflow so each consumer receives the intended source and version.

## First discussion

Define the conversation's content, schema, and file structure before designing the remaining phase artifacts. Distinguish human intent, agent findings, evidence, approvals, and execution state. Determine which content is protected and when.

The conversation.md name describes the existing starting document; its relationship to the intended YAML representation remains to be defined.

## Status

Discussion intake only. This issue records the requested vision, not an approved implementation contract or finalized schema. Server signing and exclusive AgentGuard writes are eventual goals.

