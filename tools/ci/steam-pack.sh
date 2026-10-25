#!/usr/bin/env bash
# Checks that `export --steam` finds Valve's library in a redistributable folder and copies it next to the program.
# Valve's library is never in this repository and cannot be downloaded here, so a placeholder file with the right
# name stands in: this proves the lookup and the packing, not the loading (the Steam tests load the real library
# when ONEGAME_STEAM_REDIST names a folder that has it).
#
#   tools/ci/steam-pack.sh <onegame command> <target> <out folder>
set -eu
cli=$1
target=$2
out=$3
repo="$(cd "$(dirname "$0")/../.." && pwd)"
lib=libsteam_api.so
[ "$target" = win-x64 ] && lib=steam_api64.dll
redist="$out/steam-redist/$target"
mkdir -p "$redist"
printf 'placeholder, not the Steam library' > "$redist/$lib"
rm -rf "$out/steam-export"
"$cli" export --project "$repo/samples/solitaire" --target "$target" --steam --steam-redist "$out/steam-redist" --no-archive --out "$out/steam-export" --json > "$out/steam-export.json"
grep -q '"steam":true' "$out/steam-export.json" || { echo "the result does not say steam"; head -c 600 "$out/steam-export.json"; exit 1; }
found=$(ls "$out"/steam-export/*/"$lib" 2>/dev/null | head -1)
[ -n "$found" ] || { echo "$lib is not in the export"; ls "$out"/steam-export/*/ | head -20; exit 1; }
cmp "$found" "$redist/$lib"
# The Steam client's app id file must never ship, or a game would start outside Steam.
if ls "$out"/steam-export/*/steam_appid.txt >/dev/null 2>&1; then echo "steam_appid.txt was shipped"; exit 1; fi
echo "packed $lib into $(dirname "$found")"
