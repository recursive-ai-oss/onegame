#!/usr/bin/env bash
# Builds, runs and exports a game server from packages alone (Linux). Packs the engine into a local folder that stands in for
# nuget.org, installs the onegame tool from it, then: onegame new --server, run the server, export it NativeAOT without plugins
# and with the plugin host, start the exported plugin host with the game's plugin, and let an exported game client connect and
# agree with it. No engine checkout is involved: ONEGAME_ROOT is unset and the work folder is outside the repository.
#
#   tools/ci/package-smoke-server.sh [work folder]
set -eu
repo="$(cd "$(dirname "$0")/../.." && pwd)"
work=${1:-${RUNNER_TEMP:-${TMPDIR:-/tmp}}/package-smoke-server}
version=$(sed -n 's|.*<Version>\(1\.[^<]*\)</Version>.*|\1|p' "$repo/Directory.Build.props" | head -1)
[ -n "$version" ] || { echo "cannot read the engine version from Directory.Build.props"; exit 1; }

unset ONEGAME_ROOT
# A development run: a server given no key makes a development key for itself (a real server is given one from 'onegame keys').
export ONEGAME_DEV=1
export ONEGAME_KEYS="$work/dev-keys"
rm -rf "$work"
mkdir -p "$work/feed" "$work/app" "$work/tools"
work="$(cd "$work" && pwd)"
export NUGET_PACKAGES="$work/nuget"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$work/cli-home}"

step() { printf '\n== %s\n' "$*"; }

# Nothing started here may outlive the script: the plugin host and the cluster's processes watch this one (--parent) and
# are stopped here as well.
cleanup() {
  status=$?
  [ -z "${host_pid:-}" ] || kill "$host_pid" 2>/dev/null || true
  [ ! -x "$work/tools/onegame" ] || (cd "$work/app/mygame" 2>/dev/null && "$work/tools/onegame" cluster down --dir "$work/cluster" >/dev/null 2>&1) || true
  left=$(pgrep -af "$work" | grep -v "pgrep\|package-smoke-server" || true)
  if [ -n "$left" ]; then echo "processes left running:"; echo "$left"; pkill -f "$work" || true; [ "$status" -ne 0 ] || status=1; fi
  exit "$status"
}
trap cleanup EXIT

step "pack every packable project under src/ (version $version)"
cd "$repo"
for project in src/*/*.csproj; do
  dotnet pack "$project" -c Release -m:2 -o "$work/feed" -v quiet -clp:NoSummary
done
count=$(ls "$work/feed"/*.nupkg | wc -l | tr -d ' ')
echo "$count packages"
unzip -l "$work/feed/RecursiveAI.OneGame.Cli.$version.nupkg" | grep -q 'tools/net10.0/any/server-host/onegame-server.dll' || { echo "the tool package carries no plugin host"; exit 1; }

cd "$work/app"
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
dotnet tool install --tool-path "$work/tools" --configfile "$work/app/nuget.config" RecursiveAI.OneGame.Cli --version "$version"
og="$work/tools/onegame"

step "onegame new mygame --server"
"$og" new mygame --server --json
cd mygame
ls . server plugin

step "run the server for 120 ticks"
dotnet run --project server -c Release -- --config server/server.yaml --listen udp:0 --ticks 120 2>&1 | tee "$work/server-run.log" | tail -5
grep -q '"stopped":true' "$work/server-run.log" || { echo "the server did not stop cleanly"; exit 1; }

step "export the server: NativeAOT, no plugins"
"$og" export --server --project server/mygame-server.csproj --out "$work/aot" --no-archive --json | tee "$work/aot.json"
grep -q '"runtime":"nativeaot"' "$work/aot.json" || { echo "the server export is not NativeAOT"; exit 1; }
aot=$(ls "$work"/aot/*/MygameServer)
"$aot" --scope 1:1 --listen udp:0 --ticks 120 2>&1 | tee "$work/aot-run.log" | tail -3
grep -q '"stopped":true' "$work/aot-run.log" || { echo "the exported server did not stop cleanly"; exit 1; }

step "build the plugin and export the plugin host"
dotnet build plugin -c Release -v quiet -clp:NoSummary
ls plugin/bin/Release/net10.0/MygamePlugin.dll
"$og" export --server --plugins --out "$work/host" --no-archive --json | tee "$work/host.json"
grep -q '"runtime":"coreclr-framework-dependent"' "$work/host.json" || { echo "the plugin host export is the wrong kind"; exit 1; }
host=$(ls -d "$work"/host/*)
test -f "$host/onegame-server.dll" && test -x "$host/run.sh"

step "the plugin host loads the plugin and serves it"
# The cluster gives the host its scope, port and live socket; the file only names the plugin.
cat > plugin/cluster-host.yaml <<'EOF'
name: mygame-plugins
plugins:
  - { name: counter, path: bin/Release/net10.0/MygamePlugin.dll, scopes: [1] }
EOF

step "export the game client"
"$og" export --target linux --out "$work/game" --no-archive --json | tee "$work/game.json"
client=$(ls "$work"/game/*/Mygame)

cat > cluster.yaml <<EOF
name: server-from-packages
startupTimeout: 60000
nodes:
  - name: host
    role: server
    exe: $host/run.sh
    listen: udp
    scopes: [{ id: 1, seed: 1 }]
    args: [--config, $PWD/plugin/cluster-host.yaml]
  - name: p1
    role: client
    exe: $client
    release: true
    connect: host
    transport: udp
    scope: 1
    args: [--until-tick, "300"]
EOF
cat > cluster-test.yaml <<'EOF'
name: plugin-host-agrees
steps:
  - wait: { node: p1, tick: 300, timeout: 60000 }
expect:
  - state: { entity: Counter, property: Count, min: 0 }
    node: host
  - agree: { server: host, clients: [p1] }
  - drift: { nodes: [host, p1], equals: 0 }
EOF
"$og" cluster test cluster-test.yaml --spec cluster.yaml --dir "$work/cluster" --json | tee "$work/cluster.json"
grep -q '"ok":true' "$work/cluster.json" || { echo "the cluster check failed"; exit 1; }

echo
echo "server package smoke: ok ($count packages, version $version)"
