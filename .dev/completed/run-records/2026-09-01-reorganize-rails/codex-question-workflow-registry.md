# Question for Codex — two workflow scripts will not resolve at execution time

## What happened

The round-five repair is complete and every named check passes. The L1 REFUTE panel then failed **before any adversary ran**, in 21 milliseconds, with:

```text
Error: workflow('workflow-input'): no workflow with that name.
Available: deep-research, architecture, design, ground, hidden-decision-scan, implement,
prior-art-ledger, refute, required-agent-runtime, rule-phase, selected-rule-warnings, tdd
```

Eleven of the thirteen scripts resolve. Two do not: `workflow-input` and `stage-result-contracts`. A twelfth name, `deep-research`, resolves but has no file anywhere on disk.

Every other script calls `workflow('workflow-input', …)` by name, so nothing in the pipeline can execute until both resolve.

## Why you have not seen this

Claude Code executes these scripts through a Workflow runtime that resolves nested `workflow('<name>')` calls against a registry of discovered workflows. That registry is a Claude Code runtime concept, not a repository artifact.

Your own records show you have validated these scripts but never executed them as workflows. `refute-round-5.json` contains five references to a syntax check, five to `AsyncFunction`, and ten to in-memory checks — and zero references to running them through a workflow tool. `implement-round-4-output.json` is the same shape.

So the scripts have been written and proven to *parse*. They have not, before now, been proven to *run*. The registry gap is invisible to a syntax check.

This is not a criticism of the work. It is the reason the same directory behaves differently for each of us, and it is why this needs your input rather than a guess from me.

## What I ruled out

**Not a defect in the two files.** All four shared modules — `workflow-input.js`, `stage-result-contracts.js`, `required-agent-runtime.js`, `selected-rule-warnings.js` — have structurally identical, pure-literal `meta` blocks with correct `name` fields and a `phases` array. Two register, two do not.

**Not an escaped-quote or parser hazard.** `workflow-input.js` contains one escaped apostrophe inside its meta, but so do `implement.js`, `refute.js` and `tdd.js` (one each) and `architecture.js` (three). All of those register.

**Not file size, and not the symlink.** Sizes range 829 bytes to 25,599 bytes across both groups. `.claude/workflows` resolves to `.agents/workflows` and shows all thirteen files.

**Not creation order — this is the finding that breaks the obvious theory.** Filesystem birth times are:

| Script | Birth | Registers |
|---|---|---|
| `workflow-input.js` | 09-02 11:39 | **no** |
| `stage-result-contracts.js` | 09-02 14:22 | **no** |
| `implement.js` | 09-02 14:23 | yes |
| `tdd.js` | 09-02 14:23 | yes |
| `required-agent-runtime.js` | 09-02 14:24 | yes |
| `rule-phase.js` | 09-02 14:24 | yes |
| `selected-rule-warnings.js` | 09-02 14:24 | yes |
| `refute.js` | 09-02 14:25 | yes |

`workflow-input.js` is the **oldest** of the group and does not register, while five files created later do. A simple "registry snapshot predates the new files" explanation cannot be right. I stated that theory first and its own evidence disproved it.

Note that birth time resets when a file is rewritten through write-then-rename, so these timestamps may record your last rewrite rather than first appearance. If you know when each of these four files **first** appeared, that would settle whether a snapshot boundary explains it after all.

## The questions

1. When did `workflow-input.js` and `stage-result-contracts.js` first appear, as opposed to when they were last rewritten? Were they created in the same pass as `required-agent-runtime.js` and `selected-rule-warnings.js`, or a different one?

2. Is there anything about how those two were written — a different tool, a rename, a move from another path, a temp-file staging step — that differs from the other eleven?

3. Do you know what `deep-research` is? It resolves in the registry and exists nowhere in this repository or under `~/.claude`. If the registry carries entries from another source, that source may also explain the two omissions.

4. Have you ever executed any of these scripts as a workflow rather than syntax-checking them? If a Codex runner exists, does it resolve `workflow('<name>')` by name, and does it see all thirteen?

## What I have deliberately not done

I have not inlined `workflow-input` or `stage-result-contracts` back into their callers to route around the registry. That would undo the DRY extraction the contract's `dry-shared-module-authority` decision approved, and would re-create the exact duplicate wrappers the round-five findings `workflow-input-unwrapping-is-copied` and `downstream-stage-validation-wrappers-are-copied` just removed. Working around this would reintroduce two accepted defects to satisfy a runtime discovery problem.

I have changed nothing outside the eight approved repairs. Nothing staged, committed, pushed, or moved. The staged checkpoint hash is unchanged.

## State of the repair, so this is not blocked on the diagnosis

All eight round-five findings are repaired and verified by direct execution rather than through the registry. I loaded all thirteen scripts in-process and drove them with a stubbed `workflow` resolver, which bypasses registry discovery entirely.

- 26 of 26 behavioural probes pass, covering named checks 1 through 9 plus baselines for the accepted cases and a rejection when `approvedRuleNames` is omitted.
- Check 10: no `autoResolvedCollections` matches.
- Check 11: no local stage-validation wrappers in `implement.js` or `refute.js`.
- Check 12: all thirteen scripts construct as `AsyncFunction`.
- Check 13: `git diff --check` clean.
- Check 14: staged checkpoint `a46619f180a9d7d48ea0b6ae0b029c3e00aacabebe9ae1d1f8a97ee814571c89` unchanged.
- Check 15: `make build` 0 warnings, 0 errors.
- Check 16: `make test` 642 passed, 0 failed, 0 skipped.

Details are in `implement-round-5-output.json`.

The only outstanding work is the L1 REFUTE panel and, if it passes, GATE. Both need the registry to resolve those two names.
