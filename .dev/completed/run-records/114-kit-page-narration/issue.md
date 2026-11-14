The kit renders a document as a page, and JARVIS shows that page inside a package. A package is meant to walk its reader through what it carries, and this one cannot, for two reasons that both live in `ts/metalang`.

**The page never loads the story helper.** `pageHtml` in `ts/metalang/render.ts` writes the doctype, the title, the styles, the data block and the bundle, and nothing else. JARVIS's narration helper is `<script src="/api/packages/story.js"></script>`, which draws the pointer and the rings and runs a beat's actions inside the package frame. Without it a beat can speak and can point at nothing. A page opened outside JARVIS simply has no helper, which is the documented behaviour for every other page package.

**Nothing on the page names the thing it draws.** The live DOM carries `data-part` on each section of a declaration card (`reach`, `exactness`, `groups`, `relates`, `sits`, `members`) and `data-col` on each member column, so a selector can reach every members table at once. It cannot reach one: `app.tsx` draws each declaration as `<section className="d-decl">` with no attribute carrying its id, and each member as a `<tr>` with no attribute carrying its name. Rendering the nine-declaration surface for agent-guard issue 106 gives nine indistinguishable cards.

The intended targets, from the approved repair plan, are `[data-declaration="IGrantStore"] [data-part="members"]` and `[data-declaration="IWindowsHelloNativeOps"] [data-member="BeginVerification"]`.

## What this needs

- `pageHtml` emits the story helper script tag.
- Each declaration card carries `data-declaration="<its id>"`.
- Each member row carries `data-member="<its name>"`. The row's identity has to be derived from the shape being tabulated rather than assumed to be a column called `name`, because `Table` draws any shape the language declares.
- The existing `data-part` and `data-col` targets keep working unchanged.
- Values are escaped, and identical source plus a pinned kit still renders identical bytes.
- Tests cover a repeated member name across two declarations, punctuation and quotes in an id, and a target that matches nothing, which must be reported rather than silently widened to a broad substitute.

A changed kit means a new kit release through the existing controls, and any document checked under the old kit needs revalidating and a new revision; already-signed packages stay immutable.

Upstream half of 4thWAIV/jarvis#120, which has the JARVIS-side defects: the projection gate pins the story to one slide, the fresh author may write no story, and the package head offers only the `understood` question. Authorized by Tim as `authorize-walkthrough-repair: approve-build`, recorded 2026-11-11T16:56:46-06:00 in `architecture-walkthrough-repair-authoriz-1.0.0-local61c/results/decisions.json`. Tracked on JARVIS Work item `work-0ce6071af7d5`.
