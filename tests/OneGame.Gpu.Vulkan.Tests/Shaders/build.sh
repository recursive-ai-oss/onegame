#!/usr/bin/env bash
# Rebuilds the checked-in SPIR-V fixtures with the Khronos glslang reference compiler (a test tool, not shipped).
set -euo pipefail
cd "$(dirname "$0")"
for f in *.vert *.frag *.comp; do
  glslang -V --target-env vulkan1.3 -o "$f.spv" "$f"
done
