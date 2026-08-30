# Spike result — Option B: net10.0-macos + first-party LocalAuthentication (2026-08-23)

**VERDICT: VIABLE.** Once Xcode 26.6 (Universal) is installed and `xcode-select` points at it, `net10.0-macos` builds and RUNS on this Intel Tahoe. This reverses the earlier "B ruled out" — that was only because Xcode wouldn't install; the Universal build fixed that.

## Proven (Intel Mac, macOS 26.6.2, Xcode 26.6, macOS workload 26.5, SDK 10.0.400)
- Builds 0 warnings / 0 errors. The 26.5 workload tolerated Xcode 26.6 (no version-mismatch error). One requirement: `<ApplicationId>` (a bundle id) — every `net10.0-macos` app needs one.
- The `net10.0-macos` osx-x64 binary LAUNCHES on Intel Tahoe: `net10.0-macos binary LAUNCHED. OS=macOS 26.6.2, Arch=X64`, exit 0 — NO AMFI kill. This was B's make-or-break unknown; it passes.
- The first-party LocalAuthentication binding works cleanly: `new LAContext().CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics / DeviceOwnerAuthentication, out NSError)` returned `True`/`True`, no error — idiomatic managed C#, no `objc_msgSend`.

## Cost of B (both options are now proven — this is the tradeoff)
- Requires the macOS workload + Xcode 26 on EVERY build machine (Tim's dev box AND CI). GitHub macOS runners ship Xcode, but it adds a workload-install step and conditional-TFM handling to CI.
- Requires the conditional `net10.0-macos` target framework in `Directory.Build.props` (`net10.0-macos` on the mac leg, `net10.0` elsewhere) — decision-2's "platform mess" concern, centralized but real.
- The `net10.0-macos` build produces a **.app BUNDLE** (`spike.app`), not a plain single-file executable. The guard is a CLI; on macOS it would ship as a `.app` while Windows/Linux ship a plain executable — an inconsistent, awkward packaging change for a CLI.
- Only cleans up macOS. Windows (Hello) and Linux (PAM) are P/Invoke regardless, so B mixes two mechanisms.

## Option A (flat net10.0 + P/Invoke) — the alternative, also proven (see spike-objc-msgsend.md)
Uniform flat-`net10.0` on all three OS; no Xcode/workload anywhere; macOS = ~4 `objc_msgSend` P/Invoke declarations (proven clean); the guard stays a plain single-file executable everywhere.

Both launch and call LocalAuthentication on Intel Tahoe — a genuine choice, now Tim's to make.
