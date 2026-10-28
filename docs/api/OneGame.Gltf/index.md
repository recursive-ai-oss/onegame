[API reference](../index.md) › OneGame.Gltf

# OneGame.Gltf

glTF 2.0 import: scenes, meshes, materials, skins and animations, safe on untrusted input.

**Package:** `RecursiveAI.OneGame.Gltf`

```sh
dotnet add package RecursiveAI.OneGame.Gltf
```

## OneGame.Gltf

| Type | Kind | Description |
| --- | --- | --- |
| [`AnimationChannel`](OneGame.Gltf/AnimationChannel.md) | class | Connects a sampler to a node property. |
| [`AnimationClip`](OneGame.Gltf/AnimationClip.md) | class | An animation clip: channels that drive node properties over time. |
| [`AnimationInterpolation`](OneGame.Gltf/AnimationInterpolation.md) | enum | How values between two keys are computed. |
| [`AnimationPath`](OneGame.Gltf/AnimationPath.md) | enum | Which node property a channel animates. |
| [`AnimationSampler`](OneGame.Gltf/AnimationSampler.md) | class | Keys and values of one animated property. |
| [`ClearcoatInfo`](OneGame.Gltf/ClearcoatInfo.md) | class | KHR\_materials\_clearcoat. |
| [`GltfAlphaMode`](OneGame.Gltf/GltfAlphaMode.md) | enum | How the alpha of the base colour is used. |
| [`GltfBounds`](OneGame.Gltf/GltfBounds.md) | struct | An axis-aligned box. |
| [`GltfCamera`](OneGame.Gltf/GltfCamera.md) | class | A perspective or orthographic camera. |
| [`GltfException`](OneGame.Gltf/GltfException.md) | class | The file cannot be imported: it is damaged, hostile, over a limit or uses something the importer does not support. |
| [`GltfFolderResources`](OneGame.Gltf/GltfFolderResources.md) | class | Reads external files from a folder and refuses to leave it. |
| [`GltfImage`](OneGame.Gltf/GltfImage.md) | class | An image of the model. |
| [`GltfLight`](OneGame.Gltf/GltfLight.md) | class | A KHR\_lights\_punctual light. |
| [`GltfLightType`](OneGame.Gltf/GltfLightType.md) | enum | The kind of a punctual light. |
| [`GltfLimits`](OneGame.Gltf/GltfLimits.md) | class | Upper bounds the importer enforces before it allocates. |
| [`GltfLoadOptions`](OneGame.Gltf/GltfLoadOptions.md) | class | How a file is imported. |
| [`GltfLoader`](OneGame.Gltf/GltfLoader.md) | static class | Imports glTF 2.0 files (`.gltf` and `.glb`) into a [`GltfModel`](OneGame.Gltf/GltfModel.md). |
| [`GltfMagFilter`](OneGame.Gltf/GltfMagFilter.md) | enum | Texture magnification filter. |
| [`GltfMaterial`](OneGame.Gltf/GltfMaterial.md) | class | A metal/rough material with the supported extensions. |
| [`GltfMesh`](OneGame.Gltf/GltfMesh.md) | class | A mesh: one or more primitives sharing a node and morph weights. |
| [`GltfMinFilter`](OneGame.Gltf/GltfMinFilter.md) | enum | Texture minification filter. |
| [`GltfModel`](OneGame.Gltf/GltfModel.md) | class | An imported glTF file. |
| [`GltfNode`](OneGame.Gltf/GltfNode.md) | class | A node in the transform tree. |
| [`GltfPrimitive`](OneGame.Gltf/GltfPrimitive.md) | class | One drawable piece of a mesh: vertex streams, indices and a material. |
| [`GltfSampler`](OneGame.Gltf/GltfSampler.md) | class | Texture sampling state. |
| [`GltfScene`](OneGame.Gltf/GltfScene.md) | class | A scene: the root nodes drawn together. |
| [`GltfSkin`](OneGame.Gltf/GltfSkin.md) | class | A skin: the joints that deform a mesh. |
| [`GltfTexture`](OneGame.Gltf/GltfTexture.md) | class | A texture: an image with a sampler. |
| [`GltfTopology`](OneGame.Gltf/GltfTopology.md) | enum | How a primitive's indices form shapes. |
| [`GltfUri`](OneGame.Gltf/GltfUri.md) | static class | The uri rules: data uris are decoded in memory, everything else must be a plain relative path. |
| [`GltfWarning`](OneGame.Gltf/GltfWarning.md) | struct | Something that loaded but was degraded or ignored. |
| [`GltfWrap`](OneGame.Gltf/GltfWrap.md) | enum | Texture coordinate wrapping. |
| [`IGltfResources`](OneGame.Gltf/IGltfResources.md) | interface | Supplies the external files (buffers and images) a `.gltf` names. |
| [`ImageStatus`](OneGame.Gltf/ImageStatus.md) | enum | The outcome of reading an image. |
| [`IndexWidth`](OneGame.Gltf/IndexWidth.md) | enum | The index width chosen by [`GltfPrimitive.PackIndices()`](OneGame.Gltf/GltfPrimitive.md#m-packindices-indexwidth). |
| [`JointIndices`](OneGame.Gltf/JointIndices.md) | struct | Four joint indices of a skinned vertex. |
| [`MorphTarget`](OneGame.Gltf/MorphTarget.md) | class | Per-vertex deltas of one morph target. |
| [`NodePose`](OneGame.Gltf/NodePose.md) | class | The animatable state of every node: translation, rotation, scale and morph weights. |
| [`SheenInfo`](OneGame.Gltf/SheenInfo.md) | class | KHR\_materials\_sheen. |
| [`SpecularInfo`](OneGame.Gltf/SpecularInfo.md) | class | KHR\_materials\_specular. |
| [`TangentMode`](OneGame.Gltf/TangentMode.md) | enum | When tangents are generated. |
| [`TextureRef`](OneGame.Gltf/TextureRef.md) | class | A reference from a material slot to a texture. |
| [`TextureTransform`](OneGame.Gltf/TextureTransform.md) | struct | A KHR\_texture\_transform: offset, rotation and scale of the texture coordinates. |
| [`TransmissionInfo`](OneGame.Gltf/TransmissionInfo.md) | class | KHR\_materials\_transmission. |
| [`VolumeInfo`](OneGame.Gltf/VolumeInfo.md) | class | KHR\_materials\_volume. |
