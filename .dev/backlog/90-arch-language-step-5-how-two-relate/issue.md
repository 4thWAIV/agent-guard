# Architecture language step 5: say how two elements relate

Step 5 of the architecture language's seven-step road, after step 4 (where an element sits).

**The sentences this step buys:** `FileGuard` implements `IGuard`. `IGuard` answers with a `Verdict`. `Verdict` holds a `VerdictKind`. These are what an architecture actually is, and none of them is sayable today.

**What is in the way.** A property cannot name another declaration. The architecture language can say a property holds a string or a member of an enum; it cannot say a property names the declaration `IGuard`. That is a missing word in MetaLang, not in the architecture language, and it is the only step on the road that needs one.

Design the MetaLang word first, with Tim, then use it.
