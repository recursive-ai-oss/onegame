# glTF 2.0 import (CPU side)

`OneGame.Gltf` reads `.gltf` (JSON with data-URI or external buffers and images) and `.glb` into a plain model the 3D renderer
uploads and draws. It has no GPU code. It references the BCL and OneImage (PNG and JPEG decoding) only. All input is untrusted:
servers can ship content to clients, so every size is checked against `GltfLimits` before anything is allocated, and every failure is a
`GltfException` with a stable `Code`: damaged or hostile input is refused with that exception and nothing else (the fuzz targets below check this).

References: glTF 2.0 specification (Khronos) and the KHR extension specifications named below. The importer is written from those.

## Loading

```csharp
GltfModel model = GltfLoader.LoadFile("helmet.glb");                       // external files resolve under the file's folder
GltfModel model = GltfLoader.Load(bytes, options, resources);               // .gltf or .glb bytes; resources resolves external uris
```

`GltfLoadOptions`: `Limits` (below), `DecodeImages` (default true; false only reads each image's size), `Tangents`
(`TangentMode.WhenNormalMapped` default, `Always` regenerates even shipped tangents, `Never`), `GenerateNormals` (default true).
`IGltfResources.Read(uri, maxBytes)` supplies external files; `GltfFolderResources(root)` is the file-system one. `Load` without a
resolver refuses every external uri (`external_not_allowed`), which is the right default for content that arrives over the network.

**URI rules.** `data:` uris are base64 only and their decoded size is computed from the text length before decoding. Anything else must
be a relative path: percent-escapes are decoded, then a scheme (`http:`, `file:`), a leading `/` or `\`, a drive colon, a NUL, any `..`
segment, or a backslash is refused (`uri_not_allowed`). The folder resolver also checks that the full path stays under its root. The
importer never opens a network connection.

## Limits (`GltfLimits`, all configurable)

| Limit | Default | Checked |
|---|---|---|
| `MaxFileBytes` | 256 MiB | before reading a file or chunk |
| `MaxJsonBytes` | 32 MiB | JSON chunk / text length |
| `MaxJsonDepth` | 32 | by the JSON reader (no recursion on input depth) |
| `MaxBufferBytes`, `MaxTotalBufferBytes` | 256 MiB, 512 MiB | declared `byteLength`, before loading |
| `MaxAccessors`, `MaxBufferViews`, `MaxBuffers` | 100 000, 100 000, 256 | array lengths |
| `MaxNodes`, `MaxNodeDepth` | 100 000, 256 | counts and tree depth (iterative walk) |
| `MaxMeshes`, `MaxPrimitives`, `MaxMaterials`, `MaxTextures`, `MaxImages`, `MaxAnimations`, `MaxSkins` | 10 000 – 100 000 | array lengths |
| `MaxElementsPerAccessor` | 16 M | accessor `count`, including sparse |
| `MaxVerticesPerPrimitive`, `MaxIndicesPerPrimitive` | 16 M, 64 M | after strip/fan expansion and unwelding |
| `MaxMorphTargets`, `MaxJointsPerSkin` | 64, 1024 | per mesh, per skin |
| `MaxKeyframes` | 4 M | per animation sampler |
| `MaxImageDimension`, `MaxImagePixels` | 16 384, 64 M | read from the header by OneImage before pixels are decoded |
| `MaxOutputBytes` | 1 GiB | running total of everything the importer allocates for the model, charged before each allocation |

## The model (`GltfModel`)

Index-based, like the file: references are `int` indices into the lists, `-1` for none, all validated at load.
`Scenes` + `DefaultScene`, `Nodes`, `Meshes`, `Materials`, `Textures`, `Images`, `Samplers`, `Skins`, `Animations`, `Cameras`,
`Lights`, `ExtensionsUsed`, `ExtensionsRequired`, `Warnings` (`GltfWarning(Code, Message)`: things that loaded but were
degraded, for example an unsupported image format or an ignored attribute set).

Conventions:

- Right-handed, +Y up, metres; front faces counter-clockwise (glTF's own). Nothing is flipped.
- `System.Numerics` types. A glTF column-major matrix array is stored as is into a `Matrix4x4`, which is the transpose in
  `System.Numerics`' row-vector convention: use `v * M`, child world = `child.Local * parent.World`.
- **Nodes** carry `Translation`, `Rotation`, `Scale` and `Local` (a node given by `matrix` is decomposed; if it has shear the TRS fields
  hold a best fit and `Local` is exact), `Parent`, `Children`, `World` (rest pose), `Mesh`, `Skin`, `Camera`, `Light`, `Weights`.
  A node has at most one parent and the graph is acyclic (`node_graph` otherwise).
- **Mesh primitives** are *engine-ready*: always triangle, line or point **lists** with `uint` indices (strips, fans and line loops are
  expanded; non-indexed primitives get sequential indices). `Positions`, `Normals` always exist. When a triangle primitive has no
  normals they are generated flat, which unwelds its vertices (`NormalsGenerated`). `Tangents` (`Vector4`, w = ±1, bitangent =
  `cross(normal, tangent.xyz) * w` as in the glTF spec) exist when shipped or generated (`TangentsGenerated`). Generation follows the
  published MikkTSpace description: per-triangle tangent from the UV derivatives (V flipped to glTF's top-left origin), angle-weighted
  accumulation over vertices sharing position, normal and UV, vertices split where the UV orientation mirrors, Gram-Schmidt against the
  normal. `Uv0`, `Uv1`, `Colors` (RGBA, `Vector4`), `Joints` (`JointIndices`, 4 × ushort), `Weights` (`Vector4`) are null when absent.
  Normalized and quantized integer attributes (KHR_mesh_quantization, normalized UVs and colours) are converted to float.
  `MorphTargets` hold position/normal/tangent **deltas** (dense; sparse accessors are resolved). `Bounds` is the position AABB.
- **Packing for the GPU.** `Primitive.PackVertices()` writes the fixed 96-byte little-endian vertex below (absent attributes get the
  default shown), and `PackIndices()` returns 16-bit indices when every index fits, else 32-bit.

  | Offset | Size | Attribute | Default |
  |---|---|---|---|
  | 0 | 12 | position `float3` | — |
  | 12 | 12 | normal `float3` | (0,0,1) |
  | 24 | 16 | tangent `float4` | (0,0,0,1) |
  | 40 | 8 | uv0 `float2` | (0,0) |
  | 48 | 8 | uv1 `float2` | (0,0) |
  | 56 | 16 | color0 `float4` | (1,1,1,1) |
  | 72 | 8 | joints0 `ushort4` | 0 |
  | 80 | 16 | weights0 `float4` | (1,0,0,0) |

- **Materials** (`GltfMaterial`): metal/rough core (`BaseColorFactor/Texture`, `MetallicFactor`, `RoughnessFactor`,
  `MetallicRoughnessTexture`, `NormalTexture` + `NormalScale`, `OcclusionTexture` + `OcclusionStrength`, `EmissiveFactor/Texture`),
  `AlphaMode` (`Opaque`, `Mask`, `Blend`), `AlphaCutoff`, `DoubleSided`. `TextureRef` = texture index, UV set, and the
  KHR_texture_transform (`Offset`, `Rotation`, `Scale`, optional UV-set override) as a ready `Matrix3x2 UvMatrix` (row-vector
  convention, `uv' = uv * M`). Extensions parsed and exposed, rendering left to the consumer: `EmissiveStrength`
  (KHR_materials_emissive_strength), `Unlit` (KHR_materials_unlit), `Clearcoat`, `Transmission`, `Ior`, `Specular`, `Sheen`, `Volume`
  (null when absent). A primitive with `Material == -1` uses the glTF default material.
- **Textures/images/samplers.** `Image.Pixels` is straight-alpha RGBA8, top row first, only for PNG and JPEG (detected from the
  bytes, not the mime type); other formats leave `Pixels` null, `Status = Unsupported` and add a warning, so a model with a WebP texture
  still loads. `UsedAsColor` / `UsedAsData` tell the renderer which images are sRGB (base colour, emissive, sheen/specular colour) and
  which linear. A `Sampler` has `MagFilter`, `MinFilter`, `WrapS`, `WrapT` as enums.
- **Skins**: `Joints`, `InverseBindMatrices` (identity when absent), `Skeleton`. `skin.ComputeJointMatrices(world, meshNode, dest)`
  gives `inverse(meshNodeWorld) * jointWorld * inverseBind` in glTF terms.
- **Animations**: `AnimationClip` with `Channels` (node, `Translation | Rotation | Scale | Weights`, sampler) and `Samplers`
  (`Input` times, `Output` values, `Linear | Step | CubicSpline`), `Duration`. Evaluation is on the CPU:

  ```csharp
  var pose = model.CreateRestPose();                 // NodePose: per node T, R, S and morph weights
  clip.Apply(pose, timeSeconds);                     // overwrites the animated channels; time outside the keys is clamped
  pose.ComputeWorld(model, worldMatrices);           // Matrix4x4[] per node
  ```
  Linear rotation is a normalized spherical interpolation (shortest arc), step holds the earlier key, cubic spline uses the spec's
  Hermite basis with `(in-tangent, value, out-tangent)` triplets scaled by the key interval, rotations renormalized. Looping is the
  caller's (`time % Duration`).
- **Cameras** (`GltfCamera`: perspective `YFov`, `AspectRatio?`, `ZNear`, `ZFar?` (null = infinite); orthographic `XMag`, `YMag`,
  `ZNear`, `ZFar`) and **lights** (`GltfLight`: KHR_lights_punctual directional/point/spot with colour, intensity, range, cone angles).

## Extensions

Supported (parsed): KHR_lights_punctual, KHR_materials_emissive_strength, KHR_texture_transform, KHR_materials_unlit,
KHR_mesh_quantization, KHR_materials_clearcoat, _transmission, _ior, _specular, _sheen, _volume. An extension named in
`extensionsRequired` that is not on that list is `unsupported_extension`. Unknown entries in `extensionsUsed` only are ignored with a
warning (`ignored_extension`).

## Errors

`GltfException.Code` is one of: `file_too_large`, `glb_invalid`, `json_invalid` (including strings that are not UTF-8), `schema` (wrong
type, missing required field, number out of range), `reference` (index out of range), `limit_exceeded`, `buffer_range` (view or accessor
outside its buffer, or arithmetic overflow), `buffer_truncated`, `accessor_invalid`, `sparse_invalid`, `index_out_of_range`, `node_graph`
(a node with two parents, a cycle, a scene root that is not a root), `animation_invalid`, `skin_invalid`, `uri_not_allowed`,
`external_not_allowed`, `resource_missing`, `image_invalid`, `unsupported_extension`, `unsupported_version`.

Buffer views may overlap each other: the specification allows sharing data, and reading shared bytes is harmless. What is refused is
anything that reaches outside its buffer. `ComputeWorldBounds()` gives a scene's box at the rest pose.

Warnings (`GltfModel.Warnings`, at most 1000 per file) use these codes: `ignored_extension`, `ignored_attribute`, `ignored_animation_channel`,
`animated_matrix_node`, `unsupported_image`, `unsupported_texture_source`, `incomplete_shape`, `incomplete_skin`, `no_tangents`,
`quantization_without_extension`, `warnings_truncated`.

The default `MaxOutputBytes` is the bound on what a small file can make the importer allocate: a sparse accessor with no data, or the same
accessor named by many primitives, is charged each time it is materialised.

## Harness

`onegame asset inspect <file.gltf|file.glb> [--max-items N] [--json]` loads the file with image decoding off (headers only) and prints one
JSON object, or a readable summary without `--json`. Fields (all camelCase): `ok`, `file`, `format` (`gltf` or `glb`), `bytes`,
`generator`, `copyright`, `counts` (`scenes nodes meshes primitives vertices indices triangles lines points morphTargets materials
textures images samplers skins animations cameras lights`; vertices and indices are those the engine draws), `bounds` (`min`, `max`
of the default scene at rest, omitted when nothing is drawn), `nodes` (`count maxDepth rootCount roots withMesh skinned withCamera
withLight`), `meshes[]` (`index name primitives vertices indices morphTargets materials hasTangents skinned bounds`), `materials[]`
(`index name alphaMode doubleSided unlit textureSlots extensions`), `images[]` (`index name format mimeType width height status
usedAsColor usedAsData`), `animations[]` (`index name duration channels paths interpolations`), `extensions` (`used`, `required`),
`warnings[]` (`code message`), `truncated` (a list was cut at `--max-items`, default 200), `outputBytes`, `seconds`.

A refused file prints `{"ok":false,"error":{"code":"gltf_<code>","message":"..."}}` and exits 1 (`<code>` is the `GltfException.Code`);
a missing file exits 4 with `file_not_found`; a bad command line exits 2.

## Tests and data

`tests/OneGame.Gltf.Tests` loads the committed models under `testdata/gltf` (licences in its README) and, when
`ONEGAME_GLTF_SAMPLES` points at a folder made by `tools/fetch-gltf-samples.sh`, the full corpus (Sponza, DamagedHelmet, …).
`tests/OneGame.Core.Fuzz` runs the `gltf-*` targets (`gltf-glb-chunks`, `gltf-json`, `gltf-edit` numeric edits of a rich document,
`gltf-accessors` with every accessor parameter taken from the input, `gltf-import`, `gltf-image`, `gltf-uri`) with tight limits; their
seeds and every input that ever crashed one are committed under `testdata/fuzz-gltf/<target>/` and replayed by the tests.
