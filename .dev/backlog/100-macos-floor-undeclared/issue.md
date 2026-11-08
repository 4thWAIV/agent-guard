AgentGuard ships `guard` as a self-contained single file for `osx-arm64` and `osx-x64`, so the macOS floor is whatever the .NET 10 host and runtime bundled into it require. Nothing in the repository states that floor, nothing checks it at run time, and no build is tested below macOS 26.

## What the binaries require today

The .NET 10 single-file host and the runtime native libraries that get bundled are built for macOS 12.0:

```
singlefilehost:  platform MACOS  minos 12.0  sdk 15.5
libcoreclr.dylib: platform MACOS  minos 12.0  sdk 15.5
```

Microsoft supports .NET 10 on macOS 27, 26 and 15 only; macOS 14 left support on 2026-09-14.

Against that, the `pin-sdk-10.0.100-ga` decision records that on a macOS 14.6.1 machine the GA `10.0.100` build was the only .NET 10 that would launch, because later patches are linked against the macOS 15.5 SDK and were killed by AMFI. The pin has since moved to SDK `10.0.400`, which is in that later family. So the floor the binary advertises, macOS 12, is lower than the floor it is known to work on.

## What has actually been run

The CI macOS leg runs on the `macos-26-arm64` image, and the `osx-x64` build is exercised on that same machine under Rosetta. The development machine is Intel macOS 26.6.2. No build has been run on macOS 15, and no test asserts a minimum.

## Why this matters

A user on a macOS version below the real floor does not get a diagnostic. The process is killed, or the dynamic loader refuses it, with nothing that names the cause. That is the silent-failure class the project's first rule forbids.

## What needs to happen

1. Tim decides the supported macOS floor, which is a product support decision rather than a technical one.
2. Declare that floor in one place and have the build set it, rather than inheriting whatever the host pack carries.
3. Refuse to start with a message naming the required version when the running OS is below the floor.
4. Prove the floor: run the gate on the lowest supported macOS image, not only on the latest.

Found while answering a question about which macOS versions the compiled assemblies run on; no code was changed.
