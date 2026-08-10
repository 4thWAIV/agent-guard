#!/usr/bin/env bash
# CI-only (decision verify-sigs-before-publish): after a per-OS job has signed its two binaries, verify THIS
# platform's signatures and record a machine-checkable result artifact. No single runner hosts both codesign
# and signtool, so each job proves its own native seals here and the release job trusts these artifacts.
#
# Verifies, over dist/guard-<rid>[<ext>]:
#   - native codesign (mac)  : codesign --verify --strict
#   - native signtool (win)  : signtool verify /pa /v  (self-signed cert already trusted on the runner)
#   - none (linux)           : cosign only
#   - all platforms          : cosign verify-blob against this workflow's keyless identity
# Writes the verbatim command output plus a final "RESULT: PASS" (or "RESULT: FAIL") line to <result-file>,
# echoes it to the log, and exits non-zero if anything failed — so the job goes red on a bad signature.
#
# Usage: eng/ci-verify-signatures.sh <result-file> <codesign|signtool|none> <ext> <identity> <rid> [<rid> ...]
#   ext = "" on macOS/Linux, ".exe" on Windows.  identity = cosign certificate-identity for this run.
# Requires cosign on PATH; runs on macOS, Linux, and Windows (Git bash).
set -uo pipefail

RESULT="$1"; NATIVE="$2"; EXT="$3"; IDENTITY="$4"; shift 4
ISSUER="https://token.actions.githubusercontent.com"
mkdir -p "$(dirname "$RESULT")"

find_signtool() {
  find "/c/Program Files (x86)/Windows Kits/10/bin" -name signtool.exe 2>/dev/null \
    | grep -i '/x64/' | sort -r | head -1
}

ok=1
{
  echo "verify results for ${GITHUB_SHA:-<local>} at $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "identity: $IDENTITY"
  SIGNTOOL=""
  if [ "$NATIVE" = signtool ]; then
    SIGNTOOL="$(find_signtool)"
    if [ -z "$SIGNTOOL" ]; then echo "signtool.exe not found on the runner"; ok=0; fi
  fi
  for rid in "$@"; do
    bin="dist/guard-$rid$EXT"
    bundle="dist/guard-$rid.cosign.bundle"
    case "$NATIVE" in
      codesign)
        echo "== codesign --verify --strict guard-$rid =="
        if codesign --verify --strict --verbose=2 "$bin" 2>&1; then echo "codesign PASS: $rid"; else echo "codesign FAIL: $rid"; ok=0; fi
        ;;
      signtool)
        echo "== signtool verify /pa /v guard-$rid$EXT =="
        if [ -n "$SIGNTOOL" ] && "$SIGNTOOL" verify /pa /v "$bin" 2>&1; then echo "signtool PASS: $rid"; else echo "signtool FAIL: $rid"; ok=0; fi
        ;;
      none) ;;
      *) echo "unknown native mode '$NATIVE'"; ok=0 ;;
    esac
    echo "== cosign verify-blob guard-$rid$EXT =="
    if cosign verify-blob --certificate-identity "$IDENTITY" --certificate-oidc-issuer "$ISSUER" \
         --bundle "$bundle" "$bin" 2>&1; then echo "cosign PASS: $rid"; else echo "cosign FAIL: $rid"; ok=0; fi
  done
  if [ "$ok" = 1 ]; then echo "RESULT: PASS"; else echo "RESULT: FAIL"; fi
} > "$RESULT" 2>&1

cat "$RESULT"
grep -q "^RESULT: PASS$" "$RESULT"
