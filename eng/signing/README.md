# eng/signing

Signing material and config for AgentGuard builds.

## What's here (committed)
- `agentguard.entitlements` — macOS hardened-runtime entitlements a .NET binary needs to launch
  (`allow-jit`, `allow-unsigned-executable-memory`).
- `../signing.props` — strong-naming + the keyless-`InternalsVisibleTo` transform, imported by
  `Directory.Build.props`. Signed iff a strong-name key is present; otherwise the build is unsigned.
- `../generate-dev-keys.cs` / `.sh` / `.ps1` — mint the local dev keys.

## What's NOT here (generated, git-ignored)
`local/` holds keys minted by `eng/generate-dev-keys.sh` (or `.ps1` on Windows):
- `agentguard-strongname.snk` — the strong-name key
- `agentguard-strongname.publickey` — its public-key blob (injected into `InternalsVisibleTo`)
- `agentguard-codesign.pfx` — a self-signed code-signing cert

**These keys are UNTRUSTED, developer-only, and never committed.** They exist to rehearse the signing
pipeline locally. Real release keys live only in GitHub secrets. A build with no keys is simply unsigned;
CI sets `AGENTGUARD_REQUIRE_SIGNED=true` so a missing key fails the build instead of shipping unsigned.

## Generate them
```sh
eng/generate-dev-keys.sh          # macOS / Linux
eng/generate-dev-keys.ps1         # Windows
```
Requires the .NET 10 SDK. The macOS/Windows trust step prompts for your password (expected, one-time).

## Local cosign co-signing (opt-in)
cosign is keyless everywhere; CI signs with the ambient GitHub identity. Locally it is an **opt-in** pass, OFF
by default and skipped in CI/headless, so a small contributor community can verify each other's local builds by
known Sigstore identity (decision `cosign-keyless-and-community-cosign`):
```sh
AGENTGUARD_COSIGN=1 eng/local-cosign.sh <binary>      # macOS / Linux (a browser login opens)
AGENTGUARD_COSIGN=1 pwsh eng/local-cosign.ps1 <binary> # Windows
```
Unset (the default) it is a no-op. Official releases are the CI identity, so a personal co-sign is a community
convention, not an official signature.
