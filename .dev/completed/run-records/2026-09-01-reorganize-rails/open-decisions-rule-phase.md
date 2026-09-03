# Resolved decisions: RULE-PHASE behavior and diagnostic ID source

## `rule-phase-green-proof`

At the pre-worker checkpoint, `rule-phase.js` said:

> “Wire each rule in EVEN WHERE existing code violates it. It is SUPPOSED to fail the build (RED) against the current violations — that red is the forcing function for the later IMPLEMENT worker.”

Tim said:

> “WHEN the RULES are put in place we are GREEN (nothing has broken anything).  AND the new rules should not be covering anything yet and THE TDD can be writen to comply (so RULE is not forced to be RED).  HOWEVER, we can clarify that the HUMAN can allow rules to be set to warning for builds SHOULD IT be needed depending on the structure of what is being worked.”

### Approved decision

RULE-PHASE installs every approved rule before ARCHITECTURE and TDD. It proves each rule with a violating fixture that produces the diagnostic and a compliant fixture that does not. It does not require production code to violate a new rule or require the production build to be RED.

The original stop-on-conflict handoff in this decision is superseded by `rule-phase-red-handoff`:

- Violating and compliant fixtures determine whether each rule works.
- Production RED is recorded as the rule's effect on existing code. Production RED is not proof and does not fail RULE-PHASE.
- If production is RED, RULE-PHASE reports every responsible diagnostic ID and production location.
- Before ARCHITECTURE begins, Tim approves the exact diagnostic IDs and intermediate builds where those diagnostics may remain warnings.
- `architecture.js` and `tdd.js` receive the same approved `ruleWarningIds` list and translate it into `WarningsNotAsErrors`.
- The warning list contains only IDs Tim approved for that run.
- GATE never receives the override and must be green normally.

Tim:

> “YES, but is it posible you CAN LOOK over all of this first rather than starting and stoping every turn on a new question that PREEXISTED from the data we had rather than START and STOP and only THEN raise the FUCKING question this is takeing forever!”

Tim:

> ## rule-phase-red-handoff
>
> Approved.

## `current-analyzer-id-source`

At the pre-worker checkpoint, `rule-phase.js` said:

> “following the existing AG0001–AG0007 analyzers exactly”

and:

> “Use the next free ids (AG0008, AG0009, …).”

The live release tracking already contains:

```text
AG0116  | AgentGuard.Architecture | Warning | NoStateInNativeOpsAnalyzer
AGS5443 | AgentGuard.Architecture | Warning | TempRootBackDoorAnalyzer
```

At that checkpoint, the reorganization contract did not yet authorize changing analyzer ID allocation instructions. The approved decision below supplied that authorization and is now recorded in `contract.md`.

### Approved decision

Replace the stale hardcoded range and examples with:

> “Follow the current analyzer implementations. Before choosing a `DiagnosticId`, read both `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md` and use the next free, never-used ID. Register the new ID in `AnalyzerReleases.Unshipped.md`.”

Tim:

> “YES, but is it posible you CAN LOOK over all of this first rather than starting and stoping every turn on a new question that PREEXISTED from the data we had rather than START and STOP and only THEN raise the FUCKING question this is takeing forever!”
