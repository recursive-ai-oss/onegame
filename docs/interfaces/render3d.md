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

`brdf_lut` and `env_*` (once, compute) → `shadows` (only with shadow-casting lights) → `depth_normal_prepass`, `ssao`, `ssao_blur` (only with SSAO on) → `cluster_lights` (compute, only with punctual lights) → `planar_reflection` (only while a material opts in, see Planar reflection) → `forward` (opaque and masked sorted near to far, instanced opaque, outlines, backdrop, then blended surfaces far to near; MSAA with resolve) → `bloom_down_i` / `bloom_up_i` → `tonemap` (writes the whole target).
A later 2D pass must declare `ReadWrite` on the target; `Render3DStage` (Host) does this.

## Quality

`Scene3D.Quality` (low, medium, high (default), ultra) is a **cap**; features stay opt-in. `Samples` 0 takes the level's count. `QualitySettings.For(level)`:

| Level | Shadow tile | Cascades | PCF | SSAO | MSAA | Bloom | Env cube | Reflection image |
|---|---|---|---|---|---|---|---|---|
| low | 512 | 2 | 1 tap | off | 1 | off | 64 | off |
| medium | 1024 | 3 | 1 tap | 8 samples, half res | 4 | on | 128 | 0.5x |
| high | 2048 | 4 | 3x3 | 14 samples | 4 | on | 256 | 0.75x |
| ultra | 4096 | 4 | 5x5 | 24 samples | 8 | on | 512 | 1x |

MSAA is 1, 4 or 8 (a request for 2 uses 4: many formats lack it). `CreateEnvironment` with size 0 follows the level and is rebuilt when the level changes.
Harness: control op `environment` (`fog`, `fogColor`, `fogStart`, `fogEnd`, `fogMaxDensity`, `blend`, `intensity`, `yaw`; reads and sets them, returns the values; the game may overwrite them next frame if it sets them itself); control op `quality` (`level`, `ssao`, `ssaoRadius`; returns the settings, last-frame stats and live pass names); `onegame view --quality L --shadows --ssao --ssao-radius R --style pbr|toon|flat --outline PX`.

## Fog

`Scene3D.Fog` (`FogSettings`): `Enabled` (off by default, and then the output is identical to a build without fog), `Color` (linear), `Start`, `End`, `MaxDensity` (0..1). The amount is linear in the distance from the camera (view depth with an orthographic camera): 0 at `Start`, `MaxDensity` at `End`, constant beyond; a surface is `colour * (1 - amount) + Color * amount`, in HDR before tone mapping. It is a per-frame constant (in the frame constants, `Frame.hlsli`: `FogAmount`, `ApplyFog`), so changing it never touches meshes, pipelines or the GPU memory, and steady frames still allocate nothing. Every surface shader applies it: PBR, toon, flat and unlit, outlines, masked and blended surfaces (blended ones fade by their own alpha), instanced draws and dynamic meshes (they share the shader); billboards fade by coverage and additive quads fade out instead of turning into fog. Left alone: the backdrop (an environment or flat colour) and debug geometry. To make far terrain meet the sky, set `Color` to the sky colour at the horizon, and `MaxDensity` below 1 when the horizon should stay faintly visible. Not built: exponential or height fog, and tinting by view direction.

## Planar reflection and UV scroll

For water and polished floors. `Material3D.PlanarReflection = true` opts a material in; `Scene3D.Reflection.Height` is the world height (y) of the one mirror plane (always horizontal; `Reflection.Enabled` switches the feature off for the scene). Load a file, then opt its materials in by glTF name: `model.FindMaterial("Water")!.PlanarReflection = true` (the importer does not read glTF `extras`, so there is no authoring-side switch). `ReflectionStrength` (0..1, default 1) scales the effect per material.

- **What is drawn.** While a visible material (regular or instanced draw) opts in, the graph gets a `planar_reflection` pass before `forward`: the scene seen from the camera mirrored about the plane, into `reflection_hdr` (single-sampled, sized by the quality level: off at low, 0.5x, 0.75x and 1x of the frame at medium, high and ultra). It holds the backdrop, opaque and blended surfaces, instanced batches, directional and spot shadows, fog and the mirrored camera's view-dependent shading. It leaves out the opted-in surfaces themselves (no water in the water), everything below the plane (cut in the pixel shader with a world-height test), outlines, billboards and debug geometry. Clustered point and spot lights do not light it (their grid belongs to the main view); directional lights do. Blended surfaces keep the main view's far-to-near order, which is only approximate from the mirror. A camera at or below the plane gets no pass, and neither does a frame with no opted-in material in view.
- **Zero cost when unused.** No pass, no image, no extra binding work: set 0 always carries a binding for the image, and a 1x1 placeholder fills it, so the shader path is one uniform branch. Frames without an opt-in are unchanged (the renderer's reference images are byte-identical).
- **Shading.** In the forward pass the opted-in PBR surface projects its world position through the mirrored view-projection, reads the image there, and replaces the environment's specular light with it by `ReflectionStrength * (1 - roughness)`, faded out over the last 1/16 of the image edge (where the mirror has nothing). The split-sum Fresnel term (`f0 * A + B`) still scales it, so grazing angles reflect more, and the environment's intensity and occlusion do not apply to the already lit image. A normal map shifts the lookup by 0.05 of its horizontal tilt, so ripples distort the picture. Roughness does not blur the image, it fades to the blurred environment. Toon, flat and unlit surfaces ignore it.
- **Cost and limits.** One more scene draw at the image size (the camera's instances are culled again for the mirror frustum). One plane per frame, horizontal only.

`Material3D.UvScroll` (texture units per second, default zero) is added to every texture coordinate of the material after its own `KHR_texture_transform`: `uv + UvScroll * Scene3D.Time`. The renderer keeps no clock of its own, so the game sets `Scene3D.Time` (seconds, a double; a tick count over the tick rate keeps captures and replays exact). Nothing else changes per frame; a zero velocity changes no output. The 32-bit offset loses precision after many hours of `Time`; wrap it (a texture with the glTF default repeat sampler is seamless at any whole-unit wrap). `FrameStats3D.PlanarReflection` tells whether the pass ran. Tests: `WaterReflectionTests` (analytic projection of the mirrored box within 3 px, instanced batches with shadows and fog, plane height, pass gating by material, level, switch and camera side, quality sizes, scroll offsets, 0 B steady frames, `pond_reflection` reference).

## Environment crossfade

`scene.Environment.NextMap` and `Blend` (0..1) fade from `Map` to a second `EnvironmentMap`: the visible backdrop, the diffuse irradiance and the specular prefiltered light are all `first * (1 - Blend) + second * Blend`. Both maps are filtered once, when first used (`renderer.PrepareEnvironment` does it ahead of time, for example at load); changing `Blend` costs one more cube sample per pixel (none at 0) and refilters nothing, and the weight can change every frame without allocating. `Intensity`, `Yaw` and `BackgroundBlur` are shared by both maps. A day cycle over keyframes A, B, C, D, E: show `Map = A, NextMap = B` and raise `Blend` to 1; then set `Map = B, NextMap = C, Blend = 0` (the picture is identical, test `SwappingKeyframesAtTheBoundaryChangesNothing`) and carry on. Sun and moon are the game's: set the directional light's colour, intensity and direction from the same time of day (they are not part of the environment). Maps of different size are fine; both are bound at once, so keep the keyframes to a handful.

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

## Runtime materials: emissive, opacity, textures

A material changes without reloading the model; each setter applies to the next frame.

- `Material3D.Emissive` (linear RGB, the file's factor times its strength at load) and `EmissiveStrength` (a multiplier, 1 by default), for glows that pulse or fade.
- `Material3D.Opacity` (0..1) and `ModelInstance3D.Opacity` (0..1, per instance; the two multiply). Below 1 a surface goes to the same path as a glTF BLEND material: drawn after the opaque surfaces and the sky, sorted far to near by its centre, no depth write, alpha blend (`BlendState.AlphaBlend`), no shadow and no outline. Back at 1 it returns to the opaque path. The shader reads the material's `1 - opacity` from `Emissive.w` and the instance's from bits of `ObjInfo.z`, so zeroed constants mean opaque. Instanced batches keep their per-copy tint.
- `Material3D.SetTexture(MaterialSlot3D, GpuTexture)` / `ResetTexture(slot)` replace one texture (base colour, metallic-roughness, normal, occlusion, emissive) by rebuilding the material's bind group; the old group is freed at the start of the next render.

## A view inside a view (portal windows, monitors, mirrors)

```csharp
using var portal = new Offscreen3D(renderer, 512, 288, GpuFormat.Rgba8UnormSrgb, sampled: true);   // a render target shaders may sample
window.Materials[0].SetTexture(MaterialSlot3D.Emissive, portal.Target);                            // the surface shows it (base colour black, emissive 1)
// each frame, before the main scene is rendered:
portal.Render(otherPlace);                                                                          // its own Scene3D and camera
```

`Offscreen3D.Render` runs its own render graph frame and submits it, so the main frame (`Render3DStage` or `renderer.Render`) that samples `Target` comes later in submission order; the backend inserts the layout change when the material's group is bound. Models, textures and buffers are the ones both scenes share: nothing is uploaded per frame, only the portal's draws, lights and passes are recorded again. Costs: one more set of passes per portal per frame at the portal's resolution, and its own `Scene3D` (quality, MSAA, post settings). The image is tone-mapped already, so show it through the emissive slot with a black base colour (or the flat style on the base colour slot). One level only: a portal scene may not contain a surface that samples the same target, and two views of one renderer cannot be recorded into one graph frame (the renderer keeps one frame's constants at a time), which is why the portal has its own frame. A game that draws the portal from `Render3DStage.UpdateScene` renders it there, before the main frame is recorded.

## Meshes built at run time

For content a game makes while running (streamed voxel chunks, procedural terrain): `renderer.CreateMaterial(name, MaterialAlpha3D.Opaque|Mask|Blend, doubleSided, baseColor)` makes a shared `Material3D` without a glTF file (set textures, emissive, opacity, style on it as usual; materials live as long as the renderer; make a few). `renderer.CreateMesh(material, vertices, indices, name)` returns a `DynamicMesh3D`: `Update(vertices, indices)` replaces the geometry, `Dispose()` frees it, and `Model` is a `Model3D` that `scene.Add` draws like any model (so labels, opacity, transforms, shadows, blending and the quality ladder all work). Vertices are `MeshVertex3D` (position, normal, uv, linear RGBA colour multiplied into the base colour); indices are a triangle list.

- **Memory:** vertex and index buffers are sized in classes (at most 12.5% slack). An update that fits (and is not under half the capacity) keeps its buffers and costs one copy through the device's reused staging memory; otherwise new buffers are made and the old ones go through the device's deferred destruction, which waits for the submissions that may still read them. Creating thousands of meshes in one frame is fine: the uploads go through the device's bounded staging (`gpu.md`), so the call that overruns it waits for earlier copies to drain (a burst takes some milliseconds longer, and uses 32 MiB of staging, not the burst's size); all of them are drawn from the next frame. Otherwise nothing waits for the GPU (no idle waits), and the device's sub-allocator reuses freed ranges. `device.MemoryStats` (`ReservedBytes`, `UsedBytes`, `Allocations`; Vulkan only, zeros elsewhere) is the tally the soak test holds flat.
- **Allocation:** the pack buffers are reused, so an update allocates nothing once they are big enough; creating a mesh allocates its small managed objects. Steady frames with an unchanged set allocate zero bytes.
- **Not built:** triangle picking of these meshes (a game picks with its own structure), skinning, morphing, mesh LOD, one draw for many chunks. Each mesh is one draw (plus its shadow draws), so a few thousand live meshes is the design size. Call from the thread that renders; build the vertices anywhere.

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

## Debug drawing

`scene.Debug` is a reusable `DebugDraw3D` list. Fill it each frame and call `Clear()` before refilling.
`Line(from, to, color, width = 1, units = DebugSize3D.Pixels, mode = DebugDepth3D.DepthTested)`,
`Point(position, color, size = 4, units = DebugSize3D.Pixels, mode = DebugDepth3D.DepthTested)` and
`Arrow(from, to, color, width = 1, units = DebugSize3D.Pixels, mode = DebugDepth3D.DepthTested)`
use `Vector3` positions and linear straight-alpha `Vector4` colours. `DebugSize3D.World` sizes are world units.
Lines are camera-facing quads, points are squares, arrows have a shaft and a screen-facing triangular head.
Depth-tested primitives test the scene depth without writing it; `AlwaysOnTop` primitives draw last.
Storage grows and is reused. Each nonempty depth mode uses one instanced draw, independent of primitive count.
Debug drawing is unlit HDR geometry after surfaces, before post-processing; graph reports name it `forward + debug3d`.
Direct lists are unconditional; the host view helpers are gated by `SceneView3D.DebugVisible` (off by default).

```csharp
scene.Debug.Clear();
scene.Debug.Line(new Vector3(-2, 0, 0), new Vector3(2, 0, 0),
    new Vector4(1, 0, 0, 0.5f), width: 3);
scene.Debug.Arrow(Vector3.Zero, Vector3.UnitY, Vector4.One,
    width: 0.02f, units: DebugSize3D.World, mode: DebugDepth3D.AlwaysOnTop);
```

Sizes are full widths/diameters, not radii. Points are camera-facing squares. Arrowheads are six widths long,
capped at half the projected shaft length. Lines clip their centreline at the camera near plane; a line with no
projected length produces no pixels. Colours must be finite with alpha in 0..1; positions must be finite and
sizes finite and positive (`ArgumentOutOfRangeException` otherwise). Alpha blends in submission order within
each mode, without depth writes or distance sorting. Like other scene data, fill and render on the render thread.
`Renderer3D.DebugDrawCalls` reports the last frame's zero, one or two debug draws. `DebugDraw3D.Append` copies
another reusable list, for producers that collect geometry separately. Warming storage includes the CPU lists,
upload buffer and pipelines for the modes/sample counts in use.
