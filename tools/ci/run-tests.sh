#!/usr/bin/env bash
# Runs every built test app (a project whose name ends in "Tests") one after the other and prints a line each.
# A failing app does not stop the others; the exit code is 1 when any failed. Each app's output is kept in
# $ONEGAME_ARTIFACTS/test-logs/<app>.log so a CI run can upload it.
#
#   tools/ci/run-tests.sh [configuration]      (default: Debug)
set -u
config=$(echo "${1:-Debug}" | tr 'A-Z' 'a-z')
: "${ONEGAME_ARTIFACTS:?set ONEGAME_ARTIFACTS to the build output folder}"
logs="$ONEGAME_ARTIFACTS/test-logs"
mkdir -p "$logs"
# Process clusters started by the tests leave their node logs here (uploaded with the test logs).
export ONEGAME_CLUSTER_LOGS="$logs/cluster"

failed=0
count=0
for dir in "$ONEGAME_ARTIFACTS"/bin/*Tests/"$config"; do
  [ -d "$dir" ] || continue
  name=$(basename "$(dirname "$dir")")
  exe="$name"
  [ -f "$dir/$name.exe" ] && exe="$name.exe"
  [ -f "$dir/$exe" ] || continue
  count=$((count + 1))
  # Each app runs from its own folder: test data is copied beside it.
  (cd "$dir" && timeout 1500 "./$exe" -noColor > "$logs/$name.log" 2>&1)
  rc=$?
  summary=$(grep -E 'Total:' "$logs/$name.log" | tail -1 | sed 's/^ *//')
  echo "$name rc=$rc ${summary:-no summary}"
  if [ "$rc" -ne 0 ]; then
    failed=$((failed + 1))
    # The failures themselves, so the run log says what broke without opening the artifact.
    grep -E '\[FAIL\]' -A6 "$logs/$name.log" | head -60
  fi
done

echo "$count test apps, $failed failed"
[ "$count" -gt 0 ] && [ "$failed" -eq 0 ]
