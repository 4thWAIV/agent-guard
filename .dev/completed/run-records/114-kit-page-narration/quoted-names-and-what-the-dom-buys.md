# A quoted member name cannot be in a checked arch document, and the DOM library is still needed

Written while RULE-PHASE runs, so the contract is not edited underneath a running panel. Folded into the contract's Acceptance section when that panel returns.

## The fact

Diagnostics reported that `arch`'s member name constraint refuses quotes and backslashes. Checked against the live checker rather than taken on trust. A probe document declaring one member named `PlainName` and one named `Get"Odd\Name`, checked with `bun run ts/metalang/cli.ts check`:

> 1 problem:
>   declare[1].constant.value.members[1].name  "Get"Odd\Name" does not match [A-Za-z_][A-Za-z0-9_]*, which "name" requires of every value of it

One problem, and that is it. `member.name` is `is: name` at `ts/metalang/arch.yaml:519-521`, and the `name` type at `ts/metalang/metalang.yaml:872` carries `pattern: '[A-Za-z_][A-Za-z0-9_]*'`.

So no checked `arch` document can hold a member name with a quote, a backslash, a space or a hyphen.

## What that corrects

Tim was told, when `selector-proof` was put to him, that without the DOM library "the tests cannot prove a name containing a quote is reachable at all". That sentence is true and it was the wrong reason to lead with, because it implies the quoted case arises in the documents this work is for. It does not.

He approved the library. The approval stands and is not re-asked; this records the better reason.

## The reason that holds

Without a DOM in this repository the tests cannot assert what a selector selects at all — not a quoted name, not a plain one, not an ambiguous id.

`pageHtml` in `ts/metalang/render.ts` writes `<main id="root"></main>` empty; the whole page is built in the browser by the bundle. A test aimed at the written page's text finds no declaration card and no member row. There is no browser or DOM harness anywhere in this repository: fourteen test files under `ts/metalang`, all `bun test`, and nothing in `package.json` or `ts/` matching playwright, puppeteer, chromium, happy-dom or jsdom. Bun's own `HTMLRewriter` was measured on bun 1.2.14 in this worktree and cannot stand in: `getAttribute` returns raw entity text rather than the value, and a selector carrying an escaped value matched nothing where a browser matches it.

So the library is what lets this repository's gate assert selector behaviour. The quoted case is one of the things it then proves, and the smaller part of why it is needed.

## Where the quoted case does arise

1. **Drafts.** The renderer draws un-checked documents deliberately — `ts/metalang/render.ts` states that a document it cannot fully draw still builds and the part it cannot draw appears on the page as an error. A draft carrying a quoted member name renders, and a narration can be pointed at that page. Diagnostics' fixture `tests/fixtures/architecture/quoted-members.yaml` is such a draft, and this repository's own tests use it the same way.
2. **A language whose name property is not identifier-typed.** The attribute is written from whatever property carries the `name` role, in whatever language. `string` at `metalang.yaml:860` is a primitive with no pattern, and this repository's own tests already declare a name property typed `string`: the `equivalent` entity at `ts/metalang/app.test.tsx:1074-1078`. A language declaring `role: name` on a string-typed property admits a quoted name in a document that checks clean.

## The two acceptance checks this changes

Check 4, the quoted name, is proven on a draft the renderer draws and on a test language that permits one, and the contract says plainly that `arch` itself forbids it. It is not proven on a checked `arch` document, because none can exist.

Check 5's ambiguous case is unchanged and now matches the helper: Diagnostics reports a selector matching several rows as `story.ambiguous`, separate from `story.missing`, and neither is ever widened to a broader match.
