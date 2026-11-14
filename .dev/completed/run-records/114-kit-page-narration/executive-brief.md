# Executive brief: how architecture reaches you, and what it costs to fix it

Covers two work items — the silent-failure cleanup (`work-80aefef5b75e`, issue 106) and the architecture walkthrough repair (`work-0ce6071af7d5`) — because the second exists to serve the first.

## Five choices are open

Three are in `pkg-dedc4434d455`, which is the surface for issue 106:

1. **Sign the interface surface, or name a signature you want different.** Two new types and six changed interfaces. Once the acceptance tests are written against these signatures, changing one costs a test rewrite. My pick: sign it.
2. **`Loaded<T>` on all four reads, or only the two you named.** Your decision `loaded` named `GetActiveGrantsAsync` and `LoadGrantPublicKey`. The contract added a third and the architecture stage added a fourth, both private helpers. My pick: keep all four, because a second type carrying a value and a reason is the same record under another name.
3. **When one grant read refuses three files, does every cause reach your sink file, or only the first.** My pick: every cause, because a read that refuses three and records one is the failure that does not report itself.

Two are in `pkg-34b725d6a79d`, which is the kit repair:

4. **Change the architecture language so a beat can point at one member, or ship pointing at whole declarations only.** One line in `ts/metalang/arch.yaml` plus the wording in `ts/metalang/metalang.yaml` that the line relies on. My pick: change it.
5. **Let the tests take one pinned development-only DOM library, or take none and prove less.** My pick: take it, because without it the tests cannot prove that a name containing a quote is reachable at all.

## Why this exists

The rails exist so the architecture an AI builds is visible to you before it is built against. Everything else in the pipeline is checked by something — the analyzers check the code, the tests check the behaviour, the adversaries check the claims. The one artifact that reached you with nothing checking it was the architecture itself, which arrived as a Markdown diff an agent wrote.

The architecture language was built to close that: an architecture is a YAML document, checked against a language, bound to its work item, and immutable once checked. What you read is a rendering of that exact revision rather than an agent's prose about it.

It was not being used. You asked why, on 11 November, about the package asking you to sign the presence and grants interfaces.

## What was wrong, and what is fixed

Four breaks, each enough on its own. Three are closed in JARVIS as of commits `f887c18` and `b3707b6`: an agent reaches the language through a signed command, the package factory takes a checked document and projects it, and the production document says so. Tracked as agent-guard#112.

The fourth is open. `.agents/workflows/architecture.js` neither writes nor binds the document; its result contract still requires a prose signature diff. The document for issue 106 was written by hand after the stage had finished.

Two more came out of using it. The language check never opens the code it describes, so a document can claim it lists every member of a type, omit one, and check clean — which happened, and was caught by a package author reading the source (agent-guard#113). And the package carrying a document could ask you one thing only, "Got it, or do you have questions", and could not walk you through anything: the author's six open findings reached you only as a raw Markdown link (jarvis#120).

## What is being built, and what it buys

You approved the repair on 11 November as `authorize-walkthrough-repair: approve-build`. It runs in two lanes.

The JARVIS lane lets a package author write a narrated story beside the document, lets the package ask ordinary review questions and record their answers, and renders the author's explanation in place instead of linking it raw.

This repository's lane makes the drawn page reachable: the narration helper loads in the page, each declaration card carries its own identity, and each member row carries its own, so a beat can ring `IGrantStore`'s members and then `BeginVerification` inside `IWindowsHelloNativeOps` rather than every members table at once.

What that buys: an architecture is read to you, pointing at each piece as it is explained, and you answer questions about it in the same package.

## What it does not buy, and this is the part worth your attention

After all of it, you still cannot sign an interface surface inside the canonical document.

The `arch` language carries a member's name, its kind, who can see it and one line of prose. It has no construct for a parameter, a return type, a callback type, a type parameter or a documentation comment. So the document cannot say `Task<Loaded<IReadOnlyList<Grant>>> GetActiveGrantsAsync(CancellationToken)`; it can only say that `GetActiveGrantsAsync` is a method. Seven such gaps are recorded on the revision itself as `outside_language`.

That is why you have two artifacts for one surface: a checked document that carries the architecture, and a deck that carries the signatures and your vote. The repair makes the document explain itself properly. It does not merge the two.

Closing that is a separate piece of work, and it is not authorized: the language would have to carry exact signatures, `architecture.js` would have to emit and bind the document instead of prose, and the signature vote would have to bind to the exact revision it was given on. Until then, every architecture sign-off is two artifacts and the prose one is what you actually sign.

## Where each thing stands

| | |
|---|---|
| Issue 106, the silent-failure cleanup | The surface is built and compiles. The stages through ARCHITECTURE are done. It cannot enter TDD until choice 1 is answered. |
| Its canonical document | `document-40f2beb547b345b9b35d75d728e538c1`, revision `33c6c6b3`, nine declarations, bound to the work item. Checked, standing proposed. |
| The repair, JARVIS lane | Diagnostics is building against the agreed selector contract. |
| The repair, this repository's lane | GROUND and DESIGN are done. The contract is blocked on choices 4 and 5. |
| What is proven | Nothing is shown to you as working until the tests and a run against the real issue-106 document both pass. |

## What happens after you answer

Choices 1 to 3 unblock issue 106: tests get written against the signed surface, then the implementation, then the adversary round and the gate.

Choices 4 and 5 unblock the kit contract, then its rules and tests, then a kit release that JARVIS adopts, then the document for issue 106 is re-cut as a new revision under the new kit, and only then is a walkthrough put in front of you.

Nothing here signs anything by itself, and no answer to the repair touches the interface surface.
