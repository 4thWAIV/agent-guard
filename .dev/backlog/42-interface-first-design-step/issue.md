# 42 — Design step: minimize static classes/functions; most functionality behind an interface; surface the interface structure for review

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/42

---

New process step Tim wants for the design stage.

Principle: for new classes/items, minimize static classes and static methods, and put most functionality behind an interface — so the interface set IS the visible structure of the system, reviewable without reading class internals. (Service-holding statics are already banned by AG0024; utility/logic statics are widespread today: ~30 static classes, ~264 static methods in src.)

Process: at DESIGN, surface the NEW/EXPANDED product interfaces for the human to review the architecture (eventually a visualization; for now, the C# interfaces). Consider an analyzer that discourages new static classes outside a narrow whitelist (consts, pure stateless mappers).

Applies going forward; being applied manually to the presence contract now.