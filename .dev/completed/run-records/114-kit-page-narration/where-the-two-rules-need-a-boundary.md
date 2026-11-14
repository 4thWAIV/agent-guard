# Two approved rule sentences have no boundary where they meet legitimate code

The rules were written and three reviewers judged them. SOLID and DRY passed. The lie-catcher failed them with two findings, both confirmed against the live code.

## Finding 1, confirmed

Rule 1's banned word list was narrowed to the words three constants name — `BOOTSTRAP`, `SELF` and `ROLES` — and the rule file justifies that by citing a contract sentence naming four constants. The contract names no such sentence: it says "The only legal route to a language word is a named constant in `ts/metalang/words.ts` or a rulebook lookup." The four-constant wording came from the arguments this session passed the panel, not from the contract Tim approved, and that is this session's error.

The narrowing drops live instances of the exact defect. `words.ts:25` names `LANGUAGE = 'language'`, and `app.tsx:1545` writes `'language'` out as a literal in a list whose other entries are reached through `SCOPE`, `IMPORT` and `BOOTSTRAP.declare`. `words.ts:479` names `OPTIONAL = 'optional'`, and `app.tsx:363` writes `const OPTIONAL = 'optional'` again. The rule file's own comment admits the second one is outside the narrowed rule.

## Finding 2, confirmed

Rule 2's approved sentence bans "any declaration id, any member name" in non-test `ts/` source, and the implementation checks neither. The author's justification was that there is no list to check against. There is: `arch.yaml` declares 21 ids, and five are written out as literals in non-test source — `form` in `examples.ts`, `kind` in `syntax.ts` and `words.ts`, `mark` in `syntax.ts` and `words.ts`, `member` in `syntax.ts`, `word` in `syntax.ts`.

## Why neither can be fixed by this session alone

Enforcing rule 1 exactly as worded flags 24 literals in `app.tsx`. Some are the defect and some are not:

| Where | Literal | What it is |
|---|---|---|
| `app.tsx:370-386`, `:1411`, `:1491-1517`, `:1545` | `id`, `is`, `properties`, `options`, `language` | the page reading a drawn document's keys by spelling — the defect |
| `app.tsx:363`, `:543` | `optional`, `object` | a local constant naming a word `words.ts` already names — the defect |
| `app.tsx:914` | `object` | `typeof value === 'object'`, which is JavaScript's own word |
| `app.tsx:85`, `:96`, `:107`, `:115`, `:150`, `:1603` | `pending` | a member of the page's own union of finding kinds |
| `app.tsx:1415` | `property`, `properties` | English prose shown to the reader: `no properties`, `1 property` |

Fifteen are the defect — thirteen key reads plus the two local constants — and nine are not, and the sentence Tim approved does not say which. Counted per literal, so `app.tsx:1415` is two because it holds both `property` and `properties`. Writing that boundary here would be this session deciding what his rule means.

Rule 2 has the same shape with a sharper edge. Taken literally, "any declaration id" includes `arch.yaml`'s own vocabulary ids, so the rule would forbid `words.ts` — the single owner of the vocabulary, which `words.ts:395-404` states it is — from naming its own words. The rule would ban the owner from owning.

`rails-run-a-workflow` forbids the alternative: "The change narrows, softens, or reinterprets a requirement so it matches what the code already does. This is a top-line finding." So neither rule may be quietly narrowed to fit, and neither may be enforced literally without flagging JavaScript keywords and English prose.

## What is being asked

**Rule 1's boundary.** Recommended: the ban covers a literal used to look something up in the document being drawn or in its rulebook, and a local constant renaming a word `words.ts` already names. Outside it: the page's own union members, English prose shown to the reader, and a comparison against a JavaScript type. That keeps all fifteen real instances and none of the nine others.

**Rule 2's boundary.** Recommended: a declaration id means one declared in a document being drawn, which is what the two acceptance targets are. It does not mean the architecture language's own vocabulary ids, which `words.ts` owns and rule 1 already governs.

## State

`ts/metalang/guardrails.test.ts` is written and is the only changed file. `make build`, `make test`, `bun run typecheck` and `bun run lint` are all green. `bun test` is one failure: rule 1 over the live drawing path, which is the rule's impact on existing code and not a defect in the rule. Nothing was suppressed, exempted or downgraded to clear it.
