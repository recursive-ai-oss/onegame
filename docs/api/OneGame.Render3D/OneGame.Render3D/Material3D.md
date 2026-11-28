[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Material3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class Material3D
```

A material on the GPU: constants and textures bound as one group. Its [`Material3D.Style`](Material3D.md#m-style) picks physically based, cel-shaded or flat lighting, and its outline settings add a silhouette line; changes apply to the next frame. Emissive light, opacity and textures can be changed at any time without reloading the model.

## Properties

| Member | Description |
| --- | --- |
| [`Emissive`](#m-emissive) | Emitted colour, linear RGB (the file's emissive factor times its strength at load). |
| [`EmissiveStrength`](#m-emissivestrength) | A multiplier on [`Material3D.Emissive`](Material3D.md#m-emissive) (1 by default), for pulsing or fading a glow without touching its colour. |
| [`FogExempt`](#m-fogexempt) | Leaves the material out of [`Scene3D.Fog`](Scene3D.md#m-fog) (false by default). |
| [`LayerCount`](#m-layercount) | Number of texture layers chosen per vertex (see [`Renderer3D.CreateLayeredMaterial()`](Renderer3D.md#m-createlayeredmaterial-string-int-int-readonlyspan-readonlymemory-byte-materialalpha3d-bool-vector4-float-bool)); 0 for an ordinary material. |
| [`Name`](#m-name) | The material's name in the glTF file (empty for the default material). |
| [`Opacity`](#m-opacity) | 1 (default) draws the material as the file says; below 1 it multiplies the alpha and moves the material to the blended pass, which sorts far to near and writes no depth, as for a glTF BLEND material. |
| [`OutlineColor`](#m-outlinecolor) | Outline colour, linear RGB. |
| [`OutlineWidth`](#m-outlinewidth) | Outline thickness in pixels of the render target (0 draws none). |
| [`PlanarReflection`](#m-planarreflection) | Opts the material in to planar reflection: its specular light comes from the scene mirrored about [`Scene3D.Reflection`](Scene3D.md#m-reflection)'s plane (one horizontal plane per frame) instead of the environment alone, where the mirror image has something to show. |
| [`ReflectionStrength`](#m-reflectionstrength) | How much of the mirror image replaces the environment's specular light, 0 to 1 (default 1). |
| [`Style`](#m-style) | The lighting model. |
| [`ToonBands`](#m-toonbands) | Toon: number of light steps (2 to 8). |
| [`ToonHighlight`](#m-toonhighlight) | Toon: strength of the hard specular highlight (0 none). |
| [`ToonSoftness`](#m-toonsoftness) | Toon: width of the transition between two steps, as a fraction of one step (0 is a hard edge). |
| [`UvScroll`](#m-uvscroll) | A constant texture-space velocity, in texture units per second, added to every texture's coordinates of this material after its own transform: the water drifts when [`Scene3D.Time`](Scene3D.md#m-time) advances. |
| [`ViewTintColor`](#m-viewtintcolor) | The colour the base colour is blended toward by [`Material3D.ViewTintStrength`](Material3D.md#m-viewtintstrength), linear RGB (white by default). |
| [`ViewTintEdge`](#m-viewtintedge) | The facing value (cosine of the angle between normal and view direction, 0 to 1) at the middle of the tint's edge. |
| [`ViewTintPower`](#m-viewtintpower) | An exponent on the tint weight (default 1; at least 0.01). |
| [`ViewTintSoftness`](#m-viewtintsoftness) | Half the width of the edge's transition, in facing units, 0 to 1. |
| [`ViewTintStrength`](#m-viewtintstrength) | How strongly the base colour is blended toward [`Material3D.ViewTintColor`](Material3D.md#m-viewtintcolor) by the angle between the surface normal and the view direction, 0 to 1 (0 by default: off, and the output is identical to a material without a tint). |
| [`ViewTintZone`](#m-viewtintzone) | Which side of the edge is tinted ([`ViewTintZone3D.OutsideCone`](ViewTintZone3D.md#m-outsidecone) by default). |

### <a id="m-emissive"></a>Emissive

```csharp
public Vector3 Emissive { get; set; }
```

Emitted colour, linear RGB (the file's emissive factor times its strength at load). It is multiplied by the emissive texture and by [`Material3D.EmissiveStrength`](Material3D.md#m-emissivestrength).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-emissivestrength"></a>EmissiveStrength

```csharp
public float EmissiveStrength { get; set; }
```

A multiplier on [`Material3D.Emissive`](Material3D.md#m-emissive) (1 by default), for pulsing or fading a glow without touching its colour.

**Type:** `float`

### <a id="m-fogexempt"></a>FogExempt

```csharp
public bool FogExempt { get; set; }
```

Leaves the material out of [`Scene3D.Fog`](Scene3D.md#m-fog) (false by default). Use it for a backdrop layer that must keep its own colours: a sky dome or skybox mesh, distant mountains painted into the sky. The material is still lit, tone mapped and depth tested as usual.

**Type:** `bool`

### <a id="m-layercount"></a>LayerCount

```csharp
public int LayerCount { get; }
```

Number of texture layers chosen per vertex (see [`Renderer3D.CreateLayeredMaterial()`](Renderer3D.md#m-createlayeredmaterial-string-int-int-readonlyspan-readonlymemory-byte-materialalpha3d-bool-vector4-float-bool)); 0 for an ordinary material.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The material's name in the glTF file (empty for the default material).

**Type:** `string`

### <a id="m-opacity"></a>Opacity

```csharp
public float Opacity { get; set; }
```

1 (default) draws the material as the file says; below 1 it multiplies the alpha and moves the material to the blended pass, which sorts far to near and writes no depth, as for a glTF BLEND material. Translucent materials cast no shadows and have no outline.

**Type:** `float`

### <a id="m-outlinecolor"></a>OutlineColor

```csharp
public Vector3 OutlineColor { get; set; }
```

Outline colour, linear RGB.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-outlinewidth"></a>OutlineWidth

```csharp
public float OutlineWidth { get; set; }
```

Outline thickness in pixels of the render target (0 draws none). Works with every style; it needs smooth vertex normals.

**Type:** `float`

### <a id="m-planarreflection"></a>PlanarReflection

```csharp
public bool PlanarReflection { get; set; }
```

Opts the material in to planar reflection: its specular light comes from the scene mirrored about [`Scene3D.Reflection`](Scene3D.md#m-reflection)'s plane (one horizontal plane per frame) instead of the environment alone, where the mirror image has something to show. The material itself is left out of the mirror image. Meant for flat, horizontal, usually blended surfaces such as water; it affects PBR style only. Costs one extra scene pass, only while a material that opts in is drawn.

**Type:** `bool`

### <a id="m-reflectionstrength"></a>ReflectionStrength

```csharp
public float ReflectionStrength { get; set; }
```

How much of the mirror image replaces the environment's specular light, 0 to 1 (default 1). The share also falls with roughness (it is strength times one minus roughness), so rough surfaces keep the blurred environment. The Fresnel term is the usual one: grazing views reflect more.

**Type:** `float`

### <a id="m-style"></a>Style

```csharp
public MaterialStyle3D Style { get; set; }
```

The lighting model.

**Type:** [`MaterialStyle3D`](MaterialStyle3D.md)

### <a id="m-toonbands"></a>ToonBands

```csharp
public int ToonBands { get; set; }
```

Toon: number of light steps (2 to 8).

**Type:** `int`

### <a id="m-toonhighlight"></a>ToonHighlight

```csharp
public float ToonHighlight { get; set; }
```

Toon: strength of the hard specular highlight (0 none).

**Type:** `float`

### <a id="m-toonsoftness"></a>ToonSoftness

```csharp
public float ToonSoftness { get; set; }
```

Toon: width of the transition between two steps, as a fraction of one step (0 is a hard edge).

**Type:** `float`

### <a id="m-uvscroll"></a>UvScroll

```csharp
public Vector2 UvScroll { get; set; }
```

A constant texture-space velocity, in texture units per second, added to every texture's coordinates of this material after its own transform: the water drifts when [`Scene3D.Time`](Scene3D.md#m-time) advances. (0, 0) by default, which changes nothing. Textures need a repeating sampler (the glTF default) to scroll without a seam.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-viewtintcolor"></a>ViewTintColor

```csharp
public Vector3 ViewTintColor { get; set; }
```

The colour the base colour is blended toward by [`Material3D.ViewTintStrength`](Material3D.md#m-viewtintstrength), linear RGB (white by default).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-viewtintedge"></a>ViewTintEdge

```csharp
public float ViewTintEdge { get; set; }
```

The facing value (cosine of the angle between normal and view direction, 0 to 1) at the middle of the tint's edge. Default 0.66 (about 48.6 degrees, the edge of Snell's window).

**Type:** `float`

### <a id="m-viewtintpower"></a>ViewTintPower

```csharp
public float ViewTintPower { get; set; }
```

An exponent on the tint weight (default 1; at least 0.01). Above 1 pulls the tint toward its extreme, which makes a rim tint fall off like Fresnel.

**Type:** `float`

### <a id="m-viewtintsoftness"></a>ViewTintSoftness

```csharp
public float ViewTintSoftness { get; set; }
```

Half the width of the edge's transition, in facing units, 0 to 1. 0 makes a sharp cone; larger values fade it. Default 0.02.

**Type:** `float`

### <a id="m-viewtintstrength"></a>ViewTintStrength

```csharp
public float ViewTintStrength { get; set; }
```

How strongly the base colour is blended toward [`Material3D.ViewTintColor`](Material3D.md#m-viewtintcolor) by the angle between the surface normal and the view direction, 0 to 1 (0 by default: off, and the output is identical to a material without a tint). Per pixel, with f the clamped dot product of the (normal-mapped) normal and the direction to the camera, k = smoothstep([`Material3D.ViewTintEdge`](Material3D.md#m-viewtintedge) - [`Material3D.ViewTintSoftness`](Material3D.md#m-viewtintsoftness), [`Material3D.ViewTintEdge`](Material3D.md#m-viewtintedge) + [`Material3D.ViewTintSoftness`](Material3D.md#m-viewtintsoftness), f) (a hard step at `ViewTintEdge` when the softness is 0); the weight is k for [`ViewTintZone3D.InsideCone`](ViewTintZone3D.md#m-insidecone) and 1 - k for [`ViewTintZone3D.OutsideCone`](ViewTintZone3D.md#m-outsidecone), raised to [`Material3D.ViewTintPower`](Material3D.md#m-viewtintpower); the colour becomes `lerp(base, tint, strength * weight)`. For a Snell's window seen from under water, point the surface normal at the viewer (back faces are flipped for you), set the edge to cos(48.6 degrees) = 0.66 and tint outside the cone with a small softness; for a soft Fresnel-like rim tint use an edge of 0, a softness near 1 and a power of 2 or more. Applies to the PBR and toon styles (flat and unlit surfaces have no lit normal and ignore it).

**Type:** `float`

### <a id="m-viewtintzone"></a>ViewTintZone

```csharp
public ViewTintZone3D ViewTintZone { get; set; }
```

Which side of the edge is tinted ([`ViewTintZone3D.OutsideCone`](ViewTintZone3D.md#m-outsidecone) by default).

**Type:** [`ViewTintZone3D`](ViewTintZone3D.md)

## Methods

| Member | Description |
| --- | --- |
| [`ResetTexture(MaterialSlot3D)`](#m-resettexture-materialslot3d) | Puts back the texture the file gave (or the plain white one) in a slot. |
| [`SetTexture(MaterialSlot3D, GpuTexture)`](#m-settexture-materialslot3d-gputexture) | Replaces one texture of this material, for example with the target of an [`Offscreen3D`](Offscreen3D.md) built with `sampled: true`, so a portal window shows another view. |

### <a id="m-resettexture-materialslot3d"></a>ResetTexture(MaterialSlot3D)

```csharp
public void ResetTexture(MaterialSlot3D slot);
```

Puts back the texture the file gave (or the plain white one) in a slot.

**Parameters**

- `slot` ([`MaterialSlot3D`](MaterialSlot3D.md)): Which texture to restore.

### <a id="m-settexture-materialslot3d-gputexture"></a>SetTexture(MaterialSlot3D, GpuTexture)

```csharp
public void SetTexture(MaterialSlot3D slot, GpuTexture texture);
```

Replaces one texture of this material, for example with the target of an [`Offscreen3D`](Offscreen3D.md) built with `sampled: true`, so a portal window shows another view. The texture keeps being used until [`Material3D.ResetTexture()`](Material3D.md#m-resettexture-materialslot3d); it must outlive the frames that draw it. Rendered targets are tone-mapped already: show one through [`MaterialSlot3D.Emissive`](MaterialSlot3D.md#m-emissive) (base colour black) or the [`MaterialStyle3D.Flat`](MaterialStyle3D.md#m-flat) style on the base colour slot. Use an `Rgba8UnormSrgb` target so sampling returns linear light.

**Parameters**

- `slot` ([`MaterialSlot3D`](MaterialSlot3D.md)): Which texture to replace.
- `texture` (`GpuTexture`): A texture with `TextureUsage.Sampled`.
