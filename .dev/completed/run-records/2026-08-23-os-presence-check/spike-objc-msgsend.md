# Spike result — objc_msgSend → LocalAuthentication on flat net10.0 (GROUND, 2026-08-23)

**VERDICT: VIABLE and clean.** macOS presence (Touch ID) is reachable from flat `net10.0` + `osx-x64` via `objc_msgSend` P/Invoke — no .NET macOS workload, no `net10.0-macos`, no Xcode. This confirms Option A's macOS side, the last open risk on the binding fork.

## Proven (Intel Mac, macOS Tahoe 26.6.2, SDK 10.0.400)
- Builds 0 warnings / 0 errors; the `osx-x64` binary launches (exit 0, no AMFI/SIGKILL), stable across repeat runs.
- `canEvaluatePolicy(LAPolicyDeviceOwnerAuthentication = 2, error = null)` returned `True` — non-interactive, no prompt, no crash. `evaluatePolicy` (the prompting call) was never invoked.
- **No manual signing needed:** the .NET SDK apphost ships already ad-hoc-signed (`codesign` shows `flags=0x2(adhoc)`), which is why it launches on Tahoe as-built. The manual `codesign -s -` fallback was not exercised.

## The macOS native surface (for the impl)
~4 P/Invoke declarations + one framework load, under 30 lines, no unsafe:
- `objc_getClass`, `sel_registerName` from `/usr/lib/libobjc.A.dylib` (that dylib has no on-disk file on modern macOS — it resolves from the dyld shared cache; `DllImport` by name still works).
- `objc_msgSend` declared ONCE PER DISTINCT SIGNATURE (mandatory on x64): `IntPtr(IntPtr, IntPtr)` for `alloc`/`init`; `[return: MarshalAs(I1)] bool(IntPtr, IntPtr, nint, IntPtr)` for `canEvaluatePolicy:error:`.
- `NativeLibrary.Load("/System/Library/Frameworks/LocalAuthentication.framework/LocalAuthentication")` BEFORE `objc_getClass("LAContext")` so the class registers.
- Call pattern: `[[LAContext alloc] init]` → `canEvaluatePolicy:error:` (the non-interactive probe) and `evaluatePolicy:localizedReason:reply:` (the interactive gate — the untestable one).

## Two discipline items → candidate RULE-PHASE rules (the spike said both are cheap to encode)
1. One `objc_msgSend` declaration per distinct signature — never one generic signature reused across differing arg/return types.
2. ObjC `BOOL` returns marshaled as `UnmanagedType.I1` (signed char in the low byte of RAX) — never the default 4-byte Win32 `BOOL` (which reads register garbage in the upper bytes).

## Testability note
`canEvaluatePolicy` is non-interactive, so it is coverable. The truly-untestable surface is the `evaluatePolicy` SUCCESS path (needs a real human touch on CI, which never happens) — that is the thin member to coverage-exclude, not the whole implementation.

Spike lived in the scratchpad (`scratchpad/objc-spike`), now removed; the repo was untouched.
