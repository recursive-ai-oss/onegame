#!/usr/bin/env bash
# Downloads the pinned Vulkan registry used by OneGame.VkBindGen and checks its hash.
# Usage: tools/fetch-vkxml.sh <output-dir>
set -euo pipefail
url="https://raw.githubusercontent.com/KhronosGroup/Vulkan-Docs/v1.4.341/xml/vk.xml"
sha="006807a47518e9ab23ecf7f4c95d936aab477a20d27a9a7ac23cedefb7210178"
out="${1:?output directory}/vk.xml"
mkdir -p "$(dirname "$out")"
curl -fsSL -o "$out" "$url"
echo "$sha  $out" | sha256sum -c -
