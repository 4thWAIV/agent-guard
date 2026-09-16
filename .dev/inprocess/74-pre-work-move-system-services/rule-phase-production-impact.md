# RULE-PHASE production impact — one expected AG0015 failure

Recorded separately from the rule-phase findings, at Tim's direction. This is impact, not a defect, and clearing it is IMPLEMENT work.

## The diagnostic

`src/AgentGuard.Boundaries/SystemServices.cs(88,30)`: **AG0015** — "Clock access 'TimeProvider.System' bypasses the injected clock; read time off ISystemServices.Clock — a raw wall-clock read is banned everywhere and a direct TimeProvider acquisition only at the composition point".

`make build` exits 2 with exactly this one error and no warnings.

## Why it is expected

The contract retargets `CompositionPoint`'s clock construction-site identity from `AgentGuard.Boundaries` to `AgentGuard.Engine`. The identity has moved; the file has not. `SystemServices.cs` still sits in Boundaries and still reads `TimeProvider.System` at line 88, so the site no longer matches and AG0015 reports it.

## What clears it

The IMPLEMENT stage moving `src/AgentGuard.Boundaries/SystemServices.cs` to `src/AgentGuard.Engine/SystemServices.cs` in namespace `AgentGuard.Engine`. No other change is needed and none is authorized here.

## What was not done

Nothing was suppressed, exempted, `NoWarn`-ed, lowered in severity, or edited in production to hide it. The pre-change baseline of the same command was green — "Build succeeded. 0 Warning(s) 0 Error(s)", exit 0 — so the diagnostic is entirely attributable to the new rules.

## The rest of the tree

No other production diagnostic exists. The solution build stops at Boundaries, so the gated consumer compilations were measured separately, with project references off and no severity override: `src/AgentGuard.Cli` (`guard`), `tests/AgentGuard.TestHelpers` and `tests/AgentGuard.Tests` each build with zero errors. `AgentGuard.Engine` itself built successfully inside the failing solution build, so AG0040 and the AG0023 and AG0029 Engine gates report nothing on the current tree. AG0041 cannot fire yet at all, because `src/AgentGuard.Engine/AgentGuard.Engine.csproj` still grants internal access only to `AgentGuard.Tests`.
