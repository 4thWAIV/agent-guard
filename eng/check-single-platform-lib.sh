#!/usr/bin/env bash
# Regression check for the cross-RID per-OS library selection (cross-platform-engine-and-interop contract,
# scan-resolutions #1 + the windows/publish fix). A self-contained cross-RID publish must reference and bundle
# EXACTLY ONE AgentGuard.CrossPlatform.<OS> implementation library — never both the host's and the target's.
#
# Why this exists: the normal CI legs never catch a mis-selection because each OS job only publishes its own
# host's RIDs (host == target), so the RID-vs-host divergence cannot show up there. This check deliberately
# publishes a RID and inspects the output, and is meant to be run for a NON-host RID (a true cross-build), e.g.
# `eng/check-single-platform-lib.sh linux-x64` on a macOS or Windows host.
#
# The per-OS DLLs are bundled inside the single-file exe, so the tell is the loose .pdb/.xml the SDK still emits
# beside the exe for each referenced assembly: exactly one AgentGuard.CrossPlatform.<OS>.* family must appear.
#
# Usage: eng/check-single-platform-lib.sh <rid> [extra dotnet publish args ...]
# Requires the pinned .NET SDK on PATH. Runs on macOS, Linux, and Windows (Git bash). Exit 0 on pass.
set -euo pipefail

RID="${1:?usage: eng/check-single-platform-lib.sh <rid> [extra dotnet publish args ...]}"
shift || true

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT

echo "check-single-platform-lib: publishing AgentGuard.Cli for RID '$RID' (self-contained) ..."
dotnet publish "$REPO_ROOT/src/AgentGuard.Cli/AgentGuard.Cli.csproj" \
  -c Release -r "$RID" --self-contained -o "$OUT" "$@"

# Distinct per-OS families present among the published output files.
FAMILIES="$(ls "$OUT" | grep -oE 'AgentGuard\.CrossPlatform\.(MacOS|Linux|Windows)' | sort -u || true)"
COUNT="$(printf '%s\n' "$FAMILIES" | grep -c . || true)"

echo "check-single-platform-lib: per-OS platform libraries in the '$RID' publish: [${FAMILIES//$'\n'/ }] (count=$COUNT)"

if [ "$COUNT" -ne 1 ]; then
  echo "::error::Cross-RID publish for '$RID' selected $COUNT per-OS platform libraries; expected exactly 1." >&2
  echo "         A cross-RID publish must reference and bundle exactly one AgentGuard.CrossPlatform.<OS> library." >&2
  exit 1
fi

echo "check-single-platform-lib: PASS — exactly one per-OS platform library ($FAMILIES) for RID '$RID'."
