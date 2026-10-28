#!/usr/bin/env bash
# Shared by the package smoke scripts: pack every project under src/ into a folder feed that stands in for nuget.org, and
# write a nuget.config that uses it for RecursiveAI.OneGame.* and the real nuget.org for everything else.
#
#   . tools/ci/package-feed.sh; pack_feed <repo> <work folder>      # sets $version and $count
#   write_nuget_config <work folder> <folder to put nuget.config in>

step() { printf '\n== %s\n' "$*"; }

pack_feed() {
  local repo=$1 work=$2 project wrong
  version=$(sed -n 's|.*<Version>\(1\.[^<]*\)</Version>.*|\1|p' "$repo/Directory.Build.props" | head -1)
  [ -n "$version" ] || { echo "cannot read the engine version from Directory.Build.props"; return 1; }
  step "pack every packable project under src/ (version $version)"
  (cd "$repo" && for project in src/*/*.csproj; do
    dotnet pack "$project" -c Release -o "$work/feed" -v quiet -clp:NoSummary || exit 1
  done)
  count=$(ls "$work/feed"/*.nupkg | wc -l | tr -d ' ')
  echo "$count packages"
  python3 "$(dirname "${BASH_SOURCE[0]:-$0}")/check-packages.py" "$work/feed" "$version" || return 1
}

write_nuget_config() {
  cat > "$2/nuget.config" <<CFG
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$1/feed" />
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
CFG
}
