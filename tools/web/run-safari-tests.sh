#!/bin/bash
# The browser checks of the web host in Safari, on a Mac with a desktop session and "Allow Remote Automation" on (safaridriver --enable once):
#   tools/web/run-safari-tests.sh [work folder]   (ONEGAME names the onegame tool, default onegame)
repo="$(cd "$(dirname "$0")/../.." && pwd)"; work=${1:-${TMPDIR:-/tmp}/safari-tests}; L=$work; mkdir -p "$work"; work="$(cd "$work" && pwd)"
cd "$repo/tests/WebGame"
if [ ! -d "$work/export" ]; then
  ${ONEGAME:-onegame} export --target web --dev --out "$work/export" --json > "$work/export.json" 2> "$work/web-export.err" || { tail -5 "$work/web-export.err"; head -c 600 "$work/export.json"; exit 3; }
fi
folder=$(sed -n 's|.*"folder":"\([^"]*\)".*|\1|p' "$work/export.json")
echo "folder $folder"
nohup safaridriver -p 4444 </dev/null > "$work/safaridriver.log" 2>&1 &
SD=$!
trap 'kill $SD 2>/dev/null; osascript -e "tell application \"Safari\" to quit" 2>/dev/null; sleep 1; pkill -f "safaridriver -p 4444" 2>/dev/null' EXIT
sleep 2
run() { name=$1; shift; echo "== $name"; node "$repo/tools/web/run-page-safari.mjs" --timeout 240 --print 3500 "$@" 2>&1 | tail -c 3600; }
run viewport --root "$folder" --path '/index.html' --script "$repo/tools/web/safari-viewport-test.mjs" --screenshot "$work/viewport.png"
run title --root "$folder" --path '/index.html?expect=Web%20Fixture' --script "$repo/tools/web/title-test.mjs"
run capture --root "$folder" --path '/index.html?control=page' --script "$repo/tools/web/capture-test.mjs"
run material --root "$folder" --path '/index.html?control=page' --script "$repo/tools/web/material-test.mjs"
run blur --root "$folder" --path '/index.html?control=page' --script "$repo/tools/web/blur-test.mjs" --screenshot "$work/blur.png"
run chain --root "$folder" --path '/index.html?control=page' --script "$repo/tools/web/chain-test.mjs"
echo "== fingerprints"
grep -q 'src="./boot.mjs?v=' "$folder/index.html" && grep -q "onegame-platform.js?v=" "$folder/boot.mjs" && grep -q "_framework/dotnet.js?v=" "$folder/boot.mjs" && echo fingerprints ok
