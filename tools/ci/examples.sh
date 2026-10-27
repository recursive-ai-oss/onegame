#!/usr/bin/env bash
# Runs every example's scenarios with the onegame command, and the cluster scenario of an example that has a cluster.yaml
# (its server and client are published with development tools into the example's bin/ first). Stops with a failure status if
# any example fails, after running all of them.
#
#   tools/ci/examples.sh <onegame command> <out folder>
set -u
cli=$1
out=$2
mkdir -p "$out"
root="$(cd "$(dirname "$0")/../.." && pwd)/examples"
failed=()

for dir in "$root"/*/; do
  name=$(basename "$dir")
  [ -d "$dir/scenarios" ] || continue
  echo "== $name"
  # Each example is built by this run: a warning is an error in every project, so a green run means zero warnings.
  (cd "$dir" && "$cli" test --json --no-unit > "$out/$name-test.json") || failed+=("$name")
  head -c 300 "$out/$name-test.json"; echo
  if [ -f "$dir/cluster.yaml" ]; then
    (
      cd "$dir" && rm -rf bin &&
      dotnet publish server/*.csproj -c Debug -o bin -v quiet -clp:NoSummary &&
      dotnet publish ./*.csproj -c Debug -o bin -v quiet -clp:NoSummary &&
      "$cli" cluster test cluster/play.yaml --spec cluster.yaml --json > "$out/$name-cluster.json"
      status=$?
      head -c 300 "$out/$name-cluster.json"; echo
      exit $status
    ) || failed+=("$name (cluster)")
  fi
done

if [ ${#failed[@]} -gt 0 ]; then
  echo "failed: ${failed[*]}"
  exit 1
fi
echo "all examples passed"
