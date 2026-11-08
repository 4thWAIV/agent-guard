# Architecture language step 3: say what an element is made of

Step 3 of the architecture language's seven-step road. Each step is one sentence the language gains the power to write, and each needs the one above it. Steps 1 and 2 are done: the six forms are named with their marks and what four languages call each, and one thing can be named — `IGuard` is an interface.

**The sentence this step buys:** `IGuard` has a member called `PrecheckAsync`.

Rules and tests are written about members, not about whole types, so nothing can be signed while a member cannot be named.

**The change, in `ts/metalang/arch.yaml` alone.** One new entity, `member`, carrying the name it is known by, which of the five kinds of member it is, and one line saying what it does. One new property on `element` holding the members a thing has, in the order the source declares them. No change to MetaLang, to the checker or to the renderer.

**The five member kinds** are method, constructor, property, field and value, which Tim approved on 21 October 2026. `value` is one of the closed set an enum declares rather than stored data.

**Prepared lines that need redoing first.** `handoff/step3/` holds the proposed lines from before the element shape changed. The `member` entity and the `members` property carry over unchanged — a member is a value inside a list and not a declaration, so it keeps its own `name` — but the prepared `arch.yaml` still shows `element` carrying the `name` that came off it on 24 October, and the prepared `guard.yaml` still writes the five parts of the guard as the values of one enum rather than as constants.
