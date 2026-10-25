#!/usr/bin/env bash
# Downloads the community game controller mapping database (test data only; the engine ships none), pinned to one commit and
# checked against its SHA-256. Usage: tools/devices/fetch-gamecontrollerdb.sh <output file>
set -euo pipefail
OUT=${1:?usage: $0 <output file>}
COMMIT=ff333823a85ae896e73f4c53670932cf0f728adb
SHA=c6587cc65789539e61ec375aad5bdbc14f974508b57e2ae87730ac75c4445dba
mkdir -p "$(dirname "$OUT")"
curl -fsSL -o "$OUT" "https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/$COMMIT/gamecontrollerdb.txt"
echo "$SHA  $OUT" | sha256sum -c --quiet - || { echo "checksum mismatch" >&2; rm -f "$OUT"; exit 1; }
