# The issue-106 approval does not go in a deck

Tim, 11 November: the purpose of getting the architecture system working — so he could see an architecture package, be walked through it, and approve an architecture through it — was to approve the silent-failures work through an architecture package. That work is the test scenario for the system. He will not accept a separate, non-architecture package to approve it.

## What I did

I asked him to approve the issue-106 interface surface in an ordinary decision deck: first pkg-dedc4434d455, then again as one of five votes in pkg-9100658068f8. Both are ordinary decks. Neither is an architecture package.

## Why I did it

An architecture package cannot ask him to approve anything.

`language_projection.project` is the one projection of a checked revision into a package, and the `package.json` it writes carries `runs: page`, `entry: index.html`, and no `story` and no `skill` at all. The members are `source.yaml`, `check.json`, `canonical.json`, `index.html` and `UNDERSTANDING.md`. There is no story, so there is no walkthrough.

The factory then adds its standard vote story, and `language_projection.gate` compares that story byte for byte with `package_story.encode(package_inbox.with_understood_slide(None, language))`, refusing any difference with "The canonical package review must be the unchanged factory-generated standard vote". `None` means the vote slide alone. The gate's conditional at `language_projection.py:73` lifts that check when the readable member is `readable.md` rather than `UNDERSTANDING.md`, but `understanding_of` admits `readable.md` only from `metadata.readable` with origin `author-supplied`, which is the brain's publish; `make_package --document` always binds `UNDERSTANDING.md` with origin `fresh-author`. There is no agent route to the lifted branch.

`package_inbox.page_vote` returns only the `understood` question, so a story that asked anything else would have no control to answer it.

Rather than hold the approval until that was repaired, I routed it through a deck. That is the workaround he is refusing, and it means issue 106 never exercises the system it was built to exercise.

## What the approved plan already says

The execution plan he approved on 11 November, `architecture-walkthrough-repair-authoriz-1.0.0-local61c/execution-plan.md`:

> **Named dependency: canonical exact-signature approval.** The current arch model does not represent parameters, return/callback types, type parameters, documentation comments or per-member change marks. `architecture.js` still requires prose `signatureDiff`. An ordinary question cannot fill those missing semantics. The existing separate signature-deck process can carry a scope-specific signature, but it does not solve the two-artifact problem and is not presented here as completion of same-package canonical approval.

> If the acceptance objective is a single canonical package carrying exact signature approval before Bridge enters TDD, Planning must bring a separately scoped amendment: Bridge owns defining/checking exact signature representation and making ARCHITECTURE emit, validate and bind that authoritative surface; Diagnostics owns its canonical projection and binding answers to that exact revision through existing answer APIs.

That is the trigger, and his message fires it. The repair as approved restores narration, pointing, reading and ordinary review questions. It does not make the architecture package able to carry his approval of an interface surface, because the language cannot express a signature.

## What follows

The three issue-106 approval questions come out of pkg-9100658068f8, which is rebuilt as version 1.0.2 carrying only `member-identity` and `selector-proof`, the two build choices for the repair itself. Issue 106's approval waits until it can be asked inside the architecture package.

pkg-dedc4434d455 stays unopened; it is the separate signature deck he is refusing, and a sender cannot withdraw a package.

Planning owns bringing the amendment, by the plan's own assignment. My lane within it, defining and checking an exact signature representation in the language and making ARCHITECTURE emit and bind it, is not authorized and does not start until he authorizes the amendment.
