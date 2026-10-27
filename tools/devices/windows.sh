#!/usr/bin/env bash
# Checks the samples on a real Windows machine over ssh and prints a one-line verdict.
#
#   tools/devices/windows.sh --host ADDRESS [--user NAME] [--key FILE] [--samples solitaire,bricks,lanterns] [--lan-host ADDRESS]
#                            [--base FOLDER] [--out DIR] [--tests] [--no-validation] [--no-cluster] [--keep]
#
# Builds everything on the machine (a .NET SDK, and the Khronos validation layer, are installed under one user folder, nothing system-wide),
# then per sample runs the scenario folder drawing on the machine's GPU with validation on and compares every scenario's state hash with a
# run here; reports the captures' largest per-channel difference from the committed references. For Solitaire it opens a window in the
# logged-in user's session and drives it over live control through an ssh tunnel: touch, the audio device with strict counters (written,
# played and peak above 0, no underruns), a presented frame, background and back, a real F3 key press for the debug overlay. The particles
# example is shown the same way. Lanterns joins a cluster as a client of servers here. Last, `onegame export --target win` is built there
# (NativeAOT), run from another folder and its hash compared with a development run here. --tests also runs the GPU, window, audio and
# platform test apps there. Everything started is stopped, and the folders are removed unless --keep (the SDK and layer stay).
#
# ssh runs in a session with no desktop, so windows, the GPU and the audio device are reached through a one-shot scheduled task that runs as
# the logged-in user (the user must be logged in; nobody is logged out, nothing is rebooted). Environment: ONEGAME (the onegame command here),
# WIN_HOST, WIN_USER, WIN_SSH_KEY, WIN_ADAPTER (a word of the GPU's name for the tests, default: the first word of the adapter's name),
# CAPTURE_TOLERANCE and CAPTURE_MAX_DIFF (loosen every capture check; default: the scenario files' own).
set -uo pipefail
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
ONEGAME=${ONEGAME:-onegame}
HOST=${WIN_HOST:-}; USERN=${WIN_USER:-}; KEY=${WIN_SSH_KEY:-}; SAMPLES=solitaire,bricks,lanterns; LAN=; BASE=; OUT=; TESTS=; VALID=1; CLUSTER=1; KEEP=
while [ $# -gt 0 ]; do case $1 in
  --host) HOST=$2; shift 2;; --user) USERN=$2; shift 2;; --key) KEY=$2; shift 2;; --samples) SAMPLES=$2; shift 2;; --lan-host) LAN=$2; shift 2;;
  --base) BASE=$2; shift 2;; --out) OUT=$2; shift 2;; --tests) TESTS=1; shift;; --no-validation) VALID=; shift;; --no-cluster) CLUSTER=; shift;;
  --keep) KEEP=1; shift;; *) echo "unknown option $1" >&2; exit 2;; esac; done
[ -n "$HOST" ] && [ -n "$USERN" ] || { echo "usage: windows.sh --host ADDRESS --user NAME (or WIN_HOST and WIN_USER)" >&2; exit 2; }
OUT=${OUT:-${ONEGAME_ARTIFACTS:-$ROOT/artifacts}/device-windows}
mkdir -p "$OUT"
say() { echo "[windows] $*" >&2; }
FAILS=(); NOTES=(); declare -A SUMMARY
fail() { FAILS+=("$1"); say "FAIL $1"; }
jget() { python3 -c 'import json,sys
d=json.load(open(sys.argv[1], errors="replace")) if sys.argv[1]!="-" else json.loads(sys.stdin.read())
for k in sys.argv[2].split("."):
    d=d[int(k)] if k.isdigit() else d.get(k) if isinstance(d,dict) else None
print("" if d is None else d)' "$@"; }
lastjson() { python3 -c 'import sys
t=open(sys.argv[1],errors="replace").read(); i=t.find("{\"ok\"")
print(t[i:] if i>=0 else "{}")' "$1"; }

SSHO=(-o BatchMode=yes -o ConnectTimeout=8 -o ServerAliveInterval=10 -o ServerAliveCountMax=3 -o StrictHostKeyChecking=accept-new ${KEY:+-i "$KEY"})
TARGET="$USERN@$HOST"
R() { ssh "${SSHO[@]}" "$TARGET" "$@"; }
PUT() { scp -q "${SSHO[@]}" "$1" "$TARGET:$2"; }
GET() { scp -q "${SSHO[@]}" "$TARGET:$1" "$2" 2>/dev/null; }
R "echo ok" 2>/dev/null | grep -q ok || { echo "WINDOWS MISSING cannot ssh to $TARGET with a key"; exit 1; }
UP=$(R 'echo %USERPROFILE%' | tr -d '\r'); BASE=${BASE:-$UP\\onegame}
OSVER=$(R ver | tr -d '\r' | grep -i windows | head -1)
GPU=$(R 'powershell -NoProfile -Command "(Get-CimInstance Win32_VideoController | % Name) -join \"; \""' | tr -d '\r')
say "$TARGET: $OSVER; gpu $GPU; state under $BASE"
ADAPTER=${WIN_ADAPTER:-$(echo "$GPU" | awk '{print $1}')}
# relative to the home folder for scp
REL=${BASE#"$UP"\\}

# --- 1. toolchain, sources, the command ---
R "mkdir $BASE\\jobs 2>nul & mkdir $BASE\\out 2>nul & mkdir $BASE\\tools 2>nul" > /dev/null
for f in setup.ps1 sendkey.ps1 run-tests.ps1; do PUT "$HERE/windows/$f" "$REL/tools/$f"; done
SDKV=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["sdk"]["version"])' "$ROOT/global.json")
say "setting up the toolchain (first time: downloads the .NET SDK and the validation layer)"
R "powershell -NoProfile -ExecutionPolicy Bypass -File $BASE\\tools\\setup.ps1 -Base $BASE -SdkVersion $SDKV $([ -z "$VALID" ] && echo -NoValidation)" > "$OUT/setup.log" 2>&1 || { tail -5 "$OUT/setup.log" >&2; fail "toolchain setup"; }
grep -q 'validation layer at' "$OUT/setup.log" || { VALID=; NOTES+=("no validation layer on the machine: runs without validation"); }
grep -q 'C++ tools: none' "$OUT/setup.log" && NOTES+=("no Visual Studio C++ tools on the machine: no NativeAOT export")
say "copying the sources"
( cd "$ROOT" && (git ls-files; git ls-files --others --exclude-standard) | sort -u | tar -czf "$OUT/src.tgz" -T - ) && PUT "$OUT/src.tgz" "$REL/src.tgz" && R "mkdir $BASE\\src 2>nul & cd /d $BASE & tar -xzf src.tgz -C src" || fail "copying sources"
say "building the onegame command there"
R "cd /d $BASE\\src && call $BASE\\env.cmd && dotnet publish src\\OneGame.Cli -c Release -r win-x64 -o $BASE\\cli -v quiet -clp:NoSummary" > "$OUT/cli-publish.log" 2>&1
CLIV=$(R "$BASE\\cli\\onegame.exe --version --json" 2>/dev/null | tr -d '\r')
[ -n "$CLIV" ] || { tail -8 "$OUT/cli-publish.log" >&2; echo "WINDOWS FAIL the onegame command did not build there"; exit 1; }
say "command: $CLIV"

# Runs a cmd script (stdin) as the logged-in user through a one-shot task; prints its log. NOWAIT=1 returns at once (the task stays until TASKDEL).
IRUN() {
  local name=$1 tmo=${2:-600} body; body=$(cat)
  { echo '@echo off'; echo "call $BASE\\env.cmd"; echo "cd /d $BASE\\src"
    [ -n "$VALID" ] && { echo "set VK_ADD_LAYER_PATH=$BASE\\vk\\sdk\\Bin"; echo "set ONEGAME_GPU_VALIDATION=1"; echo "set ONEGAME_WINDOWED_ADAPTER=$ADAPTER"; }
    printf '%s\n' "$body"; echo "echo done > $BASE\\jobs\\$name.done"; } | sed 's/$/\r/' > "$OUT/$name.cmd"
  printf 'CreateObject("Wscript.Shell").Run "cmd /c %s\\jobs\\%s.cmd > %s\\jobs\\%s.log 2>&1", 0, True\r\n' "$BASE" "$name" "$BASE" "$name" > "$OUT/$name.vbs"
  R "del $BASE\\jobs\\$name.done 2>nul" > /dev/null
  PUT "$OUT/$name.cmd" "$REL/jobs/$name.cmd"; PUT "$OUT/$name.vbs" "$REL/jobs/$name.vbs"
  R "schtasks /create /tn onegame_$name /tr \"wscript $BASE\\jobs\\$name.vbs\" /sc once /st 00:00 /it /ru $USERN /rl limited /f >nul 2>&1 && schtasks /run /tn onegame_$name >nul" > /dev/null
  if [ -z "${NOWAIT:-}" ]; then
    for _ in $(seq 1 $((tmo / 3))); do R "if exist $BASE\\jobs\\$name.done (echo yes) else (echo no)" | grep -q yes && break; sleep 3; done
    R "schtasks /delete /tn onegame_$name /f" > /dev/null 2>&1
    GET "$REL/jobs/$name.log" "$OUT/$name.log"; tr -d '\r' < "$OUT/$name.log" 2>/dev/null
  fi
}
# Teardown runs on every way out (finish, failure, interrupt): the tunnel here, then on the machine every task and program this check starts.
STOPALL() {
  [ -n "${TUN:-}" ] && kill "$TUN" 2>/dev/null
  R "for %t in (live part export key tests scen-solitaire scen-bricks scen-lanterns) do schtasks /delete /tn onegame_%t /f >nul 2>&1 & for %p in (Solitaire ParticlesExample Lanterns) do taskkill /im %p.exe /f >nul 2>&1" > /dev/null 2>&1
  true
}
trap STOPALL EXIT INT TERM

# Starts a windowed dev build (project folder relative to src, exe name) with live control on a port, tunnels it here. Sets TUN and PORTL.
START_WINDOW() {
  local name=$1 proj=$2 exe=$3 port=$4 extra=${5:-}
  NOWAIT=1 IRUN "$name" <<EOF
${extra}
cd $proj
dotnet build -c Debug -v quiet -clp:NoSummary
cd /d $BASE\\art\\bin\\$exe\\debug
$exe.exe --live tcp:$port --realtime --seed 1
EOF
  for _ in $(seq 1 60); do R "netstat -ano | findstr :$port" | grep -q LISTEN && break; sleep 3; done
  R "netstat -ano | findstr :$port" | grep -q LISTEN || return 1
  PORTL=$((port + 10000))
  ssh "${SSHO[@]}" -f -N -o ExitOnForwardFailure=yes -L "$PORTL:127.0.0.1:$port" "$TARGET" && TUN=$(pgrep -n -f "$PORTL:127.0.0.1:$port $TARGET")
  sleep 3
}
STOP_WINDOW() { local name=$1 exe=$2; R "taskkill /im $exe.exe /f" > /dev/null 2>&1; R "schtasks /delete /tn onegame_$name /f" > /dev/null 2>&1; [ -n "${TUN:-}" ] && kill "$TUN" 2>/dev/null; TUN=; }

# --- 2. samples: scenarios on the GPU, hashes against a run here ---
SCEN_OPTS=""; [ -n "${CAPTURE_TOLERANCE:-}" ] && SCEN_OPTS="$SCEN_OPTS --capture-tolerance $CAPTURE_TOLERANCE"; [ -n "${CAPTURE_MAX_DIFF:-}" ] && SCEN_OPTS="$SCEN_OPTS --capture-max-diff $CAPTURE_MAX_DIFF"
for S in ${SAMPLES//,/ }; do
  D=$ROOT/samples/$S; W=$OUT/$S; mkdir -p "$W"
  [ -d "$D" ] || { NOTES+=("$S not in this tree"); continue; }
  say "$S: scenarios here (reference) and there"
  (cd "$D" && $ONEGAME test --no-unit --json) > "$W/reference.json" 2>&1 || NOTES+=("$S: reference run failed")
  IRUN "scen-$S" 1500 > /dev/null <<EOF
cd samples\\$S
$BASE\\cli\\onegame.exe test --json --no-unit $SCEN_OPTS > $BASE\\out\\$S.json 2> $BASE\\out\\$S.err
EOF
  GET "$REL/out/$S.json" "$W/device.json"; GET "$REL/out/$S.err" "$W/device.err"
  CMP=$(python3 "$HERE/compare-hashes.py" "$W/reference.json" "$W/device.json" 2>&1 | tail -1); echo "$CMP" > "$W/compare.json"
  H=$(echo "$CMP" | jget - hashEqual 2>/dev/null); N=$(echo "$CMP" | jget - scenarios 2>/dev/null); P=$(echo "$CMP" | jget - passed 2>/dev/null)
  MAXD=$(python3 - "$W/device.json" <<'PY'
import json, re, sys
t = open(sys.argv[1], errors='replace').read()
d = json.JSONDecoder().raw_decode(t[t.find('{"ok"'):])[0]
mx = n = 0
for sc in d['scenarios']['scenarios']:
    for e in sc.get('expectations', []):
        if e['kind'] == 'capture':
            n += 1
            m = re.match(r'(\d+) px differ, max channel delta (\d+)', e['actual'])
            mx = max(mx, int(m.group(2))) if m else mx
print(f'{n} captures, largest channel delta {mx}')
PY
)
  SUMMARY[$S]="$S scenarios $P/$N hash-equal $H/$N; $MAXD"
  [ -n "$N" ] && [ "$H" = "$N" ] || fail "$S: state hashes differ from this computer (or no result)"
  [ "$P" = "$N" ] || fail "$S: $(( ${N:-0} - ${P:-0} )) scenario(s) failed"
done

# --- 3. Solitaire in a window: touch, sound, frame, background, F3 ---
if [[ ",$SAMPLES," == *,solitaire,* ]]; then
  say "solitaire: windowed run in the user's session"
  if START_WINDOW live samples\\solitaire Solitaire 7878; then
    python3 "$HERE/live-check.py" 127.0.0.1:$PORTL "$OUT/live" --expect-audio wasapi > "$OUT/live.json" 2> "$OUT/live.err" || fail "solitaire live: $(jget "$OUT/live.json" problems 2>/dev/null | cut -c1-240) $(tail -c 200 "$OUT/live.err" | tr '\n' ' ')"
    SUMMARY[live]=$(python3 - "$OUT/live.json" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1]))
    a = d['audio']
    print(f"window {d['window']['width']}x{d['window']['height']} on {d['window']['adapter']!r}, touch={'ok' if d['finger'].get('stock_drew_a_card') else 'FAIL'}, audio {a['backend']}/{a['deviceState']} written {a['framesWritten']} played {a['framesPlayed']} peak {a['peakPlayed']:.3f} underruns {a['underruns']}/{a['deviceUnderruns']}")
except Exception as e:
    print(f"live unreadable ({e})")
PY
)
    IRUN key 90 > "$OUT/key.log" <<EOF
powershell -NoProfile -ExecutionPolicy Bypass -File $BASE\\tools\\sendkey.ps1 -Process Solitaire -VirtualKey 0x72
EOF
    sleep 1
    python3 "$HERE/capture-check.py" 127.0.0.1:$PORTL "$OUT/solitaire-overlay.png" --expect-overlay > "$OUT/overlay.json" 2>&1 \
      && SUMMARY[overlay]="F3 shows the overlay: $OUT/solitaire-overlay.png" || fail "overlay after a real F3: $(tail -c 200 "$OUT/overlay.json" | tr '\n' ' ')"
  else fail "solitaire window did not start"; fi
  STOP_WINDOW live Solitaire
fi

# --- 4. particles example: sparks, a burst, the overlay shown from the first frame ---
if [ -d "$ROOT/examples/particles" ]; then
  say "particles example"
  if START_WINDOW part examples\\particles ParticlesExample 7879 "set ONEGAME_OVERLAY=on"; then
    python3 "$HERE/capture-check.py" 127.0.0.1:$PORTL "$OUT/particles.png" --command Torch:Hit --expect-overlay --expect-particles 10 > "$OUT/particles.json" 2>&1 \
      && SUMMARY[particles]="particles and overlay drawn: $OUT/particles.png" || fail "particles: $(tail -c 200 "$OUT/particles.json" | tr '\n' ' ')"
  else fail "particles window did not start"; fi
  STOP_WINDOW part ParticlesExample
fi

# --- 5. Lanterns: a client on the machine against servers here ---
if [[ ",$SAMPLES," == *,lanterns,* ]] && [ -n "$CLUSTER" ]; then
  [ -n "$LAN" ] || LAN=$(ip route get "$HOST" 2>/dev/null | grep -oP 'src \K\S+' | head -1)
  if [ -z "$LAN" ]; then NOTES+=("lanterns cluster skipped: no address of this computer on the machine's network (--lan-host)"); else
    say "lanterns: servers on $LAN, bot on the machine"
    D=$ROOT/samples/lanterns
    R "cd /d $BASE\\src && call $BASE\\env.cmd && dotnet build samples\\lanterns -c Debug -v quiet -clp:NoSummary" > "$OUT/lanterns-build.log" 2>&1
    (cd "$D" && dotnet publish Server/Lanterns.Server.csproj -c Debug -o bin -v quiet -clp:NoSummary && dotnet publish Lanterns.csproj -c Debug -o bin -v quiet -clp:NoSummary) > "$OUT/lanterns-publish.log" 2>&1 || fail "lanterns build here"
    (cd "$D" && ONEGAME_CLUSTER_BIND=0.0.0.0 LANTERNS_HOST=$LAN SSH_TARGET=$TARGET SSH_OPTIONS="${SSHO[*]}" LANTERNS_REMOTE="$BASE\\art\\bin\\Lanterns\\debug\\Lanterns.exe" LANTERNS_DOTNET="$BASE\\dotnet" LANTERNS_TIMEOUT=400 \
       $ONEGAME cluster test cluster/play-ios.yaml --spec cluster-windows.yaml --json) > "$OUT/cluster.log" 2>&1
    if [ "$(lastjson "$OUT/cluster.log" | jget - ok 2>/dev/null)" = True ]; then SUMMARY[cluster]="lanterns cluster: bot agrees with its island, 0 drift, via $LAN"; else fail "lanterns cluster: $(tail -c 300 "$OUT/cluster.log" | tr '\n' ' ')"; fi
  fi
fi

# --- 6. the exported program ---
say "export --target win, run from another folder"
IRUN export 1500 > "$OUT/export.log" <<EOF
rmdir /s /q $BASE\\export 2>nul
$BASE\\cli\\onegame.exe export --project samples\\solitaire --target win --out $BASE\\export --json > $BASE\\out\\export.json
cd /d %TEMP%
for /d %%d in ($BASE\\export\\*) do %%d\\Solitaire.exe --headless --frames 120 --seed 42 --gpu off --json > $BASE\\out\\export-run.json
for /d %%d in ($BASE\\export\\*) do %%d\\Solitaire.exe --live tcp:7890 --headless --frames 3
echo live exit %errorlevel%
EOF
GET "$REL/out/export.json" "$OUT/export.json"; GET "$REL/out/export-run.json" "$OUT/export-run.json"
HX=$(lastjson "$OUT/export-run.json" | jget - hash 2>/dev/null)
HL=$(cd "$ROOT/samples/solitaire" && $ONEGAME run --headless --frames 120 --seed 42 --gpu off --json 2>&1 | tail -1 | jget - hash 2>/dev/null)
RT=$(lastjson "$OUT/export.json" | jget - runtime 2>/dev/null)
if [ -n "$HX" ] && [ "$HX" = "$HL" ] && [ "$RT" = nativeaot ] && grep -q 'live exit 5' "$OUT/export.log"; then
  SUMMARY[export]="win-x64 export ($RT, $(lastjson "$OUT/export.json" | jget - files.0.size 2>/dev/null) byte program) hash $HX = development run, --live refused"
else fail "export: runtime '${RT:-none}', hash '${HX:-none}' vs here '${HL:-none}'"; fi

# --- 7. test apps that touch the GPU, windows, audio, platform ---
if [ -n "$TESTS" ]; then
  say "test apps on the GPU"
  IRUN tests 3000 > "$OUT/tests.txt" <<EOF
dotnet build OneGame.slnx -v quiet -clp:NoSummary -m:4
powershell -NoProfile -ExecutionPolicy Bypass -File $BASE\\tools\\run-tests.ps1 -Base $BASE -Apps OneGame.Gpu.Vulkan.Tests,OneGame.Render2D.Tests,OneGame.Windowed.Tests,OneGame.Platform.Tests,OneGame.Audio.Tests,OneGame.Draw2D.Tests,OneGame.UI.Kit.Tests
EOF
  scp -q -r "${SSHO[@]}" "$TARGET:$REL/art/test-logs" "$OUT/" 2>/dev/null
  BAD=$(grep 'exit=' "$OUT/tests.txt" | grep -v 'exit=0 .*Failed: 0,' | awk '{print $1}' | tr '\n' ' ')
  SUMMARY[tests]="tests: $(grep -c 'exit=0 .*Failed: 0,' "$OUT/tests.txt") apps passed${BAD:+, failed: $BAD}"
  [ -z "$BAD" ] || fail "test apps failed: $BAD(logs in $OUT/test-logs)"
fi

# --- 8. clean up ---
STOP_WINDOW live Solitaire; STOP_WINDOW part ParticlesExample
[ -n "$KEEP" ] || R "rmdir /s /q $BASE\\src $BASE\\art $BASE\\cli $BASE\\export $BASE\\out $BASE\\jobs 2>nul & del $BASE\\src.tgz 2>nul" > /dev/null 2>&1

STATUS=PASS; [ ${#FAILS[@]} -gt 0 ] && STATUS=FAIL
LINE="WINDOWS $STATUS $OSVER gpu=$GPU validation=$([ -n "$VALID" ] && echo on || echo off)"
for k in solitaire bricks lanterns live overlay particles cluster export tests; do [ -n "${SUMMARY[$k]:-}" ] && LINE="$LINE | ${SUMMARY[$k]}"; done
[ ${#FAILS[@]} -gt 0 ] && LINE="$LINE | failures: $(IFS=';'; echo "${FAILS[*]}")"
[ ${#NOTES[@]} -gt 0 ] && LINE="$LINE | notes: $(IFS=';'; echo "${NOTES[*]}")"
echo "$LINE"; echo "$LINE" > "$OUT/verdict.txt"
[ "$STATUS" = PASS ]
