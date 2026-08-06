#!/usr/bin/env bash
# CI pre-job build-id computation (decision 25, option b). Computes the ONE build id for a CI run and prints it
# as key=value lines; a GitHub Actions pre-job runs this once and passes the values to every downstream job as
# -p:AgentGuard... so the 3 per-OS machine jobs + the release job all stamp the identical id. The Day/Time math
# here intentionally mirrors the pre-solution MSBuild target (authorized to live in these two places).
#
# Usage: eng/compute-build-id.sh [channel]     channel "dev" => -pre-release; else release.
# If $GITHUB_OUTPUT is set, the values are also appended there for use as job outputs.
set -euo pipefail

CHANNEL="${1:-${AGENTGUARD_CHANNEL:-}}"
EPOCH=1577836800                       # 2020-01-01T00:00:00Z
NOW=$(date -u +%s)
DAY=$(( (NOW - EPOCH) / 86400 ))
TIME=$(( (NOW % 86400) / 2 ))
COMBINED=$(( DAY * 43200 + TIME ))
HASH=$(git rev-parse --short=7 HEAD 2>/dev/null || echo local)

printf 'day=%s\ntime=%s\ncombined=%s\nhash=%s\nchannel=%s\n' "$DAY" "$TIME" "$COMBINED" "$HASH" "$CHANNEL"

if [ -n "${GITHUB_OUTPUT:-}" ]; then
  {
    printf 'day=%s\n' "$DAY"
    printf 'time=%s\n' "$TIME"
    printf 'combined=%s\n' "$COMBINED"
    printf 'hash=%s\n' "$HASH"
    printf 'channel=%s\n' "$CHANNEL"
  } >> "$GITHUB_OUTPUT"
fi
