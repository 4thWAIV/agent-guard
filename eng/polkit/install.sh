#!/usr/bin/env bash
#
# Interim Linux installer for AgentGuard (linux-policy-root-install-fail-closed, issue #44).
#
# It installs the polkit presence action root-owned under /usr/share/polkit-1/actions/ and the guard binary under
# /usr/local/bin. The guard binary NEVER writes the policy itself; this one-time `sudo install` is the only writer.
# Until a user runs it, Linux presence is fail-closed: with no policy installed the (undeclared) action faults and the
# gate denies. A real distribution system (npm, a Claude plugin, an OS package) replaces this script later (issue #44).
#
# Usage:  sudo ./install.sh [path-to-guard-binary]
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
POLICY_SRC="${SCRIPT_DIR}/agentguard-presence.policy"
POLICY_DEST_DIR="/usr/share/polkit-1/actions"
POLICY_DEST="${POLICY_DEST_DIR}/$(basename "${POLICY_SRC}")"
BIN_DEST_DIR="/usr/local/bin"
GUARD_SRC="${1:-${SCRIPT_DIR}/guard}"

if [[ "${EUID}" -ne 0 ]]; then
  echo "install.sh must run as root (sudo) to install the polkit policy root-owned." >&2
  exit 1
fi

if [[ ! -f "${POLICY_SRC}" ]]; then
  echo "polkit policy not found next to install.sh: ${POLICY_SRC}" >&2
  exit 1
fi

# The polkit policy: root-owned, world-readable (0644), under the actions directory polkit reads.
install -d -m 0755 "${POLICY_DEST_DIR}"
install -o root -g root -m 0644 "${POLICY_SRC}" "${POLICY_DEST}"
echo "installed polkit policy -> ${POLICY_DEST}"

# The guard binary (optional; skipped when it is not shipped beside this script or given as an argument).
if [[ -f "${GUARD_SRC}" ]]; then
  install -d -m 0755 "${BIN_DEST_DIR}"
  install -o root -g root -m 0755 "${GUARD_SRC}" "${BIN_DEST_DIR}/guard"
  echo "installed guard binary -> ${BIN_DEST_DIR}/guard"
else
  echo "guard binary not found at ${GUARD_SRC}; installed the polkit policy only." >&2
fi
