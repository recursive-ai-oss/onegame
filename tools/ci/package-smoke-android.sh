#!/usr/bin/env bash
# Exports an Android APK from packages alone, with and without Google Play Billing. Packs the engine into a local folder that
# stands in for nuget.org, installs the onegame tool from it, creates a game and exports it. Needs a Linux x86-64 host with the
# Android SDK and NDK (ANDROID_HOME), a JDK and clang. No engine checkout is involved: ONEGAME_ROOT is unset and the work folder
# is outside the repository. The APKs are left in <work>/plain-export and <work>/billing-export for an emulator run.
#
#   tools/ci/package-smoke-android.sh [work folder]
#
# The packages go to <work>/feed, the game to <work>/app/mygame. NuGet's cache for the run is <work>/nuget, so nothing from an
# earlier run or from the machine's own cache can hide a missing file.
set -eu
repo="$(cd "$(dirname "$0")/../.." && pwd)"
work=${1:-${RUNNER_TEMP:-${TMPDIR:-/tmp}}/package-smoke-android}
version=$(sed -n 's|.*<Version>\(1\.[^<]*\)</Version>.*|\1|p' "$repo/Directory.Build.props" | head -1)
[ -n "$version" ] || { echo "cannot read the engine version from Directory.Build.props"; exit 1; }

unset ONEGAME_ROOT

rm -rf "$work"
mkdir -p "$work/feed" "$work/app" "$work/tools"
# pwd -W gives the Windows form of the path on Git Bash; the folders go into files that dotnet reads.
work="$(cd "$work" && { pwd -W 2>/dev/null || pwd; })"
export NUGET_PACKAGES="$work/nuget"
export ONEGAME_PLAY_BILLING_DIR="$work/play-billing"
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

# A game that sells through Play: the same game with AndroidPlayBilling on. The Billing Library comes from Google's Maven
# repository at export time (pinned hashes), the Java wrapper from the tool itself.
export_apk() {
  variant=$1
  step "onegame export --target android ($variant)"
  "$og" export --target android --dev --out "$work/$variant-export" --json | tee "$work/$variant-export.json"
  grep -q '^{"ok":true' "$work/$variant-export.json" || { echo "the export failed"; exit 1; }
  apk=$(ls "$work/$variant-export"/*.apk)
  unzip -l "$apk" | grep -E 'lib/.*\.so|classes\.dex' || true
  unzip -l "$apk" | grep -q 'lib/arm64-v8a/' || { echo "no arm64-v8a library"; exit 1; }
  unzip -l "$apk" | grep -q 'lib/x86_64/' || { echo "no x86_64 library"; exit 1; }
  if [ "$variant" = billing ]; then
    unzip -l "$apk" | grep -q 'classes.dex' || { echo "no classes.dex in the Play Billing APK"; exit 1; }
  else
    if unzip -l "$apk" | grep -q 'classes.dex'; then echo "unexpected classes.dex"; exit 1; fi
  fi
}

og="$work/tools/onegame"
mkdir -p "$work/app-android"
cp "$work/app/nuget.config" "$work/app-android/"
cd "$work/app-android"
step "onegame new mygame"
"$og" new mygame --json
cd mygame
export_apk plain
sed -i 's|</PropertyGroup>|  <AndroidPlayBilling>true</AndroidPlayBilling>\n  </PropertyGroup>|' mygame.csproj
grep -q AndroidPlayBilling mygame.csproj || { echo "could not enable Play Billing in the project"; exit 1; }
export_apk billing

echo
echo "android package smoke: ok ($count packages; plain and Play Billing APKs from packages alone)"
