[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# RenderScaleResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`RenderScaleResult`](RenderScaleResult.md)&gt;

```csharp
public sealed record RenderScaleResult : IEquatable<RenderScaleResult>
```

Result of `render_scale`: the share of the window the 3D scene is drawn at.

## Constructors

| Member | Description |
| --- | --- |
| [`RenderScaleResult(double, int, int)`](#m-ctor-double-int-int) | Result of `render_scale`: the share of the window the 3D scene is drawn at. |

### <a id="m-ctor-double-int-int"></a>RenderScaleResult(double, int, int)

```csharp
public RenderScaleResult(double Scale, int RenderWidth, int RenderHeight);
```

Result of `render_scale`: the share of the window the 3D scene is drawn at.

**Parameters**

- `Scale` (`double`): The scale in effect, 0.25 to 1.
- `RenderWidth` (`int`): Width in pixels of the image the last frame drew the scene at (0 before the first frame).
- `RenderHeight` (`int`): Height in pixels of that image.

## Properties

| Member | Description |
| --- | --- |
| [`RenderHeight`](#m-renderheight) | Height in pixels of that image. |
| [`RenderWidth`](#m-renderwidth) | Width in pixels of the image the last frame drew the scene at (0 before the first frame). |
| [`Scale`](#m-scale) | The scale in effect, 0.25 to 1. |

### <a id="m-renderheight"></a>RenderHeight

```csharp
public int RenderHeight { get; init; }
```

Height in pixels of that image.

**Type:** `int`

### <a id="m-renderwidth"></a>RenderWidth

```csharp
public int RenderWidth { get; init; }
```

Width in pixels of the image the last frame drew the scene at (0 before the first frame).

**Type:** `int`

### <a id="m-scale"></a>Scale

```csharp
public double Scale { get; init; }
```

The scale in effect, 0.25 to 1.

**Type:** `double`
