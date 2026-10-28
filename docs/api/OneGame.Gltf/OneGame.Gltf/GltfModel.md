[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfModel class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfModel
```

An imported glTF file.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfModel()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfModel()

```csharp
public GltfModel();
```

## Properties

| Member | Description |
| --- | --- |
| [`Animations`](#m-animations) | The animation clips. |
| [`Cameras`](#m-cameras) | The cameras. |
| [`Copyright`](#m-copyright) | The `asset.copyright` text, or empty. |
| [`DefaultScene`](#m-defaultscene) | Index of the default scene (0 when the file names none and has scenes), or -1 when there are no scenes. |
| [`ExtensionsRequired`](#m-extensionsrequired) | `extensionsRequired` as written. |
| [`ExtensionsUsed`](#m-extensionsused) | `extensionsUsed` as written. |
| [`Generator`](#m-generator) | The `asset.generator` text, or empty. |
| [`Images`](#m-images) | The images. |
| [`IsBinary`](#m-isbinary) | True when the file was a binary GLB. |
| [`Lights`](#m-lights) | The punctual lights. |
| [`Materials`](#m-materials) | The materials. |
| [`Meshes`](#m-meshes) | The meshes. |
| [`Nodes`](#m-nodes) | The nodes. |
| [`OutputBytes`](#m-outputbytes) | Bytes the importer allocated for the model, as counted against [`GltfLimits.MaxOutputBytes`](GltfLimits.md#m-maxoutputbytes). |
| [`Samplers`](#m-samplers) | The samplers. |
| [`Scenes`](#m-scenes) | The scenes. |
| [`Skins`](#m-skins) | The skins. |
| [`Textures`](#m-textures) | The textures. |
| [`Warnings`](#m-warnings) | Things that loaded but were degraded or ignored. |

### <a id="m-animations"></a>Animations

```csharp
public AnimationClip[] Animations { get; }
```

The animation clips.

**Type:** [`AnimationClip`](AnimationClip.md)\[\]

### <a id="m-cameras"></a>Cameras

```csharp
public GltfCamera[] Cameras { get; }
```

The cameras.

**Type:** [`GltfCamera`](GltfCamera.md)\[\]

### <a id="m-copyright"></a>Copyright

```csharp
public string Copyright { get; }
```

The `asset.copyright` text, or empty.

**Type:** `string`

### <a id="m-defaultscene"></a>DefaultScene

```csharp
public int DefaultScene { get; }
```

Index of the default scene (0 when the file names none and has scenes), or -1 when there are no scenes.

**Type:** `int`

### <a id="m-extensionsrequired"></a>ExtensionsRequired

```csharp
public string[] ExtensionsRequired { get; }
```

`extensionsRequired` as written. All of them are supported, or the load failed.

**Type:** `string[]`

### <a id="m-extensionsused"></a>ExtensionsUsed

```csharp
public string[] ExtensionsUsed { get; }
```

`extensionsUsed` as written.

**Type:** `string[]`

### <a id="m-generator"></a>Generator

```csharp
public string Generator { get; }
```

The `asset.generator` text, or empty.

**Type:** `string`

### <a id="m-images"></a>Images

```csharp
public GltfImage[] Images { get; }
```

The images.

**Type:** [`GltfImage`](GltfImage.md)\[\]

### <a id="m-isbinary"></a>IsBinary

```csharp
public bool IsBinary { get; }
```

True when the file was a binary GLB.

**Type:** `bool`

### <a id="m-lights"></a>Lights

```csharp
public GltfLight[] Lights { get; }
```

The punctual lights.

**Type:** [`GltfLight`](GltfLight.md)\[\]

### <a id="m-materials"></a>Materials

```csharp
public GltfMaterial[] Materials { get; }
```

The materials.

**Type:** [`GltfMaterial`](GltfMaterial.md)\[\]

### <a id="m-meshes"></a>Meshes

```csharp
public GltfMesh[] Meshes { get; }
```

The meshes.

**Type:** [`GltfMesh`](GltfMesh.md)\[\]

### <a id="m-nodes"></a>Nodes

```csharp
public GltfNode[] Nodes { get; }
```

The nodes.

**Type:** [`GltfNode`](GltfNode.md)\[\]

### <a id="m-outputbytes"></a>OutputBytes

```csharp
public long OutputBytes { get; }
```

Bytes the importer allocated for the model, as counted against [`GltfLimits.MaxOutputBytes`](GltfLimits.md#m-maxoutputbytes).

**Type:** `long`

### <a id="m-samplers"></a>Samplers

```csharp
public GltfSampler[] Samplers { get; }
```

The samplers.

**Type:** [`GltfSampler`](GltfSampler.md)\[\]

### <a id="m-scenes"></a>Scenes

```csharp
public GltfScene[] Scenes { get; }
```

The scenes.

**Type:** [`GltfScene`](GltfScene.md)\[\]

### <a id="m-skins"></a>Skins

```csharp
public GltfSkin[] Skins { get; }
```

The skins.

**Type:** [`GltfSkin`](GltfSkin.md)\[\]

### <a id="m-textures"></a>Textures

```csharp
public GltfTexture[] Textures { get; }
```

The textures.

**Type:** [`GltfTexture`](GltfTexture.md)\[\]

### <a id="m-warnings"></a>Warnings

```csharp
public GltfWarning[] Warnings { get; }
```

Things that loaded but were degraded or ignored.

**Type:** [`GltfWarning`](GltfWarning.md)\[\]

## Methods

| Member | Description |
| --- | --- |
| [`ComputeWorldBounds(int)`](#m-computeworldbounds-int) | The axis-aligned box of every mesh in a scene, in the model's root space at the rest pose. |
| [`CreateRestPose()`](#m-createrestpose) | Creates the rest pose: every node's own translation, rotation and scale, and the default morph weights. |

### <a id="m-computeworldbounds-int"></a>ComputeWorldBounds(int)

```csharp
public GltfBounds? ComputeWorldBounds(int scene = -1);
```

The axis-aligned box of every mesh in a scene, in the model's root space at the rest pose.

**Parameters**

- `scene` (`int`): Scene index, or -1 for the default scene (all meshes when the file has no scenes).

**Returns:** [`GltfBounds`](GltfBounds.md)? — The box, or null when the scene draws nothing.

### <a id="m-createrestpose"></a>CreateRestPose()

```csharp
public NodePose CreateRestPose();
```

Creates the rest pose: every node's own translation, rotation and scale, and the default morph weights.

**Returns:** [`NodePose`](NodePose.md) — A pose to animate.
