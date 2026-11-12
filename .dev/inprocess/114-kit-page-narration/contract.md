# The kit's page can be narrated and pointed at

## Decisions

**member-identity.** The architecture language declares which property of a member carries its name, so narration can point at one member row rather than at every members table at once. Tim approved this as written on 2026-11-11, answering `member-identity` with `change-language` in pkg-681392e0d113; the record is `results/decisions.json` in `architecture-repair-the-executive-brief-1.0.3-local156`.

**selector-proof.** This repository takes one pinned, development-only DOM library so its own tests match a selector the way a browser does. Tim approved this as written on 2026-11-11, answering `selector-proof` with `add-dom-library` in the same package and record.

Both decisions were asked twice, because an earlier package carrying the same two questions stayed open when the brief was rebuilt to carry them. Tim answered that one too, `kit-narration-repair-two-language-and-te-1.0.0-local6cb` at 21:03, giving `change-language` and `add-dom-library` again. The two records agree.

**authorize-walkthrough-repair.** Tim approved the architecture walkthrough repair as a build on 2026-11-11, answering `approve-build`; the record is `results/decisions.json` in `architecture-walkthrough-repair-authoriz-1.0.0-local61c`. That plan assigns this repository: "Add the existing story helper to the kit renderer; add stable declaration and member identities; preserve section and column targets," with completion evidence "Deterministic render and selector tests, upstream PR/CI and kit release digest."

**rule-2-middle-clause.** Rule 2 keeps only its two exact bans: the strings `IGrantStore` and `IWindowsHelloNativeOps`, and any constant `data-declaration=` or `data-member=` attribute value. Its ban on any id declared in a document being drawn, and on any member name, is dropped. Tim approved this as written on 2026-11-12, answering `rule-2-middle-clause` with `drop-the-clause` in pkg-dd9cd1cb87e7; the record is `results/decisions.json` in `rule-2-s-middle-clause-1.0.0-local2d1`.

**Not decided, and not in this contract.** Refusing duplicate declaration ids at check time, filed as issue 115. Reporting a shape whose language declares no name role as an on-page error at render time, filed as issue 116. Narrating a proposal page, whose shell is a second copy at `ts/metalang/propose.ts:882`, filed as issue 117.

## Rules to add

Three rules, each a contract-directed test inside the existing `bun test` step, following `ts/metalang/examples.test.ts`. Tim approved all three as written on 2026-11-11, and told this session to proceed with the work.

None of the three is an analyzer and none carries a diagnostic id, so no diagnostic is approved to remain a warning in any intermediate build.

1. **No language word by spelling in the drawing path.** In `ts/metalang/app.tsx` and `ts/metalang/client.tsx`, a word of a drawn language may not be written as a literal where it is used to look something up in the document being drawn or in its rulebook, and may not be written as a local constant renaming a word `ts/metalang/words.ts` already names. The only legal route to a language word in those two uses is a named constant in `words.ts` or a rulebook lookup. This stops a member's identity being taken from the spelling of a column — `member['name']`, `columns.includes('name')`, `=== 'name'` — instead of asking the rulebook.

   **Where it stops.** Tim set this boundary as written on 2026-11-11, answering `rule-1-boundary` with `lookups-and-renames` in pkg-a37618205163. Outside the rule: a member of the page's own union of finding kinds, English prose shown to the reader, and a comparison against a JavaScript type. Enforced on every word `words.ts` names, the rule flags twenty-four literals in `app.tsx`; fifteen are the defect and nine are these three kinds. The fifteen are the rule's impact on existing code and IMPLEMENT clears them.

2. **No literal target in non-test source.** The strings `IGrantStore` and `IWindowsHelloNativeOps`, and any constant `data-declaration=` or `data-member=` attribute value, are banned anywhere in non-test `ts/` source. This stops the two named targets being special-cased so the acceptance passes while the general mechanism is absent.

   **Where it stops.** The rule holds those two bans and nothing wider. Tim dropped the third ban, on any id declared in a document being drawn and on any member name, on 2026-11-12, answering `rule-2-middle-clause` with `drop-the-clause`. On 2026-11-11 he had bounded that dropped ban to documents being drawn, answering `rule-2-boundary` with `drawn-documents` in pkg-a37618205163; the later answer removes the ban that boundary applied to. The dropped ban could not separate an id of a drawn document from an ordinary word of the code, because the six documents the renderer draws declare eighty-seven distinct ids, among them `string`, `object`, `name`, `value`, `language` and `part`. Rule 1 governs how the drawing code may reach a language word.

3. **One owning site per identity attribute.** In `ts/metalang`, the JSX attribute `data-declaration` may be written only in the component that draws a declaration, and `data-member` only in the component that draws member rows. This stops a second hand-written copy with a different value derivation, which would make the same selector mean two things depending on which component drew the node.

## The standard

The page a document renders to can be narrated by JARVIS and pointed at precisely.

The page loads JARVIS's story helper so a beat can ring and point inside the package frame. Each declaration the page draws carries its own identity, and each member row carries its own, so a selector reaches one declaration's members rather than every members table at once. The `data-part` and `data-col` targets already on the page keep working, spelled exactly as they are now.

A member's identity comes from the language: `ts/metalang/arch.yaml` declares `role: name` on `member.name`, and `ts/metalang/metalang.yaml` states what that role means for a thing that has a name and no id. The renderer asks `propertyCarrying` for it and never reads a property by spelling.

Identical source rendered twice under one pinned kit produces identical bytes.

A kit released from this branch carries every file `ts/metalang/kit.ts` marks as required, and CI refuses one that does not.

## Success definition

Tim's standing definition: all criteria met and no errors in the system as a result of the change.

This run: a selector naming one declaration's members matches exactly one element on the rendered page; a selector naming one member of one declaration matches exactly one element; a member name containing a double quote, a single quote or a backslash is reachable by selector in a real DOM; a member name repeated across two declarations resolves to the one asked for; two declarations sharing an id are reported as ambiguous rather than resolved to the first; every `data-part` and `data-col` target that works today still works; a kit built from this branch renders the same document to the same bytes twice; and CI refuses a kit asset missing any required file.

## Surfaces

- `ts/metalang/render.ts` — `pageHtml`, which writes the page shell, and nothing else writes one for a document.
- `ts/metalang/app.tsx` — the component that draws a declaration, and the `Table` that draws members as rows.
- `ts/metalang/arch.yaml` — `member.name`.
- `ts/metalang/metalang.yaml` — the `name` role's recorded meaning.
- `ts/metalang/kit.ts` and `ts/metalang/kit-entry.ts` — what a built kit carries.
- `.github/workflows/ci.yml` — the kit asset's required-file check.
- `ts/metalang/render.test.ts`, `ts/metalang/app.test.tsx`, `ts/metalang/kit.test.ts` — the tests for all of the above.
- `package.json` and `bun.lock` — the development-only DOM library.
- `bunfig.toml` — the 0.75 coverage floor the new code is held to.

The list is where the work is known to be, not a boundary.

## Reuse ledger

- **A member row's identity** — reuse `propertyCarrying(bodyOf, of, ROLES.name)` at `ts/metalang/words.ts:422`, called once per table beside the existing `columns` computation. `words.ts:395-404` states it is the one place the question of which property carries which role is answered. Write no second resolver, no fallback chain and no type-derivation route beside it.
- **The zero-carrier and two-carrier behaviour** — reuse what that owner already documents at `words.ts:425-428`: one carrier gives the attribute, zero or two give no attribute and no new diagnostic. The two-carrier report already exists at `app.tsx:1087-1093` and as the `distinct: { in: properties, by: role }` constraint at `metalang.yaml:1163`. Add no second report.
- **Attribute escaping** — reuse React's, by writing both attributes as JSX props exactly as `data-col` and `data-part` already are at `app.tsx:736`, `:757` and `:963`. `render.ts:39` `escapeHtml` replaces only `&` and `<`, is title-only, and is never reached for an attribute; do not extend it and do not add a second escaper.
- **The id predicate** — extract the one already written twice, at `app.tsx:1480` and `app.tsx:1500`, to a single named constant and feed the id span, the error band and the new `data-declaration` from it.
- **The built-kit page proof** — extend the existing test at `kit.test.ts:193-207`, which builds a kit, copies a document away from the repository and runs `node kit.js render` on it. Write no new harness.
- **The release and rule mechanisms** — reuse the `ci.yml` release job for the kit asset, and write every new guardrail as a contract-directed test inside the existing `bun test` step.

The ledger's earlier reuse verdict cited `arch.yaml:211`, which is the `word` entity's name property, not a member's. `member.name` carries no role today, so `propertyCarrying` returns nothing for `member` until `member-identity` is implemented. Reuse is not satisfied by calling the owner alone.

## What to do

1. Add `role: name` to `member.name` in `ts/metalang/arch.yaml`, and change the `name` role's recorded meaning in `ts/metalang/metalang.yaml` so it covers a thing that has a name and no id. The role today reads that a name is drawn beside the id where the two differ; a member has no id, which `arch.yaml` states where it declares `member`.
2. In the component that draws a declaration, write `data-declaration` from the declaration's own id, through the single extracted id predicate.
3. In `Table`, resolve the member's name property once per table through `propertyCarrying` and write `data-member` on each row from the value that property holds. A table whose shape has no carrier, or two, gets no `data-member` and no new diagnostic.
4. Load JARVIS's story helper from `pageHtml` in `ts/metalang/render.ts`, as `<script defer src="/api/packages/story.js"></script>`, after the data block and before the bundle. The helper has no order dependency on either: at load it only adds listeners and announces itself, it creates its ring layer on the first cue, and it resolves a beat's targets when that beat arrives, rechecking once after 700 milliseconds and reporting what it still cannot see. The position is forced from this side rather than by the helper: `ts/metalang/render.test.ts:96` and `:146` slice the page from the first `<script>` to the last `</script>` and assert nothing closes a script element inside, so placing the helper last lands that search on its own closing tag.
5. Take the pinned, development-only DOM library in `package.json` and `bun.lock`, pinned to an exact version, and prove in it that a member name containing a double quote, a single quote and a backslash is reachable by selector, that a member name repeated across two declarations resolves to the one asked for, and that two declarations sharing an id are reported as ambiguous rather than resolved to the first. Assertions run against the component render or the executed bundle, never against the CLI output, because `#root` in the written page is empty and the DOM is drawn in the browser.
6. Add `style.css` and `syntax.css` to the required-file check in `.github/workflows/ci.yml`, which lists seven files while `ts/metalang/kit.ts` marks nine as required.
7. Keep every `data-part` and `data-col` target working, spelled exactly as now.

## What the agent MAY do

Add test fixtures that declare shapes carrying punctuation, quotes and repeated member names. Extend the existing kit page test. Extract the duplicated id predicate to one owner.

## What the agent MUST NOT do

Read a language property by its spelling anywhere in the drawing path. Write `IGrantStore` or `IWindowsHelloNativeOps`, or a constant value of either identity attribute, into non-test source. Write either identity attribute outside its one owning component. Add a second resolver, escaper, diagnostic or page shell. Weaken, skip or delete a test, or lower the coverage floor. Loosen a target assertion because the CLI output has an empty root. Write the two named literal targets into a repository fixture to make the acceptance pass. Change scope. Commit. Stop and report on any wall.

## Acceptance

Each check is re-derivable by someone other than the worker.

1. `make build && make test` green, and `bun test` green, from the repository root. Both are named because `make test` is `dotnet test` alone and runs none of the kit's tests, which is filed as issue 118.
2. A selector naming one declaration's members matches exactly one element, asserted against the component render or the executed bundle. Command: `bun test ts/metalang/app.test.tsx`.
3. A selector naming one member of one declaration matches exactly one element, same command.
4. A member name containing a double quote, a single quote and a backslash is reachable by selector in the DOM library. Same command.
5. A member name repeated across two declarations resolves to the one asked for, and two declarations sharing an id are reported as ambiguous rather than resolved to the first. Same command.
6. Every `data-part` and `data-col` target that passes today still passes, by the existing assertions running unchanged.
7. The same document rendered twice under one built kit produces identical bytes per file. Command: `bun test ts/metalang/kit.test.ts`.
8. Each of the three rules has one violating fixture that produces its diagnostic and one compliant fixture that does not.
9. CI refuses a kit asset missing `style.css` or `syntax.css`, proven by a run with the file removed.
10. The two literal targets, `IGrantStore`'s members and `IWindowsHelloNativeOps`'s `BeginVerification`, are verified on the JARVIS side against the re-cut revision after the kit release, not in this repository: no document here declares either id, and writing them into a fixture is what rule 2 bans.
11. Coverage stays at or above the 0.75 floor in `bunfig.toml`. Command: `eng/coverage-gate.sh`.

## Level

L1, this repository's default, and no other Level was approved for this run.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path.
