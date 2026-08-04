#!/usr/bin/env bash
# One script to set up AgentGuard dev signing (decisions 20, 24, 36):
#   1. mints the strong-name key + code-signing cert via the single-file C# program (no SLN/csproj)
#   2. imports the code-signing identity so `codesign` can use it
#   3. adds narrow, code-signing-only trust (never Root) so Gatekeeper accepts locally signed builds
#
# Usage: eng/generate-dev-keys.sh [output-dir]   (default: eng/signing/local, git-ignored)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_DIR="${1:-$SCRIPT_DIR/signing/local}"
mkdir -p "$OUT_DIR"

echo "==> minting keys into $OUT_DIR"
dotnet run "$SCRIPT_DIR/generate-dev-keys.cs" -- "$OUT_DIR"

PFX="$OUT_DIR/agentguard-codesign.pfx"
CER="$OUT_DIR/agentguard-codesign.cer"

case "$(uname -s)" in
  Darwin)
    KEYCHAIN="$HOME/Library/Keychains/login.keychain-db"
    echo "==> importing code-signing identity into the login keychain"
    # No password on the pfx (decision 22); -T lets codesign use it without prompting each time.
    security import "$PFX" -k "$KEYCHAIN" -P "" -T /usr/bin/codesign -A

    echo "==> adding narrow, code-signing-only trust (never Root) — this will prompt for your password"
    # -p codeSign scopes the anchor to the code-signing policy in the USER login keychain, not the System
    # Root store (decision 24). The public-only .cer is emitted by the generator (no openssl anywhere).
    security add-trusted-cert -r trustRoot -p codeSign -k "$KEYCHAIN" "$CER" || {
      echo "   (trust step skipped/failed; codesign still works, but Gatekeeper won't trust the cert until this runs)"; }
    echo "Done. Keys are in $OUT_DIR (git-ignored)."
    ;;
  Linux)
    echo "Linux has no OS code-signing trust store; cosign (keyless) covers Linux builds. Keys are in $OUT_DIR."
    ;;
  *)
    echo "Unsupported OS for the trust step; keys were still minted into $OUT_DIR."
    ;;
esac
