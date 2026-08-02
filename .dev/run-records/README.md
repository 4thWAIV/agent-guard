# Run-records

One folder per run, named `<date>-<slug>`, holding that run's `contract.md` and (when the run finishes) its worker report and reviewer verdicts.

**Location is the status — read it, don't guess:**

- **Top level** = **ACTIVE** runs. Work in progress.
- **`complete/`** = **FINISHED** runs. The work shipped.

When a run's work lands, move its folder into `complete/`. That move *is* closing the record. Anything at the top level is, by definition, still in flight.

A run whose work was superseded or abandoned is deleted, not left at the top level pretending to be active.
