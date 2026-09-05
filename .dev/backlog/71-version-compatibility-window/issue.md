> The live GitHub issue supersedes this file. Read https://github.com/4thWAIV/agent-guard/issues/71 for the authoritative text.

# 71 — Client and daemon version compatibility — a supported-version window

## Why

The CLI and appd are the same binary in two modes, but nothing makes them the same *version* on a given machine. An upgrade installs a new binary while the old daemon is still running its login session. A login registration written months ago can point at an older release. As the channel and the request format evolve, an old client talking to a new daemon will not fail cleanly — it will half-work, which is worse.

We need a supported-version window: a stated range the two sides will talk across, and a clean refusal outside it.

## This is not the identity ladder

The three-rung ladder in #66 answers "is this our product," and it deliberately accepts an older release that is genuinely signed as the same product, because that is how upgrades work. That is correct and stays correct. Whether a version is one we still speak to is a different question with a different answer, and it needs its own mechanism.

## The version scheme is already decided — do not re-open it

SemVer is `MAJOR.MINOR.(Day×43200 + Time)[-pre-release]+<git-hash>`, settled and built in the CI/build/sign/release work. The third number combines day and time so every build sorts higher than the last; it is a build stamp and carries no compatibility meaning. The window is therefore expressed over `MAJOR.MINOR` only.

The CLI already reports all three versions through `guard version`, and `IBuildInfo` exposes `SemVer`, `AssemblyVersion`, and `FileVersion`.

## What this has to settle

**How wide the window is.** How many minor versions back a daemon will serve, and whether a major difference is ever served at all.

**Which side enforces it, and when.** The daemon refusing an old client, the client refusing an old daemon, or both — and whether that happens at connect or per request.

**Where the window is stated.** Compiled into the binary, or published somewhere both sides can read. A published list allows retiring a release with a known weakness without shipping a new binary; a compiled-in range needs no network and cannot be tampered with. This is a real trade-off, not an obvious pick.

**What the user sees when it fails.** A refusal has to say which side is out of date and what to run, and `guard doctor` should be able to diagnose and repair it, since a stale login registration pointing at an old release is install wiring.

## Done when

On all three platforms, a client outside the window is refused rather than half-served, the refusal names which side is stale and what to do about it, `guard doctor` reports the same thing, and the window's definition lives in exactly one place.

