# Design note — whole-tree snapshot (per-call, hardened)

Status: DRAFT under design-adversary review, final confirming round. The content-addressed version failed
review; the per-call version was confirmed structurally clean (only new seam is `IProtectedFileScanner`,
`IGuard` and every committed interface untouched); this revision folds the correctness hardening from rounds 2
and 3.

## Governing rule (established for the whole guard, restated here)
Any error during Pre denies the call. Reading a file, enumerating a directory, resolving a path, or writing the
snapshot — if any of it fails, Pre returns `CaptureFailed` and the Engine denies. We never proceed on a partial
snapshot, which is what makes the drift comparison safe: a file present at Post but absent from the pre-image is
genuinely new, because a Pre that could not fully enumerate the tree never let the call run.

## The need
Capture the pre-image of every configurable protected file before a call, so Post can revert unauthorized drift
however it was caused, including a Bash command whose target Pre never parsed. A call's pre-image only has to
survive from that call's Pre to its own Post.

## What is new versus reused
The only new seam is `IProtectedFileScanner`. Reused unchanged: `IFileReader`, `IContextWriter`/`IContextReader`,
`ContextRead`/`ContextMissing`, `CaptureResult`, the `FileChange` family, the `Effect` family, `IGuard`.

## The one new seam
- `IProtectedFileScanner.ScanAsync(environment, cancellationToken)` returns the configurable protected files
  currently on disk under the project root. It walks the tree, does not descend into the sealed skip-list
  locations, and matches each remaining candidate against `IProtectedSet`. Any enumeration failure denies (see
  the governing rule); the walk must surface inaccessible entries as errors rather than skipping them (the .NET
  `EnumerationOptions.IgnoreInaccessible` default is `true` and would silently under-report — it must be `false`),
  so an incomplete scan denies instead of hiding a file.

## Sealed skip-list policy (round-2 finding 2, round-3 finding 4)
The skip list is part of the Sealed/System floor, so the AI cannot edit it to hide a file. It is anchored to
specific root-relative build-output locations the Provider supplies (for C#, `<projectDir>/bin` and
`<projectDir>/obj`; later `node_modules`, `target`), decided on the canonical path — not any folder that merely
shares the name `bin` at some depth, so a real source directory named `bin` or `target` is not silently dropped.
A configurable match inside a genuine build-output location is deliberately not protected, and this is what
bounds the walk cost.

## Membership identity (round-2 finding 1)
Pre and Post are separate processes that each load config. The snapshot records a hash of the assembled protected
set — the resolved matcher patterns, the enabled-Provider set, and the effective skip list — and, per path,
whether it existed at capture. At Post, a ruleset-identity mismatch denies rather than diffing across a changed
membership. Created versus Removed is decided from recorded prior existence, which the governing rule makes
reliable.

## Internal classes (pure logic, not seams)
- Snapshot serializer: a versioned, length-framed format with an explicit "absent at capture" marker distinct
  from an empty file, plus per-file and total size ceilings whose breach returns `CaptureFailed`. A truncated or
  unparseable blob on read is treated as missing, which fails closed.
- Differ: takes the pre-image map and the current map, both already materialized, and returns
  `FileCreated`/`FileModified`/`FileRemoved`. It performs no IO, so it stays pure.
- Coverage checker: given the active grants and a path, answers covered or not, reusing `IPathMatcher.Matches`.

## Flow
- Capture at Pre: scan, read each file, record existence, serialize the map with the ruleset identity, and write
  it through `IContextWriter`. Any failure returns `CaptureFailed` (governing rule).
- Post: read the snapshot through `IContextReader`. `ContextMissing` denies. A ruleset-identity mismatch denies.
  Otherwise deserialize, scan and read the current tree, run the differ, and per change run Coverage then
  Conformance to emit a restore or delete effect.

## Degraded Post (round-3 finding 2/3, round-4 defect)
Blind-restore is wrong — it cannot delete a file the call created and it clobbers a legitimately granted edit —
so it is removed. If the Post scan or read cannot complete after a retry, the guard cannot safely diff. It does
not blind-restore and it does not silently pass: it returns `PostcheckResult(Verdict.Deny(reason), no effects)`,
and the Engine must map any Post-phase error or thrown exception to a deny, never to allow. The reason travels
out through `HostDecision.Message` into the host's channel — the agent transcript a human can inspect. There is
no separate alert type and none is added. The drift stays on disk in this case, which is acceptable because a
lazy AI does not induce filesystem errors; the outcome is a visible deny, not a silent pass.

## Storage and lifetime
- One snapshot per call, addressed by the call id in the Sealed, Engine-owned Context store. A call only writes
  and reads its own snapshot.
- `DeleteAsync(toolCall)` removes a call's snapshot at its own Post. `SweepExpiredAsync` removes only genuinely
  orphaned snapshots — calls whose Post never ran — by a generous age. The Pre-to-Post gap is bounded by the
  tool/hook timeout, not by session idle (the tool is running in that window), so a generous age never sweeps a
  live call. (Verify per host that Post fires within that bound.)

## Residual risk (round-3 finding 3, honest limit — not a defect)
Post protects only by restoring the pre-image; a deny verdict at Post does not undo a change that already landed.
So Post's protection is conditional on the pre-image being available. We keep it available — the store is Sealed,
we never sweep a live call's snapshot, and Pre fails closed rather than capture partially. A determined attacker
who defeats the Sealed store's best-effort shell protection could delete a pre-image and leave drift in place.
That residual is inside the lazy-AI threat we scoped to, and it is the nature of revert-after-the-fact, not a
fixable bug.

## Fixed
- The Context store stays Sealed and Engine-owned; guards get only the scoped write and read handles.
- Fail closed on every Pre error and on a missing, corrupt, or mismatched snapshot; alert on an un-diffable Post.
- No content-addressing, no dedup, no cross-call sharing, no garbage collection.
