# 55 — Fold the Design Review brief's three missing pieces into rails-decisions

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/55

---

## What this is

A draft from 9 August, `DRAFT-design-review-process.md`, defines a fixed brief for any decision escalated to Tim. It was never trialled and nothing live references it. The backlog folder named after this issue holds it.

## What it specifies

A seven-part brief: the ask in one line; the problem, meaning the one forcing fact, stated first and never leading with what the thing is; the actual code — the real interface or signature shown verbatim, and no code means the review is bounced as unprepared; two or three real options, one line each, the recommendation marked, never lazy-versus-right; one reason the pick wins; what the wrong call locks in and who it burns; and the blast radius in files, interfaces and callers.

Four rules on top: problem before pick; no backreferences to earlier conversation, so the brief stands alone; full sentences; and one bounded round — approve, pick, reject, or ask for one specific thing, never an open debate.

It binds the orchestrator and every delegated agent, and the outcome is Tim's ruling recorded in the contract in his own words.

## What is already live

Roughly half, spread across three files. The firing condition is `rails-decisions` — forced, lasting, costly to reverse, picks between real alternatives — including the clause that a choice a rule already answers never escalates. "Recommendation marked" and "answer first" are in `how-to-communicate`. "Stands alone, no backreferences" is in the communication rules. Recording the outcome in Tim's own words is `rails-decisions` in full.

## What exists nowhere else — the actual ask

Fold these into `rails-decisions`, which already owns the escalation boundary:

1. **Show the actual code, or the review is bounced.** The real interface, signature, or prototype the decision is about, verbatim.
2. **State the cost of the wrong call** — what it locks in, who it burns, how hard to undo.
3. **State the blast radius** — the files, interfaces, and callers it touches.

Two smaller ones to fold with them: the single bounded round, and a prescribed shape that binds a *delegated agent* escalating to Tim, not only the orchestrator.

## Why it earns its place

The 2026-08-31 session is the case study. Four consecutive reports to Tim were rejected, and the complaints map exactly onto what these three pieces legislate: leading with the wrong thing, referring to things as though he already knew them, and giving no concrete detail to decide from. The live rails did not catch any of it.

## Done when

The three pieces plus the two smaller ones are in `rails-decisions`, the backlog folder is deleted, and no fourth home for communication rules is created.