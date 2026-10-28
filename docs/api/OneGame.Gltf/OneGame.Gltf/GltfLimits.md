[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfLimits class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfLimits
```

Upper bounds the importer enforces before it allocates. The defaults suit content from a server; raise them for trusted tools.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfLimits()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfLimits()

```csharp
public GltfLimits();
```

## Properties

| Member | Description |
| --- | --- |
| [`MaxAccessors`](#m-maxaccessors) | The most accessors. |
| [`MaxAnimations`](#m-maxanimations) | The most animations. |
| [`MaxBufferBytes`](#m-maxbufferbytes) | The largest declared size of one buffer. |
| [`MaxBufferViews`](#m-maxbufferviews) | The most buffer views. |
| [`MaxBuffers`](#m-maxbuffers) | The most buffers. |
| [`MaxElementsPerAccessor`](#m-maxelementsperaccessor) | The most elements one accessor may declare. |
| [`MaxFileBytes`](#m-maxfilebytes) | The largest file (or GLB) read, in bytes (default 256 MiB). |
| [`MaxImageDimension`](#m-maximagedimension) | The longest side of a decoded image, in pixels. |
| [`MaxImagePixels`](#m-maximagepixels) | The most pixels in one image. |
| [`MaxImages`](#m-maximages) | The most images. |
| [`MaxIndicesPerPrimitive`](#m-maxindicesperprimitive) | The most indices in one primitive, after expansion. |
| [`MaxJointsPerSkin`](#m-maxjointsperskin) | The most joints in one skin. |
| [`MaxJsonBytes`](#m-maxjsonbytes) | The largest JSON text, in bytes. |
| [`MaxJsonDepth`](#m-maxjsondepth) | The deepest JSON nesting the reader accepts. |
| [`MaxKeyframes`](#m-maxkeyframes) | The most keyframes in one animation sampler. |
| [`MaxMaterials`](#m-maxmaterials) | The most materials. |
| [`MaxMeshes`](#m-maxmeshes) | The most meshes. |
| [`MaxMorphTargets`](#m-maxmorphtargets) | The most morph targets in one mesh. |
| [`MaxNodeDepth`](#m-maxnodedepth) | The deepest node tree. |
| [`MaxNodes`](#m-maxnodes) | The most nodes in the file. |
| [`MaxOutputBytes`](#m-maxoutputbytes) | The most bytes the importer allocates for the model (arrays and pixels), charged before each allocation. |
| [`MaxPrimitives`](#m-maxprimitives) | The most primitives in all meshes together. |
| [`MaxSkins`](#m-maxskins) | The most skins. |
| [`MaxSmallLists`](#m-maxsmalllists) | The most scenes, samplers, cameras and lights (each). |
| [`MaxTextures`](#m-maxtextures) | The most textures. |
| [`MaxTotalBufferBytes`](#m-maxtotalbufferbytes) | The largest total of all buffers. |
| [`MaxVerticesPerPrimitive`](#m-maxverticesperprimitive) | The most vertices in one primitive, after expansion. |

### <a id="m-maxaccessors"></a>MaxAccessors

```csharp
public int MaxAccessors { get; init; }
```

The most accessors.

**Type:** `int`

### <a id="m-maxanimations"></a>MaxAnimations

```csharp
public int MaxAnimations { get; init; }
```

The most animations.

**Type:** `int`

### <a id="m-maxbufferbytes"></a>MaxBufferBytes

```csharp
public long MaxBufferBytes { get; init; }
```

The largest declared size of one buffer.

**Type:** `long`

### <a id="m-maxbufferviews"></a>MaxBufferViews

```csharp
public int MaxBufferViews { get; init; }
```

The most buffer views.

**Type:** `int`

### <a id="m-maxbuffers"></a>MaxBuffers

```csharp
public int MaxBuffers { get; init; }
```

The most buffers.

**Type:** `int`

### <a id="m-maxelementsperaccessor"></a>MaxElementsPerAccessor

```csharp
public int MaxElementsPerAccessor { get; init; }
```

The most elements one accessor may declare.

**Type:** `int`

### <a id="m-maxfilebytes"></a>MaxFileBytes

```csharp
public long MaxFileBytes { get; init; }
```

The largest file (or GLB) read, in bytes (default 256 MiB).

**Type:** `long`

### <a id="m-maximagedimension"></a>MaxImageDimension

```csharp
public int MaxImageDimension { get; init; }
```

The longest side of a decoded image, in pixels.

**Type:** `int`

### <a id="m-maximagepixels"></a>MaxImagePixels

```csharp
public long MaxImagePixels { get; init; }
```

The most pixels in one image.

**Type:** `long`

### <a id="m-maximages"></a>MaxImages

```csharp
public int MaxImages { get; init; }
```

The most images.

**Type:** `int`

### <a id="m-maxindicesperprimitive"></a>MaxIndicesPerPrimitive

```csharp
public int MaxIndicesPerPrimitive { get; init; }
```

The most indices in one primitive, after expansion.

**Type:** `int`

### <a id="m-maxjointsperskin"></a>MaxJointsPerSkin

```csharp
public int MaxJointsPerSkin { get; init; }
```

The most joints in one skin.

**Type:** `int`

### <a id="m-maxjsonbytes"></a>MaxJsonBytes

```csharp
public int MaxJsonBytes { get; init; }
```

The largest JSON text, in bytes.

**Type:** `int`

### <a id="m-maxjsondepth"></a>MaxJsonDepth

```csharp
public int MaxJsonDepth { get; init; }
```

The deepest JSON nesting the reader accepts.

**Type:** `int`

### <a id="m-maxkeyframes"></a>MaxKeyframes

```csharp
public int MaxKeyframes { get; init; }
```

The most keyframes in one animation sampler.

**Type:** `int`

### <a id="m-maxmaterials"></a>MaxMaterials

```csharp
public int MaxMaterials { get; init; }
```

The most materials.

**Type:** `int`

### <a id="m-maxmeshes"></a>MaxMeshes

```csharp
public int MaxMeshes { get; init; }
```

The most meshes.

**Type:** `int`

### <a id="m-maxmorphtargets"></a>MaxMorphTargets

```csharp
public int MaxMorphTargets { get; init; }
```

The most morph targets in one mesh.

**Type:** `int`

### <a id="m-maxnodedepth"></a>MaxNodeDepth

```csharp
public int MaxNodeDepth { get; init; }
```

The deepest node tree.

**Type:** `int`

### <a id="m-maxnodes"></a>MaxNodes

```csharp
public int MaxNodes { get; init; }
```

The most nodes in the file.

**Type:** `int`

### <a id="m-maxoutputbytes"></a>MaxOutputBytes

```csharp
public long MaxOutputBytes { get; init; }
```

The most bytes the importer allocates for the model (arrays and pixels), charged before each allocation.

**Type:** `long`

### <a id="m-maxprimitives"></a>MaxPrimitives

```csharp
public int MaxPrimitives { get; init; }
```

The most primitives in all meshes together.

**Type:** `int`

### <a id="m-maxskins"></a>MaxSkins

```csharp
public int MaxSkins { get; init; }
```

The most skins.

**Type:** `int`

### <a id="m-maxsmalllists"></a>MaxSmallLists

```csharp
public int MaxSmallLists { get; init; }
```

The most scenes, samplers, cameras and lights (each).

**Type:** `int`

### <a id="m-maxtextures"></a>MaxTextures

```csharp
public int MaxTextures { get; init; }
```

The most textures.

**Type:** `int`

### <a id="m-maxtotalbufferbytes"></a>MaxTotalBufferBytes

```csharp
public long MaxTotalBufferBytes { get; init; }
```

The largest total of all buffers.

**Type:** `long`

### <a id="m-maxverticesperprimitive"></a>MaxVerticesPerPrimitive

```csharp
public int MaxVerticesPerPrimitive { get; init; }
```

The most vertices in one primitive, after expansion.

**Type:** `int`
