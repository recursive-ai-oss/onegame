[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageFrame struct

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PageFrame`](PageFrame.md)&gt;

```csharp
public readonly struct PageFrame : IEquatable<PageFrame>
```

How a frame of pages is drawn.

## Constructors

| Member | Description |
| --- | --- |
| [`PageFrame(int, int, float, int, int, Vector4?)`](#m-ctor-int-int-float-int-int-vector4) | How a frame of pages is drawn. |

### <a id="m-ctor-int-int-float-int-int-vector4"></a>PageFrame(int, int, float, int, int, Vector4?)

```csharp
public PageFrame(int Width, int Height, float Scale, int FrameNumber, int Layer = 0, Vector4? Clear = default);
```

How a frame of pages is drawn.

**Parameters**

- `Width` (`int`): Target width in device pixels.
- `Height` (`int`): Target height in device pixels.
- `Scale` (`float`): Device pixels per CSS pixel.
- `FrameNumber` (`int`): A number that grows with every frame, shared with the UI renderer's atlas bookkeeping (`Renderer2D.FrameNumber`).
- `Layer` (`int`): The host-scope layer to draw: pages draw their own content as layer 0 and any [`PageScopes.Layer()`](PageScopes.md#m-layer-int-int) subtree in its layer.
- `Clear` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)?): Clear the target to this linear colour first, or `null` to draw over what is there.

## Properties

| Member | Description |
| --- | --- |
| [`Clear`](#m-clear) | Clear the target to this linear colour first, or `null` to draw over what is there. |
| [`FrameNumber`](#m-framenumber) | A number that grows with every frame, shared with the UI renderer's atlas bookkeeping (`Renderer2D.FrameNumber`). |
| [`Height`](#m-height) | Target height in device pixels. |
| [`Layer`](#m-layer) | The host-scope layer to draw: pages draw their own content as layer 0 and any [`PageScopes.Layer()`](PageScopes.md#m-layer-int-int) subtree in its layer. |
| [`Scale`](#m-scale) | Device pixels per CSS pixel. |
| [`Width`](#m-width) | Target width in device pixels. |

### <a id="m-clear"></a>Clear

```csharp
public Vector4? Clear { get; init; }
```

Clear the target to this linear colour first, or `null` to draw over what is there.

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)?

### <a id="m-framenumber"></a>FrameNumber

```csharp
public int FrameNumber { get; init; }
```

A number that grows with every frame, shared with the UI renderer's atlas bookkeeping (`Renderer2D.FrameNumber`).

**Type:** `int`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Target height in device pixels.

**Type:** `int`

### <a id="m-layer"></a>Layer

```csharp
public int Layer { get; init; }
```

The host-scope layer to draw: pages draw their own content as layer 0 and any [`PageScopes.Layer()`](PageScopes.md#m-layer-int-int) subtree in its layer.

**Type:** `int`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

Device pixels per CSS pixel.

**Type:** `float`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Target width in device pixels.

**Type:** `int`
