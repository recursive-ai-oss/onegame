#!/usr/bin/env bash
# Runs onegame exports inside Valve's Steam Linux Runtime 3 ("sniper"), the environment Steam runs Linux games in (also on the Steam Deck).
# A test tool: nothing here ships. Needs bubblewrap (bwrap) and no Steam account or client.
#
#   tools/steamrt.sh fetch                      download and unpack the pinned runtime and SDK images (about 1.5 GB download, 4 GB unpacked)
#   tools/steamrt.sh run [--x11] [--wayland] [--nvidia] <game-folder> <cmd>
#                                               run an exported game folder (mounted read-only at /game) in the runtime image.
#                                               The sandbox has no network and no display unless asked:
#                                                 --x11      pass the X11 display named by $DISPLAY (its socket, and $XAUTHORITY if set)
#                                                 --wayland  pass the Wayland socket named by $WAYLAND_DISPLAY in $XDG_RUNTIME_DIR
#                                                 --nvidia   pass the host's NVIDIA driver (device nodes, libraries, Vulkan ICD) so the game can draw;
#                                                            the image has a Vulkan loader but no driver, and Steam normally supplies one
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
  shift
  extra=()
  while [ $# -gt 0 ]; do
    case "$1" in
    --x11)
      # An X display is a Unix socket in /tmp/.X11-unix, so it crosses the sandbox as a file even though the network is unshared.
      d="${DISPLAY:-}"
      case "$d" in :*) ;; *) echo "steamrt: --x11 needs a local \$DISPLAY such as :0 (got '${d}')" >&2; exit 2 ;; esac
      n="${d#:}"; n="${n%%.*}"
      [ -S "/tmp/.X11-unix/X$n" ] || { echo "steamrt: no X socket at /tmp/.X11-unix/X$n" >&2; exit 2; }
      extra+=(--bind "/tmp/.X11-unix/X$n" "/tmp/.X11-unix/X$n" --setenv DISPLAY ":$n")
      xauth="${XAUTHORITY:-}"
      if [ -n "$xauth" ] && [ -f "$xauth" ]; then extra+=(--ro-bind "$xauth" /tmp/.Xauthority --setenv XAUTHORITY /tmp/.Xauthority); fi
      ;;
    --wayland)
      w="${WAYLAND_DISPLAY:-}"; x="${XDG_RUNTIME_DIR:-}"
      [ -n "$w" ] && [ -n "$x" ] || { echo "steamrt: --wayland needs WAYLAND_DISPLAY and XDG_RUNTIME_DIR" >&2; exit 2; }
      case "$w" in /*) sock="$w" ;; *) sock="$x/$w" ;; esac
      [ -S "$sock" ] || { echo "steamrt: no Wayland socket at $sock" >&2; exit 2; }
      extra+=(--bind "$sock" "/tmp/xdg/wayland-0" --setenv XDG_RUNTIME_DIR /tmp/xdg --setenv WAYLAND_DISPLAY wayland-0)
      ;;
    --nvidia)
      # The driver's libraries are built against a very old glibc, so they load in this runtime; Mesa's, built for a newer one, do not.
      lib=/usr/lib/x86_64-linux-gnu
      [ -e "$lib/libGLX_nvidia.so.0" ] || { echo "steamrt: no NVIDIA driver libraries in $lib" >&2; exit 2; }
      for dev in /dev/nvidia*; do [ -e "$dev" ] && extra+=(--dev-bind "$dev" "$dev"); done
      [ -d /dev/dri ] && extra+=(--dev-bind /dev/dri /dev/dri)
      for f in "$lib"/libGLX_nvidia.so* "$lib"/libnvidia-*.so* "$lib"/libEGL_nvidia.so*; do
        [ -e "$f" ] && extra+=(--ro-bind "$f" "/tmp/gpu/lib/$(basename "$f")")
      done
      icd="$(mktemp "${TMPDIR:-/tmp}/steamrt-icd.XXXXXX")"
      printf '{"file_format_version":"1.0.1","ICD":{"library_path":"/tmp/gpu/lib/libGLX_nvidia.so.0","api_version":"1.3.0"}}\n' > "$icd"
      extra+=(--ro-bind "$icd" /tmp/gpu/nvidia_icd.json --setenv VK_ICD_FILENAMES /tmp/gpu/nvidia_icd.json --setenv LD_LIBRARY_PATH /tmp/gpu/lib)
      ;;
    *) break ;;
    esac
    shift
  done
  # The runtime image has no keyboard layout data, which xkbcommon needs to open any window. Steam's own container shares the
  # host's copy; do the same when a display is passed.
  if [ ${#extra[@]} -gt 0 ] && [ -d /usr/share/X11/xkb ]; then
    mkdir -p "$home/runtime/files/usr/share/X11/xkb"
    extra+=(--ro-bind /usr/share/X11/xkb /usr/share/X11/xkb)
  fi
  game="$(readlink -f "${1:?game folder}")"; shift
  r="$home/runtime/files"
  mkdir -p "$r/dev" "$r/proc" "$r/tmp" "$r/game"
  # Order matters: /tmp is a fresh tmpfs, and the display sockets are bound on top of it.
  exec bwrap --unshare-all --die-with-parent --ro-bind "$r" / --dev /dev --proc /proc --tmpfs /tmp --ro-bind "$game" /game \
    ${extra[@]+"${extra[@]}"} --chdir /tmp --setenv HOME /tmp --setenv PATH /usr/bin:/bin "$@"
  ;;
build)
  work="$(readlink -f "${2:?work folder}")"; shift 2
  d="$home/sdk"
  mkdir -p "$d/dev" "$d/proc" "$d/tmp" "$d/dotnet" "$d/nuget" "$d/engine" "$d/work" "$d/etc"
  touch "$d/etc/resolv.conf"
  exec bwrap --unshare-pid --unshare-ipc --die-with-parent --share-net --ro-bind "$d" / --dev /dev --proc /proc --tmpfs /tmp \
    --ro-bind "$dotnet_root" /dotnet --bind "$nuget" /nuget --ro-bind "$engine" /engine --bind "$work" /work \
    --ro-bind /etc/resolv.conf /etc/resolv.conf --ro-bind /etc/ssl /etc/ssl --chdir /work \
    --setenv HOME /tmp --setenv TMPDIR /tmp --setenv PATH /dotnet:/usr/bin:/bin --setenv DOTNET_ROOT /dotnet --setenv DOTNET_CLI_HOME /tmp/cli \
    --setenv NUGET_PACKAGES /nuget --setenv DOTNET_NOLOGO 1 --setenv DOTNET_CLI_TELEMETRY_OPTOUT 1 --setenv MSBUILDDISABLENODEREUSE 1 "$@"
  ;;
*)
  sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'
  exit 2
  ;;
esac
