# Which DOM library, and what each one does with the hard name

The contract orders a pinned, development-only DOM library and does not name one. Tim approved the category on 2026-11-11, answering `selector-proof` with `add-dom-library`: "This repository takes one pinned, development-only DOM library so its own tests match a selector the way a browser does." No record names the library or the version, and `rails-decisions` puts a dependency on Tim, so the choice goes to him before the tests are written.

Nothing is installed today. `package.json` devDependencies hold `@eslint/js`, `@types/bun`, `@types/js-yaml`, `@types/react`, `@types/react-dom`, `eslint`, `typescript` and `typescript-eslint`. The live tests render with `renderToStaticMarkup` from `react-dom/server` into a string, at `ts/metalang/app.test.tsx:19` and `:22`, so nothing in this repository can say what a selector selects.

## The case each candidate was put to

A member name holding one double quote, one apostrophe and one backslash — `a"b'c\d` — written as `data-member`, then looked for by a selector built from that name, once through the library's own `CSS.escape` and once through a hand-quoted selector. Then a member name repeated in two declarations, looked for inside one of them. Then the value read back off the node.

Run with bun 1.2.14 at `/private/tmp/claude-501/-Users-timothystockstill-code-macos-4thWAIV-agent-guard/35473c11-0419-4893-8f1d-297a9a2adba7/scratchpad/dom-probe/probe.mjs`.

| | `CSS.escape` | the hard name through `CSS.escape` | the hard name hand-quoted | the repeated name inside one declaration | read back exactly |
|---|---|---|---|---|---|
| jsdom 31.0.0 | yes | matches the one row | matches the one row | matches the one row | yes |
| happy-dom 21.0.4 | yes | refuses the selector | refuses the selector | matches the one row | yes |
| linkedom 0.18.15 | no | not available | matches the one row | matches the one row | yes |

happy-dom's refusal, verbatim: "Failed to execute 'querySelectorAll' on 'Element': '[data-member=a\\\"b\\'c\\\\d]' is not a valid selector." It is the library Bun's own documentation points at, and it cannot do the one thing acceptance check 4 asks for.

jsdom also passes both cases under `bun test` itself, not only under a plain script: `scratchpad/dom-probe/jsdom.test.ts`, two tests, both green.

## What each costs

Installed fresh, as development dependencies: jsdom 28 packages and 22MB, happy-dom 7 packages and 22MB, linkedom 14 packages and 8MB. None of the three ships: the kit carries `kit.js`, `page.js`, `package.json`, `README.md`, the three languages and the two stylesheets, and `ts/metalang/kit.ts` is the one owner of that list.

## The recommendation

jsdom 31.0.0, pinned exactly. It matches the hard name both ways, it carries `CSS.escape` so the escaping is the library's rather than ours, and its selector engine is the one that behaves like a browser — which is the whole reason the contract takes a library at all.

linkedom 0.18.15 is the genuine alternative: a third of the size, it matches the hard name when the selector is hand-quoted, and it has no `CSS.escape`, so the tests would have to escape names themselves.
