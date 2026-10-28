#!/usr/bin/env bash
# A game built for the browser from packages alone. Packs the engine into a folder feed, installs the onegame tool from it,
# creates a game with `onegame new` (no engine checkout: ONEGAME_ROOT is unset), exports it for the web, and runs it in
# headless Chrome. The page's scenario results must equal the desktop run of the same game, hashes included.
#
#   tools/ci/package-smoke-web.sh [work folder]       (needs Chrome: google-chrome on PATH or ONEGAME_CHROME, and node)
set -eu
repo="$(cd "$(dirname "$0")/../.." && pwd)"
work=${1:-${RUNNER_TEMP:-${TMPDIR:-/tmp}}/package-smoke-web}
. "$repo/tools/ci/package-feed.sh"

unset ONEGAME_ROOT
rm -rf "$work"
mkdir -p "$work/feed" "$work/app" "$work/tools"
work="$(cd "$work" && pwd)"
export NUGET_PACKAGES="$work/nuget"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$work/cli-home}"

pack_feed "$repo" "$work"
write_nuget_config "$work" "$work/app"

step "dotnet tool install RecursiveAI.OneGame.Cli $version"
dotnet tool install --tool-path "$work/tools" --configfile "$work/app/nuget.config" RecursiveAI.OneGame.Cli --version "$version"
og="$work/tools/onegame"

cd "$work/app"
step "onegame new mygame"
"$og" new mygame --json
cd mygame

step "desktop: onegame test --json"
"$og" test --json | tee "$work/desktop-test.json"
grep -q '^{"ok":true' "$work/desktop-test.json" || { echo "onegame test failed"; exit 1; }

step "onegame export --target web --json (release)"
"$og" export --target web --out "$work/export" --json | tee "$work/export.json"
grep -q '^{"ok":true' "$work/export.json" || { echo "the web export failed"; exit 1; }
folder=$(sed -n 's|.*"folder":"\([^"]*\)".*|\1|p' "$work/export.json")
for f in index.html boot.mjs onegame-gpu.js onegame-platform.js onegame-audio.js _framework/dotnet.js; do
  [ -f "$folder/$f" ] || { echo "the export lacks $f"; exit 1; }
done

step "the release export starts in headless Chrome"
# The page reports a start-up failure in window.__onegame.error and sets mainReturnedMs once the game's main has started.
mkdir -p "$work/tmp"
TMPDIR="$work/tmp" node "$repo/tools/web/run-page.mjs" --root "$folder" --path '/index.html?args=--no-present' --timeout 120 --log 1 \
  --eval "window.__onegame && (window.__onegame.error ? 'error: ' + window.__onegame.error : (window.__onegame.mainReturnedMs ? 'started' : ''))" \
  | tee "$work/release-start.txt"
tail -1 "$work/release-start.txt" | grep -qx started || { echo "the release export did not start"; exit 1; }

step "onegame web test --json (development export in headless Chrome)"
"$og" web test --json --tolerance 2 --max-diff 0.001 | tee "$work/web-test.json"
grep -q '^{"ok":true' "$work/web-test.json" || { echo "the scenarios failed in the browser"; exit 1; }

step "the browser's scenario hashes equal the desktop's"
py=$(command -v python3 || command -v python)
"$py" - "$work/desktop-test.json" "$work/web-test.json" <<'PY'
import json, re, sys
def hashes(path):
    text = open(path).read()
    return sorted(set(re.findall(r'"(?:hash|finalHash|stateHash)":"([0-9a-f]{8,})"', text)))
d, w = hashes(sys.argv[1]), hashes(sys.argv[2])
print("desktop", d)
print("web    ", w)
if not d or d != w:
    sys.exit("scenario hashes differ or are missing")
PY

echo
echo "web package smoke: ok ($count packages)"
