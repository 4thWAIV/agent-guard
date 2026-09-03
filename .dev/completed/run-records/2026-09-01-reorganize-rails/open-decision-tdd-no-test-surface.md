# Resolved decision: TDD when the contract authorizes no test files

## Existing requirements

`rails-run-a-workflow` says:

> “TDD | required”

The reorganization contract says:

> “Do not add a tenth rail, rename a rail slug, add a workflow, add a dependency, or change product code, tests, analyzers, build scripts, CI, or release behavior.”

The contract lists no test file in `Surfaces`.

At the pre-worker checkpoint, `tdd.js` rejected the result when the test author correctly returned no test files:

```js
if (!testGen || !testGen.testFiles || !testGen.testFiles.length) {
  return { testFiles: [], error: 'test author produced no test files', testGen }
}
```

## Approved decision

Keep TDD in the fixed stage order. When the approved contract names no test surface and forbids test changes, the TDD author returns the existing fields as an explicit no-op:

```js
{
  testFiles: [],
  redProof: 'Not applicable — the approved contract authorizes no test files.',
  coverage: [],
  notes: '<exact contract evidence>'
}
```

`tdd.js` accepts that empty result only for this contract-backed case and still sends it to the test-quality, DRY, and Lie-catcher panel. The panel returns FAIL if the contract requires any test or if the no-test claim lacks exact contract evidence.

For every contract that authorizes a test surface, the existing RED-test requirement and empty-result rejection remain unchanged.

Tim:

> “YES”
