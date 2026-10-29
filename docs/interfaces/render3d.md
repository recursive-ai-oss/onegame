# render3d — the 3D renderer

`OneGame.Render3D` draws glTF models with metallic-roughness PBR on the render graph (`render-graph.md`). It uses only `gpu.md` and `gltf.md`;
its shaders are HLSL compiled during the build (`src/OneGame.Render3D/Shaders`).

```csharp
using var renderer = new Renderer3D(device);
using Model3D model = renderer.Upload(GltfLoader.LoadFile("helmet.glb"));      // buffers, materials, textures with mips (colour textures sRGB)
using EnvironmentMap env = renderer.CreateEnvironment(HdrImage.Decode(bytes));   // Radiance .hdr panorama; filtered on the GPU at first use
var scene = new Scene3D { Camera = Camera3D.Frame(min, max, yaw: 25, pitch: 10, aspect) };
ModelInstance3D instance = scene.Add(model);                                      // own pose: instance.Animate(clip, seconds); blending, state machines and IK: ../animation.md
scene.Lights.Add(Light3D.Point(position, color, candela, range));
scene.Environment.Map = env;
renderer.Render(graph, scene, targetHandle, width, height, targetFormat);         // adds passes to a graph that has begun a frame
```

Offscreen capture: `new Offscreen3D(renderer, w, h).Capture(scene)` returns RGBA8. CLI: `onegame view model.glb --out view.png --size 1280x720
[--yaw --pitch --camera N --env pano.hdr --exposure --tonemap neutral|aces|none --bloom --animation --time --no-ibl]`, with `--json`.

## Passes (names as `render_graph` shows them)

`brdf_lut` and `env_*` (once, compute) → `shadows` (only with shadow-casting lights) → `depth_normal_prepass`, `ssao`, `ssao_blur` (only with SSAO on) → `cluster_lights` (compute, only with punctual lights) → `forward` (opaque and masked sorted near to far, instanced opaque, outlines, backdrop, then blended surfaces far to near; MSAA with resolve) → `bloom_down_i` / `bloom_up_i` → `tonemap` (writes the whole target).
A later 2D pass must declare `ReadWrite` on the target; `Render3DStage` (Host) does this.

## Quality

`Scene3D.Quality` (low, medium, high (default), ultra) is a **cap**; features stay opt-in. `Samples` 0 takes the level's count. `QualitySettings.For(level)`:

| Level | Shadow tile | Cascades | PCF | SSAO | MSAA | Bloom | Env cube |
|---|---|---|---|---|---|---|---|
| low | 512 | 2 | 1 tap | off | 1 | off | 64 |
| medium | 1024 | 3 | 1 tap | 8 samples, half res | 4 | on | 128 |
| high | 2048 | 4 | 3x3 | 14 samples | 4 | on | 256 |
| ultra | 4096 | 4 | 5x5 | 24 samples | 8 | on | 512 |

MSAA is 1, 4 or 8 (a request for 2 uses 4: many formats lack it). `CreateEnvironment` with size 0 follows the level and is rebuilt when the level changes.
Harness: control op `quality` (`level`, `ssao`, `ssaoRadius`; returns the settings, last-frame stats and live pass names); `onegame view --quality L --shadows --ssao --ssao-radius R --style pbr|toon|flat --outline PX`.

## Shadows

`Light3D.CastShadows = true` on directional (cascaded: practical split, `Shadows.SplitLambda` 0.75, `MaxDistance` 60, texel-snapped), spot (one tile) and point lights (six faces). All tiles share one depth atlas sampled with a comparison sampler; normal-offset (`Shadows.NormalOffset`) plus slope bias remove acne. Budget: 8 shadowed lights and 16 tiles per frame; extra lights are lit without shadows. Blended materials cast none.

## SSAO

`Scene3D.Ssao.Enabled` (also needs a level that allows it), `Radius`: depth and normal prepass, hemisphere samples, bilateral blur; half resolution at low and medium. It dims indirect light only.

## Styles

`Material3D` (`Model3D.Materials`; `Model3D.SetStyle(style)` sets all): `Style` pbr, toon (`ToonBands` 2..8, `ToonSoftness`, `ToonHighlight`) or flat (base colour unlit); `OutlineWidth` pixels and `OutlineColor` work with every style (inverted hull; needs smooth normals).

## Instancing

`scene.AddInstanced(model, capacity)` returns an `InstanceBatch3D` (`Add/Set/RemoveAt/Clear/Items/Touch`, `Visible`, `CastShadows`, per-copy tint). One instanced draw per primitive; copies are culled on the CPU against the camera and every shadow view, and only visible ones are uploaded (10,000 trees: 3 draws). Limits: rest pose only (no skin, animation, morphs), uniform unmirrored scale, not pickable. Entity views use `SceneWriter.Instance` (`scene3d.md`).

## Billboards

`Scene3D.Billboards` holds `BillboardBatch3D`s: lists of `Billboard3D` camera-facing quads (position, size, rotation, linear RGBA that may exceed 1, texture rectangle, additive flag) over one `BillboardTexture3D` (`renderer.CreateBillboardTexture`, premultiplied in linear light on upload; null shows `renderer.SoftSpot`). They are drawn at the end of the `forward` pass, after blended surfaces: depth tested (`Less`), no depth write, one instanced draw per batch from one shared vertex buffer written once per frame. Blended quads of a batch are sorted far to near by view depth (`Array.Sort` on reused arrays); a batch with only additive quads keeps the caller's order. Blend is `One, OneMinusSrcAlpha` with additive quads writing alpha 0, so both kinds share one pipeline. Quads are not culled, lit, shadowed or pickable, and no depth fade (soft particles) is applied. `FrameStats3D.Billboards` counts them. Shader: `Billboard.hlsl`, layout set 0 = frame constants, set 1 = texture and sampler. Steady frames allocate nothing (test: 2,000 quads in two batches).

## Conventions

- Matrices are `System.Numerics` (row vectors, right-handed, depth 0..1). Cameras look down their -Z. Panorama: `u = atan2(d.x, -d.z)/2pi + 1 (wrapping)`, `v = acos(d.y)/pi`; `Environment3D.Yaw` turns it.
- Shading follows glTF 2.0 Appendix B. Dielectric F0 comes from `ior`. Occlusion dims indirect light only. Double-sided flips the normal on back faces; mirrored nodes use a flipped-winding pipeline.
- Lights are scene-referred: lux (directional) and candela (point, spot) used as radiometric units; spot cone and range window follow KHR_lights_punctual. Exposure is a plain multiplier.
- Clustered lights: 64-pixel tiles, 24 exponential depth slices, up to 63 lights per cluster, 512 lights per frame; directional lights are always applied.
- IBL: specular cube (256 default, roughness = mip / (mips-1)), irradiance cube 32 (mean radiance, i.e. E/pi), BRDF table 128 (`Rgba16Float`, two channels used).
- Tone mapping: Khronos PBR Neutral (default), ACES rational fit, or clamp. 8-bit UNorm targets are encoded in the shader, sRGB targets by the hardware.
- Skinning on the GPU (joint matrices from `GltfSkin.ComputeJointMatrices`); morph targets blended on the CPU into a per-instance vertex buffer (`ModelInstance3D` is `IDisposable` for that). Lines and points are not drawn.
- Steady frames allocate zero managed bytes (test).

## Not built

KHR_materials clearcoat/transmission/sheen/specular/volume are parsed but not rendered; no depth prepass without SSAO; barriers are whole-resource.

## Host

`Render2DStage.AddScenePasses` is the hook; `Render3DStage` (Host) owns a `Renderer3D` and a `Scene3D`; a game returns it from `CreateRenderStage`.

## Reference renders

`testdata/render3d/cases.json` lists eight Khronos sample scenes (camera, lights, pose, environment); `testdata/render3d/ref/` holds the 480x360 images the Khronos glTF Sample Viewer drew for them (black-box output, `tools/reference3d/shoot.mjs`). `ReferenceTests` compares on model pixels with edges eroded: mean difference under 6 of 255 and p95 under 28, or 11 and 45 for scenes lit by an image-based environment (filtering differs between renderers; the reasons are in the test). The panorama centre looks down +Z, the front of a glTF model seen from the default camera. Needs `ONEGAME_GLTF_SAMPLES` (`tools/fetch-gltf-samples.sh`) and `ONEGAME_ENV_DIR` (a Poly Haven 1k studio panorama); cases are skipped without them.
