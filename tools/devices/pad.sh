#!/usr/bin/env bash
# Checks a gamepad through the platform layer's evdev path and prints a one-line verdict.
#
#   tools/devices/pad.sh [--expect NAME] [--seconds N] [--rumble] [--virtual xbox|dualsense]
#
# Lists the joystick-class input devices, builds and runs padprobe against them with the community mapping database (fetched
# and checked on first use), and reports each pad's name, device id, database entry, whether the layout was mapped, rumble, and
# the buttons and axes that moved while it ran. With --virtual the pad is a uinput stand-in playing a scripted timeline, which
# proves the path with no hardware. Presses on a real pad need a person; an unattended run reports what the idle device says.
#
# Environment: PADPROBE_DIR (where padprobe is built, default $ONEGAME_ARTIFACTS or ./artifacts), GAMEPAD_DB (database file).
set -euo pipefail
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
SECONDS_RUN=12; EXPECT=; RUMBLE=; VIRTUAL=
while [ $# -gt 0 ]; do case $1 in
  --seconds) SECONDS_RUN=$2; shift 2;; --expect) EXPECT=$2; shift 2;; --rumble) RUMBLE=--rumble; shift;;
  --virtual) VIRTUAL=$2; shift 2;; *) echo "unknown option $1" >&2; exit 2;; esac; done
ART=${ONEGAME_ARTIFACTS:-$ROOT/artifacts}
DB=${GAMEPAD_DB:-$ART/gamecontrollerdb.txt}
[ -f "$DB" ] || "$HERE/fetch-gamecontrollerdb.sh" "$DB"
DLL=$ART/bin/PadProbe/debug/padprobe.dll
[ -f "$DLL" ] || dotnet build "$HERE/PadProbe/PadProbe.csproj" -v quiet -clp:NoSummary >&2

echo "input devices with a joystick handler:" >&2
awk 'BEGIN{RS="";FS="\n"} /Handlers=.*js[0-9]/ {for(i=1;i<=NF;i++) if($i ~ /^(N|I): /) printf "  %s\n",$i}' /proc/bus/input/devices >&2 || true

VPID=
if [ -n "$VIRTUAL" ]; then
  python3 "$HERE/virtual-pad.py" 2 3 "$VIRTUAL" >&2 & VPID=$!
  trap 'kill $VPID 2>/dev/null || true' EXIT; trap 'exit 130' INT; trap 'exit 143' TERM
  SECONDS_RUN=40; [ -n "$EXPECT" ] || EXPECT=$([ "$VIRTUAL" = dualsense ] && echo dualsense || echo x-box)
  sleep 1
fi
OUT=$(dotnet "$DLL" --seconds "$SECONDS_RUN" --db "$DB" --quiet ${EXPECT:+--expect "$EXPECT"} $RUMBLE) && RC=0 || RC=$?
echo "$OUT"
if [ $RC -eq 0 ]; then
  python3 - "$OUT" <<'PY'
import json, sys
s = json.loads(sys.argv[1])
for p in s['pads']:
    print(f"PAD OK {p['name']!r} id={p['guid']} database={p['dbEntry']!r} mapped={p['mapped']} rumble={p['canRumble']} buttons={len(p['buttonsPressed'])} axes={len(p['axisRanges'])}")
PY
else
  echo "PAD MISSING no gamepad${EXPECT:+ matching '$EXPECT'} seen (is it plugged in, and are /dev/input/event* readable?)"
fi
exit $RC
