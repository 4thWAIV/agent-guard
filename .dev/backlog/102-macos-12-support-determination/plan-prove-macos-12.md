# Plan: settle whether AgentGuard can support macOS 12

Written 2026-11-01 on Tim's instruction to produce a definite determination, with the tests and the evidence that decide it either way. Revised the same day on his instruction that no money goes on hardware and that a member can provide a macOS 12 machine, so the live proof arrives as a package they download and run. Revised again when he confirmed the machine is an Intel i7 Mac with no fingerprint reader, and that another form of identity validation is acceptable. This is the design input for a contract, not a contract. The rigor level is Tim's call.

The investigation in `investigation.md` established that nothing in our own build raises the floor, that the .NET 10 host and runtime declare macOS 12.0 and are compiled against that deployment target with the availability warning as an error, and that our own macOS calls reach nothing above macOS 10.11. What is missing is proof. No build has ever been started on macOS 12, 13 or 14, and the one written claim that an older macOS fails rests on prose with no captured error text.

## What this route can and cannot prove

A package run by a member on their own Monterey machine can produce every piece of evidence the determination needs except one: repeatability. One run proves one build on one machine on one date. It cannot keep proving it.

CI cannot close that gap by running macOS 12, because GitHub's hosted macOS runners stop at `macos-14` and that image is already deprecated. What CI can do is hold the floor as an invariant and fail the moment anything the live proof depended on changes. That is the division this plan uses: the member's run establishes the claim, and CI keeps it from going stale silently.

The claim that results is therefore "proven on this build, on this date, on this machine, with CI failing if the evidence stops applying" — not "tested every build". That wording belongs in whatever we publish, rather than being discovered later.

## The bar, fixed before anything is run

**Supportable** means all five of these hold.

1. The published single file, signed exactly as CI signs it, starts on macOS 12 and prints the stamped version from `guard version` with exit code zero, on every architecture the member's machine can run.
2. Every non-weak dynamic library the binary requires, and every symbol it imports, is present on macOS 12.
3. The presence check reaches the operating system's own dialog on macOS 12 and distinguishes the three outcomes that machine can produce: a granted authentication, a dismissed prompt, and a failed challenge. The fourth outcome, the check being unavailable, cannot be produced on that machine and stays covered by the mapper test.
4. File and configuration protection behave on macOS 12 as they do on a supported version.
5. CI enforces, on every build, that the shipped binary still matches the evidence the live run was taken against.

**Unsupportable** means any one of these holds.

1. A required library or an imported symbol is absent on macOS 12.
2. The operating system refuses to launch the binary as CI signs it, and no signing change we are willing to make fixes it.
3. The presence check cannot complete on macOS 12.
4. The static invariants cannot be expressed as a check, so a pass could go stale without anyone noticing.

The machine is Intel, so `osx-x64` is exercised natively and there is no Rosetta to run the other binary under. `osx-arm64` on macOS 12 therefore rests on the static check alone, and is reported that way rather than generalised from the Intel result.

## Stage one: decide it statically, before anyone downloads anything

This stage can return "unsupportable" in an afternoon with no second machine, so it runs first.

Work: publish both macOS RIDs, list every non-weak `LC_LOAD_DYLIB` entry and every undefined symbol in each published single file, and check each against what macOS 12 declares it exports. The known shape is twelve required libraries, among them `CryptoKit`, `libswiftCore` and `libswiftFoundation`, and twenty-nine mangled Swift CryptoKit symbols covering AES-GCM and ChaCha20-Poly1305, all arriving through `libSystem.Security.Cryptography.Native.Apple`, which is linked into the single file.

The check uses the installed macOS 26.5 SDK with the deployment target set to 12.0, which is how availability is meant to be asked and is exactly what dotnet/runtime does to guarantee its own floor. The one item to prove out rather than assume is whether the Swift interface files in the current SDK carry availability back to 12.0 for the CryptoKit symbols; if they do not, the fallback is the macOS 12 SDK from Xcode 13, or the dynamic linker shared cache read out of Apple's 12.7.6 installer without booting it.

Evidence to keep: one table listing every required library and every imported symbol, each marked present or absent at macOS 12, naming the source consulted for each.

Stop condition: one missing non-weak symbol ends the question. Report unsupportable, with the symbol named, and nothing is sent to anyone.

## Stage two, run alongside stage one: explain the Sonoma story

The only recorded evidence that an older macOS ever rejected our binaries is the claim that .NET 10 patches after the GA build were killed on macOS 14.6.1. Two candidate mechanisms survive and neither is proven. The first is that our `codesign --options runtime` step writes the hardened-runtime version taken from the macOS SDK the host was linked against, so the current build declares 15.5.0 where the GA build declared 14.5.0. The second is that `dotnet-install.sh` overwrites an already-run signed `dotnet` in place, which leaves the operating system holding a stale signature and kills every later launch with signal 9, a known upstream defect whose symptom cannot be told apart from an integrity kill.

The second is testable today on the current machine at no cost: install .NET with the script over an installation that has already been run, then launch `dotnet`. If it dies with signal 9, the recorded reason for the SDK pin is explained and older macOS loses its only evidence against it.

Evidence to keep: the command sequence, the exit status, the console text, and the matching lines from `log show` filtered to the integrity daemon.

## Stage three: the package the member downloads

**It is built on the tester flow that already exists, not a new channel.** Every release already attaches the binary per RID, its checksum, its cosign bundle, our public certificate, the trust script that adds that certificate as a code-signing anchor in the user's login keychain only, and the instructions in `docs/running-alpha-builds.md`. The member follows that path, so what gets validated is the real shipped artifact rather than a special build made for the occasion.

Three things are added to it.

**A probe executable**, self-contained, one per macOS RID, that drives the production code paths a Monterey machine is needed for: the presence check through LocalAuthentication, file protection, and configuration protection. It is needed because the acceptance suite runs through VSTest and therefore needs the .NET SDK installed, and putting a .NET 10 SDK on Monterey is both unsupported and the suspected cause of the original Sonoma failure — the exact variable this exercise exists to remove. The probe is a new executable shipped to an outside tester and needs Tim's approval before it is written.

**A collection script** that runs everything in a fixed order and captures the evidence rather than a verdict: the machine's OS version, model and architecture; whether Rosetta is present; the checksum and cosign verification of the binary actually run; the full signature dump and the Gatekeeper assessment; the exit code and output of `guard version`; the dynamic linker's library-loading trace; then the probe, with its report. On any failure it also captures the console text, the crash report, and the system log for the launch window narrowed to the integrity daemon and the kernel.

**A short set of instructions** covering what is collected and why, so the member can read the archive before sending it back. The log capture is narrowed by predicate and time window because it otherwise picks up unrelated processes from their machine.

**Two confounds the script has to separate, or a failure will be ambiguous.** Gatekeeper refusing an un-notarized download looks like an incompatibility: the script runs the trust step first, records the quarantine attribute and the Gatekeeper assessment, and repeats the launch on a copy with the quarantine attribute removed. And the architecture matters: an Apple Silicon Mac on Monterey exercises `osx-arm64` natively and `osx-x64` under Rosetta 2, covering both, while an Intel Mac on Monterey covers `osx-x64` only.

## Stage four: what comes back, and what it is worth

The member returns one archive. It is read for the five bar items above, and the raw tool output is kept in the work folder as the evidence behind whatever claim we make.

**The honest weakness, written into the claim rather than left out.** This is self-reported evidence from a machine we do not control, for a product whose purpose is to detect tampering. The mitigations are that the script verifies the binary it ran against the published checksum and the cosign bundle before running it, and that the report carries raw tool output rather than a pass or fail summary, so the result can be read rather than taken. What cannot be mitigated is that we did not watch it happen. A gate run on a machine we control would be stronger, and this is weaker than that.

## Stage five: the CI tripwire that keeps a pass meaningful

Four checks, all runnable on the current hosted macOS runner, none needing a Monterey machine.

1. Every shipped macOS Mach-O declares a minimum version at or below 12.0. This is what fails the day a .NET patch raises it, and .NET 11 will raise it to 14.0.
2. The set of non-weak dynamic libraries the binary requires matches a recorded baseline. A new dependency fails the build rather than shipping unproven.
3. The set of imported symbols matches a recorded baseline, for the same reason.
4. Every symbol in that baseline is declared available at macOS 12.0 by the SDK with the deployment target set to 12.0 — the same check as stage one, kept running.

Two existing gaps are closed in the same place, both recorded on issue 100. The macOS runner is the floating `macos-latest` label in both the build and the release job, so the version we test on moves whenever GitHub repoints it; it gets pinned. And the published `osx-x64` single file is never executed anywhere in CI, so the gate launches both macOS binaries rather than only the arm64 one.

## What a pass obligates anyway

The floor has to be declared in one place in the build and refused below it with a message that names the required version, which is the work already on issue 100. And the claim expires at .NET 11, whose host moves its deployment target to macOS 14 with the stated consequence that customers on macOS 12 can no longer run .NET. A macOS 12 claim is good until the next major upgrade and no further, and that belongs beside the claim.

## Order

Stages one and two run first and together, because they need nothing from anyone and either can end the question. Stage three is built only if stage one passes. Stage five is worth deciding before the package goes out rather than after, because a determination nobody can keep is not worth the trouble of collecting it.

## Identity validation on a machine with no fingerprint reader

No code change is needed, and no substitute mechanism has to be built. The check already evaluates `LAPolicy.DeviceOwnerAuthentication`, which Apple defines as Touch ID or the account password, under the decision `macos-biometric-or-password`. On a Mac with no fingerprint reader the operating system presents the account-password dialog, which is the other form of identity validation Tim accepted.

What the member's run produces, with the raw `LAError` code recorded rather than only the mapped reason:

- the account password accepted, which maps to approved;
- the dialog dismissed, which maps to cancelled;
- a wrong password until the operating system reports the challenge failed, which maps to denied.

The unavailable outcome is not reachable there. It needs an account with no password set, which is not something to ask a member to do, so it stays covered by the mapper test against the fake runtime.

**This run is also the first test of the no-fingerprint path on any operating system, which is now issue 101.** The capability probe runs before any prompt and returns without prompting when it fails, and the one test that touches the real runtime accepts either answer from that probe by design. The CI runners have no fingerprint reader and the development machine has one, so nothing has ever asserted that a Mac without biometrics reaches the prompt at all. If macOS answers the probe with the biometry-not-available code on such a machine, the check reports unavailable and never prompts, which would make the guard decline to confirm a human on most desktop Macs. The member's machine settles that, and the two questions share one package.

## Decisions needed from Tim

1. Approval for the probe executable: a new self-contained binary for `osx-x64`, shipped to an outside tester alongside the release assets.
2. What the published claim is allowed to say, given the evidence is self-reported from a machine we do not control.

Answered already: the machine is an Intel i7 Mac with no fingerprint reader, so the scope is `osx-x64` live and `osx-arm64` on the static check alone, and the account password is the accepted identity validation.
