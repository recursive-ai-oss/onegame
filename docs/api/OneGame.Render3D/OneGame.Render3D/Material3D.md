[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Material3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class Material3D
```

A material on the GPU: constants and textures bound as one group. Its [`Material3D.Style`](Material3D.md#m-style) picks physically based, cel-shaded or flat lighting, and its outline settings add a silhouette line; changes apply to the next frame.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The material's name in the glTF file (empty for the default material). |
| [`OutlineColor`](#m-outlinecolor) | Outline colour, linear RGB. |
| [`OutlineWidth`](#m-outlinewidth) | Outline thickness in pixels of the render target (0 draws none). |
| [`Style`](#m-style) | The lighting model. |
| [`ToonBands`](#m-toonbands) | Toon: number of light steps (2 to 8). |
| [`ToonHighlight`](#m-toonhighlight) | Toon: strength of the hard specular highlight (0 none). |
| [`ToonSoftness`](#m-toonsoftness) | Toon: width of the transition between two steps, as a fraction of one step (0 is a hard edge). |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The material's name in the glTF file (empty for the default material).

**Type:** `string`

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
