# render3d — the 3D renderer (work in progress)

`OneGame.Render3D` draws glTF models with metallic-roughness PBR on the render graph (`render-graph.md`). It uses only `gpu.md` and `gltf.md`;
its shaders are HLSL compiled during the build (`src/OneGame.Render3D/Shaders`).

```csharp
using var renderer = new Renderer3D(device);
using Model3D model = renderer.Upload(GltfLoader.LoadFile("helmet.glb"));      // buffers, materials, textures with mips (colour textures sRGB)
using EnvironmentMap env = renderer.CreateEnvironment(HdrImage.Decode(bytes));   // Radiance .hdr panorama; filtered on the GPU at first use
var scene = new Scene3D { Camera = Camera3D.Frame(min, max, yaw: 25, pitch: 10, aspect) };
ModelInstance3D instance = scene.Add(model);                                      // own pose: instance.Animate(clip, seconds)
scene.Lights.Add(Light3D.Point(position, color, candela, range));
scene.Environment.Map = env;
renderer.Render(graph, scene, targetHandle, width, height, targetFormat);         // adds passes to a graph that has begun a frame
```

Offscreen capture: `new Offscreen3D(renderer, w, h).Capture(scene)` returns RGBA8. CLI: `onegame view model.glb --out view.png --size 1280x720
[--yaw --pitch --camera N --env pano.hdr --exposure --tonemap neutral|aces|none --bloom --animation --time --no-ibl]`, with `--json`.

## Passes (names as `render_graph` shows them)

`brdf_lut` and `env_*` (once, compute) → `cluster_lights` (compute, only with punctual lights) → `forward` (opaque and masked sorted near to far,
then the backdrop, then blended surfaces far to near; MSAA with resolve) → `bloom_down_i` / `bloom_up_i` → `tonemap` (writes the whole target).
A later 2D pass must declare `ReadWrite` on the target; `Render3DStage` (Host) does this.

## Conventions

- Matrices are `System.Numerics` (row vectors, right-handed, depth 0..1). Cameras look down their -Z. Panorama: `u = atan2(d.x, -d.z)/2pi + 0.5`, `v = acos(d.y)/pi`; `Environment3D.Yaw` turns it.
- Shading follows glTF 2.0 Appendix B. Dielectric F0 comes from `ior`. Occlusion dims indirect light only. Double-sided flips the normal on back faces; mirrored nodes use a flipped-winding pipeline.
- Lights are scene-referred: lux (directional) and candela (point, spot) used as radiometric units; spot cone and range window follow KHR_lights_punctual. Exposure is a plain multiplier.
- Clustered lights: 64-pixel tiles, 24 exponential depth slices, up to 63 lights per cluster, 512 lights per frame; directional lights are always applied.
- IBL: specular cube (256 default, roughness = mip / (mips-1)), irradiance cube 32 (mean radiance, i.e. E/pi), BRDF table 128 (`Rg16Float`).
- Tone mapping: Khronos PBR Neutral (default), ACES rational fit, or clamp. 8-bit UNorm targets are encoded in the shader, sRGB targets by the hardware.
- Skinning on the GPU (joint matrices from `GltfSkin.ComputeJointMatrices`); morph targets blended on the CPU into a per-instance vertex buffer (`ModelInstance3D` is `IDisposable` for that). Lines and points are not drawn.
- Steady frames allocate zero managed bytes (test).

## Not built (next)

Shadows, SSAO, depth prepass, instancing at scale, quality ladder, frustum culling; KHR_materials clearcoat/transmission/sheen/specular/volume are parsed but not rendered.

## Host

`Render2DStage.AddScenePasses` is the hook; `Render3DStage` (Host) owns a `Renderer3D` and a `Scene3D`; a game returns it from `CreateRenderStage`.
