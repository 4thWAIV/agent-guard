# EvoLang: the eight decisions of 8 November 2026

Tim decided all eight through a signed JARVIS decision package, `evolang-what-it-is-waiting-on-1.0.0-local177`, delivered as `pkg-56ce5f7fc788`. He approved each answer as written. The signed record is `/Users/timothystockstill/code/macos/4thWAIV/jarvis/data/packages/evolang-what-it-is-waiting-on-1.0.0-local177/results/decisions.json`, with `decisions.md` beside it.

Six of the eight are about EvoLang's design and nothing is built against them. Two are about this repository and both are carried out.

## EvoLang is not started yet

**build-evolang — decide later.** The question was whether to start building EvoLang now or hold it until a document needs it. He decided neither; he will decide later. Nothing of EvoLang is to be written until he says so. The four design answers below stand ready for that day and authorise no work on their own.

## The four design answers

**sentence-form — written as structure.** An EvoLang sentence is written as structure, the way every document in this repository is written today, rather than parsed from text. No grammar, no parser, no tokeniser.

**verb-roles — named roles.** A verb carries named roles rather than positional ones in a fixed order. A sentence names each part it fills.

**quantifier — inside the statement.** A quantifier sits inside a statement rather than being a separate statement about one.

**parameters — EvoLang's half.** A method's parameters and its return type are EvoLang's half, not the architecture language's. The architecture language is not missing a signature; a signature is a thing EvoLang will say. This closes the disagreement in the record, which had said both that a function is a definition with parameters and a result needing nothing new, and that parameters are EvoLang's half.

## The rule about adding a word

**declared-words — the rule covers it.** His standing rule that every word of the language is approved by him, one word at a time, covers a word a document declares for itself. A document written in a language may declare its own words and extend what the language gave it, and `handoff/guard.yaml` already does. That route now needs his approval like any other, so a document cannot grow vocabulary around the rule.

## The two carried out in this repository

**commit — commit it.** The vocabulary approved on 27 October 2026 and the Denice handover package were committed on branch `metalang`.

**push-metalang — push it.** Decided on 2026-11-08 through the signed package `evolang-to-jarvis-how-updates-travel-1.0.1-local615`, delivered as `pkg-25e34d9c78bc`. Branch `metalang` is pushed and `origin/metalang` carries all twenty commits. The default branch, `dev`, holds no `ts/` directory, so reaching the language takes `git checkout metalang` after a clone, and no CI has run against the branch because the workflow triggers only on `dev` and `main`.

**build-half-delivery — a versioned package carrying the toolchain source.** Answered on 2026-11-08 on the Work tab, on item `work-b23f8c12370b`, against the recommendation, which was the checkout JARVIS already fast-forwards on itself. The design at `distribution-design.md` records the choice and what it leaves open.

**gate — open the pull request, with the CI job.** Decided 2026-11-08 in `three-choices-on-the-language-and-the-kit-1.0.1-localc65`. Pull request 105 into `dev`, twenty-four commits, and `ci.yml` gains the `typescript` job the gate and the release now require.

**subpackage — here, beside MetaLang.** Decided in the same package. An EvoLang file goes at `ts/evolang/` when it is written, checked, drawn and carried by the scripts that already serve four languages.

**render — prebuild the page bundle.** Decided in the same package and built as `9131db7`. A page's data travels in the page rather than inside its bundle, so one bundle built once draws a document it has never seen, and the kit draws under plain node.

**denice-remote — remove the push URL.** The `brij` remote in the Denice checkout at `~/code/macos/denice/denice` pointed at this repository and was configured for push as well as fetch. Its push URL is now disabled, so a push from there fails. Its fetch URL is unchanged.
