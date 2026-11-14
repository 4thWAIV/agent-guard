# The member identity, done properly

Tim refused both choices in the package and ordered the fix made correctly now. In his decision: he does not approve the issue it had been filed as, he does not approve a delay, and he does not approve a patch around it.

## What was wrong

The contract's Success definition says a selector naming one member of one declaration matches exactly one element. Every member row carried its own name as its identity, with nothing making it unique, so a declaration with two members of the same name — ordinary in the language this exists to describe, where a method can be overloaded — gave two rows the same identity. A beat naming that member matched two elements and pointed at nothing.

## What is right

A row's identity names one member and no other.

A member name that occurs once among its declaration's members is that row's identity, exactly as it is today. A name that occurs more than once gives each of those rows an identity made of that name and the row's position among the declaration's members, in the order the source declares them, and none of them carries the bare shared name.

That keeps the two things that matter at once. An overloaded member stays describable — the document is still valid and still says there are two of them — and each one becomes pointable, which it never was before. A name that identifies one member identifies it; a name several members share identifies none of them on its own, so nobody writes a beat against a shared name and silently gets whichever row was drawn first.

## What it does not disturb

A document with no repeated member name draws exactly as it did, so every target written against the kit today keeps working, and acceptance checks 3 and 5 — which name a member repeated across two declarations — are untouched.

The alternatives were both refused and both deserved it. Accepting the ambiguity left the Success definition untrue. Refusing the repeat in the language would have made an overloaded member impossible to write down, which is a worse answer than the problem.

## Where it is written

Contract decision `member-identity-unique`, with acceptance check 18 proving all three cases: either member of a repeated pair reachable on its own, the shared name alone reaching none, and an unrepeated name drawn as before.
