# Build the member identity Tim approved

Two tests are failing and nothing else is. Build exactly what makes them pass.

## What is not in

Decision `member-identity-unique` with the spelling in decision `identity-spelling`, both in the contract. A member name that occurs once among its declaration's members stays that row's identity exactly as today. A name two or more members share gives each of those rows that name, a number sign, and its place among the rows carrying that same name counting from one, and no row carries the bare shared name.

Tim approved the spelling on 2026-11-13: the number counts among the rows sharing the name, not among all of the declaration's members, so adding an unrelated member above a repeated pair changes neither identity.

## The two reds to clear

`ts/metalang/app.test.tsx` — `two members of one declaration carrying one name`: each of the two is reached by its place among the members sharing the name, and the shared name on its own reaches no row. The third case in that group passes today and must keep passing: a declaration naming each of its members once is drawn as it was, with no number sign on any row.

## Where it lives

`ts/metalang/app.tsx:747-760`, the `identityOf` helper inside the component that draws member rows. The architecture round recorded that this changes no declared surface: `identityOf` is module-local, is not exported, and the full member list it needs is already in scope, so nothing about the component's own shape moves.

## Proving it

`bun test` from the repository root: 884 pass, 0 fail. Also `make build && make test`, `bun run typecheck` and `bun run lint`, all clean. The whole-suite run is the proof because a single-file run cannot reach the coverage floor.

No test, rule or fixture changes. Nothing under `.agents/` changes.
