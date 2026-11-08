# Where the macOS floor research lives

The investigation into what sets the macOS floor, and the plan to settle macOS 12 with evidence, moved to
`.dev/backlog/102-macos-12-support-determination/` when that work got its own issue:

- `investigation.md` — what in this repository's build and published output sets the floor, what the research
  found upstream, and why the recorded reason for the SDK pin does not hold up.
- `plan-prove-macos-12.md` — the pass and fail bar, the stages, the member-run validation package, and the CI
  invariants that keep a pass from going stale.

This issue keeps the work that follows whatever floor is settled on: declaring it in one place in the build,
refusing to start below it with a message that names the required version, pinning the macOS runner, and
launching both macOS binaries in the gate.
