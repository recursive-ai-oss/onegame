#!/usr/bin/env bash
# Runs onegame exports inside Valve's Steam Linux Runtime 3 ("sniper"), the environment Steam runs Linux games in (also on the Steam Deck).
# A test tool: nothing here ships. Needs bubblewrap (bwrap) and no Steam account or client.
#
#   tools/steamrt.sh fetch                      download and unpack the pinned runtime and SDK images (about 1.5 GB download, 4 GB unpacked)
#   tools/steamrt.sh run <game-folder> <cmd>    run an exported game folder (mounted read-only at /game) in the runtime image
#   tools/steamrt.sh build <work-folder> <cmd>  run a build in the SDK image: the work folder is /work, the engine checkout /engine
#                                               (read-only), the .NET SDK /dotnet and the NuGet cache /nuget are mounted too
#
# STEAMRT picks where the images live (default ~/.cache/onegame/steamrt). DOTNET_ROOT (default ~/.dotnet) is the .NET SDK to mount.
set -euo pipefail
version="3.0.20261021.266025"
base="https://repo.steampowered.com/steamrt-images-sniper/snapshots/$version"
runtime_sha="12bbd6bbeab9749e6f147848cc7b7d745c308fa80d1a8835ca134ac4dcefb6d0"
sdk_sha="8f71947035f97925a311b2ed753f22efcf2f13d6f1631945b8e537f8903ce2cf"
home="${STEAMRT:-$HOME/.cache/onegame/steamrt}"
engine="$(cd "$(dirname "$0")/.." && pwd)"
dotnet_root="${DOTNET_ROOT:-$HOME/.dotnet}"
nuget="${NUGET_PACKAGES:-$HOME/.nuget/packages}"

fetch_one() { # name file sha target
  [ -d "$home/$1" ] && return 0
  mkdir -p "$home"
  curl -fsSL -o "$home/$1.tar.gz" "$base/$2"
  echo "$3  $home/$1.tar.gz" | sha256sum -c -
  mkdir -p "$home/$1" && tar -xzf "$home/$1.tar.gz" -C "$home/$1" && rm "$home/$1.tar.gz"
}

case "${1:-}" in
fetch)
  fetch_one runtime "com.valvesoftware.SteamRuntime.Platform-amd64%2Ci386-sniper-runtime.tar.gz" "$runtime_sha"
  fetch_one sdk "com.valvesoftware.SteamRuntime.Sdk-amd64%2Ci386-sniper-sysroot.tar.gz" "$sdk_sha"
  ;;
run)
  game="$(readlink -f "${2:?game folder}")"; shift 2
  r="$home/runtime/files"
  mkdir -p "$r/dev" "$r/proc" "$r/tmp" "$r/game"
  exec bwrap --unshare-all --die-with-parent --ro-bind "$r" / --dev /dev --proc /proc --tmpfs /tmp --ro-bind "$game" /game \
    --chdir /tmp --setenv HOME /tmp --setenv PATH /usr/bin:/bin "$@"
  ;;
build)
  work="$(readlink -f "${2:?work folder}")"; shift 2
  d="$home/sdk"
  mkdir -p "$d/dev" "$d/proc" "$d/tmp" "$d/dotnet" "$d/nuget" "$d/engine" "$d/work" "$d/etc"
  touch "$d/etc/resolv.conf"
  exec bwrap --unshare-pid --unshare-ipc --die-with-parent --share-net --ro-bind "$d" / --dev /dev --proc /proc --tmpfs /tmp \
    --ro-bind "$dotnet_root" /dotnet --bind "$nuget" /nuget --ro-bind "$engine" /engine --bind "$work" /work \
    --ro-bind /etc/resolv.conf /etc/resolv.conf --ro-bind /etc/ssl /etc/ssl --chdir /work \
    --setenv HOME /tmp --setenv PATH /dotnet:/usr/bin:/bin --setenv DOTNET_ROOT /dotnet --setenv DOTNET_CLI_HOME /tmp/cli \
    --setenv NUGET_PACKAGES /nuget --setenv DOTNET_NOLOGO 1 --setenv DOTNET_CLI_TELEMETRY_OPTOUT 1 --setenv MSBUILDDISABLENODEREUSE 1 "$@"
  ;;
*)
  sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'
  exit 2
  ;;
esac
