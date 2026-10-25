#!/usr/bin/env bash
# Downloads the Khronos glTF-Sample-Assets models the importer tests use, pinned to one commit and checked against
# tools/gltf-samples.sha256. Output goes to the folder given as $1 (default: $ONEGAME_GLTF_SAMPLES), one folder per model
# with the same variant folders the repository uses. The models are test data only; each has its own licence in its
# LICENSE.md/README.md, which is downloaded next to it.
#
#   tools/fetch-gltf-samples.sh <dir>            download and verify
#   tools/fetch-gltf-samples.sh <dir> --record   download and rewrite tools/gltf-samples.sha256
set -euo pipefail

COMMIT=8f492390ddc7a2ae221a1369ef560589cf8c70fb
BASE=https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/$COMMIT/Models
HERE=$(cd "$(dirname "$0")" && pwd)
DEST=${1:-${ONEGAME_GLTF_SAMPLES:-}}
MODE=${2:-verify}
[ -n "$DEST" ] || { echo "usage: $0 <dir> [--record]" >&2; exit 2; }
mkdir -p "$DEST"
SUMS=$HERE/gltf-samples.sha256

# model/variant pairs; the glTF variants pull their external files by reading the uris out of the .gltf.
MODELS=(
  Box/glTF Box/glTF-Binary Box/glTF-Embedded
  BoxTextured/glTF BoxTextured/glTF-Binary BoxTextured/glTF-Embedded
  Duck/glTF Duck/glTF-Binary Duck/glTF-Embedded
  DamagedHelmet/glTF DamagedHelmet/glTF-Binary DamagedHelmet/glTF-Embedded
  Sponza/glTF
  Fox/glTF Fox/glTF-Binary
  AnimatedMorphCube/glTF AnimatedMorphCube/glTF-Binary AnimatedMorphCube/glTF-Quantized
  SimpleSparseAccessor/glTF SimpleSparseAccessor/glTF-Embedded
  TextureTransformTest/glTF AlphaBlendModeTest/glTF
  MetalRoughSpheres/glTF MetalRoughSpheres/glTF-Binary MetalRoughSpheres/glTF-Embedded
  NormalTangentTest/glTF NormalTangentMirrorTest/glTF
  EmissiveStrengthTest/glTF EmissiveStrengthTest/glTF-Binary
  InterpolationTest/glTF
  CesiumMan/glTF-Binary CesiumMan/glTF-Embedded
)

fetch() { # relative path under Models
  local rel=$1 out=$DEST/$1
  mkdir -p "$(dirname "$out")"
  [ -s "$out" ] || curl -fsSL --retry 3 -o "$out" "$BASE/$(printf '%s' "$rel" | sed 's/ /%20/g')"
}

for mv in "${MODELS[@]}"; do
  model=${mv%%/*}; variant=${mv##*/}
  for lic in LICENSE.md README.md; do fetch "$model/$lic" || true; done
  # the file name of the variant comes from the repository's own index
  file=$(curl -fsSL "$BASE/model-index.json" | jq -r --arg m "$model" --arg v "$variant" '.[] | select(.name==$m) | .variants[$v]')
  [ "$file" != null ] || { echo "no $mv in the index" >&2; exit 1; }
  fetch "$model/$variant/$file"
  if [ "$variant" != glTF-Binary ]; then
    jq -r '[.buffers[]?.uri, .images[]?.uri] | .[] | select(. != null and (startswith("data:") | not))' "$DEST/$model/$variant/$file" |
      while read -r uri; do
        decoded=$(python3 -c 'import sys,urllib.parse;print(urllib.parse.unquote(sys.argv[1]))' "$uri")
        fetch "$model/$variant/$decoded"
      done
  fi
done

if [ "$MODE" = --record ]; then
  (cd "$DEST" && find . -type f ! -name 'LICENSE.md' ! -name 'README.md' | sort | xargs sha256sum) > "$SUMS"
  echo "recorded $(wc -l < "$SUMS") files"
else
  (cd "$DEST" && sha256sum --check --quiet "$SUMS") && echo "verified $(wc -l < "$SUMS") files"
fi
