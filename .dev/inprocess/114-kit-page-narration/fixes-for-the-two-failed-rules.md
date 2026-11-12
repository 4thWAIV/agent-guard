# Fixes for the two rules that failed review

Rule 3, one owning site per identity attribute, passed every lens in both rounds. Leave it alone.

Each rule below is quoted from `.dev/inprocess/114-kit-page-narration/contract.md` as it stands now. The contract is the only source of what each rule must say; nothing in this file adds to it.

## Rule 1: no language word by spelling in the drawing path

### The contract, quoted

"**No language word by spelling in the drawing path.** In `ts/metalang/app.tsx` and `ts/metalang/client.tsx`, a word of a drawn language may not be written as a literal where it is used to look something up in the document being drawn or in its rulebook, and may not be written as a local constant renaming a word `ts/metalang/words.ts` already names. The only legal route to a language word in those two uses is a named constant in `words.ts` or a rulebook lookup. This stops a member's identity being taken from the spelling of a column — `member['name']`, `columns.includes('name')`, `=== 'name'` — instead of asking the rulebook."

"**Where it stops.** Tim set this boundary as written on 2026-11-11, answering `rule-1-boundary` with `lookups-and-renames` in pkg-a37618205163. Outside the rule: a member of the page's own union of finding kinds, English prose shown to the reader, and a comparison against a JavaScript type. Enforced on every word `words.ts` names, the rule flags twenty-four literals in `app.tsx`; fifteen are the defect and nine are these three kinds. The fifteen are the rule's impact on existing code and IMPLEMENT clears them."

### The defect

`useOf` at `ts/metalang/guardrails.test.ts:160-205` is a closed list of the positions the rule reports. A language word literal in any position the list does not name returns `null` and is passed in silence. That makes the rule's reach the list rather than the contract's sentence, and a position nobody thought of is reported as checked when it was never checked.

### The fix

Report every literal in `ts/metalang/app.tsx` and `ts/metalang/client.tsx` whose text is a word `words.ts` names, and subtract only the three kinds the boundary puts outside the rule:

- a member of the page's own union of finding kinds, which `ownWordsOf` already reads from the file being checked;
- English prose shown to the reader;
- a comparison against a JavaScript type.

Anything the three do not cover is reported. A literal the rule cannot place in one of the three is reported, never passed over.

The count in the contract is the check: on `app.tsx` the rule finds twenty-four language word literals, reports fifteen, and leaves nine to the three kinds above. The nine are `typeof value === 'object'` at `app.tsx:914`; `'pending'` as a union member at `app.tsx:85`, `:96`, `:107`, `:115`, `:150` and `:1603`; and `'property'` and `'properties'` in the prose at `app.tsx:1415`.

## Rule 2: no literal target in non-test source

### The contract, quoted

"**No literal target in non-test source.** The strings `IGrantStore` and `IWindowsHelloNativeOps`, and any constant `data-declaration=` or `data-member=` attribute value, are banned anywhere in non-test `ts/` source. This stops the two named targets being special-cased so the acceptance passes while the general mechanism is absent."

"**Where it stops.** The rule holds those two bans and nothing wider. Tim dropped the third ban, on any id declared in a document being drawn and on any member name, on 2026-11-12, answering `rule-2-middle-clause` with `drop-the-clause`. On 2026-11-11 he had bounded that dropped ban to documents being drawn, answering `rule-2-boundary` with `drawn-documents` in pkg-a37618205163; the later answer removes the ban that boundary applied to. The dropped ban could not separate an id of a drawn document from an ordinary word of the code, because the six documents the renderer draws declare eighty-seven distinct ids, among them `string`, `object`, `name`, `value`, `language` and `part`. Rule 1 governs how the drawing code may reach a language word."

### The defect

The rule as written bans every id the tree's documents declare and then exempts what it calls each language's vocabulary. That exemption is the rule's own invention: it was cited to a boundary sentence the contract did not contain, and it had to grow to cover every language document in the tree because `ts/metalang/names.yaml:45` declares `id: language` and `ts/metalang/metalang.yaml` declares `string`, `object`, `name` and `value`. An exemption written to make a ban survive its own tree is the ban being narrowed to fit the code.

### The fix

Tim has dropped that ban. Rule 2 now holds exactly two:

- the strings `IGrantStore` and `IWindowsHelloNativeOps`, anywhere in non-test `ts/` source;
- any constant value given to `data-declaration` or `data-member`, anywhere in non-test `ts/` source.

Delete the declared-id ban and everything that existed to serve it: the derived banned set, the vocabulary exemption, the document loading that fed it, and the fixtures that proved it. Keep the two bans above with a violating and a compliant fixture each. No exemption list remains in rule 2, and none is to be added.

## What both rules keep

Each rule is a contract-directed test inside the existing `bun test` step, following `ts/metalang/examples.test.ts`. None is an analyzer and none carries a diagnostic id. A check that cannot be made is reported, never passed over. Production RED from either rule is the rule's impact on existing code; do not clean it, and do not suppress or exempt anything to reach green.
