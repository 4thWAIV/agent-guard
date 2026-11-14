# Fixes the rules need

Rule 3, one owning site per identity attribute, has passed every lens in every round. Leave it alone. Rule 1's closed list of positions and rule 2's declared-id ban are both gone and neither is to come back.

Two findings are confirmed against the live rule file. Both are the same defect in different places: the rule's reach is narrower than the contract's sentence, and the gap is reported as checked.

Each rule below is quoted from `.dev/inprocess/114-kit-page-narration/contract.md` as it stands now. The contract is the only source of what each rule must say; nothing in this file adds to it.

## Rule 1: the exemption is wider than the boundary

### The contract, quoted

"**No language word by spelling in the drawing path.** In `ts/metalang/app.tsx` and `ts/metalang/client.tsx`, a word of a drawn language may not be written as a literal where it is used to look something up in the document being drawn or in its rulebook, and may not be written as a local constant renaming a word `ts/metalang/words.ts` already names. The only legal route to a language word in those two uses is a named constant in `words.ts` or a rulebook lookup. This stops a member's identity being taken from the spelling of a column — `member['name']`, `columns.includes('name')`, `=== 'name'` — instead of asking the rulebook."

"**Where it stops.** Tim set this boundary as written on 2026-11-11, answering `rule-1-boundary` with `lookups-and-renames` in pkg-a37618205163. Outside the rule: a member of the page's own union of finding kinds, English prose shown to the reader, and a comparison against a JavaScript type. Enforced on every word `words.ts` names, the rule flags twenty-four literals in `app.tsx`; fifteen are the defect and nine are these three kinds. The fifteen are the rule's impact on existing code and IMPLEMENT clears them."

### The defect

`ownWordsOf` exempts a member of every union of text literals the file declares, in any position. The boundary exempts a member of the page's own union of finding kinds. Three lines appended to `client.tsx` — `type Col = 'name' | 'id'`, a read of `m['name']` and a comparison `c === 'id'` — leave the rule reporting nothing, and those two shapes are the ones the rule's own sentence names.

### The fix

The exemption covers the members of the page's unions of finding kinds and no other union. `app.tsx` declares two, and each says which list a thing came from: `type From = 'highlight' | 'problem' | 'blocker' | 'pending'` at `app.tsx:85`, which types `Said.from` at `app.tsx:89`, and the union typing `Finding.from` at `app.tsx:95`. `client.tsx` declares none, so nothing in that file is exempt on these grounds.

Where the rule cannot establish that a union is one of those, it grants no exemption. Do not identify them by a list of type names written into the rule: identify them by what the boundary says they are, so a union the page gains for some other purpose never widens the exemption.

The counts the contract states are unchanged by this, and they are the check: twenty-four literals weighed in `app.tsx`, fifteen reported, nine left to the three kinds. `'pending'` is a member of both finding-kinds unions, so the six `'pending'` literals stay outside the rule.

## Rule 2: three routes write a constant identity value and are not read

### The contract, quoted

"**No literal target in non-test source.** The strings `IGrantStore` and `IWindowsHelloNativeOps`, and any constant `data-declaration=` or `data-member=` attribute value, are banned anywhere in non-test `ts/` source. This stops the two named targets being special-cased so the acceptance passes while the general mechanism is absent."

"**Where it stops.** The rule holds those two bans and nothing wider. Tim dropped the third ban, on any id declared in a document being drawn and on any member name, on 2026-11-12, answering `rule-2-middle-clause` with `drop-the-clause`. On 2026-11-11 he had bounded that dropped ban to documents being drawn, answering `rule-2-boundary` with `drawn-documents` in pkg-a37618205163; the later answer removes the ban that boundary applied to. The dropped ban could not separate an id of a drawn document from an ordinary word of the code, because the six documents the renderer draws declare eighty-seven distinct ids, among them `string`, `object`, `name`, `value`, `language` and `part`. Rule 1 governs how the drawing code may reach a language word."

### The defect

`writesIn` reads three routes: a JSX attribute, the attribute written inside a run of text, and `setAttribute`. The rule file asserts that set is complete. Three further routes write a constant value and draw no finding and no unreadable report:

- a props object handed to `createElement`, which is this tree's live idiom at `ts/metalang/propose.ts:273`;
- a props object spread into an element;
- `dataset`, which this tree also uses, at `ts/metalang/propose.ts:786` and `:789`.

`createElement('tr', { 'data-member': 'BeginVerification' })` in non-test source passes the rule today, and `BeginVerification` is the acceptance target the contract names at Acceptance 10. The result reported rule 2 green against production with those routes unread, which is reporting a check as made that was not made.

### The fix

Rule 2 reports a constant value given to `data-declaration` or `data-member` by any route in non-test `ts/` source, not by an enumerated three. Cover the three routes above and build the reach from what the ban says rather than from a list of the ways this tree happens to write markup today. A place the rule cannot read is reported as unreadable, as the dynamic `setAttribute` case already is. Delete the claim that the three routes are the complete set.

Rule 3 reads the same writes, so its reach follows rule 2's.

The bracket skip stays: a name inside square brackets is a selector reading the attribute, not a place writing a value, and the rule file says so where it does it.

## What every rule keeps

Each rule is a contract-directed test inside the existing `bun test` step, following `ts/metalang/examples.test.ts`. None is an analyzer and none carries a diagnostic id. A check that cannot be made is reported, never passed over. Production RED from a rule is the rule's impact on existing code; do not clean it, and do not suppress or exempt anything to reach green.
