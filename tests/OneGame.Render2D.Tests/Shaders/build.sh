#!/usr/bin/env bash
# Rebuilds the checked-in SPIR-V fixtures with the Khronos glslang reference compiler (a test tool, not shipped).
# The renderer's own shaders are the HLSL files in src/OneGame.Render2D/Shaders; at run time they come from the shader build
# package, and these fixtures stand in for it. Every fixture's entry point is named "main".
set -euo pipefail
cd "$(dirname "$0")"
src=../../../src/OneGame.Render2D/Shaders
build() { # file stage source-entry output-name
  glslang -D -V -S "$2" -e main --sep "$3" --target-env vulkan1.3 -o "$4.spv" "$1" 2>&1 | grep -v 'unrecognized attribute\|^$\|\.hlsl$' || true
  spirv-val --target-env vulkan1.3 "$4.spv"
}
for spec in Quad:VSMain:vert Quad:PSSprite:frag Quad:PSBlur:frag Quad:PSGrade:frag Tile:VSMain:vert Tile:PSIds:frag Tile:PSFlat:frag Ui:VSMain:vert Ui:PSMain:frag; do
  IFS=: read -r f e s <<< "$spec"
  build "$src/$f.hlsl" "$s" "$e" "$f.$e"
done
build Ripple.hlsl frag PSMain Ripple.PSMain
