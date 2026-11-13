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
