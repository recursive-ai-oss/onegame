#!/usr/bin/env bash
# Builds a game from packages alone. Packs the engine into a local folder that stands in for nuget.org, installs the
# onegame tool from it, and runs the first steps a developer takes: new, run, test, export, run the export. No engine
# checkout is involved: ONEGAME_ROOT is unset and the work folder is outside the repository.
#
#   tools/ci/package-smoke.sh <export target: linux|win|osx> [work folder]
#
# The packages go to <work>/feed, the game to <work>/app/mygame. NuGet's cache for the run is <work>/nuget, so nothing from an
# earlier run or from the machine's own cache can hide a missing file.
set -eu
target=${1:?usage: package-smoke.sh <linux|win|osx> [work folder]}
repo="$(cd "$(dirname "$0")/../.." && pwd)"
work=${2:-${RUNNER_TEMP:-${TMPDIR:-/tmp}}/package-smoke}
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

step "onegame new mygame"
"$og" new mygame --json
cd mygame
cat mygame.csproj
ls scenarios

step "onegame run --headless --frames 60 --json"
"$og" run --headless --frames 60 --json | tee "$work/run.json"
grep -q '"ok":true' "$work/run.json"

step "onegame test --json"
"$og" test --json | tee "$work/test.json"
grep -q '"ok":true' "$work/test.json"

step "onegame export --target $target"
"$og" export --target "$target" --out "$work/export" --json | tee "$work/export.json"
grep -q '"runtime":"nativeaot"' "$work/export.json" || { echo "the export is not NativeAOT"; exit 1; }
exe=$(ls "$work"/export/*/Mygame.exe "$work"/export/*/Mygame 2>/dev/null | head -1)
[ -n "$exe" ] || { echo "no Mygame program in the export"; ls -R "$work/export" | head -30; exit 1; }

step "run the exported game headless"
"$exe" --headless --frames 60 --seed 42 --gpu off --json | tee "$work/exported-run.json"
grep -q '"ok":true' "$work/exported-run.json"

echo
echo "package smoke: ok ($count packages, target $target)"
