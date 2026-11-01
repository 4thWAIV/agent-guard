# What makes the shipped macOS binaries unable to run on older macOS

Investigation opened 2026-11-01, on Tim's instruction to find the causes and rule on whether macOS 12 is reachable. Part one below is the evidence from this repository's own build and published output. Part two, the question of whether the floor can be lowered at all, is still being researched and will be appended.

## The claim that started it, and the only words that record it

No console output, crash report or log line of the original failure exists anywhere in the repository, in any reachable git ref. Three prose statements are all there is.

- `.dev/completed/run-records/2026-08-03-ci-cd-build-sign-release/contract.md:152` — "GA `10.0.100` is the only .NET 10 that launches on the current Intel macOS 14.6.1 dev machine; `rollForward: disable` stops `global.json` from silently rolling to a patch macOS 14 kills."
- `.dev/backlog/6-dotnet10-pin-bump/issue.md:11` — "later patches are hardened against macOS SDK 15.5 and get AMFI-killed on Sonoma".
- Commit `8976948`, in a `RESUME.md` later deleted by `227814c` — "stay on 10.0.100; patches 10.0.2+ are AMFI-killed on this macOS 14.6.1 until a macOS 15 upgrade." This is the only place the failing boundary is named: runtime 10.0.0 worked, 10.0.2 and later did not.

## Nothing we declare raises the floor

Every Mach-O file in both macOS publishes reports `platform MACOS, minos 12.0, sdk 15.5`. That covers the published single file itself, the single-file host, and all fifteen native components: `createdump`, the apphost, `libclrgc`, `libclrgcexp`, `libclrjit`, `libcoreclr`, `libhostfxr`, `libhostpolicy`, `libmscordaccore`, `libmscordbi`, `libSystem.Globalization.Native`, `libSystem.IO.Compression.Native`, `libSystem.Native`, `libSystem.Net.Security.Native`, `libSystem.Security.Cryptography.Native.Apple`.

No project sets `SupportedOSPlatformVersion`, `TargetPlatformMinVersion` or any macOS version property. The declared floor is inherited from Microsoft's packs and never set by us.

## The causes

**1. The SDK pin selects packs built against the macOS 15.5 SDK.** `global.json` pins SDK `10.0.400` with `rollForward: disable`, which brings runtime and host packs `10.0.11`. The `sdk` field in `LC_BUILD_VERSION` is the only metadata difference between the pack the record says launched and the pack pinned now: runtime `10.0.0` and host `10.0.0` carry `sdk 14.5`, and `10.0.11` carries `sdk 15.5`. The recorded working and failing boundary tracks that field and not `minos`, because 14.5 is at or below the machine's 14.6.1 and 15.5 is above it.

**2. Our codesign step writes that number into the shipped signature.** `.github/workflows/ci.yml:170` signs each binary with `codesign --force --options runtime --entitlements eng/signing/agentguard.entitlements`. The hardened-runtime version in the resulting CodeDirectory is taken from the `sdk` field, so our published binary carries `Runtime Version=15.5.0`. Signing the `10.0.0` host with the same flags produces `Runtime Version=14.5.0`. This is the one place the build turns an advisory load-command number into enforced signature metadata.

**3. Single-file publishing drops Microsoft's seal.** Microsoft ships `singlefilehost` with a Developer ID Application signature, a timestamp, a team identifier and the hardened-runtime flag. Our published `guard` has an ad-hoc signature, no authority, no team identifier, and a lower CodeDirectory version. `eng/ci-verify-signatures.sh:47` runs `codesign --verify --strict`, which passes on an ad-hoc signature, so the gate cannot see the difference. The `runtime` flag only returns at the codesign step above.

**4. The binary requires the host OS's Swift runtime and CryptoKit.** The published `guard` has twelve non-weak `LC_LOAD_DYLIB` entries, among them `CryptoKit`, `/usr/lib/swift/libswiftCore.dylib` and `/usr/lib/swift/libswiftFoundation.dylib`, and imports twenty-nine mangled Swift CryptoKit symbols covering AES-GCM, ChaCha20-Poly1305 and their key and nonce types. These come from `libSystem.Security.Cryptography.Native.Apple`, statically linked into the one Mach-O. The same symbols and the same linkage appear in runtime `10.0.0`, `10.0.11` and `9.0.19`, so this does not explain the recorded split, but it is a hard launch-time dependency on the exact export set of the host system's frameworks.

**5. Nothing declares or checks a floor, and the x64 binary is never launched.** No OS version check exists in `src/`. The launch proof at `ci.yml:179` runs only the `osx-arm64` binary; the Rosetta leg at `ci.yml:106` runs `dotnet test` rather than the published file, and the release job only makes the files executable. The published `osx-x64` single file is never executed anywhere in this repository's CI.

## Measured and ruled out

- `EnableCompressionInSingleFile` and `IncludeNativeLibrariesForSelfExtract` change nothing about the floor. Publishing with both off gives an identical dependency list and identical `minos`/`sdk`; only the file size changes. `IncludeNativeLibrariesForSelfExtract` is a no-op on this publish, because the runtime is statically linked into the host and nothing self-extracts at run time. There is no self-extraction path carrying its own floor.
- No package in `Directory.Packages.props` contributes a native binary or a macOS floor to the macOS publish. `System.CommandLine` is `netstandard2.0` and pure managed.
- Our own native surface raises nothing. `rename`, `pathconf`, `objc_getClass`, `sel_registerName`, four `objc_msgSend` overloads, `dlsym`, and loading `/System/Library/Frameworks/LocalAuthentication.framework` all resolve against libraries present well below macOS 12.
- Strong-naming and cosign have no Mach-O effect. The cosign step copies, checksums and signs a detached bundle, leaving the bytes unchanged.

## Two facts found along the way that are not about the floor

- The macOS runner is not pinned. `ci.yml:56` and `ci.yml:463` both say `runs-on: macos-latest`, a floating label; the last run resolved it to the `macos-26-arm64` image. The macOS version the product is tested on moves whenever GitHub repoints the label.
- `contract.md:295` describes the Rosetta leg installing the x64 SDK with `--version 10.0.100`, while the live workflow uses `--jsonfile global.json --architecture x64`. The pin is still honoured, by a different mechanism than the contract text states.

## What cannot be settled here

There is no macOS 12, 13 or 14 machine. Everything about behavior on those versions is inference from binary metadata and from the written record. Four things need an older machine to settle.

1. Whether `Runtime Version=15.5.0` in the CodeDirectory is what AMFI rejects on macOS 14.6.1. Proving it means launching a `10.0.0`-host build and a `10.0.11`-host build on macOS 14.6.x and capturing the console text alongside `log show` filtered to `amfid`.
2. Whether the twenty-nine CryptoKit symbols and the Swift runtime exports the binary imports exist on macOS 12, 13 and 14.
3. Whether `minos 12.0` is honest at all. The binaries claim macOS 12 and the written record claims they die on 14.6.1; both cannot be right, and nothing in the repository settles it.
4. Whether the published `osx-x64` single file launches after CI's codesign on any macOS version, since nothing has ever run it.

# Part two: can macOS 12 be supported, and what would it take

Researched 2026-11-01, same instruction. The finding reverses the shape of the question.

## There is nothing in our code to fix. The floor is already macOS 12.

The minimum is set upstream, in dotnet/runtime's native build, and baked into the prebuilt host and runtime packs. On the `release/10.0` branch, `eng/native/configurecompiler.cmake` sets `CMAKE_OSX_DEPLOYMENT_TARGET` to `12.0` for macOS hosts, and the same file turns the compiler's availability warning into an error with `-Werror=unguarded-availability`. That makes `minos 12.0` a validated claim rather than a label: the .NET 10 host and runtime contain no unguarded use of any macOS API newer than 12.0.

A consumer of the SDK cannot change that number. `SupportedOSPlatformVersion` has no effect on it, measured at three different values against an identical single-file app. The `SingleFileHostSourcePath` override exists, but the SDK refuses a host patched with `vtool`, because the patch appends the build-version load command after the code-signature command. Every .NET 10 patch from `10.0.0` to the current `10.0.14` carries `minos 12.0`, on both architectures, so no older pack helps either.

Our own macOS surface reaches nothing near that floor. From the installed SDK headers, `LAContext` and both the policy probe and the interactive evaluation are available from macOS 10.10, and `LAPolicyDeviceOwnerAuthentication`, the value the presence check uses, from 10.11. The rest is libc `rename` and `pathconf` plus the Objective-C runtime entry points.

## What actually blocks macOS 12

1. **Microsoft does not support it.** macOS 12 appears in exactly one .NET support matrix in history, .NET 8's, and was dropped on 2024-09-16. It was never in .NET 9's or .NET 10's. The lifecycle policy says only that support is removed when an OS leaves mainline support, at which point testing and servicing stop. No Microsoft statement says .NET 10 fails on macOS 12; it says macOS 12 is untested. Both .NET 9 and .NET 10 ship a host whose declared floor, 12.0, is below their own lowest supported version, so the declared floor and the matrix have been out of step for two releases.
2. **Nothing has ever been run there, and proving it is awkward.** GitHub-hosted macOS runners bottom out at `macos-14`, already deprecated; the `macos-12` image was retired on 2024-12-03. Proving a macOS 12 claim needs a self-hosted Monterey runner or a local virtual machine, outside CI.
3. **Apple stopped patching Monterey.** The last macOS 12 security release was 12.7.6 on 2024-07-29, which is twenty-seven months ago. An anti-tamper guard running under an unpatched kernel is a product judgement rather than a technical one.
4. **The window closes at .NET 11.** dotnet/runtime's `main` branch already sets the deployment target to `14.0`, and the pull request that did it states that customers on macOS 12 will no longer be able to run .NET. A macOS 12 claim would hold until the next major upgrade and no further, and the floor then jumps straight to 14.0.

The lowest macOS version that is both reachable and supported is macOS 15 Sequoia: the bottom of .NET 10's matrix, and the lowest with a current GitHub-hosted runner image.

## The reason recorded for the SDK pin does not hold

Contract decision `pin-sdk-10.0.100-ga` and issue 6 both attribute the Sonoma failure to the later patches being "hardened against macOS SDK 15.5". Three measurements work against that mechanism. No .NET 10 build ever declared a floor above 12.0, which macOS 14.6.1 clears. Neither the `minos` nor the `sdk` field is load-gated for a single-file main executable: a binary patched to `minos 99.0` and re-signed ad hoc launched and exited cleanly. And `10.0.0` differs from `10.0.11` only in that `sdk` field.

Two candidate mechanisms remain, and neither is proven.

- Our own codesign step, `codesign --force --options runtime`, copies the `sdk` field into the signature as the hardened-runtime version, so our binary carries `Runtime Version=15.5.0` where a `10.0.0`-based build carries `14.5.0`. Whether macOS 14 rejects a signature declaring a newer hardened-runtime version is untested.
- The .NET install method. `dotnet-install.sh` copies over an already-run signed `dotnet` in place, which leaves macOS holding a stale cached signature and kills every later launch with signal 9, a symptom indistinguishable from an integrity kill. This is a known upstream defect, tracked in dotnet/install-scripts issue 232 with a fix still open as pull request 743, and it matches the standing local rule to install .NET here from the notarized package rather than the script. CI is unaffected because every runner starts clean.

The machine has since been upgraded, so the original failure cannot be reproduced. Issue 6's work is already done — `global.json` is pinned to SDK `10.0.400` and CI reads the pin from `global.json` rather than a hard-coded version — while the issue remains open on a reason that appears to be a misdiagnosis.

## Two gaps in the proof, beyond the floor itself

- The published `osx-x64` single file is never executed anywhere in CI. The launch proof runs only the `osx-arm64` binary, the Rosetta leg runs `dotnet test` rather than the published file, and the release job only marks the files executable. Half the macOS matrix ships without ever having been started.
- The macOS runner is the floating `macos-latest` label in both the build and release jobs, so the macOS version the product is tested on moves whenever GitHub repoints it. The last run resolved to the macOS 26 Apple Silicon image.
