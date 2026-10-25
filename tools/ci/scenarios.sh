#!/usr/bin/env bash
# Runs a sample's scenarios with the onegame command and leaves a "<scenario> <hash>" list in <out>/<sample>-hashes.txt,
# so a run on another system can be compared with it.
#
#   tools/ci/scenarios.sh <onegame command> <sample folder> <out folder> [extra arguments for onegame test]
set -eu
cli=$1
sample=$2
out=$3
shift 3
mkdir -p "$out"
name=$(basename "$sample")
result="$out/$name-test.json"

status=0
(cd "$sample" && "$cli" test --json "$@" > "$result") || status=$?
head -c 400 "$result"; echo
# One line per scenario: its name and the hash of the world when it ended.
grep -oE '"name":"[^"]+","source":"[^"]*","passed":(true|false),"tick":[0-9]+,"hash":"[0-9a-f]+"' "$result" \
  | sed -E 's/"name":"([^"]+)".*"hash":"([0-9a-f]+)"/\1 \2/' | sort > "$out/$name-hashes.txt"
echo "$(wc -l < "$out/$name-hashes.txt") scenario hashes in $out/$name-hashes.txt"
exit "$status"
