#!/usr/bin/env bash
# CI-only: materialize the strong-name signing key AND its public key from two GitHub secrets, so a CI build is
# strong-named (decision no-committed-keys — CI must never ship an unsigned build).
#
# The public key is its OWN secret (decision ci-three-secret-slots, updated 2026-08-06): Tim ruled to STORE the
# public material as a secret rather than build a deriver tool. So there is nothing to derive here — both files
# are just base64-decoded out of their secrets into eng/signing/local/, exactly where eng/signing.props already
# looks for them, so no MSBuild overrides are needed. eng/signing/local/ is git-ignored.
#
# Env:
#   AGENTGUARD_STRONGNAME_SNK        = base64 of the .snk (the same key eng/generate-dev-keys mints locally)
#   AGENTGUARD_STRONGNAME_PUBLICKEY  = base64 of the 576-hex strong-name public-key blob (the .publickey file)
# Requires openssl on PATH (present on macOS, Linux, and Windows Git bash runners), so the three per-OS jobs
# share one path. No .NET run, no deriver.
set -euo pipefail

OUT="eng/signing/local"
mkdir -p "$OUT"

if [ -z "${AGENTGUARD_STRONGNAME_SNK:-}" ]; then
  echo "::error::AGENTGUARD_STRONGNAME_SNK is empty — CI cannot produce a signed build (decision no-committed-keys)." >&2
  exit 1
fi
if [ -z "${AGENTGUARD_STRONGNAME_PUBLICKEY:-}" ]; then
  echo "::error::AGENTGUARD_STRONGNAME_PUBLICKEY is empty — the strong-name public key is a required secret (decision ci-three-secret-slots)." >&2
  exit 1
fi

printf '%s' "$AGENTGUARD_STRONGNAME_SNK" | openssl base64 -d -A > "$OUT/agentguard-strongname.snk"
printf '%s' "$AGENTGUARD_STRONGNAME_PUBLICKEY" | openssl base64 -d -A > "$OUT/agentguard-strongname.publickey"
echo "Strong-name key + public key materialized into $OUT (git-ignored)."
