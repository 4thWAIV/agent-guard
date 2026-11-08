# Distributing EvoLang to JARVIS: the design

Tim added this work on the JARVIS Work tab on 2026-11-08, on item `work-b23f8c12370b`, piece `figure-out-a-way-to-distribute-updates-f`. In his own words it is to figure out a way to distribute updates from EvoLang to JARVIS and a way for JARVIS to get those updates at regular intervals, as package dumps of what is needed to build and render EvoLang documents, reusable by agents.

Nothing here is approved. This is the shape put to him for a yes.

## What already does the job

Before proposing anything, what exists.

`ts/metalang/render.ts` builds a document into one self-contained page that React draws in the browser. A rendered document needs nothing from this repository to be read.

`ts/metalang/bundle.ts` is the bundler behind it, one module with two shapes of output. `ts/metalang/check.ts` checks a document against the language it names.

`jarvis/self_update.py` is JARVIS's own git-backed pull, and it is the mechanism this work is asking for, already written. It updates a checkout from `origin/main`, fast-forward only. It refuses when the checkout is not on main, has modified tracked files, has commits of its own not on `origin/main`, has untracked files the update would overwrite, or the version chosen has not passed CI. It reads the CI result from GitHub Actions without a login, because the repository is public.

`POST /api/packages/inbox`, in the `packages` access-key category, is the one route a package enters JARVIS by. Everything delivered through it lands in Tim's package inbox and is shown to him when he asks for it.

`jarvis/work_packages.py` unpacks a work package to `packages_dir()/<name>-<version>-<id>/`. The format is in `jarvis/docs/work-packages.md`: a zip with `package.json` at its root, `runs` one of `conversation`, `agent` or `page`, carrying `name`, `version`, `purpose` and `returns` plus the file its `runs` needs.

## His sentence holds two different needs, and they want different answers

**Reading an EvoLang document is already solved.** `render.ts` produces a self-contained page. A dump for that half is just the rendered pages, and an agent or a person needs nothing installed to read one.

**Building or checking a document is not a static dump.** It needs the toolchain source — `check.ts`, `constraints.ts`, `rulebook.ts`, `words.ts`, `syntax.ts`, `highlight.ts`, `expression.ts`, `bundle.ts`, `render.ts`, `app.tsx`, `client.tsx` and the two stylesheets, about 300 KB — plus the language files, plus bun, `js-yaml` 4.1.0, `react` 19.3.0 and `react-dom` 19.3.0. A folder of source files with no dependency tree builds nothing. That half wants a checkout, which is what JARVIS already keeps and already updates.

## The proposal

**The reading half: one versioned work package carrying rendered pages.** `runs: page`, one entry per document, built by `render.ts` from the committed language files. Nothing to install, nothing to resolve, and it is reusable by any agent exactly as the work-package format intends.

**The building half: a versioned package carrying the toolchain source.** Tim decided this on 2026-11-08, on the Work tab, against the recommendation below. The package carries the checker, the renderer, the language files and the manifest, and something on the JARVIS side installs bun and the three pinned libraries before it can build anything. That install step is work this design now owns and did not before.

**The refresh interval is a pull on CI-green commits, not a push on every change.** The same gate `self_update.py` uses: a commit on the default branch whose `ci` workflow passed.

**Not the package inbox for the interval refresh.** Every delivery through `/api/packages/inbox` surfaces to Tim. A library that refreshes on a schedule must not put something in front of him each time it does. The inbox stays for what he is meant to look at.

## What this depends on: met in part, on 2026-11-08

Tim approved pushing the branch and it is pushed. `origin/metalang` carries all twenty commits, so the language exists somewhere other than this machine and both halves have something to fetch.

Two things the push did not settle.

The default branch is `dev`, which holds no `ts/` directory at all, so a plain clone does not land on the language. Anything that fetches it names the `metalang` branch.

No CI has run against any of it. The workflow triggers only on pushes to `dev` and `main`, so pushing this branch started nothing. A CI-gated refresh has no green commit to gate on until a pull request is opened against `dev`. That bears directly on the delivery route Tim chose, since a package has to be gated on something.

## Decided on 2026-11-08

**How the building half reaches JARVIS: a versioned package carrying the toolchain source.** Tim answered this on the Work tab, decision `build-half-delivery` on item `work-b23f8c12370b`.

The recommendation put to him was the other option — JARVIS fast-forwarding a checkout of agent-guard the way `self_update.py` already fast-forwards JARVIS itself, because that mechanism is written and already gated on CI. He chose the package. That is the decision and the design follows it.

What the package route still has to answer, and the checkout route would not have: where the package is installed on the JARVIS side, what installs bun and the three pinned libraries, and what the CI gate becomes when the delivery is a zip rather than a commit. None of that is designed yet.

## What still needs his yes

**Pushing the branch.** Asked as decision `push-metalang` on the same item, with the package `evolang-to-jarvis-how-updates-travel` beside it. Recommended: push it, because nothing can be built or tested end to end until something can fetch the language.

## The kit, decided and built — 2026-11-08

Tim decided all three choices through the signed package `an-evolang-build-kit-others-can-use-1.0.0-localb58`: the kit ships as one built bundle, it is built from the one copy rather than a second folder of source, and a consumer calls a command. All three were the recommendation.

It is built, at `ts/metalang/kit.ts`, and committed as `8ba1e09`. `bun run kit` writes `dist/evolang-kit/`: the bundled command, the language file, the two stylesheets drawing reads from disk, a `package.json` saying the bundle is a module, and a readme. `evolang.yaml` is listed and reported absent, because not one word of EvoLang is written.

**One half of the kit does not work, and it is the half the slides drew as working.** `check` runs under plain node, outside this repository, and reads a document against the language file the kit carries. `render` cannot run from the kit at all. Drawing a page builds a browser bundle from `client.tsx` and React **as the page is written**, so it needs the page's own TypeScript source and a bundler, and one built file carries neither. The kit now refuses it with that reason and exits 2, under node and under bun alike, and the refusal is tested through the built kit rather than only in the repository.

This was found by running the built kit rather than by reading it: the first version threw out of the bundler. The tests at the time only ran `check` through the kit, which is the gap that let it through.

### What that leaves for Tim

Three ways to give the kit a working `render`, and none of them is free.

**Carry the page's source and React.** This is the source kit he rejected, and it needs an install on the consumer's side.

**Prebuild the page bundle once, when the kit is built, and have the page read its document at run time.** The kit then draws a page under plain node with nothing to install, which is the shape he chose. It changes how the page is built: the document, the vocabulary, the marks and the check result are injected by `define` at build time today, and render.ts says in its own header that nothing in the bundle reaches the filesystem. Reading them at run time instead is a change to that decision, so it is his.

**Leave the kit checking only.** Drawing stays a command in this repository, and a drawn page already travels on its own, so a consumer who wants pages gets the pages rather than the means to make them.

Recommended: prebuild the page bundle. It is the only one that delivers what the slides showed, and a page whose document is read at run time is also what would let one kit draw a document it has never seen.


## All three answered, and the render half built — 2026-11-08

Tim answered the three questions in `three-choices-on-the-language-and-the-kit-1.0.1-localc65`, each as recommended.

**The kit's render: prebuild the page bundle.** Built, as `9131db7`. A page's data used to be replaced inside its bundle at build time, which meant a bundle per document and a bundler wherever a page was written. It now travels in the page itself as one JSON block, written by the renderer and read by the bundle when the page loads. `page-data.ts` owns the block and both sides share it, so neither holds a second copy of the shape.

The two properties `render.ts` stated about itself are kept, which is why this needed his word rather than mine. Nothing in the bundle reaches the filesystem, because the block is in the same file. The page cannot disagree with the checker, because the same checker writes the block. Every `<` in the block is written as its JSON escape, so a document carrying the text that ends a script element cannot end the block early.

The kit now draws a page under plain node. Proven by drawing MetaLang's own page from the built kit outside the repository, serving it, opening it in a browser and reading back a drawn heading, 17,210 characters of text and 243 sections, with no console error but a missing favicon.

**The gate: open the pull request, with the CI job.** Open as pull request 105 into `dev`, carrying twenty-four commits. `ci.yml` gains the `typescript` job on all three systems — the type check, the lint, and the suite with its 75 percent floor — and the gate and the release now require it. Without that job the gate could have gone green while checking none of the language, which is why it rides in the same pull request.

The `eng/` fail-loud fixes and the two other edits to `ci.yml` are the silent-failure audit, a different piece of work, and were deliberately kept out: the committed `ci.yml` carries the `typescript` job and nothing else, and the audit edits are still uncommitted in the working tree.

**Where an EvoLang folder sits: here, beside MetaLang.** Nothing to build. No word of EvoLang is written, and he decided on 2026-11-08 to decide later whether to write it, so this settles where the file goes when that day comes: `ts/evolang/`, checked by the same `check.ts`, drawn by the same `render.ts`, carried by the same `kit.ts`.
