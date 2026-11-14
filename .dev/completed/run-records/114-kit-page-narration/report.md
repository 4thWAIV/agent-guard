# Issue 114: the kit's page can be narrated and pointed at

The kit's rendered page can now be walked through by JARVIS: a beat can ring one declaration and one member of it, and a page drawn where JARVIS is not says so rather than looking narratable and doing nothing.

## What shipped

Each declaration card carries `data-declaration` written from the declaration's own id, and each member row carries `data-member` written from the value the language says holds a member's name — `role: name` on `member.name` in `arch.yaml`, with the `name` role in `metalang.yaml` widened to cover a thing that has a name and no id. Where two members of one declaration share a name, each row takes that name, a number sign and its place among the rows sharing it, counting from one, and the bare shared name stands on no row. The page shell loads JARVIS's story helper, and the page's own bundle reads the helper's announcement once the deferred scripts have run, drawing one plain line when nothing answered. The kit readme says a drawn page asks its origin for that script. CI's required-file check lists `style.css` and `syntax.css`, which the kit has always marked required.

Three rules hold it: no language word reached by spelling in the drawing path, no literal target in non-test source, and one owning site per identity attribute.

## What Tim decided

`member-identity` and `selector-proof` on 11 November; `rule-1-boundary`, `rule-2-boundary` and the three rules the same day; `rule-2-middle-clause` and `dom-library` on 12 November. Then on 13 November: `jsdom-types`, `green-criterion`, `impact-test`, `helper-absent`, `when-the-page-says-so`, `passing-row-reason`, `fix-round-authority`, `member-identity-unique`, `scan-ran-late` and `identity-spelling`. Every one is recorded in the contract in the wording he approved, with the package it came from.

## What the reviewers refused along the way

Four rounds were refused and fixed rather than argued with. A test that could not fail, because the harness announced the narrator before the page could look. A coverage record that called criteria failing when they passed. An identity spelling chosen by an agent rather than by Tim. And a test that passed on a quiet machine and failed on a busy one, which a reviewer reproduced under load and a round then made deterministic.

Two findings were against the orchestrator rather than the work: a test round run before Tim authorized it, which he ratified by answering `fix-round-authority`; and a readiness finding carried into a package handoff without being checked, which the package author caught and refused — the behaviour it claimed was missing had been in the checker since October.

## The state at the gate

`make build && make test` exits 0 with 834 tests passed. `bun test` is 884 passed and 0 failed, coverage 96.5% of lines against the 0.75 floor. `bun run typecheck` and `bun run lint` are clean. `node eng/check-copied-modules.mjs` passes with all 41 copied blocks identical. All eighteen acceptance checks are met, each with its own evidence, and the five independent reviewers returned no finding.

## What is not here, and where it is

Acceptance check 10's two literal targets are verified on the JARVIS side against the re-cut document after the kit release, which the contract says and rule 2 enforces by banning those names from this repository.

Issues 115, 116, 117 and 118 remain open and are named in the contract. Issue 119 holds the architecture language carrying exact signatures; issue 121 holds publishing a build-produced digest beside the released kit.
