#!/usr/bin/env bash
# Exports a game as a development build (export --dev), starts it with --live and drives it: state, input, a UI click and a PNG capture.
# Then checks the release export of the same game still refuses --live (exit 5, no_dev_tools) and carries no live-control assembly.
#
#   tools/ci/export-dev-live.sh <onegame command> <target> <out folder> [game project folder]
#
# The game runs headless, so no display is needed; set DISPLAY (and drop OG_HEADLESS) to drive a window instead.
set -eu
cli=$1
target=$2
out=$3
repo="$(cd "$(dirname "$0")/../.." && pwd)"
project=${4:-$repo/samples/solitaire}
# The program is named by the export's own JSON: the folder name and the program name come from the project file, not its folder.
program() { dir=$(sed -n 's/.*"directory":"\([^"]*\)".*/\1/p' "$1"); exe=$(sed -n 's/.*"executable":"\([^"]*\)".*/\1/p' "$1"); echo "$dir/$exe" | sed "s,\\\\,/,g"; }
py=python3
command -v python3 >/dev/null 2>&1 || py=python
mkdir -p "$out"
rm -rf "$out/dev" "$out/rel"

"$cli" export --project "$project" --target "$target" --dev --no-archive --out "$out/dev" --json > "$out/dev.json"
head -c 400 "$out/dev.json"; echo
grep -q '"devTools":true' "$out/dev.json" || { echo "the dev export does not say devTools"; exit 1; }
grep -q '"runtime":"nativeaot"' "$out/dev.json" || { echo "the dev export is not NativeAOT"; exit 1; }
dev=$(program "$out/dev.json")
[ -f "$dev" ] || { echo "no program in the dev export: $dev"; ls -R "$out/dev" | head -20; exit 1; }

port=$((20000 + RANDOM % 20000))
headless=--headless
[ -n "${OG_WINDOWED:-}" ] && headless=
"$dev" $headless --live "tcp:$port" --gpu "${OG_GPU:-auto}" > "$out/dev-run.log" 2>&1 &
pid=$!
trap 'kill $pid 2>/dev/null || true' EXIT
"$py" "$repo/tools/ci/live-probe.py" "$port" --png "$out/dev-capture.png"
kill $pid 2>/dev/null || true
wait $pid 2>/dev/null || true
trap - EXIT

"$cli" export --project "$project" --target "$target" --no-archive --out "$out/rel" --json > "$out/rel.json"
grep -q '"devTools":false' "$out/rel.json" || { echo "the release export says devTools"; exit 1; }
rel=$(program "$out/rel.json")
set +e
"$rel" --headless --json --live "tcp:$((port + 1))" > "$out/rel-run.log" 2>&1
code=$?
set -e
[ "$code" = 5 ] || { echo "the release export exited $code on --live, not 5"; cat "$out/rel-run.log"; exit 1; }
grep -q no_dev_tools "$out/rel-run.log" || { echo "the release refusal does not say no_dev_tools"; cat "$out/rel-run.log"; exit 1; }
if ls "$(dirname "$rel")"/OneGame.Live* >/dev/null 2>&1; then echo "the release export carries OneGame.Live"; exit 1; fi
echo "dev export drove cleanly; release export refused --live with exit 5"
