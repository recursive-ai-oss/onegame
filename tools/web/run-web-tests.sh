#!/usr/bin/env bash
# The browser tests of the web host, on a dev export of tests/WebGame (headless Chrome, the engine's own page):
#   tools/web/run-web-tests.sh [work folder]
# Builds the onegame tool from this checkout, exports the fixture game, and runs the scripts in tools/web: pointer positions, the canvas on a
# phone, the safe area, the page title, the script fingerprints, capture after a failing frame, a game's own material, and render textures with post chains. The scripts that
# present to a canvas need a hardware WebGPU adapter in Chrome (CHROME_GPU_FLAGS below, the default); the software one cannot read back then.
set -eu
repo="$(cd "$(dirname "$0")/../.." && pwd)"
work=${1:-${TMPDIR:-/tmp}/web-tests}
mkdir -p "$work"
work="$(cd "$work" && pwd)"
run() { node "$repo/tools/web/run-page.mjs" --timeout 240 --print 3000 "$@"; }
hw="${CHROME_GPU_FLAGS:---enable-unsafe-webgpu --enable-features=Vulkan --use-angle=vulkan --ignore-gpu-blocklist --disable-vulkan-surface}"

dotnet build -m:4 "$repo/src/OneGame.Cli/OneGame.Cli.csproj" -v quiet -clp:NoSummary
(cd "$repo/tests/WebGame" && dotnet run --no-build --project "$repo/src/OneGame.Cli" -- export --target web --dev --out "$work/export" --json) | tee "$work/export.json"
folder=$(sed -n 's|.*"folder":"\([^"]*\)".*|\1|p' "$work/export.json")

fail=0
check() { echo "== $1"; shift; out=$("$@") || true; echo "$out"; case "$out" in '{"ok":true'*) ;; *) fail=1 ;; esac; }

check "pointer positions" run --root "$folder" --path '/index.html' --script "$repo/tools/web/pointer-test.mjs"
check "phone canvas, safe area" run --root "$folder" --path '/index.html' --script "$repo/tools/web/mobile-test.mjs"

# Titles: the stock page has none, so the game's name shows as written; a page with its own title keeps it.
check "title: the game's name" run --root "$folder" --path '/index.html?expect=Web%20Fixture' --script "$repo/tools/web/title-test.mjs"
rm -rf "$work/titled" && cp -r "$folder" "$work/titled"
sed -i 's|<meta charset="utf-8">|<meta charset="utf-8"><title>My Page</title>|' "$work/titled/index.html"
check "title: the page's own" run --root "$work/titled" --path '/index.html?expect=My%20Page' --script "$repo/tools/web/title-test.mjs"

echo "== script fingerprints"
grep -q 'src="./boot.mjs?v=' "$folder/index.html" && grep -q "onegame-platform.js?v=" "$folder/boot.mjs" && grep -q "_framework/dotnet.js?v=" "$folder/boot.mjs" || { echo "the page's scripts are not fingerprinted"; fail=1; }

export CHROME_GPU_FLAGS="$hw"
check "capture after a failing frame" run --root "$folder" --path '/index.html?control=page' --script "$repo/tools/web/capture-test.mjs"
check "a game's own material as the first draw" run --root "$folder" --path '/index.html?control=page' --script "$repo/tools/web/material-test.mjs"
check "passes that start with a game material" run --root "$folder" --path "/index.html?control=page" --script "$repo/tools/web/layers-test.mjs"
check "backdrop blur over a drawn scene" run --root "$folder" --path '/index.html?control=page' --script "$repo/tools/web/blur-test.mjs"
check "render texture, bloom and a game pass on a layer" run --root "$folder" --path '/index.html?control=page' --script "$repo/tools/web/chain-test.mjs"
exit $fail
