#!/usr/bin/env bash
# Checks the samples on a Steam Deck (or any SteamOS machine) over ssh and prints a one-line verdict.
#
#   tools/devices/deck.sh [--host ADDRESS] [--user deck] [--samples solitaire,lanterns,bricks] [--lan-host ADDRESS]
#                         [--out DIR] [--no-cluster] [--keep]
#
# Finds the Deck (find-host.py: $DECK_HOST, then steamdeck.local, then a scan of the local network for ssh), builds Linux NativeAOT programs
# with development tools, copies them over, and per sample runs the scenario folder on the Deck (headless, drawing on its GPU)
# and compares every scenario's state hash with a run here. For Solitaire it then opens a window on the Deck's desktop session,
# drives it over live control through an ssh tunnel (touch injected into the platform's queue, the audio device, a presented frame,
# background and back), and looks at the gamepads the platform layer sees. Lanterns also joins a cluster as a client of servers
# here. If Steam's Linux runtime ("sniper") is installed there, one more run goes through it. Everything started is stopped and the
# copied folders are removed unless --keep.
#
# The Deck needs ssh on and this computer's public key in ~/.ssh/authorized_keys of the user. Environment: ONEGAME (the onegame
# command), DECK_HOST, DECK_USER, DECK_SSH_KEY (a key file other than the default), DECK_REMOTE_ENV (extra NAME=value words for the
# programs run there, for a machine standing in for a Deck), CAPTURE_TOLERANCE (default 48) and CAPTURE_MAX_DIFF (default 0.02), as for android.sh.
set -uo pipefail
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
ONEGAME=${ONEGAME:-onegame}
HOST=${DECK_HOST:-}; USERN=${DECK_USER:-deck}; SAMPLES=; LAN=; OUT=; CLUSTER=1; KEEP=
while [ $# -gt 0 ]; do case $1 in
  --host) HOST=$2; shift 2;; --user) USERN=$2; shift 2;; --samples) SAMPLES=$2; shift 2;; --lan-host) LAN=$2; shift 2;;
  --out) OUT=$2; shift 2;; --no-cluster) CLUSTER=; shift;; --keep) KEEP=1; shift;; *) echo "unknown option $1" >&2; exit 2;; esac; done
OUT=${OUT:-${ONEGAME_ARTIFACTS:-$ROOT/artifacts}/device-deck}
TOL=${CAPTURE_TOLERANCE:-48}; MAXD=${CAPTURE_MAX_DIFF:-0.02}
mkdir -p "$OUT"
say() { echo "[deck] $*" >&2; }
FAILS=(); NOTES=(); LINES=()
fail() { FAILS+=("$1"); say "FAIL $1"; }
jget() { python3 -c 'import json,sys
d=json.load(open(sys.argv[1], errors="replace")) if sys.argv[1]!="-" else json.loads(sys.stdin.read())
for k in sys.argv[2].split("."):
    d=d[int(k)] if k.isdigit() else d.get(k) if isinstance(d,dict) else None
print("" if d is None else d)' "$@"; }
lastjson() { python3 -c 'import sys
t=open(sys.argv[1],errors="replace").read(); i=t.find("{\"ok\"")
print(t[i:] if i>=0 else "{}")' "$1"; }

# --- 1. find it ---
if [ -z "$HOST" ]; then
  HOST=$(python3 "$HERE/find-host.py" --user "$USERN" ${DECK_SSH_KEY:+--ssh-key "$DECK_SSH_KEY"} 2> "$OUT/find-host.log") || { cat "$OUT/find-host.log" >&2; echo "DECK MISSING no SteamOS machine answered ssh as $USERN (ssh on? this computer's key authorized? same network?)"; exit 1; }
  say "found $HOST ($(tail -1 "$OUT/find-host.log"))"
fi
SSHO=(-o BatchMode=yes -o ConnectTimeout=8 -o StrictHostKeyChecking=accept-new ${DECK_SSH_KEY:+-i "$DECK_SSH_KEY"})
R() { ssh "${SSHO[@]}" "$USERN@$HOST" "$@"; }
R true 2>/dev/null || { echo "DECK MISSING cannot ssh to $USERN@$HOST with a key"; exit 1; }
OSREL=$(R '. /etc/os-release; echo "$PRETTY_NAME $VERSION_ID"'); KERNEL=$(R uname -r)
GPU=$(R 'vulkaninfo --summary 2>/dev/null | grep -m1 deviceName | sed "s/.*= //"' ); [ -n "$GPU" ] || GPU=$(R 'lspci 2>/dev/null | grep -i vga | head -1 | cut -d: -f3')
say "deck $HOST: $OSREL kernel $KERNEL gpu ${GPU:-unknown}"
SESSION=$(R 'ls /run/user/$(id -u) 2>/dev/null | tr "\n" " "')
# What display is there? Desktop mode has an X or Wayland socket under the user's runtime folder; game mode runs gamescope.
UID_R=$(R id -u)
XSOCK=$(R 'ls /tmp/.X11-unix 2>/dev/null | head -1'); WSOCK=$(R "ls /run/user/$UID_R 2>/dev/null | grep -m1 '^wayland-[0-9]*$'")
MODE=none; [ -n "$XSOCK" ] && MODE=x11; R 'pgrep -x gamescope >/dev/null' && MODE="gamescope($MODE)"
say "display: mode=$MODE x=${XSOCK:-none} wayland=${WSOCK:-none}"
REMOTE='$HOME/onegame-check'
RENV=${DECK_REMOTE_ENV:-}

# --- 2. samples ---
[ -n "$SAMPLES" ] || { SAMPLES="solitaire,lanterns"; [ -d "$ROOT/samples/bricks" ] && SAMPLES="$SAMPLES,bricks"; }
declare -A SUMMARY
R "mkdir -p $REMOTE"
for S in ${SAMPLES//,/ }; do
  D=$ROOT/samples/$S
  [ -d "$D" ] || { NOTES+=("$S not in this tree"); continue; }
  W=$OUT/$S; mkdir -p "$W"
  say "$S: build linux-x64 NativeAOT with development tools"
  # `onegame export` makes release builds, which leave the development tools out; the same publish in Debug keeps them.
  (cd "$D" && dotnet publish -c Debug -r linux-x64 -p:OneGameAot=true -o "$W/publish" -v quiet -clp:NoSummary) > "$W/publish.log" 2>&1 || { fail "$S build"; tail -c 300 "$W/publish.log" >&2; continue; }
  FOLDER=$W/publish; EXE=$(basename "$(ls "$D"/*.csproj | head -1)" .csproj)
  [ -x "$FOLDER/$EXE" ] || EXE=$(find "$FOLDER" -maxdepth 1 -type f -perm -u+x ! -name '*.so' ! -name '*.sh' | head -1 | xargs -r basename)
  R "rm -rf $REMOTE/$S; mkdir -p $REMOTE/$S"
  tar -C "$FOLDER" -cf - . | R "tar -C $REMOTE/$S -xf -" || { fail "$S copy"; continue; }
  tar -C "$D" -cf - scenarios | R "tar -C $REMOTE/$S -xf -"
  say "$S: desktop reference run"
  (cd "$D" && $ONEGAME test --no-unit --json) > "$W/desktop.json" 2>&1 || NOTES+=("$S: reference run failed")
  say "$S: scenarios on the Deck"
  R "cd $REMOTE/$S && env $RENV ./$EXE --headless --scenario scenarios --artifacts artifacts --json --capture-tolerance $TOL --capture-max-diff $MAXD" > "$W/device.json" 2> "$W/device.err"
  CMP=$(python3 "$HERE/compare-hashes.py" "$W/desktop.json" "$W/device.json" 2>&1 | tail -1); echo "$CMP" > "$W/compare.json"
  H=$(echo "$CMP" | jget - hashEqual 2>/dev/null); N=$(echo "$CMP" | jget - scenarios 2>/dev/null); P=$(echo "$CMP" | jget - passed 2>/dev/null)
  SUMMARY[$S]="$S scenarios $P/$N hash-equal $H/$N"
  [ -n "$N" ] && [ "$H" = "$N" ] || fail "$S: state hashes differ from this computer (or no result: $(tail -c 200 "$W/device.err" | tr '\n' ' '))"
  [ "$P" = "$N" ] || NOTES+=("$S: $(( ${N:-0} - ${P:-0} )) scenario(s) failed (captures past tolerance $TOL/$MAXD?)")

  if [ "$S" = solitaire ]; then
    if [ "$MODE" = none ]; then
      NOTES+=("solitaire windowed run skipped: no display session on the Deck (log in to desktop mode)")
    else
      say "solitaire: windowed run in the Deck's session, live control through a tunnel"
      BACKEND=x11; ENVS="DISPLAY=:${XSOCK#X} XDG_RUNTIME_DIR=/run/user/$UID_R"
      [ -n "$XSOCK" ] || { BACKEND=wayland; ENVS="WAYLAND_DISPLAY=$WSOCK XDG_RUNTIME_DIR=/run/user/$UID_R"; }
      R "cd $REMOTE/$S && { env $ENVS $RENV nohup ./$EXE --live tcp:7878 --realtime --backend $BACKEND --seed 1 > live.log 2>&1 & echo \$! > live.pid; }"
      sleep 5
      ssh "${SSHO[@]}" -f -N -o ExitOnForwardFailure=yes -L 17878:127.0.0.1:7878 "$USERN@$HOST"
      TUN=$(pgrep -n -f "17878:127.0.0.1:7878 $USERN@$HOST")
      python3 "$HERE/live-check.py" 127.0.0.1:17878 "$W/live" --expect-audio alsa > "$W/live.json" 2> "$W/live.err" || fail "solitaire live: $(jget "$W/live.json" problems 2>/dev/null | cut -c1-200) $(tail -c 200 "$W/live.err" | tr '\n' ' ')"
      SUMMARY[$S]="${SUMMARY[$S]} $(python3 - "$W/live.json" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1]))
    print(f"touch={'ok' if d['finger'].get('stock_drew_a_card') else 'FAIL'} audio={d['audio']['backend']}/{d['audio']['deviceState']} peak={d['audio']['peakPlayed']:.3f} capture={d['capture']['width']}x{d['capture']['height']} gpu={d['window']['adapter']!r}")
except Exception as e:
    print(f"live=unreadable({e})")
PY
)"
      [ -n "${TUN:-}" ] && kill "$TUN" 2>/dev/null
      R "kill \$(cat $REMOTE/$S/live.pid) 2>/dev/null; sleep 1; kill -9 \$(cat $REMOTE/$S/live.pid) 2>/dev/null; true"
    fi
    # Steam's own Linux runtime, when the Deck has it (Steam installs it for games that ask).
    RT=$(R 'ls -d $HOME/.local/share/Steam/steamapps/common/SteamLinuxRuntime_sniper 2>/dev/null | head -1')
    if [ -n "$RT" ]; then
      HR=$(R "cd $REMOTE/$S && $RT/_v2-entry-point --verb=run -- ./$EXE --headless --frames 60 --json 2>&1 | tail -1")
      HL=$(cd "$D" && $ONEGAME run --headless --frames 60 --json 2>&1 | tail -1)
      [ "$(echo "$HR" | jget - hash 2>/dev/null)" = "$(echo "$HL" | jget - hash 2>/dev/null)" ] && SUMMARY[$S]="${SUMMARY[$S]} steam-runtime=hash-equal" || NOTES+=("steam runtime run differs or failed: $(echo "$HR" | cut -c1-160)")
    else
      NOTES+=("Steam Linux Runtime (sniper) not installed on the Deck: no run through it")
    fi
  fi

  if [ "$S" = lanterns ] && [ -n "$CLUSTER" ]; then
    [ -n "$LAN" ] || LAN=$(hostname -I | tr ' ' '\n' | grep -F "${HOST%.*}." | head -1)
    if [ -z "$LAN" ]; then NOTES+=("lanterns cluster skipped: no address of this computer on the Deck's network"); else
      say "lanterns: servers on $LAN, bot on the Deck"
      (cd "$D" && dotnet publish Server/Lanterns.Server.csproj -c Debug -o bin -v quiet -clp:NoSummary && dotnet publish Lanterns.csproj -c Debug -o bin -v quiet -clp:NoSummary) > "$W/publish.log" 2>&1 || fail "lanterns desktop build"
      (cd "$D" && ONEGAME_CLUSTER_BIND=0.0.0.0 LANTERNS_HOST=$LAN SSH_TARGET="$USERN@$HOST" SSH_OPTIONS="${SSHO[*]}" LANTERNS_REMOTE="$REMOTE/lanterns/$EXE" LANTERNS_TIMEOUT=400 \
         $ONEGAME cluster test cluster/play-ios.yaml --spec cluster-deck.yaml --json) > "$W/cluster.log" 2>&1
      if [ "$(lastjson "$W/cluster.log" | jget - ok 2>/dev/null)" = True ]; then SUMMARY[$S]="${SUMMARY[$S]} cluster=agree+0drift via $LAN"; else fail "lanterns cluster: $(tail -c 300 "$W/cluster.log" | tr '\n' ' ')"; fi
    fi
  fi
done

# --- 3. the gamepads the platform layer sees on the Deck ---
PADDIR=$OUT/padprobe
if dotnet publish "$HERE/PadProbe/PadProbe.csproj" -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o "$PADDIR" -v quiet -clp:NoSummary > "$OUT/padprobe-publish.log" 2>&1; then
  R "mkdir -p $REMOTE && cat > $REMOTE/padprobe && chmod +x $REMOTE/padprobe" < "$PADDIR/padprobe" 2>/dev/null
  PADS=$(R "cd $REMOTE && ./padprobe --seconds 4 --quiet" 2>&1 | tail -1)
  echo "$PADS" > "$OUT/pads.json"
  SUMMARY[pads]="pads: $(echo "$PADS" | python3 -c 'import json,sys
try:
    d=json.loads(sys.stdin.read()); print("; ".join(p["name"]+(" ["+p["dbEntry"]+"]" if p["dbEntry"] else "") for p in d["pads"]) or "none seen (Steam grabs the Deck controls while it runs)")
except Exception: print("probe failed")')"
else
  NOTES+=("padprobe did not build; no pad listing")
fi

# --- 4. clean up ---
[ -n "$KEEP" ] || R "rm -rf $REMOTE"

STATUS=PASS; [ ${#FAILS[@]} -gt 0 ] && STATUS=FAIL
LINE="DECK $STATUS $OSREL gpu=${GPU:-?} mode=$MODE"
for S in ${SAMPLES//,/ } pads; do [ -n "${SUMMARY[$S]:-}" ] && LINE="$LINE | ${SUMMARY[$S]}"; done
[ ${#FAILS[@]} -gt 0 ] && LINE="$LINE | failures: $(IFS=';'; echo "${FAILS[*]}")"
[ ${#NOTES[@]} -gt 0 ] && LINE="$LINE | notes: $(IFS=';'; echo "${NOTES[*]}")"
echo "$LINE"; echo "$LINE" > "$OUT/verdict.txt"
[ "$STATUS" = PASS ]
