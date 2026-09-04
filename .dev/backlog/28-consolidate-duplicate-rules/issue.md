# 28 — Rule cleanup (deferred from FileInfo/IFileSystem bridge): consolidate one-door (AG0023+AG0029) and OS-confined (AG0008+AG0009)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/28

---

Deferred from the FileInfo/DirectoryInfo + IFileSystem bridge. During that bridge the analyzer set is reorganized by pattern; pieces 1 (owner fold, 8→1) and 2 (construction fold + wrapper lock) are done IN the bridge because they overlap the wrapper work. Two independent consolidations were deferred to keep them off the bridge's critical path (Tim agreed, 2026-08-13):

**Piece 3 — one-door rule.** Fold AG0023 (Boundaries → CrossPlatform-core, door = the CrossPlatformAdapters factory) and AG0029 (Boundaries → a per-OS assembly, door = Platform.Create) into one table-driven 'a cross-assembly call is legal only through the one designated door' rule.

**Piece 4 — OS-confined rule.** Fold AG0008 (native interop — LibraryImport/DllImport and its call site) and AG0009 (OS branching — OperatingSystem.IsWindows, RuntimeInformation.IsOSPlatform, platform #if) into one 'OS-specific machinery lives only in the CrossPlatform assemblies' rule.

**Constraints:** both are pure reorganization of *working* rules, so RED-first — every case that fires today must still fire, proven — with the SOLID/DRY/lie-catcher panel clean before any code (L1). Keep the lowest folded id (AG0023, AG0008), retire the other; update AnalyzerReleases and re-point the tests.

**Also:** to be written into the main lockdown contract's remaining scope when the bridge folds back, so it is tracked in two places.