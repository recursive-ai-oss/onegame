#!/usr/bin/env bash
# A physics scene must end in the same numbers on every CPU and runtime. Runs the probe natively and,
# with --web, on .NET's WebAssembly runtime under node, and compares each with the pinned list.
#
#   tools/ci/physics-probe.sh <work folder> [--web]
set -eu
repo="$(cd "$(dirname "$0")/../.." && pwd)"
work=$1
mkdir -p "$work"
expected="$repo/tools/OneGame.PhysicsProbe/expected.txt"
status=0

check() {
  if diff <(tr -d '\r' < "$2") "$expected" > /dev/null; then
    echo "$1: equal to the pinned values"
  else
    echo "$1: differs from the pinned values:"; diff <(tr -d '\r' < "$2") "$expected" || true; status=1
  fi
}

dotnet run --project "$repo/tools/OneGame.PhysicsProbe" > "$work/native.txt"
check native "$work/native.txt"

if [ "${3:-${2:-}}" = "--web" ]; then
  dotnet publish "$repo/tools/OneGame.PhysicsProbe.Web" -o "$work/web" -v quiet -clp:NoSummary
  node "$repo/tools/OneGame.PhysicsProbe.Web/run.mjs" "$work/web/wwwroot" > "$work/web.txt"
  check webassembly "$work/web.txt"
fi
exit $status
