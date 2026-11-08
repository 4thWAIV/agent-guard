Step 6 of the architecture language's seven-step road, after step 5 (how two elements relate).

**The sentence this step buys:** to find `IGuard` in the C# source, look for `interface IGuard`.

**Where it stands.** The first half is written: `ts/metalang/names.yaml` says what each of C#, Python, Rust and TypeScript calls each of the six forms, with one entry for each pairing and a decided absence written as `none`. Those names are written for a reader — Python's interface is "a Protocol or an ABC", TypeScript's record is "interface, or a type alias" — so they name a form rather than give a way to find one.

**What is left.** The second half: the word the language gains for how a form is found in a language's source, written on the entries that name a form. That is a language change and it goes through a written proposal, the same way `map` (step 4) and `link` (step 5) did.

**Corrected 26 October 2026.** This issue previously said the work left was a reader for each of the four languages that goes into real source and reports whether what the document claims is there. That plan was withdrawn on 25 October 2026: each language already has its own code-analysis system for enforcing rules, and a reader of our own would be a second enforcement mechanism beside the Roslyn analyzers and ESLint this repository already runs. The mechanism for enforcing architecture rules in Python and Rust is now tracked separately in #93 (pylint checkers and dylint lints). This issue is the language step only.

**Still open.** Whether anything beyond the language change belongs in step 6 — in particular whether a document's claims are held against real source, and by what — is not answered here.

