# Every round of this contract, its direction and its result

The readiness check found that the instruction given to the adversary panel enumerated eight round directions when there are nine, and claimed each round's result sat beside its direction when five did not. Both were mine. This file is the map, and the enumeration handed to any panel is built from `ls ../fixes-*.md` rather than from memory.

A round's direction says what that round was asked to do. Its result is the stage's own return value, copied without change from the run that produced it — from the completed task's output where that still exists, and otherwise from the run's journal, which is noted on the line.

| Direction | Round | Result |
|---|---|---|
| `../fixes-for-the-coverage-record.md` | test | `../tdd/result.json` |
| `../fixes-for-the-two-failed-rules.md` | rule | `../rule-phase/author-result.json`, with `../rule-phase/provenance.md` |
| `../fixes-for-the-impact-test.md` | rule | `rule-round-the-impact-test.json`, the author's result from that run's journal |
| `../fixes-for-the-implementation.md` | implementation | `implement-the-kit-targets.json` |
| `../fixes-for-the-page-saying-so.md` | test, twice | `tdd-the-page-saying-so-refused.json`, then `../tdd/result-page-says-so.json` |
| `../fixes-for-the-flaky-test.md` | test | `tdd-the-flaky-test.json` |
| `../fixes-for-the-rule-round.md` | rule | `rule-round-with-the-scan.json` |
| `../fixes-for-the-identity-test.md` | test, twice | `tdd-the-identity-refused.json`, then `../tdd/result-identity.json` |
| `../fixes-for-the-identity-build.md` | implementation | `implement-the-member-identity.json` |

Two rounds had no direction of their own, because nothing had been refused yet: the architecture stage, whose result is `../architecture-result.json`, and the first adversary panel, whose result is `refute-first-panel.json` and whose findings are written up in `../what-the-panel-found.md`.

A result named "refused" is a round whose panel returned FAIL. It is kept because what a reviewer refused, and why, is part of the record: the test round for the page saying so was refused for a case that could not fail and a record that claimed criteria were failing when they passed, and the test round for the member identity was refused because the identity spelling had not been approved by Tim. Both were fixed and both rounds were then passed.
