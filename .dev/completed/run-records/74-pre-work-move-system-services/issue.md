> The live GitHub issue supersedes this file. Read https://github.com/4thWAIV/agent-guard/issues/74 for the authoritative text.

# 74 — Evaluate every static class in src for conversion to an instance class, then fence the count

## Why

A static class cannot be faked. Anything that calls one is welded to it, so the only way to test around it is to thread its dependencies through as method arguments — which is exactly how these got written, and it is why so much of the Engine passes `ISystemServices` down as a parameter instead of taking it in a constructor. The result is a testability pit and a persistent code smell, and every new one makes the next one look normal.

The goal is to remove them. Not now — the count simply must not grow while we get there.

## What exists today

Thirty-five static classes in `src`, thirty-three of them in `AgentGuard.Engine`. They are not one problem:

**Name and path holders**, roughly eight — the setup verb strings, the hook command tokens, the polkit action id, the machine and project paths. These hold constants and nothing else.

**Pure functions with no dependencies**, roughly a dozen — hashing, version parsing, the snapshot and region differs, the serializers, the verdict aggregator.

**Orchestrators doing real work with services handed in as arguments**, roughly fourteen. This is the group that actually hurts. `ApprovalGate`, `SetupCommands`, `GuardHost` and `GuardEngine` are the notable ones — each does substantial work, each takes the services it needs as a parameter on every call because it has no constructor to inject them into, and none of them can be substituted in a test.

Out of scope: the analyzers project, where a hundred and twelve static classes are conventional stateless Roslyn helpers, and the test projects.

## The work

Evaluate every static class in `src` and rule each one: becomes an instance class with an interface, or stays static with a stated reason. The third group above is where the answer is most likely to be "convert"; the first two may well survive, and the evaluation is what decides that rather than a blanket rule applied up front.

Then add an analyzer rule that fails a new static class in `src`, carrying an allowlist of whatever the evaluation ruled acceptable. The rule has to come after the evaluation, or it has nothing correct to allow.

## Done when

Every static class in `src` carries a ruling, the ones ruled convertible are instance classes reached through an interface, an analyzer fails any new static class outside the allowlist, and the allowlist names each survivor with the reason it survived.

