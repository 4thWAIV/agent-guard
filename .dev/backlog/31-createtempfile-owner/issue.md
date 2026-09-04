# 31 — Add IFileWriter.CreateTempFile behind an owner when a real caller lands

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/31

---

Deferred from the test-system contract (`.dev/inprocess/2026-08-11-lockdown-and-coverage/test-system-contract.md`).

**Why deferred:** unlike `Directory.CreateTempSubdirectory` (atomic, uniquely-named, cross-platform), .NET 10 has no atomic *prefixed temp-file* primitive. `Path.GetTempFileName()` ignores a prefix and is capped at 65535 names on Windows; a hand-rolled compose (temp root + `GetRandomFileName` + create) must be made atomic (`FileMode.CreateNew` in a retry loop) or it reintroduces the not-reliably-unique-under-a-constant-fake bug. The member has no consumer yet, so we are not shipping an unvalidated member with a bad default.

**When to add:** when a real caller exists. Then decide the atomic approach and add `IFileWriter.CreateTempFile(prefix)` behind the `IFileWriter` owner (the file-write/creation owner), faked in-memory, with the raw primitive exempted only in that owner.

Decided with Tim 2026-08-20: defer + file this issue.