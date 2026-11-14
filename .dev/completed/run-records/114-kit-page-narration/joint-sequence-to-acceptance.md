# The sequence to Tim approving the issue-106 architecture inside an architecture package

Tim, 11 November: the whole point was for him to use the kit to understand, review, acknowledge and approve an architecture design presented through an architecture package. He has told Bridge, Planning and Diagnostics to coordinate with each other, involve Diagnostics in the build, and deliver something that works.

## The correction this makes to Bridge's earlier framing

Bridge told him the exact-signature gap stands between him and what he asked for. It does not. There are two artifacts and they are approved at two different gates.

`architecture/surface.yaml` is the checked architecture document: nine declarations, every member each one has, their reach, and the law that nothing in `AgentGuard.Abstractions` may hold anything in `AgentGuard.Engine`. That is the architecture design, and approving it inside an architecture package is reachable with the repair he already approved.

`architecture/signature-diff.md` carries the exact C# signatures. `rails-run-a-workflow` requires that he approves the signature diff before TDD begins. The `arch` language carries no parameters, return types, callback types, type parameters or documentation comments, so that approval has no home inside the document. That blocks issue 106 entering TDD. It does not block the acceptance case he named.

## Owners

Planning holds this sequence and the repair Work item, confirmed on work-0ce6071af7d5. Diagnostics holds the JARVIS lane. Bridge holds the kit and the issue-106 source.

## Bridge, in the agent-guard kit

| Step | What | Waiting on |
|---|---|---|
| K1 | The kit's rendered page loads `/api/packages/story.js`, so a beat can speak against it. jarvis#120 R3. | nothing |
| K2 | `data-declaration="<id>"` on each `.d-decl` card, so a beat can ring one declaration. The declaration half of R4. | nothing |
| K3 | `data-member="<name>"` on each member row, the member half of R4. Needs `role: name` on `member.name` in `ts/metalang/arch.yaml`, which is the `member-identity` choice. | Tim |
| K4 | The tests that prove a selector reaches a member name containing a quote, which is the `selector-proof` choice. | Tim |
| K5 | `.github/workflows/ci.yml:593` checks the kit asset for `kit.js page.js package.json README.md metalang.yaml arch.yaml propose.yaml` and not `style.css` or `syntax.css`, which `ts/metalang/kit.ts:73-74` carries as required. Fix before any release. | nothing |
| K6 | Release the kit and send Diagnostics the version string, the 40-character commit sha, the SHA-256 of the uploaded asset bytes and the download URL. | K1-K5 |
| K7 | Re-cut revision `33c6c6b3800f92b66761c5927496bee70d6df716d78e90ad405755c34b7e0c47` of `document-40f2beb547b345b9b35d75d728e538c1` under the released kit. | K6 |

K1, K2 and K5 are reachable now. Issue 114's contract covers all of K1 to K5 as one piece of work and is not written while `member-identity` and `selector-proof` are unanswered, so none of it starts before Tim answers; splitting the contract to start the unblocked half would be scoping the approved work back without his say.

## Diagnostics, in JARVIS

| Step | What | State |
|---|---|---|
| J1 | The gate accepts an authored story composed with the standard vote, binding the authored bytes to the exact revision. jarvis#120 R1. | PR 123, green on `a21effd` |
| J2 | `understanding_in` accepts `story.json` beside `UNDERSTANDING.md` and nothing else. R2. | PR 123 |
| J3 | `page_vote` returns every question the story waits on with its options, and the package head draws them. R5, first route. | PR 123 |
| J4 | The author's purpose reaches the package head and `UNDERSTANDING.md` is rendered in the view rather than linked as raw Markdown. R6. | PR 123 |
| J5 | Adopt the released kit digest. | waits on K6 |

## Then, jointly

Bridge authors the issue-106 architecture package from the re-cut revision: a story that walks the nine declarations, each beat ringing the card it describes, and a real review question on the design, sent on `work-80aefef5b75e` through the existing factory. That is the acceptance case, and the approved plan already names it as the real one.

## The dependency that stays open

Approving the exact signatures. It needs an exact signature representation in the language, the ARCHITECTURE stage emitting and binding it instead of a hand-written prose diff, and JARVIS projecting it and binding Tim's answer to that revision. The execution plan assigns the amendment to Planning, who is unreachable, so it needs Tim's authorization directly. Neither Bridge nor Diagnostics starts it without that.
