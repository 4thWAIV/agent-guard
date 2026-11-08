# How to use this package, and what it does not authorise

## Read this repository, do not change it

The package is handed over for reading. Nothing in it authorises editing, staging, committing, pushing or building anything in `4thWAIV/agent-guard`. If something in the language needs to change for Denice's sake, that change is Tim's to approve, and it is raised with him rather than made.

## Two rules that outrank anything written here

- **Every word of the language is approved by Tim, one word at a time.** A vocabulary word that is not in the language file has not been approved, whatever a design document says about it.
- **No language feature is raised until a problem cannot be solved without it.** Where this package reports a gap, the gap is the finding. Filling it with a proposed word is the opposite of what is wanted.

## How the material is ordered

Files are numbered by weight, not by subject. `00` is the most recent and the most authoritative, because it is what Tim said last. Inside each file, entries run newest first, and each one carries its own date and a pointer to the file and line it came from.

Three states are used throughout, and they are not interchangeable:

- **approved** — Tim said yes to this exact thing, on the date given.
- **declined and withdrawn** — it was built or proposed and then taken out. The reason is recorded, because the same idea tends to come back.
- **open** — it was put to Tim and he has not answered. An open item is not a decision and must not be built on.

A fourth state appears in a few places: **recorded but not started**, meaning Tim has said the work is needed and said not to begin it.

## What to do with a disagreement

Where two sources in this repository disagree, the package says so rather than picking a winner. The live file always outranks a document about it: the language is whatever `ts/metalang/metalang.yaml` and the language files beside it actually say, and a design document describing them may be behind. `03-repository-map.md` says which of those files are committed and which exist only in the working tree.

## What is not here

- No design for Denice's user-facing model. This repository holds none.
- No definition of any of the eight controls. Tim named them; nothing defines them.
- No word in the language for most of the eight. `02-controls-against-the-language.md` reports exactly which, with the evidence.
