# Both proposed shortcuts read the code wrong

Planning holds the joint sequence and directs the smallest existing-package path: use the existing arch surface, no exact-signature schema for this acceptance, no coverage rule, approval shown distinctly from Got it. All of that is right and the acceptance case needs none of my two pending choices to be answered one particular way.

Two specific technical shortcuts were proposed to remove those choices. Neither holds.

## Finding a member's name by its declared type does not work

Diagnostics proposed: "Finding the member property whose declared type is MetaLang's name reuses existing type information and changes no language file."

`ts/metalang/arch.yaml:517` declares `member` with the properties `name` (`is: name`), `kind` (`is: kinds`, `role: kind`), `reach` (`is: reaches`), `type` (`is: builtins`) and `does` (`is: string`, `role: prose`). Only `name` has no role.

The type `name` is declared at `ts/metalang/metalang.yaml:872`:

> id: name
> is: string
> title: The type an id has.
> description: An id is a name, never any text.
> constraints:
>   - pattern: { value: '[A-Za-z_][A-Za-z0-9_]*' }

That is a string subtype constraining a value to an identifier. It is not a statement that the property holding it is the thing's name. Any identifier-valued property is typed `name`, and the renderer finding "the one property typed `name`" works today only because `member` happens to have exactly one. Nothing holds that true, and the day a second identifier-valued property is added to `member` the lookup becomes ambiguous with no diagnostic.

The role `name` at `ts/metalang/metalang.yaml:1011` is the designated mechanism:

> id: name
> value: name
> title: It holds the name this thing is shown by.

`propertyCarrying(bodyOf, owner, role)` at `ts/metalang/words.ts:422` is the one owner of which property of a type carries which role. Reading a value constraint instead duplicates that knowledge in the renderer, which is the same defect as hardcoding the string `'name'`.

## The role change is not bounded by the approved selector scope

Planning proposed: "The required member-name role metadata implements the approved individual-member selectors; keep it bounded."

The role as written does not fit a member. Its description at `ts/metalang/metalang.yaml:1013`: "A value naming this thing is drawn with that name beside the id where the two differ, because the id is what it is written as and the name is what it is called." A member has no id — `arch.yaml:519` says so: "Its name is written here, because a member is not declared and has no id." That is why `member.name` carries no role.

So putting `role: name` on `member.name` also changes what the `name` role means in `metalang.yaml`, the shared vocabulary every language built on it reads, from "the name shown beside an id" to "the name a thing is known by, with or without an id". That is a change to the agreed design of the base language, not metadata inside the approved selector scope.

## The quoted-name proof moves repositories rather than disappearing

Planning and Diagnostics propose proving the quoted-name selector in JARVIS's existing browser harness instead of adding a DOM library. JARVIS has Playwright (`pyproject.toml:18`) and page tests that drive it, so that part is right and it covers a real browser, which a DOM library never would.

It does not settle the kit's own gate. `ts/metalang/` has fourteen test files, all `bun test`, and the repository has no browser or DOM harness of any kind: nothing in `package.json` or `ts/` matches playwright, puppeteer, chromium, happy-dom or jsdom. So agent-guard would ship `data-member` with its own tests unable to match a selector the way a browser does, and the behaviour proven only on another repository's gate.

`CLAUDE.md` rule 1: "Where a thing cannot be checked, report it as a blocker or an error; never report it as checked." Reporting it is this file.

So the choice is unchanged in substance and sharper in form: JARVIS proves it end to end either way, and the question is whether agent-guard's own gate proves it too, which needs one development-only DOM library in this repository.

## What is actually left

One decision changes the architecture and is Tim's: `member-identity`, because it changes what the `name` role means in `metalang.yaml`.

One decision adds a dependency and is Tim's: `selector-proof`, restated as above.

Both are live in front of him in pkg-a2538de9eb8f. Issue 114's contract covers the whole kit lane and reads differently depending on the first answer, so it is not written before he answers, and splitting it to start the unblocked half would be inventing a stage. `rails-run-a-workflow`: "No panel launches while work depends on an unrecorded decision."
