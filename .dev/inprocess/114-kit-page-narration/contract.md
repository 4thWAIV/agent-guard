# DRAFT — The kit's page can be narrated and pointed at

Draft for the GROUND hidden-decision scan. Nothing here is decided and nothing is approved.

## Decisions

Recorded so far: Tim approved the architecture walkthrough repair as a build, `authorize-walkthrough-repair: approve-build`, 2026-11-11T16:56:46-06:00. The approved plan assigns this repository "Add the existing story helper to the kit renderer; add stable declaration and member identities; preserve section and column targets," with completion evidence "Deterministic render and selector tests, upstream PR/CI and kit release digest."

No other decision is recorded. The selector spelling, how a member's identity is derived, and what the renderer does with a target that matches nothing are open.

## Rules to add

To be determined at DESIGN.

## The standard

The page a document renders to can be narrated by JARVIS and pointed at precisely.

The page loads JARVIS's story helper so a beat can ring and point inside the package frame. Each declaration the page draws carries its own identity, and each member row carries its own, so a selector reaches one declaration's members rather than every members table at once. The section and column targets already on the page keep working, spelled exactly as they are now.

Identical source rendered twice under one pinned kit produces identical bytes.

## Success definition

Tim's standing definition: all criteria met and no errors in the system as a result of the change.

This run: `[data-declaration="IGrantStore"] [data-part="members"]` matches exactly one element on the page rendered from the silent-failure cleanup surface, `[data-declaration="IWindowsHelloNativeOps"] [data-member="BeginVerification"]` matches exactly one, every `data-part` and `data-col` target that works today still works, and a kit built from this branch renders that document to the same bytes twice.

## Surfaces

- `ts/metalang/render.ts` — `pageHtml`, which writes the page shell.
- `ts/metalang/app.tsx` — the `section.d-decl` that draws a declaration, and the `Table` that draws members as rows.
- `ts/metalang/render.test.ts`, `ts/metalang/app.test.tsx` — the tests for both.
- `ts/metalang/kit.ts`, `ts/metalang/kit-entry.ts` — what a built kit carries.

The list is where the work is known to be, not a boundary.

## Reuse ledger

To be determined at GROUND. One capability to rule on: finding the property that names a member of a shape, generically, from the rulebook rather than by assuming a column called `name`.

## What to do

To be determined at DESIGN and CONTRACT.

## What the agent MUST NOT do

To be determined.

## Acceptance

To be derived from the requirement sentences at CONTRACT, carrying the approved plan's renderer gate: repeated identical renders compared byte for byte; the intended declaration and member selected and nothing else, including when a member name repeats across declarations and when an id carries punctuation or quotes; a target that matches nothing reported rather than widened to a broad substitute; helper initialisation and its diagnostics.

## Level

L1. No Level was approved for this run, and L1 is this repository's default.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path.
