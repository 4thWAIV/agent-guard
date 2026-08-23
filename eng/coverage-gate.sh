#!/usr/bin/env bash
#
# The coverage gate (decisions threshold-75-hard-gate, covered-assemblies). It merges every per-project cobertura
# report that `dotnet test` produced with ReportGenerator, then fails the build when line coverage of a requested
# set is below 75% — the same standing a failed test would.
#
# Checks (each argument after the report directory):
#   aggregate                          Engine + guard (AgentGuard.Cli) + AgentGuard.CrossPlatform + AgentGuard.Boundaries,
#                                      together >= 75%.
#   <AssemblyName>                     that single assembly (a per-OS implementation) >= 75% on its own OS leg.
#
# The Linux leg runs `aggregate AgentGuard.CrossPlatform.Linux`; the macOS and Windows legs run only their own
# per-OS implementation. AgentGuard.Cli builds as `guard.dll`, so its assembly name is `guard`. The gate FAILS
# loudly when a required assembly is absent from the merged report, so an un-instrumented assembly can never be
# silently dropped from the number.
#
# Usage: eng/coverage-gate.sh <report-dir> <check> [<check> ...]
set -euo pipefail

# The coverage floor is a HARD 75% with NO environment override (decisions threshold-75-hard-gate,
# coverage-threshold-hard-no-override): a failing coverage run has the same standing as a failing test and cannot be
# knobbed down for a green build.
MIN=75

if [ "$#" -lt 2 ]; then
  echo "usage: eng/coverage-gate.sh <report-dir> <check> [<check> ...]" >&2
  exit 2
fi

# Resolve the report directory to an absolute path so report paths survive the ReportGenerator run below regardless of
# the working directory.
REPORT_DIR="$(cd "$1" && pwd)"
shift
CHECKS=("$@")

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

# The aggregate counted set — the single source of truth, shared by the counted-set self-check below and the gate math
# at the end. AgentGuard.Cli builds as `guard.dll`, so its assembly name is `guard`.
AGGREGATE=(AgentGuard.Engine guard AgentGuard.CrossPlatform AgentGuard.Boundaries)

# Resolve the Python used for the self-check, report selection, and the gate math below.
PYTHON="python3"
command -v python3 >/dev/null 2>&1 || PYTHON="python"

# Counted-set consistency self-check (decision counted-set-consistency-check). Every instrumented product assembly must
# be gated SOMEWHERE, or it is measured and then silently ignored (under-measured). Parse the .runsettings <Include>
# set, split it into the per-OS implementations (the .MacOS/.Linux/.Windows suffix, each gated on its own OS leg) and
# everything else, and assert "everything else" is exactly the AGGREGATE list — so the instrumented set equals the union
# of the aggregate-gated set and the per-OS-gated set. Runs before any report work so a drift fails the gate fast, with
# no coverage run needed. This is the direction the report-presence check does NOT catch (that one fails on a gated but
# un-instrumented assembly; this fails on an instrumented but un-gated one).
RUNSETTINGS="$REPO_ROOT/.runsettings"
"$PYTHON" - "$RUNSETTINGS" "${AGGREGATE[*]}" <<'PY'
import re, sys

runsettings_path, aggregate_joined = sys.argv[1], sys.argv[2]
aggregate = set(aggregate_joined.split())

text = open(runsettings_path).read()
match = re.search(r"<Include>(.*?)</Include>", text, re.DOTALL)
if not match:
    sys.stderr.write("::error::counted-set: could not find <Include> in %s.\n" % runsettings_path)
    sys.exit(1)

# Each instrumented assembly appears as `[Name]*`.
included = set(re.findall(r"\[([^\]]+)\]\*", match.group(1)))
per_os = {name for name in included if name.endswith((".MacOS", ".Linux", ".Windows"))}
everything_else = included - per_os

if everything_else != aggregate:
    sys.stderr.write(
        "::error::counted-set consistency: the non-per-OS Include set %s does not equal the AGGREGATE set %s "
        "(an instrumented assembly would be measured but gated nowhere).\n"
        % (sorted(everything_else), sorted(aggregate)))
    sys.exit(1)

sys.stderr.write(
    "coverage-gate: counted-set consistency OK (non-per-OS Include == AGGREGATE == %s; per-OS == %s).\n"
    % (sorted(aggregate), sorted(per_os)))
PY

# Restore ReportGenerator at the single pinned version from the committed tool manifest (.config/dotnet-tools.json,
# decision reportgenerator-pinned) — one pin, no drift, and no on-demand global install. Every run uses the identical
# version the manifest names, and CI needs no separate install step.
echo "coverage-gate: restoring pinned tools from .config/dotnet-tools.json" >&2
dotnet tool restore --tool-manifest "$REPO_ROOT/.config/dotnet-tools.json" >&2

# Gather the cobertura reports. coverlet writes one per run into
# <test-project>/TestResults/<guid>/coverage.cobertura.xml, and prior runs accumulate beside the fresh one in the
# local edit-test loop; merging all of them would mix a stale run with the current run and report a nonsense number
# (the gate must measure reality). Select the NEWEST report under each TestResults directory — the current run — and
# skip the older, stale ones, printing both so nothing is dropped silently.
REPORTS=()
while IFS= read -r file; do
  [ -n "$file" ] && REPORTS+=("$file")
done < <("$PYTHON" - "$REPORT_DIR" <<'PY'
import os, sys

report_dir = sys.argv[1]
newest = {}   # TestResults directory -> (mtime, path) of its latest report
stale = []
for root, _dirs, files in os.walk(report_dir):
    if "coverage.cobertura.xml" not in files:
        continue
    path = os.path.join(root, "coverage.cobertura.xml")
    # <test-project>/TestResults/<guid>/coverage.cobertura.xml: group on the TestResults directory, so each test
    # project contributes exactly its latest run and re-runs of one project never double-count.
    group = os.path.dirname(os.path.dirname(path))
    mtime = os.path.getmtime(path)
    if group not in newest:
        newest[group] = (mtime, path)
    elif mtime > newest[group][0]:
        stale.append(newest[group][1])
        newest[group] = (mtime, path)
    else:
        stale.append(path)

selected = sorted(p for _m, p in newest.values())
for p in sorted(stale):
    sys.stderr.write("coverage-gate: skipping stale report %s\n" % p)
sys.stderr.write("coverage-gate: merging %d current cobertura report(s):\n" % len(selected))
for p in selected:
    sys.stderr.write("  %s\n" % p)
    sys.stdout.write(p + "\n")
PY
)
if [ "${#REPORTS[@]}" -eq 0 ]; then
  echo "::error::no cobertura reports found under '$REPORT_DIR' — coverage was not collected." >&2
  exit 1
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
IFS=';'; JOINED="${REPORTS[*]}"; unset IFS
# Run the pinned local tool from the manifest root so `dotnet tool run` finds .config/dotnet-tools.json regardless of
# the caller's working directory; the report paths and $WORK are absolute, so the subshell cd is safe.
(cd "$REPO_ROOT" && dotnet tool run reportgenerator "-reports:$JOINED" "-targetdir:$WORK" "-reporttypes:JsonSummary") >/dev/null

"$PYTHON" - "$WORK/Summary.json" "$MIN" "${AGGREGATE[*]}" "${CHECKS[@]}" <<'PY'
import json, sys

summary_path, min_pct = sys.argv[1], int(sys.argv[2])
# The aggregate set is passed in from the one bash AGGREGATE definition, so the self-check and the gate never drift.
AGGREGATE = sys.argv[3].split()
checks = sys.argv[4:]

data = json.load(open(summary_path))
asm = {a["name"]: (a["coveredlines"], a["coverablelines"]) for a in data["coverage"]["assemblies"]}

failed = False

def gate(label, names):
    global failed
    covered = sum(asm.get(n, (0, 0))[0] for n in names)
    coverable = sum(asm.get(n, (0, 0))[1] for n in names)
    if coverable == 0:
        print(f"::error::{label}: no coverable lines (assemblies {names} absent from the report).")
        failed = True
        return
    # Exact integer comparison for ">= min%": covered/coverable >= min/100.
    ok = covered * 100 >= min_pct * coverable
    print(f"{'PASS' if ok else 'FAIL'} {label}: {covered}/{coverable} = {100.0 * covered / coverable:.2f}% (min {min_pct}%)")
    if not ok:
        print(f"::error::{label} is below {min_pct}% line coverage.")
        failed = True

for check in checks:
    if check == "aggregate":
        missing = [n for n in AGGREGATE if n not in asm]
        if missing:
            print(f"::error::aggregate: required assemblies absent from the merged report: {missing}.")
            failed = True
        gate("aggregate(Engine+Cli+CrossPlatform+Boundaries)", AGGREGATE)
    elif check in asm:
        gate(f"per-OS {check}", [check])
    else:
        print(f"::error::per-OS {check}: absent from the merged report (it was not instrumented).")
        failed = True

sys.exit(1 if failed else 0)
PY
