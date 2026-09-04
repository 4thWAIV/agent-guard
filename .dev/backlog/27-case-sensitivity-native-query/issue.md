# 27 — IsCaseSensitive: add the native query (pathconf / GetFileInformationByHandleEx) — currently managed-probe only

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/27

---

The three-layer case-sensitivity probe (contract decision `info-construction`/case-sensitivity, lockdown) is implemented **managed-probe only**: layer 2 (the read-only fallback probe — pick a directory entry with no case-variant sibling, flip its case, confirm the same entry by identity) plus the layer-3 documented default. The **layer-1 native query is deferred**: macOS `pathconf(path, _PC_CASE_SENSITIVE)` and Windows `GetFileInformationByHandleEx` with `FileCaseSensitiveInfo`.

Correctness is unaffected — the managed probe returns the right answer; the native query is the fast/authoritative path. Implement it in `PosixFileSystem`/`WindowsFileSystem` (the AG0101 owners), **verifying the P/Invoke signatures against the OS headers rather than guessing**. Deferred during the CLR-primitive lockdown IMPLEMENT to avoid guessing at interop.