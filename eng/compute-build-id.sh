#!/usr/bin/env bash
# CI pre-job build-id computation (decision 25, option b). Computes the ONE build id for a CI run and prints it
# as key=value lines; a GitHub Actions pre-job runs this once and passes the values to every downstream job as
# -p:AgentGuard... so the 3 per-OS machine jobs + the release job all stamp the identical id. The Day/Time math
# here intentionally mirrors the pre-solution MSBuild target (authorized to live in these two places).
#
# Usage: eng/compute-build-id.sh [channel]     channel "dev" => -pre-release; else release.
# If $GITHUB_OUTPUT is set, the values are also appended there for use as job outputs.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CHANNEL="${1:-${AGENTGUARD_CHANNEL:-}}"
EPOCH=1577836800                       # 2020-01-01T00:00:00Z
NOW=$(date -u +%s)
DAY=$(( (NOW - EPOCH) / 86400 ))
TIME=$(( (NOW % 86400) / 2 ))
COMBINED=$(( DAY * 43200 + TIME ))
HASH=$(git rev-parse --short=7 HEAD 2>/dev/null || echo local)

# The SINGLE owner of the assembled version string on the CI side (decisions compute-version-once /
# two-version-forms / channel-by-prerelease-id / release-fields). MAJOR.MINOR comes from the one config file
# (eng/version.props); the -pre-release suffix is applied HERE and nowhere else — no downstream job re-reads
# version.props or re-joins the string. `tag` is the release tag / MSBuild Version (no +hash); `version` is the
# full SemVer / InformationalVersion (with +hash). This mirrors the MSBuild owner in eng/version.compute.targets
# (the same authorized two-place mirror as the Day/Time formula above).
MAJOR_MINOR=$(grep -oE '<AgentGuardMajorMinor>[^<]+' "$SCRIPT_DIR/version.props" | sed 's/.*>//')
if [ -z "$MAJOR_MINOR" ]; then
  echo "compute-build-id: could not read <AgentGuardMajorMinor> from $SCRIPT_DIR/version.props" >&2
  exit 1
fi
if [ "$CHANNEL" = "dev" ]; then PRE="-pre-release"; else PRE=""; fi
TAG="${MAJOR_MINOR}.${COMBINED}${PRE}"
VERSION="${TAG}+${HASH}"

printf 'day=%s\ntime=%s\ncombined=%s\nhash=%s\nchannel=%s\ntag=%s\nversion=%s\n' \
  "$DAY" "$TIME" "$COMBINED" "$HASH" "$CHANNEL" "$TAG" "$VERSION"

if [ -n "${GITHUB_OUTPUT:-}" ]; then
  {
    printf 'day=%s\n' "$DAY"
    printf 'time=%s\n' "$TIME"
    printf 'combined=%s\n' "$COMBINED"
    printf 'hash=%s\n' "$HASH"
    printf 'channel=%s\n' "$CHANNEL"
    printf 'tag=%s\n' "$TAG"
    printf 'version=%s\n' "$VERSION"
  } >> "$GITHUB_OUTPUT"
fi
