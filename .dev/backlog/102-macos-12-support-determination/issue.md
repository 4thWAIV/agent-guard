Settle, with evidence, whether AgentGuard's shipped macOS binaries can be supported on macOS 12 Monterey, and record the result either way. The plan, the research behind it, and the pass and fail bar are in `.dev/backlog/102-macos-12-support-determination/`. Nothing here has been built or approved yet; Tim evaluates the plan first.

Related: issue 100 owns declaring a floor in the build and refusing to start below it, whatever floor this determination settles on. Issue 101 owns the presence check on a Mac with no fingerprint reader, which the same validation package answers.

## What the research established

Nothing in this repository raises the macOS floor. Every Mach-O in both macOS publishes reports a minimum version of 12.0, no project sets any macOS version property, and the only macOS native calls are libc `rename` and `pathconf` plus the Objective-C runtime into LocalAuthentication, whose APIs are available from macOS 10.10 and 10.11. The floor comes from dotnet/runtime, which sets `CMAKE_OSX_DEPLOYMENT_TARGET` to `12.0` on the `release/10.0` branch and compiles with `-Werror=unguarded-availability`, so the declared floor is validated rather than nominal. A consumer of the SDK cannot change it: `SupportedOSPlatformVersion` has no effect on it, the SDK refuses a host patched after the fact, and every .NET 10 patch from `10.0.0` to `10.0.14` carries the same 12.0.

So the question is not technical capability. What blocks macOS 12 is that Microsoft has never supported it on .NET 9 or 10, that no build has ever been started on it, that Apple's last Monterey security release was 2024-07-29, and that dotnet/runtime's `main` branch already moves the deployment target to macOS 14 for .NET 11.

The one written claim that an older macOS rejects our binaries does not hold up. Contract decision `pin-sdk-10.0.100-ga` attributes a failure on macOS 14.6.1 to later .NET patches being hardened against the macOS 15.5 SDK. No .NET 10 build ever declared a floor above 12.0, and neither the minimum-version nor the SDK field is load-gated for a single-file main executable: a binary patched to claim a minimum of macOS 99 and re-signed ad hoc launches cleanly. Two candidate mechanisms remain and neither is proven — our own `codesign --options runtime` step writing the hardened-runtime version taken from the SDK the host was linked against, and `dotnet-install.sh` overwriting an already-run signed `dotnet` in place, which leaves a stale signature and kills later launches with signal 9 (dotnet/install-scripts issue 232, fix open as pull request 743). No console output, crash report or log line of the original failure exists anywhere in the repository or in any reachable git ref.

## The route

No hardware is bought. A member provides a macOS 12 machine, an Intel i7 Mac with no fingerprint reader, and runs a package built on the tester flow that already exists: the per-RID binary, its checksum, its cosign bundle, the public certificate, `eng/signing/trust-cert.sh` and `docs/running-alpha-builds.md`. Added to it are a self-contained probe executable, a collection script that captures raw evidence rather than a verdict, and instructions stating what is collected.

Because the machine is Intel, `osx-x64` is exercised natively and there is no Rosetta to run the other binary under, so `osx-arm64` on macOS 12 rests on the static symbol check alone and is reported that way.

CI cannot run macOS 12, because GitHub's hosted macOS images stop at the deprecated `macos-14`. CI instead gains four invariants that fail the moment the live evidence stops applying: every shipped macOS binary declares a minimum version at or below 12.0; the set of non-weak dynamic libraries matches a recorded baseline; the set of imported symbols matches a recorded baseline; and every symbol in that baseline is declared available at macOS 12.0 by the SDK with the deployment target set to 12.0.

The resulting claim is proven on one build, on one date, on one machine, with CI failing if the evidence stops applying — not tested every build. It is also self-reported from a machine we do not control, which is a weakness for a product whose purpose is detecting tampering.

## The bar, fixed before anything runs

Supportable means all five of these hold.

1. The published single file, signed exactly as CI signs it, starts on macOS 12 and prints the stamped version from `guard version` with exit code zero.
2. Every non-weak dynamic library the binary requires, and every symbol it imports, is present on macOS 12.
3. The presence check reaches the operating system's own dialog and distinguishes the three outcomes that machine can produce: the account password accepted, the dialog dismissed, and the challenge failed.
4. File and configuration protection behave on macOS 12 as they do on a supported version.
5. CI enforces, on every build, that the shipped binary still matches the evidence the live run was taken against.

Unsupportable means any one of these holds.

1. A required library or an imported symbol is absent on macOS 12.
2. The operating system refuses to launch the binary as CI signs it, and no signing change we are willing to make fixes it.
3. The presence check cannot complete on macOS 12.
4. The static invariants cannot be expressed as a check, so a pass could go stale unnoticed.

## Stages

1. Decide it statically first, with no second machine: list every non-weak dynamic library and every imported symbol in both published single files and check each against what macOS 12 declares it exports, using the installed SDK with the deployment target set to 12.0. The risk is concentrated in twenty-nine mangled Swift CryptoKit symbols and the Swift and CryptoKit libraries the binary requires without a weak link. One missing symbol ends the question and nothing is sent to anyone.
2. Alongside it, at no cost, test whether `dotnet-install.sh` overwriting an already-run signed `dotnet` produces the signal 9 kill, which either explains the recorded reason for the SDK pin or leaves the signature theory standing.
3. Build the package, only if stage one passes.
4. Read what comes back against the bar, and keep the raw tool output as the evidence behind the claim.
5. Add the CI invariants, pin the floating `macos-latest` runner label, and launch both macOS binaries in the gate rather than only the arm64 one.

## Open decisions

1. Approval for the probe executable: a new self-contained `osx-x64` binary shipped to an outside tester alongside the release assets. It exists because the acceptance suite runs through VSTest and so needs the .NET SDK installed, and putting a .NET 10 SDK on Monterey is both unsupported and the suspected cause of the original failure.
2. What the published claim is allowed to say, given the evidence is self-reported from a machine we do not control.
3. The rigor level this work runs at.

## What a pass obligates anyway

The floor has to be declared in one place in the build and refused below it with a message naming the required version, which is issue 100. And the claim expires at .NET 11, whose host moves its deployment target to macOS 14 with the stated consequence that customers on macOS 12 can no longer run .NET, so a macOS 12 claim is good until the next major upgrade and no further.
