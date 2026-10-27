#!/usr/bin/env bash
# Fails when a Linux program needs a glibc newer than 2.31, the one in the Steam Linux Runtime and so on the Steam Deck.
# Reads the symbol version names out of the file, so it needs nothing but grep.
#
#   tools/ci/glibc-baseline.sh <program>
set -eu
newest=$(grep -aoE 'GLIBC_2\.[0-9]+' "$1" | sed 's/GLIBC_2\.//' | sort -n | tail -1)
echo "$1 needs glibc 2.${newest:-0} (baseline 2.31)"
[ "${newest:-0}" -le 31 ] || { echo "glibc baseline exceeded"; exit 1; }
