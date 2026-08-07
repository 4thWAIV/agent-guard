#!/usr/bin/env bash
# Local, OPT-IN cosign co-signing (decision cosign-keyless-and-community-cosign). OFF by default: a developer
# sets AGENTGUARD_COSIGN=1 to co-sign a LOCALLY-produced binary with their OWN Sigstore identity via the
# interactive browser login, so a small contributor community can verify and trust each other's local builds.
#
# This is NOT the CI path — CI signs keyless with the ambient GitHub OIDC identity in ci.yml. This pass is
# skipped in CI and when headless, so a normal local or CI build is completely unaffected.
#
# Usage: eng/local-cosign.sh <binary> [<bundle-out>]   (default bundle: <binary>.cosign.bundle)
set -euo pipefail

BIN="${1:?usage: eng/local-cosign.sh <binary> [<bundle-out>]}"
OUT="${2:-$BIN.cosign.bundle}"

is_true() { case "${1:-}" in 1|true|TRUE|yes|YES|on|ON) return 0 ;; *) return 1 ;; esac; }

if ! is_true "${AGENTGUARD_COSIGN:-}"; then
  echo "AGENTGUARD_COSIGN is off — skipping local cosign co-signing (opt-in only)."
  exit 0
fi

# Never run in CI: CI has its own keyless-identity path. GitHub Actions sets CI=true and GITHUB_ACTIONS=true.
if [ -n "${CI:-}" ] || [ -n "${GITHUB_ACTIONS:-}" ]; then
  echo "Running in CI — skipping the LOCAL cosign pass (CI uses the ambient GitHub OIDC identity)."
  exit 0
fi

# The interactive Sigstore login needs a terminal + a browser; skip when headless so a build never hangs.
if [ ! -t 0 ] || [ ! -t 1 ]; then
  echo "No interactive terminal — skipping local cosign (the Sigstore login needs a browser)."
  exit 0
fi

if ! command -v cosign >/dev/null 2>&1; then
  echo "cosign is not installed — install it to use AGENTGUARD_COSIGN (https://docs.sigstore.dev)." >&2
  exit 1
fi

if [ ! -f "$BIN" ]; then
  echo "Binary '$BIN' not found." >&2
  exit 1
fi

echo "Local cosign co-sign of '$BIN' with YOUR interactive Sigstore identity (a browser window will open)..."
# No --yes: keep the confirmation + browser OIDC flow interactive (the developer's own identity, keyless).
cosign sign-blob --bundle "$OUT" "$BIN"
echo "Wrote '$OUT'. Share it so community members can verify this local build by your Sigstore identity."
