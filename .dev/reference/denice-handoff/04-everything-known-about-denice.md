# Everything this repository knows about Denice

The honest total is: nothing in this repository, apart from what Tim said on 2026-11-06 and what is written in this package. That is not a gap in the search; it is the finding. One record does exist elsewhere on this machine, and it is named below without being copied.

## What was searched, and what came back

Searched case-insensitively for Denice across the whole working tree, including every dotfolder, across all of git history on every ref, across the stash, and across both memory stores.

| where | result |
|---|---|
| the working tree, outside this package | no mention anywhere |
| git history, every ref, by commit message and by content added or removed | no mention |
| the stash | empty |
| this repository's Claude memory store, 62 files | no mention |
| the shared Codex memory store, 2 files | no mention |

No file, commit, issue folder, design document or memory entry in `4thWAIV/agent-guard` names Denice. Nothing was recorded about it before today.

What is on the record is in `00-what-tim-has-asked-for.md`: the eight controls, and the three instructions Tim gave about them on 2026-11-06.

## One other Denice record exists on this machine, and it is not mine to republish

A needs board for the Denice product plan sits at `~/code/macos/4thWAIV/jarvis/data/needs/denice-product-plan.md`, last updated 2026-10-28. It holds seven decisions and one question, all commercial — the build model, pricing and hours, deck edits, investor and partner terms, the growth model's audience, reach assumptions, and a markup figure that disagrees between two documents.

None of it is copied here. It is another project's record, it is about Denice as a venture rather than as a work model, and the Denice session is closer to it than this repository is. It is named only so that "nothing is recorded about Denice" is not mistaken for "nothing exists anywhere".

## Denice already has a remote to this repository

The checkout at `/Users/timothystockstill/code/macos/denice/denice` carries two remotes:

```
brij	git@github.com:4thWAIV/agent-guard.git (fetch)
brij	git@github.com:4thWAIV/agent-guard.git (push)
origin	git@github.com:denice-aios/denice.git (fetch)
origin	git@github.com:denice-aios/denice.git (push)
```

The remote named `brij` is this repository — the same URL as agent-guard's own `origin`, configured for both fetch and push. So that checkout can already fetch agent-guard, and can already push to it, without any new access grant.

The other checkout, `/Users/timothystockstill/code/macos/nuevco/denice`, has one remote, `git@github.com:Nuevco/denice.git`, and shares nothing with this repository. Three organisations are involved in all: `4thWAIV` owns agent-guard, and `denice-aios` and `Nuevco` each own a Denice repository.

Nothing inside either Denice checkout was read. Only the one `git remote -v` on each.

**What that means for this handover.** Less than it first appears, because there was never an access problem: `4thWAIV/agent-guard` is public, so anyone can clone it and no grant is needed. Corrected on 2026-11-08: this file said a clone gets the language as of commit `e8394e5`, which was wrong, because no remote branch carried it. Tim then approved pushing it, so `origin/metalang` now has the whole language. A clone still lands on `dev`, which holds no `ts/` at all, so it takes `git checkout metalang` to reach it. The design record is still on this machine only, so reading the working tree at `/Users/timothystockstill/code/macos/4thWAIV/agent-guard` remains the only route to that.

**And a caution that belongs with it.** That remote was configured for push as well as fetch. On 2026-11-08 Tim approved disabling its push URL, and it is disabled, so a push from that checkout to agent-guard now fails; it still fetches. Nothing in this handover authorises a push to agent-guard by any route. The language now exists on `origin/metalang` as well as on this machine, so a bad push would no longer lose the only copy — but it would still rewrite a branch nothing else protects. Read, do not write.
