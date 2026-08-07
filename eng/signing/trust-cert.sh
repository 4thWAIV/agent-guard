#!/usr/bin/env bash
# Trust the AgentGuard alpha code-signing certificate on macOS — narrowly, for code signing ONLY, and NEVER as
# a system root (decision tester-trust-public-cer). Run this once before launching a downloaded alpha build so
# `codesign` accepts our self-signed signature. It is fully reversible (see the last line).
#
# Usage: ./trust-cert.sh [path/to/agentguard-macos-public.cer]
#   Defaults to agentguard-macos-public.cer in the current directory (as attached to the release).
set -euo pipefail

CER="${1:-agentguard-macos-public.cer}"
if [ ! -f "$CER" ]; then
  echo "Certificate '$CER' not found. Pass the path to the .cer you downloaded from the release." >&2
  exit 1
fi

KEYCHAIN="$HOME/Library/Keychains/login.keychain-db"
echo "Adding '$CER' as a code-signing anchor in your login keychain (never the System root)."
# -p codeSign scopes the anchor to the code-signing trust policy only; -k targets your USER login keychain,
# not the System keychain — so this can never make the cert a general-purpose root (decision tester-trust-public-cer).
security add-trusted-cert -r trustRoot -p codeSign -k "$KEYCHAIN" "$CER"

echo "Done. AgentGuard alpha binaries signed with this certificate will now pass codesign verification."
echo "To undo later: security remove-trusted-cert \"$CER\""
