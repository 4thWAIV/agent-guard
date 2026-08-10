#!/usr/bin/env bash
# CI-only DRY helper (decision ci-cert-expiry-check / two-public-certs-per-release): extract the certificate
# (public half, no private key) from a PKCS#12 (.p12/.pfx) into a PEM, trying modern OpenSSL first and falling
# back to the -legacy provider when the p12 uses the old RC2/3DES MAC that OpenSSL 3 refuses by default.
#
# One place owns this fallback so the mac cert-expiry check and the release cert export can never drift apart.
#
# Usage: eng/p12-to-pem.sh <in.p12> <passphrase> <out.pem>
set -euo pipefail

IN="$1"; PW="$2"; OUT="$3"

if ! openssl pkcs12 -in "$IN" -clcerts -nokeys -passin pass:"$PW" -out "$OUT" 2>/dev/null; then
  openssl pkcs12 -legacy -in "$IN" -clcerts -nokeys -passin pass:"$PW" -out "$OUT"
fi
