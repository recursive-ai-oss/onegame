#!/usr/bin/env bash
# Compares what two systems recorded: every scenario's final world hash and the exported game's hash must be equal.
# Each argument is a folder holding one system's "*-hashes.txt" and "export-*.json" files (searched recursively).
#
#   tools/ci/compare-hashes.sh <linux folder> <windows folder>
set -u
a=$1
b=$2
status=0

# character is an example: its scenarios run the baked root motion.
for sample in solitaire lanterns character; do
  fa=$(find "$a" -name "$sample-hashes.txt" | head -1)
  fb=$(find "$b" -name "$sample-hashes.txt" | head -1)
  if [ -z "$fa" ] || [ -z "$fb" ]; then
    echo "$sample: a hash list is missing (${fa:-none} / ${fb:-none})"; status=1; continue
  fi
  if diff <(tr -d '\r' < "$fa") <(tr -d '\r' < "$fb") > /dev/null; then
    echo "$sample: $(wc -l < "$fa") scenario hashes are equal"
  else
    echo "$sample: scenario hashes differ:"; diff <(tr -d '\r' < "$fa") <(tr -d '\r' < "$fb"); status=1
  fi
done

ha=$(find "$a" -name 'export-linux-x64.json' | head -1 | xargs -r grep -oE '"hash":"[0-9a-f]+"')
hb=$(find "$b" -name 'export-win-x64.json' | head -1 | xargs -r grep -oE '"hash":"[0-9a-f]+"')
if [ -n "$ha" ] && [ "$ha" = "$hb" ]; then
  echo "exported game: $ha on both"
else
  echo "exported game hashes differ or are missing: linux ${ha:-none}, windows ${hb:-none}"; status=1
fi
exit $status
