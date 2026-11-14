# Rule 2's middle clause cannot be enforced, and rule 1 needs a position-agnostic check

Second rule round. DRY passed. SOLID and the lie-catcher failed, and both findings hold against the code.

## Rule 2's middle clause, which has now failed twice

The approved sentence bans, in non-test `ts/` source: the two named target strings, **any id declared in a document being drawn, any member name**, and any constant `data-declaration=` or `data-member=` attribute value.

Tim's boundary narrowed the middle clause to ids declared in a document being drawn, excluding the architecture language's own vocabulary. That does not resolve it, because in this tree those are the same set.

`ts/metalang` holds six language documents — `arch.yaml`, `metalang.yaml`, `names.yaml`, `parts.yaml`, `propose.yaml`, `stage.yaml` — and the renderer draws all of them. Between them they declare 87 distinct ids, including:

| id | declared in | as a plain literal in non-test source |
|---|---|---|
| `string` | `metalang.yaml` | 101 times |
| `object` | `metalang.yaml` | 6 times |
| `part` | `parts.yaml`, `propose.yaml` | `'part' in step` at `highlight.ts:140`, `:149`, `:157` |
| `language` | `names.yaml:45` | `app.tsx:1545`, `words.ts:25` |
| `name`, `value` | `metalang.yaml` | throughout |

So enforcing the clause reports 101 uses of `'string'` that have nothing to do with any drawn document, and the only way to avoid that is to exempt the words — which is what both rounds' authors did, each inventing a different exemption and each citing a contract sentence that does not exist.

Taken the other way, if "a document being drawn" means the document an acceptance check runs against, the clause covers exactly the two named strings, which the first clause already bans. So as worded the clause either flags ordinary TypeScript or adds nothing.

## Rule 1, which this session can fix without a decision

SOLID's finding: the check recognises a language word only in a closed list of syntactic positions — element access, a call to one of a named set of methods, an equality comparison, an array literal, a JSX attribute, and a `const` initialiser. It returns nothing for `'name' in member`, for `case 'name':`, and for an object literal, so the next spelling-based lookup passes silently. That is matching the instance rather than the structural property.

The approved sentence does not list positions. The fix is to report a language word literal wherever it appears in the two drawing files, except the three kinds Tim's boundary puts outside the rule, rather than only in positions someone enumerated. That meets the sentence as written and needs nothing from him.

## Rule 3

Passed both rounds with nothing against it. SOLID observed that a nested helper component inside the owning component would also pass; it is not blocking and the rule stands.

## Process notes, not Tim's to settle

The round's own report claimed each rule's recorded sentence carried the approved wording verbatim, and for rules 1 and 3 it did not — text was appended. A count in its notes said `propose.yaml` declares 13 ids; it declares 12. Both were caught by the lie-catcher. The arguments this session passes a panel must quote the contract rather than paraphrase it; the first round's failure had the same root.

## What is asked

Whether rule 2 keeps its middle clause. Recommended: drop it. Rule 2 then bans the two named target strings and any constant identity-attribute value, both of which are exact and are what the rule was written to stop. The general protection comes from rule 1, which bans reading a language word by its spelling, and from an acceptance that proves the mechanism on fixtures rather than on the two named targets.
