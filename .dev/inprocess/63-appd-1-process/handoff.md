# Handoff — appd-1-process

For the agent taking over issue #63. Read this once, then work from the files it points at rather than from this document.

---

## 1. Boot

Read these four in this order before anything else. They are the rails and they override any instinct you have about how to work.

```
.agents/skills/rails-read-me/SKILL.md
.agents/skills/rails-run-a-workflow/SKILL.md
.agents/skills/rails-decisions/SKILL.md
.agents/skills/rails-real-work/SKILL.md
```

Then read the work folder, which is `.dev/inprocess/63-appd-1-process/`:

- `conversation.md` — every decision Tim has made, in the wording he approved. This is the brief. Treat it as binding.
- `design-challenges.md` — what the next design round has to work out.
- `ground-output.json` and `design-output.json` — the recorded stage results. Background, not spec.

The live GitHub issue always wins over anything in the folder. Read #63 with `gh issue view 63` and read #62, which is the parent holding the whole appd design.

**Derive state, never read it from a status document — including this one.** The rails are `ls .agents/skills/`, the workflow scripts are `ls .agents/workflows/`, shipped work is `ls .dev/completed/run-records/`, work in flight is `ls .dev/inprocess/`, and what is planned or open is `gh issue list`.

---

## 2. Where the work stands

Branch `appd-1-process`, cut from `rails-doc-cleanup`. Ten commits, working tree clean, nothing pushed.

Two of eleven stages are complete. The pipeline is GROUND, DESIGN, CONTRACT, RULE-PHASE, ARCHITECTURE, TDD, IMPLEMENT, READINESS, REFUTE, GATE, REPORT. Stage order never changes and re-running a completed stage needs Tim's explicit yes.

**GROUND** ran fifteen agents. All five capabilities came back `new` with both search lenses empty, so nothing is reused and the contract carries no reuse instruction. Twenty open questions were triaged: seven closed by Tim's decisions, one dead, five handed to DESIGN, three delegated to RULE-PHASE, and one resolved as not needed. Twelve auto-resolved records were adjudicated; two over-reached and Tim ruled on them, and that ruling is in `conversation.md`.

**DESIGN** ran four agents. The chosen approach turns on one idea: no string, path or command crosses a port into the OS abstraction, so `Start()` takes no argument saying what to launch. A second startup path is not forbidden by a rule someone could suppress — it cannot be written. Sixteen guardrail rules came with it, and Tim signs each one off before RULE-PHASE writes any of them.

**Nothing has been built.** No source file, analyzer or test has been touched. Every change so far is under `.dev/` plus one commit to `rails-real-work`.

**The next action** is to draft the contract, then run the hidden-decision scan over that draft. The scan is documented under GROUND but cannot run there because it requires a contract file — that defect is filed as issue #73, and for this run the scan happens at CONTRACT where the script works.

---

## 3. How to talk to Tim

This is not style. Getting it wrong costs him time, which is the thing you are optimising against.

**Open every message with the verdict or the answer.** No preamble, no recap, no narrating your own mistakes.

**Answer the literal question asked**, in his vocabulary, in the fewest words. When the honest answer is "it does not exist" or "I did not do it", say that plainly and first.

**Name the mechanism, never a label for it.** Say "put the user's SID in the Windows pipe name" — not "my pick: one per user". A label makes him ask what it means and costs a whole round trip. This applies to every option, every recommendation, and every line you write into a record.

**Set the context for each part before you say the point.** He has not read what you read. Referring to a report, an agent, or a finding as though he has seen it makes the message unreadable.

**Problems and decisions on top, then a fold** marked `OPTIONAL DETAILS (do not need to read)`, always present, blank when empty. Below the fold means he does not have to read it — so anything raised so a later stage or another agent will not trip on it goes above the fold, whatever kind of note it looks like. Naming a thing and then filing it where he is told not to look is the same as not naming it.

**Report a failure before anything that passed.** Never soften it, never open with what went right.

**One recommendation, marked, with one reason.** Not a menu of caveats. If a correct approach and a cheaper wrong one are both visible, take the correct one and say so.

**Do not surface a settled question.** Before bringing him anything, check whether an approved rule, a recorded decision, or `.dev/reference/best-practices-guide.md` already answers it. If it does, apply it and never raise it.

**Do not surface a race or defect that fails closed.** Judge it by what happens when it fires, not whether it can. A benign no-op is not a finding.

**When he corrects you, rebuild the whole answer in one turn** rather than patching the one line.

**Give the count you claim.** If you say four things, show four numbered items.

---

## 4. What Tim controls, and what you can decide alone

The dividing test: does the choice outlive its function and make other code depend on it? If yes, it is his.

### His, always — surface it and wait for an explicit yes

**The Top Layer interface.** The service the CLI calls, and the same service the dashboard will call. Its operations, their shapes, what they return. This is the API footprint and it is the thing he most wants to own.

**The OS Abstraction Layer.** The interfaces that make all three operating systems look identical from above, and what sits on each.

**The OS Native Layer grouping.** The wrappers themselves are fixed by the OS or the BCL — they mirror one to one and exist only so they can be faked. What is his is which interface each one sits on and how like concepts are grouped.

**Every analyzer rule, before it is written.** A rule written into `analyzers/` without his words is an unapproved decision ranked with a weakened test.

**Anything written onto a user's machine.** Its layout, its contents, its location.

**Any user- or developer-visible name.** Verb names, file names, identifiers, wire formats.

**The rigor level, and waiving any finding.** L1 is the default and needs no approval; L2 and L3 need his explicit yes for that run.

**Committing, pushing, or opening a pull request.** Never do any of these without his word for that act.

### Yours — do it and report it

**The Middle Layer.** Everything wiring the Top Layer to the OS Abstraction Layer: the logic inside the service, mapping outcomes into results, hooking work onto machinery that already exists.

**How any named thing is internally coded.** A loop, a helper, a private name. Cheaply changed, no ripple.

**Recordkeeping.** Filing a GitHub issue, creating its work folder, moving that folder between `backlog`, `inprocess` and `completed`, writing the records. These never need his approval, and file the issue the moment work is deferred rather than noting it.

**Running the stages in order** and doing all the work inside an approved unit without stopping to re-ask.

### Rules that bind you regardless

Approval of a unit of work approves all of it. Never split it into stages he did not ask for, stop at a checkpoint he did not ask for, or cut scope without asking a direct question naming exactly what would be cut.

Silence, a topic change, or a request to reword or clarify is never approval. But equally — when he designs by talking, the conversation *is* the ruling, and a record is where you write down what he decided rather than a second gate where you ask again. If you genuinely think something is unruled, say so in the turn it comes up, in one line. Running that judgement silently and revealing it later in a document is the failure that cost the most time in this work so far.

Anchor approval to his actual words, never to "it is already in the code."

Never reach for an exemption, a suppression, or a reclassification to escape a rule that already answers a question. When a rule blocks the clean design, stop and bring him the exception; never reshape the code to slip past the analyzer.

---

## 5. What must be resolved before the contract locks

Four things, and the first blocks outright.

### 5.1 What goes inside the three autostart entries

The autostart entry is what makes appd start at login: a LaunchAgent property list on macOS, a systemd user unit on Linux, a Task Scheduler task on Windows. appd never reads it — the operating system does.

The names and locations are decided and are in `conversation.md`. The contents are not, and this is the on-disk layout of something written onto a user's machine, which is his by the guide. No contract advances with this unanswered.

Five capabilities such a definition can express. Four work on all three platforms: starting at login, running as that user with desktop access, restarting on failure with a delay, and refusing a second copy. Two do not travel. Only Linux and Windows can cap the restart retries — launchd will respawn forever, throttled to one attempt every ten seconds. And **Windows cannot redirect the program's output to a file at all**; a scheduled task's action is an executable and arguments with no output field, and the only workaround is wrapping the command in a shell, which this design's own rules ban.

Two recommendations follow, both awaiting his word. Have appd write its own log file rather than using any service manager's redirection, so all three behave identically and nothing is platform-specific. And restart on failure only, with a delay, capped where the platform has a cap, accepting that macOS keeps trying.

The Linux question stated at the level he can answer: does appd start when the user logs in at all, or only once a desktop is up? Only-with-a-desktop never starts on desktops that lack systemd session integration — sway and i3 among them — and it fails silently. The recommendation is: at login, always.

**None of this was verified against live documentation.** Both research tools were exhausted in the previous session — Perplexity returned a quota error and the web search budget was spent. The Windows output-redirection claim in particular should be confirmed before it goes into the contract.

### 5.2 Which of three shapes the Top Layer takes

Five operations are in play: is appd running, ensure appd is running, what state is the autostart entry in, write it, remove it.

*All five in one service.* One place to look for anything about appd. The cost is that the first two are about a process running right now and the last three are about a file on disk — one interface answering two different questions.

*Two services.* One for the running process, one for the autostart entry. Each has one reason to change. The cost is that ensuring it runs cannot report why it failed without asking the other whether the entry exists, so they are coupled anyway and most callers hold both.

*Two operations, autostart entry stays with setup.* The service is only the two runtime operations; writing, checking and removing the entry stay in the existing setup-condition machinery that `install` and `doctor --fix` already drive. Smallest new surface. The cost is that the dashboard reaches setup rather than the daemon service to show entry state.

### 5.3 How the per-OS pieces group on the platform container

The platform container has two members today: the filesystem, and presence. This work adds three per-OS things — the lock, asking the service manager to start, and the autostart entry.

*Three siblings* takes the container to five flat members, matches exactly what is there, and adds no type.

*One member holding three* takes it to three members with the daemon's three beneath, adds one interface, and buys container-level coverage on the grouping as well as leaf coverage on each part.

The real question is whether the platform container is a flat list of capabilities or a list of subsystems. Presence was one thing so it never forced the choice.

### 5.4 The sixteen guardrail rules

Each needs his sign-off before RULE-PHASE writes it. They are in `design-output.json` under the verdict's rule list. They are three kinds: table entries giving a banned primitive an owner so the raw call is legal in exactly one class, new checks that make a specific shortcut unwritable, and three ordinary tests rather than analyzers.

---

## 6. Lingering questions

`design-challenges.md` holds three things the next design round must work out: who owns reading an embedded resource and what that interface looks like, how the lock answers "is it held" on POSIX without taking it — where his double-checked lock is the recorded answer — and whether writing the autostart entry is enough to start it in a session that is already running, which nobody has run on any platform.

Two items came out of GROUND that were never chased. Three analyzer doc comments assert a type "does not exist yet" when the concrete types now do. And the CLI's runtime-identifier threading was flagged as possibly inert, which matters because of what that threading exists to prevent.

Four issues were filed during this work and are not part of #63: **#71** the client and daemon supported-version window, **#72** the auto-resolved record contract, **#73** the hidden-decision scan's stage placement, **#74** evaluating every static class in `src`.

---

## 7. Visual status

He needs to see where things stand without reading prose, and the standard is higher than a hand-written page.

A page was published during this work at `https://claude.ai/code/artifact/1af2dbbf-556f-4d71-aefb-5a4653403ef4` showing the layer stack, the two processes, and the three candidate shapes. He called it a good start and not good enough, and the reasons are worth understanding before building the next one.

It was hand-authored from what an agent happened to remember, so it is a snapshot that goes stale the moment anything moves. It did not show position in the pipeline. It did not separate what is waiting on him from what is running from what is done. And it was a one-off rather than something regenerated whenever state changes.

What to build instead: a status surface **derived from the actual state** — the git tree, the work folder, the issue list, the recorded stage outputs — that leads with what is waiting on him, shows where the run sits in the eleven stages, and is regenerated rather than remembered. Issue #65 states the standard he wants in his own words: it populates itself from every build, it is not something an agent updates or remembers to mention, and every value carries its age and whether it still means anything, because a green result from four hours and six commits ago is not evidence.

---

## 8. Standing constraints

Never push. Never commit, stage, or open a pull request without his word for that act.

Ask before anything over ten minutes, with the estimate.

Bringing a decision means showing the full current text and the full replacement text, never a summary. What he approves goes in word for word.

Never write his raw words into a spec, document, commit, issue or agent instruction. Clean it, show him, get approval, then write.

Never put an exclusion in an agent's instruction unless it quotes a Decision from the contract.

Nothing goes in the root of `.dev/inprocess/` — work folders only.

Move the work folder from `inprocess` to `completed/run-records/` inside the shipping pull request, before merge, not after.
