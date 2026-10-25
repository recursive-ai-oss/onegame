#!/usr/bin/env bash
# Checks the samples on a real Android phone (or an emulator) over adb and prints a one-line verdict.
#
#   tools/devices/android.sh [--serial S] [--samples solitaire,lanterns,bricks] [--lan-host ADDRESS] [--out DIR]
#                            [--no-cluster] [--keep] [--wait SECONDS]
#
# Per sample: detect the device, export a dev build for its ABI, install it, run the scenario folder on the device and compare
# every scenario's state hash with a desktop run, then (Solitaire) drive the running game over live control: a presented frame,
# real finger taps through `adb shell input tap`, the audio device (backend, frames played, peak after a draw) and home-and-back.
# Lanterns also joins a cluster as a client of servers on this computer over the network. Samples that are not in the tree
# (Bricks, before it lands) are skipped. Packages are uninstalled at the end unless --keep.
#
# Environment: ONEGAME (the onegame command; default `onegame`), ANDROID_SERIAL, CAPTURE_TOLERANCE (default 48) and
# CAPTURE_MAX_DIFF (default 0.02): a phone's GPU draws edges differently from the software renderer the references came from,
# so captures get a per-channel tolerance and a budget of differing pixels; state hashes must match exactly regardless.
# The desktop reference run needs the same GPU setup as `onegame test` (for example VK_ICD_FILENAMES for a software Vulkan).
set -uo pipefail
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
ONEGAME=${ONEGAME:-onegame}
SAMPLES=; LAN=; OUT=; CLUSTER=1; KEEP=; WAIT=30; SERIAL=${ANDROID_SERIAL:-}
while [ $# -gt 0 ]; do case $1 in
  --serial) SERIAL=$2; shift 2;; --samples) SAMPLES=$2; shift 2;; --lan-host) LAN=$2; shift 2;; --out) OUT=$2; shift 2;;
  --no-cluster) CLUSTER=; shift;; --keep) KEEP=1; shift;; --wait) WAIT=$2; shift 2;; *) echo "unknown option $1" >&2; exit 2;; esac; done
OUT=${OUT:-${ONEGAME_ARTIFACTS:-$ROOT/artifacts}/device-android}
TOL=${CAPTURE_TOLERANCE:-48}; MAXD=${CAPTURE_MAX_DIFF:-0.02}
mkdir -p "$OUT"
say() { echo "[android] $*" >&2; }
jget() { python3 -c 'import json,sys
d=json.load(open(sys.argv[1], errors="replace")) if sys.argv[1]!="-" else json.loads(sys.stdin.read())
for k in sys.argv[2].split("."):
    d=d[int(k)] if k.isdigit() else d.get(k) if isinstance(d,dict) else None
print("" if d is None else d)' "$@"; }
lastjson() { python3 -c 'import sys,json
t=open(sys.argv[1],errors="replace").read(); i=t.find("{\"ok\"")
print(t[i:] if i>=0 else "{}")' "$1"; }
FAILS=(); NOTES=()
fail() { FAILS+=("$1"); say "FAIL $1"; }

# --- 1. the device ---
command -v adb >/dev/null || { echo "ANDROID FAIL adb not found (install platform-tools or put it on PATH)"; exit 1; }
for ((t = 0; t < WAIT; t += 2)); do
  if [ -z "$SERIAL" ]; then
    mapfile -t DEVS < <(adb devices | awk 'NR>1 && $2=="device" {print $1}')
    PHONES=(); for d in "${DEVS[@]}"; do case $d in emulator-*|127.0.0.1:*) ;; *) PHONES+=("$d");; esac; done
    if [ ${#PHONES[@]} -ge 1 ]; then SERIAL=${PHONES[0]}; elif [ ${#DEVS[@]} -eq 1 ]; then SERIAL=${DEVS[0]}; fi
  fi
  [ -n "$SERIAL" ] && adb -s "$SERIAL" get-state 2>/dev/null | grep -q device && break
  sleep 2
done
if [ -z "$SERIAL" ] || ! adb -s "$SERIAL" get-state 2>/dev/null | grep -q device; then
  UNAUTH=$(adb devices | awk '$2=="unauthorized"{print $1}' | head -1)
  [ -n "$UNAUTH" ] && echo "ANDROID WAITING $UNAUTH is unauthorized: accept the computer's key on the phone's screen" || echo "ANDROID MISSING no device on adb (USB debugging on? cable carries data?)"
  exit 1
fi
export ANDROID_SERIAL=$SERIAL
A() { adb -s "$SERIAL" "$@"; }
MODEL=$(A shell getprop ro.product.model | tr -d '\r'); ABI=$(A shell getprop ro.product.cpu.abi | tr -d '\r')
SDK=$(A shell getprop ro.build.version.sdk | tr -d '\r'); SOC=$(A shell getprop ro.board.platform | tr -d '\r')
VK=$(A shell pm list features | tr -d '\r' | grep -o 'android.hardware.vulkan.version=[0-9]*' | head -1)
say "device $SERIAL: $MODEL abi=$ABI sdk=$SDK soc=$SOC ${VK:-no vulkan feature}"
case $ABI in arm64-v8a|x86_64) ;; *) echo "ANDROID FAIL unsupported ABI $ABI (the export builds arm64-v8a and x86_64)"; exit 1;; esac
[ -n "$VK" ] || fail "no Vulkan feature reported"

# Reaching this computer from the phone: the address of this computer on the phone's network.
if [ -z "$LAN" ] && [ -n "$CLUSTER" ]; then
  PIP=$(A shell ip -f inet addr show wlan0 2>/dev/null | tr -d '\r' | awk '/inet /{sub("/.*","",$2); print $2}' | head -1)
  if [ -z "$PIP" ]; then PIP=$(A shell ip route 2>/dev/null | tr -d '\r' | awk '/src/{for(i=1;i<=NF;i++) if($i=="src") print $(i+1)}' | grep -v '^10\.0\.2\.' | head -1); fi
  if [ -n "$PIP" ]; then
    NET=${PIP%.*}.
    LAN=$(hostname -I | tr ' ' '\n' | grep -F "$NET" | head -1)
  fi
  [ -n "$LAN" ] || { NOTES+=("lanterns cluster skipped: no address of this computer on the phone's network (phone ip: ${PIP:-none}); pass --lan-host"); CLUSTER=; }
fi

# --- 2. samples ---
[ -n "$SAMPLES" ] || { SAMPLES="solitaire,lanterns"; [ -d "$ROOT/samples/bricks" ] && SAMPLES="$SAMPLES,bricks"; }
declare -A SUMMARY
PKGS=()
for S in ${SAMPLES//,/ }; do
  D=$ROOT/samples/$S
  [ -d "$D" ] || { NOTES+=("$S not in this tree"); continue; }
  W=$OUT/$S; mkdir -p "$W"
  say "$S: export ($ABI)"
  (cd "$D" && $ONEGAME export --target android --abi "$ABI" --dev --json --out "$W/export") > "$W/export.log" 2>&1 || { fail "$S export"; tail -c 400 "$W/export.log" >&2; continue; }
  lastjson "$W/export.log" > "$W/export.json"
  APK=$(jget "$W/export.json" apk); PKG=$(jget "$W/export.json" package); PKGS+=("$PKG")
  say "$S: install $PKG"
  A uninstall "$PKG" >/dev/null 2>&1   # an older build signed with another debug key would refuse the update
  (cd "$D" && $ONEGAME android install "$APK" --json) > "$W/install.log" 2>&1 || { fail "$S install"; tail -c 400 "$W/install.log" >&2; continue; }
  say "$S: desktop reference run"
  (cd "$D" && $ONEGAME test --no-unit --json) > "$W/desktop.json" 2>&1 || NOTES+=("$S: desktop reference run failed")
  say "$S: scenarios on the device"
  (cd "$D" && $ONEGAME android test --scenario scenarios --package "$PKG" --timeout 900 --json -- --capture-tolerance "$TOL" --capture-max-diff "$MAXD") > "$W/device.json" 2>&1
  CMP=$(python3 "$HERE/compare-hashes.py" "$W/desktop.json" "$W/device.json" 2>&1 | tail -1)
  echo "$CMP" > "$W/compare.json"
  H=$(echo "$CMP" | jget - hashEqual 2>/dev/null); N=$(echo "$CMP" | jget - scenarios 2>/dev/null)
  P=$(echo "$CMP" | jget - passed 2>/dev/null)
  SUMMARY[$S]="$S scenarios $P/$N hash-equal $H/$N"
  [ "$H" = "$N" ] && [ -n "$N" ] || fail "$S: state hashes differ from desktop"
  [ "$P" = "$N" ] || NOTES+=("$S: $(( ${N:-0} - ${P:-0} )) scenario(s) failed (captures past tolerance $TOL/$MAXD?) $(echo "$CMP" | jget - captureNotes 2>/dev/null | cut -c1-200)")

  if [ "$S" = solitaire ]; then
    say "$S: live control (finger, sound, frame, home and back)"
    EP=$(cd "$D" && $ONEGAME android live --package "$PKG" --port 7878 --json -- --seed 1 | grep -o 'tcp:127.0.0.1:[0-9]*' | head -1)
    if [ -n "$EP" ]; then
      python3 "$HERE/live-check.py" "${EP#tcp:}" "$W/live" --serial "$SERIAL" --expect-audio aaudio > "$W/live.json" 2> "$W/live.err" || fail "solitaire live: $(jget "$W/live.json" problems | cut -c1-200)"
      LIVE=$(python3 - "$W/live.json" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1]))
    print(f"finger={'ok' if d.get('finger', {}).get('stock_drew_a_card') else 'FAIL'} audio={d['audio']['backend']}/{d['audio']['deviceState']} peak={d['audio']['peakPlayed']:.3f} capture={d['capture']['width']}x{d['capture']['height']} gpu={d['window']['adapter']!r}")
except Exception as e:
    print(f"live=unreadable({e})")
PY
)
      SUMMARY[$S]="${SUMMARY[$S]} $LIVE"
      (cd "$D" && $ONEGAME android stop --package "$PKG" --json) >/dev/null 2>&1
    else
      fail "solitaire live: no endpoint"
    fi
  fi

  if [ "$S" = lanterns ] && [ -n "$CLUSTER" ]; then
    say "lanterns: servers on $LAN, bot on the phone"
    (cd "$D" && dotnet publish Server/Lanterns.Server.csproj -c Debug -o bin -v quiet -clp:NoSummary && dotnet publish Lanterns.csproj -c Debug -o bin -v quiet -clp:NoSummary) > "$W/publish.log" 2>&1 || fail "lanterns desktop build"
    (cd "$D" && ONEGAME_CLUSTER_BIND=0.0.0.0 LANTERNS_HOST=$LAN LANTERNS_TIMEOUT=${LANTERNS_TIMEOUT:-400} ONEGAME="$ONEGAME" \
       $ONEGAME cluster test cluster/play-android.yaml --spec cluster-android.yaml --json) > "$W/cluster.log" 2>&1
    CJ=$(lastjson "$W/cluster.log"); echo "$CJ" > "$W/cluster.json"
    if [ "$(echo "$CJ" | jget - ok 2>/dev/null)" = True ]; then
      SUMMARY[$S]="${SUMMARY[$S]} cluster=agree+0drift via $LAN"
    else
      fail "lanterns cluster: $(tail -c 300 "$W/cluster.log" | tr '\n' ' ')"
    fi
  fi
done

# --- 3. clean up ---
$ONEGAME android stop --json >/dev/null 2>&1
if [ -z "$KEEP" ]; then for p in "${PKGS[@]}"; do A uninstall "$p" >/dev/null 2>&1; done; fi
A forward --remove-all >/dev/null 2>&1

# --- 4. verdict ---
STATUS=PASS; [ ${#FAILS[@]} -gt 0 ] && STATUS=FAIL
LINE="ANDROID $STATUS $MODEL abi=$ABI sdk=$SDK"
for S in ${SAMPLES//,/ }; do [ -n "${SUMMARY[$S]:-}" ] && LINE="$LINE | ${SUMMARY[$S]}"; done
[ ${#FAILS[@]} -gt 0 ] && LINE="$LINE | failures: $(IFS=';'; echo "${FAILS[*]}")"
[ ${#NOTES[@]} -gt 0 ] && LINE="$LINE | notes: $(IFS=';'; echo "${NOTES[*]}")"
echo "$LINE"
echo "$LINE" > "$OUT/verdict.txt"
[ "$STATUS" = PASS ]
