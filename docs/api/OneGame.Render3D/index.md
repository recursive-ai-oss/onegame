[API reference](../index.md) › OneGame.Render3D

# OneGame.Render3D

Physically based 3D renderer: glTF materials, clustered forward lighting, image-based lighting, bloom and tone mapping on the render graph.

**Package:** `RecursiveAI.OneGame.Render3D`

```sh
dotnet add package RecursiveAI.OneGame.Render3D
```

## OneGame.Render3D

| Type | Kind | Description |
| --- | --- | --- |
| [`BackgroundMode`](OneGame.Render3D/BackgroundMode.md) | enum | What fills the pixels no model covers. |
| [`Billboard3D`](OneGame.Render3D/Billboard3D.md) | struct | One camera-facing quad of a [`BillboardBatch3D`](OneGame.Render3D/BillboardBatch3D.md). |
| [`BillboardBatch3D`](OneGame.Render3D/BillboardBatch3D.md) | class | Quads that always face the camera, drawn after the opaque and blended surfaces: particles, glows, lens spots, markers. |
| [`BillboardTexture3D`](OneGame.Render3D/BillboardTexture3D.md) | class | An image for billboards, uploaded with its colours premultiplied by alpha in linear light. |
| [`Camera3D`](OneGame.Render3D/Camera3D.md) | class | A camera. |
| [`Environment3D`](OneGame.Render3D/Environment3D.md) | class | Image-based lighting and backdrop of a scene. |
| [`EnvironmentMap`](OneGame.Render3D/EnvironmentMap.md) | class | An environment prepared for lighting: a GGX-prefiltered specular cube (roughness grows with the mip level, mip 0 is the sharp environment) and a cosine-convolved diffuse cube. |
| [`FrameStats3D`](OneGame.Render3D/FrameStats3D.md) | struct | What one frame of 3D drew. |
| [`HdrException`](OneGame.Render3D/HdrException.md) | class | The file is not a valid Radiance (.hdr) image, or exceeds a limit. |
| [`HdrImage`](OneGame.Render3D/HdrImage.md) | class | A floating-point panorama in linear RGB, rows from the top. |
| [`HdrLimits`](OneGame.Render3D/HdrLimits.md) | class | Limits applied before a Radiance file allocates anything. |
| [`InstanceBatch3D`](OneGame.Render3D/InstanceBatch3D.md) | class | Many copies of one static model (trees, crowds, rubble), drawn with one instanced draw per primitive. |
| [`InstanceData3D`](OneGame.Render3D/InstanceData3D.md) | struct | One copy of a model in an [`InstanceBatch3D`](OneGame.Render3D/InstanceBatch3D.md): where it stands and a colour that multiplies the material's base colour. |
| [`Light3D`](OneGame.Render3D/Light3D.md) | class | A punctual light. |
| [`LightKind`](OneGame.Render3D/LightKind.md) | enum | The kind of a punctual light (KHR\_lights\_punctual). |
| [`Material3D`](OneGame.Render3D/Material3D.md) | class | A material on the GPU: constants and textures bound as one group. |
| [`MaterialStyle3D`](OneGame.Render3D/MaterialStyle3D.md) | enum | How a material is lit. |
| [`Model3D`](OneGame.Render3D/Model3D.md) | class | A glTF model uploaded to the GPU: meshes, materials and textures. |
| [`ModelInstance3D`](OneGame.Render3D/ModelInstance3D.md) | class | One placed copy of a [`Model3D`](OneGame.Render3D/Model3D.md), with its own pose. |
| [`Offscreen3D`](OneGame.Render3D/Offscreen3D.md) | class | Renders a scene into an offscreen colour image and reads it back: the capture path for tools and tests. |
| [`PostSettings`](OneGame.Render3D/PostSettings.md) | class | Bloom, exposure and tone mapping. |
| [`Primitive3D`](OneGame.Render3D/Primitive3D.md) | class | A drawable triangle list of a mesh, with its GPU buffers and material. |
| [`QualityLevel`](OneGame.Render3D/QualityLevel.md) | enum | A named rung of the quality ladder. |
| [`QualitySettings`](OneGame.Render3D/QualitySettings.md) | struct | What a [`QualityLevel`](OneGame.Render3D/QualityLevel.md) means in resources and work. |
| [`Render3DModule`](OneGame.Render3D/Render3DModule.md) | static class | Identifies the Render3D assembly. |
| [`Renderer3D`](OneGame.Render3D/Renderer3D.md) | class | The physically based 3D renderer. |
| [`Scene3D`](OneGame.Render3D/Scene3D.md) | class | Everything one frame draws: camera, models, lights, environment and post-processing. |
| [`ShadowSettings`](OneGame.Render3D/ShadowSettings.md) | class | Scene-wide shadow controls. |
| [`SsaoSettings`](OneGame.Render3D/SsaoSettings.md) | class | Screen-space ambient occlusion, computed from the depth and normals of the scene. |
| [`ToneMapper`](OneGame.Render3D/ToneMapper.md) | enum | The curve that maps scene-referred HDR light to display values. |

## OneGame.Render3D.Shaders

| Type | Kind | Description |
| --- | --- | --- |
| [`Billboard`](OneGame.Render3D.Shaders/Billboard.md) | static class |  |
| [`Billboard.Bindings`](OneGame.Render3D.Shaders/Billboard.Bindings.md) | static class |  |
| [`Billboard.Bindings.Frame`](OneGame.Render3D.Shaders/Billboard.Bindings.Frame.md) | static class |  |
| [`Billboard.Bindings.Samp`](OneGame.Render3D.Shaders/Billboard.Bindings.Samp.md) | static class |  |
| [`Billboard.Bindings.Tex`](OneGame.Render3D.Shaders/Billboard.Bindings.Tex.md) | static class |  |
| [`Billboard.Frame`](OneGame.Render3D.Shaders/Billboard.Frame.md) | struct |  |
| [`Billboard.PSBillboard`](OneGame.Render3D.Shaders/Billboard.PSBillboard.md) | static class |  |
| [`Billboard.VSBillboard`](OneGame.Render3D.Shaders/Billboard.VSBillboard.md) | static class |  |
| [`Billboard.VSBillboard.Vertex`](OneGame.Render3D.Shaders/Billboard.VSBillboard.Vertex.md) | struct |  |
| [`Cluster`](OneGame.Render3D.Shaders/Cluster.md) | static class |  |
| [`Cluster.Bindings`](OneGame.Render3D.Shaders/Cluster.Bindings.md) | static class |  |
| [`Cluster.Bindings.Clusters`](OneGame.Render3D.Shaders/Cluster.Bindings.Clusters.md) | static class |  |
| [`Cluster.Bindings.Frame`](OneGame.Render3D.Shaders/Cluster.Bindings.Frame.md) | static class |  |
| [`Cluster.Bindings.Lights`](OneGame.Render3D.Shaders/Cluster.Bindings.Lights.md) | static class |  |
| [`Cluster.CSMain`](OneGame.Render3D.Shaders/Cluster.CSMain.md) | static class |  |
| [`Cluster.Frame`](OneGame.Render3D.Shaders/Cluster.Frame.md) | struct |  |
| [`Cluster.LightData`](OneGame.Render3D.Shaders/Cluster.LightData.md) | struct |  |
| [`EnvBrdf`](OneGame.Render3D.Shaders/EnvBrdf.md) | static class |  |
| [`EnvBrdf.Bindings`](OneGame.Render3D.Shaders/EnvBrdf.Bindings.md) | static class |  |
| [`EnvBrdf.Bindings.Out`](OneGame.Render3D.Shaders/EnvBrdf.Bindings.Out.md) | static class |  |
| [`EnvBrdf.Bindings.Params`](OneGame.Render3D.Shaders/EnvBrdf.Bindings.Params.md) | static class |  |
| [`EnvBrdf.CSMain`](OneGame.Render3D.Shaders/EnvBrdf.CSMain.md) | static class |  |
| [`EnvBrdf.Params`](OneGame.Render3D.Shaders/EnvBrdf.Params.md) | struct |  |
| [`IblEquirect`](OneGame.Render3D.Shaders/IblEquirect.md) | static class |  |
| [`IblEquirect.Bindings`](OneGame.Render3D.Shaders/IblEquirect.Bindings.md) | static class |  |
| [`IblEquirect.Bindings.Equirect`](OneGame.Render3D.Shaders/IblEquirect.Bindings.Equirect.md) | static class |  |
| [`IblEquirect.Bindings.OutCube`](OneGame.Render3D.Shaders/IblEquirect.Bindings.OutCube.md) | static class |  |
| [`IblEquirect.Bindings.Params`](OneGame.Render3D.Shaders/IblEquirect.Bindings.Params.md) | static class |  |
| [`IblEquirect.Bindings.Samp`](OneGame.Render3D.Shaders/IblEquirect.Bindings.Samp.md) | static class |  |
| [`IblEquirect.CSMain`](OneGame.Render3D.Shaders/IblEquirect.CSMain.md) | static class |  |
| [`IblEquirect.Params`](OneGame.Render3D.Shaders/IblEquirect.Params.md) | struct |  |
| [`IblIrradiance`](OneGame.Render3D.Shaders/IblIrradiance.md) | static class |  |
| [`IblIrradiance.Bindings`](OneGame.Render3D.Shaders/IblIrradiance.Bindings.md) | static class |  |
| [`IblIrradiance.Bindings.OutCube`](OneGame.Render3D.Shaders/IblIrradiance.Bindings.OutCube.md) | static class |  |
| [`IblIrradiance.Bindings.Params`](OneGame.Render3D.Shaders/IblIrradiance.Bindings.Params.md) | static class |  |
| [`IblIrradiance.Bindings.Samp`](OneGame.Render3D.Shaders/IblIrradiance.Bindings.Samp.md) | static class |  |
| [`IblIrradiance.Bindings.Source`](OneGame.Render3D.Shaders/IblIrradiance.Bindings.Source.md) | static class |  |
| [`IblIrradiance.CSMain`](OneGame.Render3D.Shaders/IblIrradiance.CSMain.md) | static class |  |
| [`IblIrradiance.Params`](OneGame.Render3D.Shaders/IblIrradiance.Params.md) | struct |  |
| [`IblPrefilter`](OneGame.Render3D.Shaders/IblPrefilter.md) | static class |  |
| [`IblPrefilter.Bindings`](OneGame.Render3D.Shaders/IblPrefilter.Bindings.md) | static class |  |
| [`IblPrefilter.Bindings.OutCube`](OneGame.Render3D.Shaders/IblPrefilter.Bindings.OutCube.md) | static class |  |
| [`IblPrefilter.Bindings.Params`](OneGame.Render3D.Shaders/IblPrefilter.Bindings.Params.md) | static class |  |
| [`IblPrefilter.Bindings.Samp`](OneGame.Render3D.Shaders/IblPrefilter.Bindings.Samp.md) | static class |  |
| [`IblPrefilter.Bindings.Source`](OneGame.Render3D.Shaders/IblPrefilter.Bindings.Source.md) | static class |  |
| [`IblPrefilter.CSMain`](OneGame.Render3D.Shaders/IblPrefilter.CSMain.md) | static class |  |
| [`IblPrefilter.Params`](OneGame.Render3D.Shaders/IblPrefilter.Params.md) | struct |  |
| [`Pbr`](OneGame.Render3D.Shaders/Pbr.md) | static class |  |
| [`Pbr.Bindings`](OneGame.Render3D.Shaders/Pbr.Bindings.md) | static class |  |
| [`Pbr.Bindings.AoTex`](OneGame.Render3D.Shaders/Pbr.Bindings.AoTex.md) | static class |  |
| [`Pbr.Bindings.BaseSamp`](OneGame.Render3D.Shaders/Pbr.Bindings.BaseSamp.md) | static class |  |
| [`Pbr.Bindings.BaseTex`](OneGame.Render3D.Shaders/Pbr.Bindings.BaseTex.md) | static class |  |
| [`Pbr.Bindings.BrdfLut`](OneGame.Render3D.Shaders/Pbr.Bindings.BrdfLut.md) | static class |  |
| [`Pbr.Bindings.Clusters`](OneGame.Render3D.Shaders/Pbr.Bindings.Clusters.md) | static class |  |
| [`Pbr.Bindings.EmisSamp`](OneGame.Render3D.Shaders/Pbr.Bindings.EmisSamp.md) | static class |  |
| [`Pbr.Bindings.EmisTex`](OneGame.Render3D.Shaders/Pbr.Bindings.EmisTex.md) | static class |  |
| [`Pbr.Bindings.EnvSampler`](OneGame.Render3D.Shaders/Pbr.Bindings.EnvSampler.md) | static class |  |
| [`Pbr.Bindings.Frame`](OneGame.Render3D.Shaders/Pbr.Bindings.Frame.md) | static class |  |
| [`Pbr.Bindings.Irradiance`](OneGame.Render3D.Shaders/Pbr.Bindings.Irradiance.md) | static class |  |
| [`Pbr.Bindings.Joints`](OneGame.Render3D.Shaders/Pbr.Bindings.Joints.md) | static class |  |
| [`Pbr.Bindings.Lights`](OneGame.Render3D.Shaders/Pbr.Bindings.Lights.md) | static class |  |
| [`Pbr.Bindings.Material`](OneGame.Render3D.Shaders/Pbr.Bindings.Material.md) | static class |  |
| [`Pbr.Bindings.MrSamp`](OneGame.Render3D.Shaders/Pbr.Bindings.MrSamp.md) | static class |  |
| [`Pbr.Bindings.MrTex`](OneGame.Render3D.Shaders/Pbr.Bindings.MrTex.md) | static class |  |
| [`Pbr.Bindings.NormalSamp`](OneGame.Render3D.Shaders/Pbr.Bindings.NormalSamp.md) | static class |  |
| [`Pbr.Bindings.NormalTex`](OneGame.Render3D.Shaders/Pbr.Bindings.NormalTex.md) | static class |  |
| [`Pbr.Bindings.Object`](OneGame.Render3D.Shaders/Pbr.Bindings.Object.md) | static class |  |
| [`Pbr.Bindings.OccSamp`](OneGame.Render3D.Shaders/Pbr.Bindings.OccSamp.md) | static class |  |
| [`Pbr.Bindings.OccTex`](OneGame.Render3D.Shaders/Pbr.Bindings.OccTex.md) | static class |  |
| [`Pbr.Bindings.ShadowAtlas`](OneGame.Render3D.Shaders/Pbr.Bindings.ShadowAtlas.md) | static class |  |
| [`Pbr.Bindings.ShadowSamp`](OneGame.Render3D.Shaders/Pbr.Bindings.ShadowSamp.md) | static class |  |
| [`Pbr.Bindings.Shadows`](OneGame.Render3D.Shaders/Pbr.Bindings.Shadows.md) | static class |  |
| [`Pbr.Bindings.Specular`](OneGame.Render3D.Shaders/Pbr.Bindings.Specular.md) | static class |  |
| [`Pbr.Frame`](OneGame.Render3D.Shaders/Pbr.Frame.md) | struct |  |
| [`Pbr.LightData`](OneGame.Render3D.Shaders/Pbr.LightData.md) | struct |  |
| [`Pbr.Material`](OneGame.Render3D.Shaders/Pbr.Material.md) | struct |  |
| [`Pbr.Object`](OneGame.Render3D.Shaders/Pbr.Object.md) | struct |  |
| [`Pbr.PSMain`](OneGame.Render3D.Shaders/Pbr.PSMain.md) | static class |  |
| [`Pbr.PSOutline`](OneGame.Render3D.Shaders/Pbr.PSOutline.md) | static class |  |
| [`Pbr.PSPrepass`](OneGame.Render3D.Shaders/Pbr.PSPrepass.md) | static class |  |
| [`Pbr.PSShadowMask`](OneGame.Render3D.Shaders/Pbr.PSShadowMask.md) | static class |  |
| [`Pbr.PSSky`](OneGame.Render3D.Shaders/Pbr.PSSky.md) | static class |  |
| [`Pbr.ShadowData`](OneGame.Render3D.Shaders/Pbr.ShadowData.md) | struct |  |
| [`Pbr.VSInst`](OneGame.Render3D.Shaders/Pbr.VSInst.md) | static class |  |
| [`Pbr.VSInst.Vertex`](OneGame.Render3D.Shaders/Pbr.VSInst.Vertex.md) | struct |  |
| [`Pbr.VSMain`](OneGame.Render3D.Shaders/Pbr.VSMain.md) | static class |  |
| [`Pbr.VSMain.Vertex`](OneGame.Render3D.Shaders/Pbr.VSMain.Vertex.md) | struct |  |
| [`Pbr.VSOutline`](OneGame.Render3D.Shaders/Pbr.VSOutline.md) | static class |  |
| [`Pbr.VSOutline.Vertex`](OneGame.Render3D.Shaders/Pbr.VSOutline.Vertex.md) | struct |  |
| [`Pbr.VSOutlineInst`](OneGame.Render3D.Shaders/Pbr.VSOutlineInst.md) | static class |  |
| [`Pbr.VSOutlineInst.Vertex`](OneGame.Render3D.Shaders/Pbr.VSOutlineInst.Vertex.md) | struct |  |
| [`Pbr.VSSky`](OneGame.Render3D.Shaders/Pbr.VSSky.md) | static class |  |
| [`Post`](OneGame.Render3D.Shaders/Post.md) | static class |  |
| [`Post.Bindings`](OneGame.Render3D.Shaders/Post.Bindings.md) | static class |  |
| [`Post.Bindings.Bloom`](OneGame.Render3D.Shaders/Post.Bindings.Bloom.md) | static class |  |
| [`Post.Bindings.Color`](OneGame.Render3D.Shaders/Post.Bindings.Color.md) | static class |  |
| [`Post.Bindings.Params`](OneGame.Render3D.Shaders/Post.Bindings.Params.md) | static class |  |
| [`Post.Bindings.Samp`](OneGame.Render3D.Shaders/Post.Bindings.Samp.md) | static class |  |
| [`Post.PSDown`](OneGame.Render3D.Shaders/Post.PSDown.md) | static class |  |
| [`Post.PSTonemap`](OneGame.Render3D.Shaders/Post.PSTonemap.md) | static class |  |
| [`Post.PSUp`](OneGame.Render3D.Shaders/Post.PSUp.md) | static class |  |
| [`Post.Params`](OneGame.Render3D.Shaders/Post.Params.md) | struct |  |
| [`Post.VSFull`](OneGame.Render3D.Shaders/Post.VSFull.md) | static class |  |
| [`Ssao`](OneGame.Render3D.Shaders/Ssao.md) | static class |  |
| [`Ssao.Bindings`](OneGame.Render3D.Shaders/Ssao.Bindings.md) | static class |  |
| [`Ssao.Bindings.Gbuf`](OneGame.Render3D.Shaders/Ssao.Bindings.Gbuf.md) | static class |  |
| [`Ssao.Bindings.Params`](OneGame.Render3D.Shaders/Ssao.Bindings.Params.md) | static class |  |
| [`Ssao.Bindings.Raw`](OneGame.Render3D.Shaders/Ssao.Bindings.Raw.md) | static class |  |
| [`Ssao.PSBlur`](OneGame.Render3D.Shaders/Ssao.PSBlur.md) | static class |  |
| [`Ssao.PSSsao`](OneGame.Render3D.Shaders/Ssao.PSSsao.md) | static class |  |
| [`Ssao.Params`](OneGame.Render3D.Shaders/Ssao.Params.md) | struct |  |
