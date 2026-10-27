#!/usr/bin/env bash
# Exports solitaire for a target, runs the exported game headless and leaves its world hash in <out>/export-<target>.json.
# Proves the shipped program starts and that its simulation matches the one on another system.
#
#   tools/ci/export-run.sh <onegame command> <target> <out folder>
set -eu
cli=$1
target=$2
out=$3
mkdir -p "$out"
repo="$(cd "$(dirname "$0")/../.." && pwd)"
rm -rf "$out/export-$target"
"$cli" export --project "$repo/samples/solitaire" --target "$target" --out "$out/export-$target" --json > "$out/export-$target-result.json"
head -c 700 "$out/export-$target-result.json"; echo
grep -q '"runtime":"nativeaot"' "$out/export-$target-result.json" || { echo "the export is not NativeAOT"; exit 1; }
exe=$(ls "$out"/export-"$target"/*/Solitaire.exe "$out"/export-"$target"/*/Solitaire 2>/dev/null | head -1)
[ -n "$exe" ] || { echo "no Solitaire program in the export"; ls -R "$out/export-$target" | head -30; exit 1; }
ls -l "$exe"
case "$target" in linux*) bash "$repo/tools/ci/glibc-baseline.sh" "$exe" ;; esac
"$exe" --headless --frames 120 --seed 42 --gpu off --json | tee "$out/export-$target.json"
