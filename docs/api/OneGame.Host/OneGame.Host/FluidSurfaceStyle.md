[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# FluidSurfaceStyle class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class FluidSurfaceStyle
```

How a [`FluidSurface2D`](FluidSurface2D.md) looks. All lengths are in the units of the layer the fluid is drawn on (the units you pass to [`FluidSurface2D.Draw()`](FluidSurface2D.md#m-draw-draw2d-readonlyspan-vector2-float-bool-float)).

## Constructors

| Member | Description |
| --- | --- |
| [`FluidSurfaceStyle()`](#m-ctor) |  |

### <a id="m-ctor"></a>FluidSurfaceStyle()

```csharp
public FluidSurfaceStyle();
```

## Properties

| Member | Description |
| --- | --- |
| [`Blur`](#m-blur) | How far the blur softens the splats, in layer units (the visible softening reaches about twice this). |
| [`Fill`](#m-fill) | The body colour (sRGB, straight alpha). |
| [`ParticleRadius`](#m-particleradius) | The radius of each particle's soft splat, where its falloff reaches zero. |
| [`Rim`](#m-rim) | The colour of the band just inside the outline, which reads as a lit edge or foam. |
| [`RimStrength`](#m-rimstrength) | How much of the rim colour shows at the outline, 0 to 1 (0 turns the rim off). |
| [`RimWidth`](#m-rimwidth) | How far inside the outline the rim reaches, in field units. |
| [`Softness`](#m-softness) | How smoothly the outline blends, in field units. |
| [`Threshold`](#m-threshold) | The field level (0 to 1) where the surface sits. |

### <a id="m-blur"></a>Blur

```csharp
public float Blur { get; set; }
```

How far the blur softens the splats, in layer units (the visible softening reaches about twice this). Roughly the particle spacing; larger merges more and rounds off detail. Default 0.1.

**Type:** `float`

### <a id="m-fill"></a>Fill

```csharp
public Color Fill { get; set; }
```

The body colour (sRGB, straight alpha). Default a mid blue.

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-particleradius"></a>ParticleRadius

```csharp
public float ParticleRadius { get; set; }
```

The radius of each particle's soft splat, where its falloff reaches zero. About 1.2 to 1.8 times the particle spacing fills the body without lumps; smaller lets a thin stream break into drops. Default 0.2.

**Type:** `float`

### <a id="m-rim"></a>Rim

```csharp
public Color Rim { get; set; }
```

The colour of the band just inside the outline, which reads as a lit edge or foam. Default a light blue.

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-rimstrength"></a>RimStrength

```csharp
public float RimStrength { get; set; }
```

How much of the rim colour shows at the outline, 0 to 1 (0 turns the rim off). Default 1.

**Type:** `float`

### <a id="m-rimwidth"></a>RimWidth

```csharp
public float RimWidth { get; set; }
```

How far inside the outline the rim reaches, in field units. Default 0.25.

**Type:** `float`

### <a id="m-softness"></a>Softness

```csharp
public float Softness { get; set; }
```

How smoothly the outline blends, in field units. Default 0.05.

**Type:** `float`

### <a id="m-threshold"></a>Threshold

```csharp
public float Threshold { get; set; }
```

The field level (0 to 1) where the surface sits. Lower swells the body and bridges gaps between particles; higher shrinks it and lets thin streams break up. Default 0.45.

**Type:** `float`
