# The kit's page can be narrated and pointed at

## Decisions

**member-identity.** The architecture language declares which property of a member carries its name, so narration can point at one member row rather than at every members table at once. Tim approved this as written on 2026-11-11, answering `member-identity` with `change-language` in pkg-681392e0d113; the record is `results/decisions.json` in `architecture-repair-the-executive-brief-1.0.3-local156`.

**selector-proof.** This repository takes one pinned, development-only DOM library so its own tests match a selector the way a browser does. Tim approved this as written on 2026-11-11, answering `selector-proof` with `add-dom-library` in the same package and record.

Both decisions were asked twice, because an earlier package carrying the same two questions stayed open when the brief was rebuilt to carry them. Tim answered that one too, `kit-narration-repair-two-language-and-te-1.0.0-local6cb` at 21:03, giving `change-language` and `add-dom-library` again. The two records agree.

**authorize-walkthrough-repair.** Tim approved the architecture walkthrough repair as a build on 2026-11-11, answering `approve-build`; the record is `results/decisions.json` in `architecture-walkthrough-repair-authoriz-1.0.0-local61c`. That plan assigns this repository: "Add the existing story helper to the kit renderer; add stable declaration and member identities; preserve section and column targets," with completion evidence "Deterministic render and selector tests, upstream PR/CI and kit release digest."

**dom-library.** The development-only DOM library this repository takes is jsdom 31.0.0. Adding it to `package.json` devDependencies and `bun.lock` at that exact version is what the answer authorizes, and nothing else. Tim approved this as written on 2026-11-12, answering `dom-library` with `jsdom-31` in pkg-eeea4f159ee9; the record is `results/decisions.json` in `choose-the-dom-library-1.0.2-localafe`.

**jsdom-types.** The jsdom import is typed by the published declaration package `@types/jsdom` 30.0.0. Adding it to `package.json` devDependencies and `bun.lock` at that exact version is what the answer authorizes, and nothing else; no declaration file for jsdom is written in this repository. Tim approved this as written on 2026-11-13, answering `jsdom-types` with `types-package` in pkg-6d9db6691d8c; the record is `results/decisions.json` in `unblock-the-kit-acceptance-tests-1.0.1-localea7`.

**green-criterion.** The test stage may record an acceptance criterion that passes today, with the reason it passes. A criterion that only guards behavior this contract requires to keep working is recorded in the coverage list as `red: false` carrying the reason in `whyPassing`; every other criterion is still RED, a `red: false` row with no reason is refused, and a `red: true` row that carries one is refused. Tim approved this as written on 2026-11-13, answering `green-criterion` with `record-it-green` in pkg-6d9db6691d8c; the record is `results/decisions.json` in `unblock-the-kit-acceptance-tests-1.0.1-localea7`. The same sentence states that requirement in four places, so the change lands in all of them: the condition the result contract is checked with, in the `required-agent-runtime` block copied into eleven workflow scripts; the coverage record's shape and the `tdd-author` contract, in the `stage-result-contracts` block copied into nine; the test author's, the reviewers' and the implementer's instructions; and the two rails that state the all-RED requirement in prose.

**rule-2-middle-clause.** Rule 2 keeps only its two exact bans: the strings `IGrantStore` and `IWindowsHelloNativeOps`, and any constant `data-declaration=` or `data-member=` attribute value. Its ban on any id declared in a document being drawn, and on any member name, is dropped. Tim approved this as written on 2026-11-12, answering `rule-2-middle-clause` with `drop-the-clause` in pkg-dd9cd1cb87e7; the record is `results/decisions.json` in `rule-2-s-middle-clause-1.0.0-local2d1`.

**Not decided, and not in this contract.** Refusing duplicate declaration ids at check time, filed as issue 115. Reporting a shape whose language declares no name role as an on-page error at render time, filed as issue 116. Narrating a proposal page, whose shell is a second copy at `ts/metalang/propose.ts:882`, filed as issue 117.

**impact-test.** The test that records rule 1's impact on existing code stops pinning what that impact was. It asserts the exemptions by the word and the reason, with no totals and no line numbers, so a tenth exemption or a changed reason still fails it while moving code does not. Tim approved this as written on 2026-11-13, answering `impact-test` with `pin-what-not-where` in pkg-fc1d5afd07c9; the record is `results/decisions.json` in `clear-the-last-kit-test-1.0.2-localfe2`. The answer authorizes one RULE-PHASE round touching that one test and nothing else, which is the authorization `rails-run-a-workflow` reserves to him: "Re-running any completed stage is the human's decision and needs their explicit yes."

**helper-absent.** A page the kit draws says so when JARVIS's story helper is not there. The page's own bundle reads the helper's announcement once the deferred scripts have run, and draws one plain line saying the page cannot be narrated here when it finds none. The kit readme gains a sentence saying a drawn page reaches out to its origin for that script. Tim approved this as written on 2026-11-13, answering `helper-absent` with `page-says-so` in pkg-fac2f7a24fe3. The alternative, writing the helper tag only into a page built to be served by JARVIS, was put to him and he chose against it. Until this answer the page drew, was never narratable away from JARVIS, and said nothing, which is the rule in `CLAUDE.md` that nothing may ever fail silently.

**when-the-page-says-so.** That behaviour is built into this work before the kit is released, rather than following it as its own issue. Tim approved this as written on 2026-11-13, answering `when-the-page-says-so` with `fold-it-in` in pkg-d785acd7ea59, and that answer is also his authorization for one test round writing the checks it needs, which `rails-run-a-workflow` reserves to him: "Re-running any completed stage is the human's decision and needs their explicit yes."

**member-identity-unique.** Every member row carries an identity that names one member and no other. A member name that occurs once among its declaration's members is that row's identity, exactly as now. A name that occurs more than once gives each of those rows an identity written as that name, a number sign, and the row's place among the members of that declaration carrying that same name, counting from one in the order the source declares them — `PrecheckAsync#1` and `PrecheckAsync#2` for two members both named `PrecheckAsync` — and no row carries the shared name on its own. The number sign can never collide with a name a document writes, because `ts/metalang/metalang.yaml:878` holds a name to letters, digits and underscores. The place counts among the rows sharing the name rather than among all of the declaration's members, so adding an unrelated member above them leaves both identities as they were — a name that identifies one member identifies it, and a name several members share identifies none of them alone. Tim ordered this on 2026-11-13, answering `overload-target` with none of the choices offered and the instruction to fix it correctly now, refusing the acceptance, the language ban, the issue it had been filed as, and any delay. A document that declares no repeated member name is unchanged by this, so every target written against the kit today keeps working.

**scan-ran-late.** The stages that ran before the hidden-decision scan existed run again with its result in hand: the rule round, the architecture stage, the test rounds and the implementation rounds. Tim approved this as written on 2026-11-13, answering `scan-ran-late` with `re-run-the-stages` in pkg-b0dad98888f2. The alternative, recording what happened and taking the work to its gate as it stood, was put to him and he chose against it.

**passing-row-reason.** Any acceptance check that passes today is recorded as passing, carrying the reason it passes, whatever makes it pass — a guard on behaviour that must keep working, a prior stage's finished work, or an implementation this contract has already built. Both bans in `green-criterion` stand: a passing row with no reason is refused, and a failing row that carries one is refused. Tim approved this as written on 2026-11-13, answering `passing-row-reason` with `say-why-it-passes` in pkg-e560286af329. It widens his `green-criterion` wording, which covered only the guard case and left a check whose code was already built with no true way to be recorded; the alternative, keeping that wording and having each round record only the checks it is for, was put to him and he chose against it.

**fix-round-authority.** A finding a reviewer confirms against a file lets the stage that owns that file run again to fix exactly that finding, with no new behaviour and no new check, and the reviewers judge the result as they judge every round. Tim approved this as written on 2026-11-13, answering `fix-round-authority` with `confirmed-finding-opens-it` in pkg-e560286af329. It also ratifies the round that gave the two page-reading cases a stated time budget, which ran before this answer and without an authorization. The alternative, each re-entry coming to him as its own yes, was put to him and he chose against it.

**identity-spelling.** The number in a repeated member's identity counts only the rows carrying that same name, starting at one, so two members of one declaration both named `PrecheckAsync` are `PrecheckAsync#1` and `PrecheckAsync#2` however many other members the declaration holds, and adding an unrelated member above them changes neither. Tim approved this as written on 2026-11-13, answering `identity-spelling` with `among-the-shared` in pkg-f1d7e85abb9f. The alternative, numbering by the row's place among all of the declaration's members, was put to him and he chose against it, because adding any member above a repeated pair would then change both identities and break every beat written against the old numbers. A member whose name is not repeated keeps its plain name, unchanged.

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
- `ts/metalang/client.tsx` — the bundle's entry, the only thing in the bundle that touches the DOM, which is where the helper's announcement is read.
- `ts/metalang/arch.yaml` — `member.name`.
- `ts/metalang/metalang.yaml` — the `name` role's recorded meaning.
- `ts/metalang/kit.ts` and `ts/metalang/kit-entry.ts` — what a built kit carries.
- `.github/workflows/ci.yml` — the kit asset's required-file check.
- `ts/metalang/render.test.ts`, `ts/metalang/app.test.tsx`, `ts/metalang/kit.test.ts` — the tests for all of the above.
- `ts/metalang/guardrails.test.ts` — the three rules in `Rules to add` with a violating and a compliant fixture each, and the test decision `impact-test` rewrote.
- `package.json` and `bun.lock` — jsdom 31.0.0 and `@types/jsdom` 30.0.0 as development dependencies.
- `bunfig.toml` — the 0.75 coverage floor the new code is held to.
- `.agents/workflows/*.js` — the `required-agent-runtime` copied block in all eleven scripts, the `stage-result-contracts` copied block in nine, and the test author's, reviewers' and implementer's instructions in `tdd.js` and `implement.js`.
- `.agents/skills/rails-test-code/SKILL.md` and `.agents/skills/rails-run-a-workflow/SKILL.md` — the all-RED requirement in prose.

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
5. Take jsdom 31.0.0 and `@types/jsdom` 30.0.0 as development dependencies in `package.json` and `bun.lock`, each pinned to that exact version, and prove in jsdom that a member name containing a double quote, a single quote and a backslash is reachable by selector, that a member name repeated across two declarations resolves to the one asked for, and that two declarations sharing an id are reported as ambiguous rather than resolved to the first. Assertions run against the component render or the executed bundle, never against the CLI output, because `#root` in the written page is empty and the DOM is drawn in the browser.
6. Add `style.css` and `syntax.css` to the required-file check in `.github/workflows/ci.yml`, which lists seven files while `ts/metalang/kit.ts` marks nine as required.
7. Keep every `data-part` and `data-col` target working, spelled exactly as now.
9. Under decision `helper-absent`, have the page say so when the helper is not there. The helper sets `window.__jarvisStory` as it loads; the page's bundle is written inline and runs before any deferred script, so the reading happens once the deferred scripts have run rather than at mount. When there is no announcement, the page draws one plain line saying it cannot be narrated here, in the page's own styles, and nothing else changes. Add one sentence to the kit readme in `ts/metalang/kit.ts` saying a drawn page reaches out to its origin for that script.

8. Under decision `green-criterion`, let the coverage record carry a criterion that passes today with its reason, in the four places named in that decision. This one is already done, by the orchestrator, because the test stage cannot record its own result until the contract it is checked against exists. The IMPLEMENT worker does not repeat it and changes nothing under `.agents/`.

## What the agent MAY do

Add test fixtures that declare shapes carrying punctuation, quotes and repeated member names. Extend the existing kit page test. Extract the duplicated id predicate to one owner.

## What the agent MUST NOT do

Read a language property by its spelling anywhere in the drawing path. Write `IGrantStore` or `IWindowsHelloNativeOps`, or a constant value of either identity attribute, into non-test source. Write either identity attribute outside its one owning component. Add a second resolver, escaper, diagnostic or page shell. Weaken, skip or delete a test, or lower the coverage floor. Loosen a target assertion because the CLI output has an empty root. Write the two named literal targets into a repository fixture to make the acceptance pass. Edit anything under `.agents/`, which `What to do` item 8 has already settled. Change scope. Commit. Stop and report on any wall.

## Acceptance

Each check is re-derivable by someone other than the worker.

1. `make build && make test` green, and `bun test` green, from the repository root. Both are named because `make test` is `dotnet test` alone and runs none of the kit's tests, which is filed as issue 118.
2. A selector naming one declaration's members matches exactly one element, asserted against the component render or the executed bundle. Command: `bun test ts/metalang/app.test.tsx`.
3. A selector naming one member of one declaration matches exactly one element, same command.
4. A member name containing a double quote, a single quote and a backslash is reachable by selector in jsdom. Same command.
5. A member name repeated across two declarations resolves to the one asked for, and two declarations sharing an id are reported as ambiguous rather than resolved to the first. Same command.
6. Every `data-part` and `data-col` target that passes today still passes, by the existing assertions running unchanged.
7. The same document rendered twice under one built kit produces identical bytes per file. Command: `bun test ts/metalang/kit.test.ts`.
8. Each of the three rules has one violating fixture that produces its diagnostic and one compliant fixture that does not.
9. CI refuses a kit asset missing `style.css` or `syntax.css`, proven by a run with the file removed.
10. The two literal targets, `IGrantStore`'s members and `IWindowsHelloNativeOps`'s `BeginVerification`, are verified on the JARVIS side against the re-cut revision after the kit release, not in this repository: no document here declares either id, and writing them into a fixture is what rule 2 bans.
11. Coverage stays at or above the 0.75 floor `bunfig.toml` sets, which `bun test` enforces itself through `coverage = true` and `coverageThreshold = 0.75`. Command: `bun test`. The earlier command named here, `eng/coverage-gate.sh`, is the .NET gate and reads none of the kit's coverage.
12. The rendered page loads the story helper as `<script defer src="/api/packages/story.js"></script>`, after the data block and before the bundle, and a page built from a released kit loads it too. Commands: `bun test ts/metalang/render.test.ts` and `bun test ts/metalang/kit.test.ts`. The standard and `What to do` item 4 both require the helper, and no numbered check named it.
13. `bun run typecheck` and `bun run lint` are clean, with `@types/jsdom` 30.0.0 the only typing added for jsdom and no declaration file for it in this repository. Commands: `bun run typecheck` and `bun run lint`. CI runs both, at `.github/workflows/ci.yml:437` and `:439`, and the untyped import failed each one before decision `jsdom-types`.
14. The coverage record takes a criterion that passes today only with its reason: a `red: false` row carrying a reason is accepted, the same row with an empty reason is refused, and a `red: true` row carrying a reason is refused. Re-derivable by anyone from the repository root, by slicing the three copied blocks out of `.agents/workflows/tdd.js` and validating four rows through the real result contract, which prints `[true,true,false,false]` and exits 0:
   `node --input-type=module -e 'import fs from "fs";const s=fs.readFileSync(".agents/workflows/tdd.js","utf8");const b=(n)=>s.split("// ##COPIED-MODULE-BEGIN## "+n+"\n")[1].split("// ##COPIED-MODULE-END## "+n)[0];const F=Object.getPrototypeOf(async function(){}).constructor;const run=new F("rows",b("workflow-input")+b("required-agent-runtime")+b("stage-result-contracts")+"const out=[];for(const r of rows){try{await __stageResultContracts({name:\"tdd-author\",operation:\"validate\",result:{testFiles:[\"t\"],redProof:\"p\",coverage:[r],notes:\"n\"},context:{noTestEvidence:\"x\"}});out.push(true)}catch(e){out.push(false)}}return out;");const row=(red,why)=>({acceptanceItem:"6",tests:["t"],kind:"unit",red,whyPassing:why});const got=await run([row(true,""),row(false,"guards what works today"),row(false,""),row(true,"passes today")]);console.log(JSON.stringify(got));process.exit(JSON.stringify(got)==="[true,true,false,false]"?0:1)'`
15. Every copy of a changed block stays byte-identical. Command: `node eng/check-copied-modules.mjs`.
16. A page drawn where the helper never announces itself shows exactly one plain line saying it cannot be narrated here, and a page where the helper announced itself shows none. Both cases, named in the output. Command: `bun test`.
17. The kit readme says a drawn page reaches out to its origin for the narration script. Command: `bun test ts/metalang/kit.test.ts`.
18. A declaration with two members both named `PrecheckAsync` draws one row with `data-member="PrecheckAsync#1"` and one with `data-member="PrecheckAsync#2"`, each selector matching exactly one element, while a selector naming `PrecheckAsync` alone matches none. A declaration with no repeated name is drawn exactly as before. All three cases named in the output. Command: `bun test ts/metalang/app.test.tsx`.

## Level

L1, this repository's default, and no other Level was approved for this run.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path.
