#!/usr/bin/env bash
# CI-only: stage each signed per-RID binary under its FINAL release name, write a GNU two-column SHA-256
# checksum (decision checksum-gnu-format), self-verify it (sha256sum -c / shasum -c — "proof in the build"),
# and cosign-sign it keyless (decision signing-mechanisms). Reused by all three per-OS jobs so the checksum and
# cosign bundle are computed over the EXACT bytes that ship (decisions release-asset-names / verify-sigs-before-publish).
#
# Naming the asset here (guard-<rid>[.exe]) — not in the release job — is what keeps each .sha256 and
# .cosign.bundle valid against the released file: the artifact round-trip preserves bytes, and the release job's
# chmod +x changes only the mode, never the bytes/hash.
#
# Usage: eng/ci-stage-and-cosign.sh <dist-dir> <ext> <rid> [<rid> ...]
#   ext = "" on macOS/Linux, ".exe" on Windows.
# Requires cosign (from sigstore/cosign-installer) and sha256sum or shasum on PATH. Runs on macOS, Linux, and
# Windows (Git bash).
set -euo pipefail

DIST="$1"; EXT="$2"; shift 2
mkdir -p "$DIST"

for rid in "$@"; do
  cp "src/AgentGuard.Cli/bin/Release/net10.0/$rid/publish/guard$EXT" "$DIST/guard-$rid$EXT"
done

cd "$DIST"
if command -v sha256sum >/dev/null 2>&1; then SUM=(sha256sum); else SUM=(shasum -a 256); fi

for rid in "$@"; do
  bin="guard-$rid$EXT"
  "${SUM[@]}" "$bin" > "guard-$rid.sha256"          # GNU two-column: "<hex>  <name>"
  "${SUM[@]}" -c "guard-$rid.sha256"                # proof in the build
  cosign sign-blob --yes --bundle "guard-$rid.cosign.bundle" "$bin"
  echo "staged + checksummed + cosigned $bin"
done
