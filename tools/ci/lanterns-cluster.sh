#!/usr/bin/env bash
# Builds Lanterns' server and client with development tools, then runs one scenario on a cluster of five
# processes (a master, two islands, two bots) over real sockets.
#
#   tools/ci/lanterns-cluster.sh <onegame command> <out folder>
set -eu
cli=$1
out=$2
mkdir -p "$out"
cd "$(dirname "$0")/../../samples/lanterns"
rm -rf bin
dotnet publish Server/Lanterns.Server.csproj -c Debug -o bin -v quiet -clp:NoSummary
dotnet publish Lanterns.csproj -c Debug -o bin -v quiet -clp:NoSummary
status=0
"$cli" cluster test cluster/play.yaml --spec cluster.yaml --json > "$out/lanterns-cluster.json" || status=$?
head -c 600 "$out/lanterns-cluster.json"; echo
exit "$status"
