# Recovered opening proof

This proof is governed by contract Decision `reorganization-opening-proof-recovery`. It proves the staged reorganization snapshot plus the selected-warning delta before the implementation worker; it does not claim to predate those checkpoint edits.

## `make build`

Exit code: `0`

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:35.84
```

## `make test`

Exit code: `0`

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)

Passed!  - Failed:     0, Passed:   427, Skipped:     0, Total:   427 - AgentGuard.Analyzers.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:    56, Skipped:     0, Total:    56 - AgentGuard.CrossPlatform.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:   148, Skipped:     0, Total:   148 - AgentGuard.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11 - AgentGuard.Cli.Tests.dll (net10.0)
```

Totals: `642` passed, `0` failed, `0` skipped.

## Checkpoint

The staged revert-point fingerprint remained:

```text
a46619f180a9d7d48ea0b6ae0b029c3e00aacabebe9ae1d1f8a97ee814571c89
```
