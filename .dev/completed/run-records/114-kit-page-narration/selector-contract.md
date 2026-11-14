# The renderer's selector contract

What the kit's page will carry, so the JARVIS lane can write beats against it. One copy; both lanes cite this file. Nothing here is built yet.

## Settled

| Target | Where it sits |
|---|---|
| `[data-declaration="<id>"]` | On the `section.d-decl` that draws one declaration. The value is the declaration's own id, exactly as the document writes it. |
| `[data-part="<part>"]` | Unchanged. `reach`, `exactness`, `groups`, `relates`, `sits`, `members`, nested inside the declaration's section. |
| `[data-col="<column>"]` | Unchanged. On each `th` and `td` of a member table. |
| `<script defer src="/api/packages/story.js"></script>` | In the page shell that `ts/metalang/render.ts` writes, after the `#page-data` script and before the drawing bundle, from one named constant. The page carries no copy of the helper; JARVIS serves it. A page opened outside JARVIS has no helper. |

So `[data-declaration="IGrantStore"] [data-part="members"]` resolves to one element.

## Open until DESIGN settles it

`[data-member="<name>"]` on the row that draws one member. The value is what the member is called, and the question is how the renderer finds that generically. The `Table` component draws whatever shape the language declares, and `arch`'s `member` has no `role: name` for the renderer to find its name by — it is typed `name` and carries no role. Three ways it could go, and the answer changes whether `[data-member="BeginVerification"]` works for every language or only for `arch`:

- find the property whose declared type is MetaLang's `name`;
- add `role: name` to `member.name` in `arch.yaml`, which changes the language and needs its own approval;
- use the first column, which works for `arch` today and breaks on the first shape that declares something else first.

GROUND is running on this now. Until it is settled, write no beat that depends on `data-member`.

## Values and matching

Attribute values are escaped. A selector that matches nothing, or matches more than one element, is reported as a missing or ambiguous target rather than widened to a broad substitute. Identical source under one pinned kit renders to identical bytes.

## Release

Diagnostics asks for four things in one message to the session named `Diagnostics` when the kit is released, and JARVIS's `language_kit.stage` refuses an asset whose bytes do not match:

1. the release tag or version string, as `PINNED_VERSION` carries it, for example `0.1.108172209-pre-release`;
2. the full forty-character commit sha it was built from;
3. the SHA-256 of the released kit asset file itself, the bytes JARVIS downloads;
4. the asset's download URL.

JARVIS keeps the kit in force — `0.1.108172209-pre-release`, commit `c918581`, digest `0112ee38` — staged for rollback. A kit built from this branch gets a new digest, and JARVIS adopts it through its existing controls. Every document checked under the old kit needs revalidating and a new revision; packages already signed stay as they are. The surface to revalidate for the model case is revision `33c6c6b3800f92b66761c5927496bee70d6df716d78e90ad405755c34b7e0c47` of `document-40f2beb547b345b9b35d75d728e538c1`, whose lineage is in `.dev/inprocess/106-silent-failure-cleanup/architecture/revision-lineage.md`.

## The helper tag, settled

Diagnostics read `jarvis/package_story.js` and `jarvis/make_package.py` and answered the three open questions on 11 November 2026.

The helper touches no DOM at load: it adds its listeners and posts `story.ready` to the parent. Its style and ring layer are created on the first cue, and a beat's targets are queried when that beat arrives. A target that is not there yet is reported as missing after 700 milliseconds and never widened. So loading before the page's content exists is fine, which is what settles the position.

The position is after the `#page-data` script and before the drawing bundle. Putting it last breaks two existing assertions in `ts/metalang/render.test.ts`, which slice the page from `indexOf('<script>')` to `lastIndexOf('</script>')` and assert the slice carries no `</script`: with the helper last, that slice spans the bundle and the helper. Before the bundle the slice is exactly the bundle, `#page-data` still precedes the bundle that `ts/metalang/client.tsx` throws without, and the helper's own tag carries an attribute so `indexOf('<script>')` still finds the bundle's bare tag.

`defer` is safe and is what the shell writes. The helper has no order dependency on the bundle or on `#page-data`, and deferring avoids blocking the parser while the request resolves, which outside JARVIS is a 404. Not `async`, and not `type="module"`: it is a classic script.

The URL is exactly `/api/packages/story.js`, with no version and no query string. JARVIS checks that this string appears in the page, so the deferred form passes its check.

## The release digest, settled

`language_kit.stage` needs a digest that matches, not one that is reproducible. It checks that the SHA-256 of the asset's bytes equals the digest in the release metadata, and binds that digest to the version, the commit and the repository, so a second asset carrying the same digest from other provenance is refused. The zip storing file modification times is therefore not a problem, as long as the digest sent is of the exact file uploaded.

Diagnostics agrees the release check should cover `style.css` and `syntax.css`, which the kit carries and its drawing reads, and which `.github/workflows/ci.yml` omits today.
