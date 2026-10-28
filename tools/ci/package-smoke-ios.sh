#!/usr/bin/env bash
# Exports an iOS Simulator app from packages alone, then runs it in the Simulator. Packs the engine into a local folder that
# stands in for nuget.org, installs the onegame tool from it, creates a game, exports it for the Simulator and checks that a
# scenario reaches the same state hash in the Simulator as on the Mac. Needs an Apple-silicon Mac with Xcode and a Simulator
# runtime. No engine checkout is involved: ONEGAME_ROOT is unset and the work folder is outside the repository.
#
#   tools/ci/package-smoke-ios.sh [work folder]
#
# The packages go to <work>/feed, the game to <work>/app/mygame. NuGet's cache for the run is <work>/nuget, so nothing from an
# earlier run or from the machine's own cache can hide a missing file.
set -eu
repo="$(cd "$(dirname "$0")/../.." && pwd)"
work=${1:-${RUNNER_TEMP:-${TMPDIR:-/tmp}}/package-smoke-ios}
version=$(sed -n 's|.*<Version>\(1\.[^<]*\)</Version>.*|\1|p' "$repo/Directory.Build.props" | head -1)
[ -n "$version" ] || { echo "cannot read the engine version from Directory.Build.props"; exit 1; }

unset ONEGAME_ROOT

rm -rf "$work"
mkdir -p "$work/feed" "$work/app" "$work/tools"
# pwd -W gives the Windows form of the path on Git Bash; the folders go into files that dotnet reads.
work="$(cd "$work" && { pwd -W 2>/dev/null || pwd; })"
export NUGET_PACKAGES="$work/nuget"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$work/cli-home}"

step() { printf '\n== %s\n' "$*"; }

step "pack every packable project under src/ (version $version)"
cd "$repo"
for project in src/*/*.csproj; do
  dotnet pack "$project" -c Release -o "$work/feed" -v quiet -clp:NoSummary
done
ls "$work/feed"/*.nupkg | sed 's|.*/||'
count=$(ls "$work/feed"/*.nupkg | wc -l | tr -d ' ')
echo "$count packages"
wrong=$(ls "$work/feed"/*.nupkg | grep -v "\.$version\.nupkg$" || true)
[ -z "$wrong" ] || { echo "packages not at $version:"; echo "$wrong"; exit 1; }

step "no dependency outside the one* family"
py=$(command -v python3 || command -v python)
"$py" - "$work/feed" <<'EOF'
import glob, re, sys, zipfile
bad = []
for path in sorted(glob.glob(sys.argv[1] + "/*.nupkg")):
    with zipfile.ZipFile(path) as z:
        spec = z.read([n for n in z.namelist() if n.endswith(".nuspec")][0]).decode("utf-8-sig")
    for dep in re.findall(r'<dependency id="([^"]+)"', spec):
        if not (dep.startswith("RecursiveAI.One") or dep == "OneYaml"):
            bad.append(path.split("/")[-1] + " -> " + dep)
if bad:
    print("\n".join(bad))
    sys.exit(1)
print("ok")
EOF

cd "$work/app"
# The local folder stands in for nuget.org; the one* siblings and the .NET packs come from the real one.
cat > nuget.config <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$work/feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local">
      <package pattern="RecursiveAI.OneGame.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

step "dotnet tool install RecursiveAI.OneGame.Cli $version"
dotnet tool install --tool-path "$work/tools" RecursiveAI.OneGame.Cli --version "$version"
og="$work/tools/onegame"
"$og" --version --json

og="$work/tools/onegame"
mkdir -p "$work/app-ios"
cp "$work/app/nuget.config" "$work/app-ios/"
cd "$work/app-ios"
step "onegame new mygame"
"$og" new mygame --json
cd mygame

step "onegame export --target ios-simulator"
"$og" export --target ios-simulator --dev --out "$work/export" --json | tee "$work/export.json"
grep -q '^{"ok":true' "$work/export.json" || { echo "the export failed"; exit 1; }
app=$(find "$work/export" -maxdepth 2 -name "*.app" | head -1)
[ -d "$app" ] || { echo "no .app in the export"; ls -R "$work/export" | head -20; exit 1; }
echo "$app"
for f in art/ship.png fonts/NotoSans-Regular.ttf; do [ -f "$app/$f" ] || { echo "the game's $f is not in the app"; exit 1; }; done

# The state scenario only: the look scenario needs a rendered frame (a capture loads the picture), and a hosted Mac may give the
# Simulator no GPU. The content check above (art and font inside the app) stands in for it.
mkdir -p "$work/scn"
cp scenarios/steer-ship.yaml "$work/scn/"

step "scenarios on the Mac"
"$og" test --scenario "$work/scn" --no-unit --json | tee "$work/mac-test.json"

# Prints "name hash passed" per scenario from either tool's JSON.
summary() {
  python3 - "$1" "$2" <<'PY'
import json, sys
t = open(sys.argv[1], errors="replace").read()
d = json.loads(t[t.find('{"ok"'):])
node = d["scenarios"] if sys.argv[2] == "mac" else d["result"]
for s in sorted(node["scenarios"], key=lambda s: s["name"]):
    print(s["name"], s["hash"], s["passed"])
PY
}

device=
cleanup() {
  [ -n "$device" ] && xcrun simctl shutdown "$device" >/dev/null 2>&1 || true
  [ -n "$device" ] && xcrun simctl delete "$device" >/dev/null 2>&1 || true
}
trap cleanup EXIT

step "boot an iPhone Simulator"
# The newest iOS runtimes, each with the iPhone types it lists as supported (a runtime rejects other types: SimError 403). Take
# the first pair that creates and boots.
pairs=$(xcrun simctl list runtimes available -j | python3 -c 'import json,sys
rs=[r for r in json.load(sys.stdin)["runtimes"] if r["identifier"].startswith("com.apple.CoreSimulator.SimRuntime.iOS") and r.get("isAvailable", True)]
rs.sort(key=lambda r: [int(x) if x.isdigit() else 0 for x in r.get("version","0").split(".")], reverse=True)
for r in rs:
    for d in reversed([d for d in r.get("supportedDeviceTypes", []) if "iPhone" in d["name"]]):
        print(r["identifier"], d["identifier"])')
echo "$pairs" | head -5
[ -n "$pairs" ] || { echo "no iOS Simulator runtime with an iPhone type on this Mac"; exit 1; }
while read -r runtime devtype; do
  if device=$(xcrun simctl create u67-smoke "$devtype" "$runtime") && xcrun simctl boot "$device"; then break; fi
  echo "cannot use $devtype on $runtime"
  [ -z "$device" ] || xcrun simctl delete "$device" >/dev/null 2>&1 || true
  device=
done <<< "$pairs"
[ -n "$device" ] || { echo "no iPhone Simulator could be created and booted"; xcrun simctl list devicetypes runtimes | head -30; exit 1; }
xcrun simctl bootstatus "$device" -b >/dev/null
echo "simulator $device ($runtime)"

step "onegame ios test (scenario in the Simulator)"
"$og" ios test --device "$device" --app "$app" --scenario "$work/scn" --json | tee "$work/sim-test.json"
summary "$work/mac-test.json" mac | tee "$work/mac.txt"
summary "$work/sim-test.json" sim | tee "$work/sim.txt"
grep -q False "$work/mac.txt" "$work/sim.txt" && { echo "a scenario failed"; exit 1; }
cmp "$work/mac.txt" "$work/sim.txt" || { echo "state hashes differ between the Mac and the Simulator"; exit 1; }

echo
echo "ios package smoke: ok ($count packages; Simulator app from packages alone, same hash as the Mac)"
