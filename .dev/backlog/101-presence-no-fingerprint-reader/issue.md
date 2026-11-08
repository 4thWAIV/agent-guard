The macOS presence check evaluates `LAPolicy.DeviceOwnerAuthentication`, which Apple defines as Touch ID or the account password, so a Mac with no fingerprint reader is meant to get the password dialog. Nothing in the repository proves that it does.

## Why it is unproven

`src/AgentGuard.CrossPlatform.MacOS/Presence/LocalAuthentication.cs` runs the non-interactive capability probe first and returns without prompting when it fails:

```csharp
if (!_objc.CanEvaluate(context, DeviceOwnerAuthentication, out IntPtr probeError))
{
    LaResult unavailable = MapErrorCode(_objc.GetErrorCode(probeError));
    ReleaseHandle(context);
    return Task.FromResult(unavailable);
}
```

`MapErrorCode` turns `LAErrorBiometryNotAvailable` into `LaResult.NotAvailable`, and `MacOsPresenceCheck.Map` turns that into `ApprovalReason.Unavailable`.

The one test that touches the real runtime, `RealObjCRuntime_RunsTheNonInteractiveOps_WithoutShowingAPrompt` in `tests/AgentGuard.CrossPlatform.Tests/Presence/MacOS/MacOsObjCRuntimeSpecTests.cs:32`, accepts either answer from the probe by design, on the grounds that both are machine-independent. So no test asserts that the probe succeeds, and no test asserts that a machine without biometrics reaches the prompt at all.

The CI runners have no fingerprint reader either, and the development machine has one, so the no-biometrics path has never been exercised on any operating system version.

## The failure it would hide

If macOS answers the probe with `LAErrorBiometryNotAvailable` on a Mac with no fingerprint reader, the check reports `Unavailable` and never prompts. The guard would then decline to confirm a human on most desktop Macs, and the behaviour would read as a configuration problem rather than a defect.

## What needs to happen

1. Exercise the presence check on a Mac with no fingerprint reader and record what the capability probe returns, with the raw `LAError` code rather than the mapped reason.
2. Assert the expected outcome in a pointed-integration test rather than accepting either answer, with the machine's biometric capability as the input that selects the expected result.
3. If the probe does report the policy unavailable on such a machine, decide what the check should do instead, since reporting `Unavailable` makes the guard unusable there.

A member's macOS 12 Intel machine with no fingerprint reader is being prepared to validate the macOS floor (issue 100) and is the first opportunity to settle this; the two runs share one package.
