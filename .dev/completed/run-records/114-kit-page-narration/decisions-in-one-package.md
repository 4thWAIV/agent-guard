# One package carries every choice it describes

Tim asked why the five choices sit in packages other than the brief, and said there is no system for him to open another package.

## What is true

Four packages from this session are open and unanswered in his inbox:

| Inbox id | Title | Work item | Choices |
|---|---|---|---|
| pkg-dedc4434d455 | Sign the presence and grants interfaces | work-80aefef5b75e | sign-interface-surface, loaded-reuse, grant-why |
| pkg-34b725d6a79d | Kit narration repair: two language and test choices | work-0ce6071af7d5 | member-identity, selector-proof |
| pkg-5c9f4b95f15d | The declared surface: the silent failure document | work-80aefef5b75e | the standard understood vote only |
| pkg-d23e671bd70c | Architecture repair: the executive brief | work-0ce6071af7d5 | the standard understood vote only |

On his screen a package is reachable only through the one message that carries it, on that Work item's Messages tab, or in his Inbox when it is on no item (`frontend/src/MessageCard.tsx`, `frontend/src/WorkDetail.tsx`, both calling `openInbox`). There is no list of his packages anywhere in the page: `frontend/src/QueuePanel.tsx`, `QueueFiles.tsx` and `queue.ts` contain no reference to the package inbox, although the `package_inbox` brain tool's own description names "the Queue tab's Packages section" (`jarvis/jarvis_mcp.py:2608`).

By voice the two tools do exist and are wired: `package_inbox` lists them and `open_inbox_package` opens the one he names (`jarvis/server.py:10413-10414`).

## The cause

Each set of choices was delivered with the work item that raised it, and the brief was then written as a package that deliberately asks nothing and points at the other two. That arrangement assumes he can move between packages, and he cannot.

## The rule this sets

A package asks every choice it describes. A package that describes a choice and leaves the vote somewhere else is not delivered.

## The fix

Version 1.0.1 of `architecture-repair-the-executive-brief` carries all five votes under their existing decision ids and option keys. Delivered as pkg-9100658068f8, message 21576, 15 slides, five decisions. pkg-d23e671bd70c is closed as superseded, as `jarvis/package_inbox.py:567` closes an open package of the same name from the same sender.

pkg-dedc4434d455 and pkg-34b725d6a79d keep a different name each, so neither is replaced by it and both stay open asking the same five questions. There is no sender-side way to withdraw a package: `make_package` has no such option, and `dismissed` is his own act (`jarvis/package_inbox.py:23`). He sets them aside by telling JARVIS, or they are answered in whichever package he reaches first.

Answers to sign-interface-surface, loaded-reuse and grant-why arrive against work-0ce6071af7d5 because that is the item the brief is on; this session records them onto work-80aefef5b75e, whose decisions carry the same three keys.

## Filed

The missing on-screen list of his open packages is JARVIS issue 125, which also raises whether a sender can withdraw or redirect a package it has already sent.
