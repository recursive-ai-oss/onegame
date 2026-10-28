[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Offscreen3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class Offscreen3D : IDisposable
```

Renders a scene into an offscreen colour image and reads it back: the capture path for tools and tests. It owns a render graph and a target texture of a given size.

## Constructors

| Member | Description |
| --- | --- |
| [`Offscreen3D(Renderer3D, int, int, GpuFormat)`](#m-ctor-renderer3d-int-int-gpuformat) | Creates a target of `width` x `height` pixels. |

### <a id="m-ctor-renderer3d-int-int-gpuformat"></a>Offscreen3D(Renderer3D, int, int, GpuFormat)

```csharp
public Offscreen3D(Renderer3D renderer, int width, int height, GpuFormat format = 4);
```

Creates a target of `width` x `height` pixels.

**Parameters**

- `renderer` ([`Renderer3D`](Renderer3D.md)): The renderer to draw with.
- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `format` ([`GpuFormat`](../../OneGame.Gpu/OneGame.Gpu/GpuFormat.md)): `Rgba8UnormSrgb` (default), `Rgba8Unorm` or `Rgba16Float`.

## Properties

| Member | Description |
| --- | --- |
| [`Format`](#m-format) | Format of [`Offscreen3D.Target`](Offscreen3D.md#m-target). |
| [`Graph`](#m-graph) | The render graph the frame ran on (for inspection). |
| [`Height`](#m-height) | Height in pixels. |
| [`Target`](#m-target) | The image that was drawn. |
| [`Width`](#m-width) | Width in pixels. |

### <a id="m-format"></a>Format

```csharp
public GpuFormat Format { get; }
```

Format of [`Offscreen3D.Target`](Offscreen3D.md#m-target).

**Type:** [`GpuFormat`](../../OneGame.Gpu/OneGame.Gpu/GpuFormat.md)

### <a id="m-graph"></a>Graph

```csharp
public RenderGraph Graph { get; }
```

The render graph the frame ran on (for inspection).

**Type:** [`RenderGraph`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraph.md)

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Height in pixels.

**Type:** `int`

### <a id="m-target"></a>Target

```csharp
public GpuTexture Target { get; }
```

The image that was drawn.

**Type:** [`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Width in pixels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Capture(Scene3D)`](#m-capture-scene3d) | Draws one frame and reads it back as tightly packed RGBA bytes (sRGB-encoded for the default format). |
| [`CaptureAsync(Scene3D)`](#m-captureasync-scene3d) | Like [`Offscreen3D.Capture()`](Offscreen3D.md#m-capture-scene3d), for backends that cannot block on a read (a browser): the frame is drawn now and the read completes later. |
| [`Dispose()`](#m-dispose) |  |
| [`Render(Scene3D)`](#m-render-scene3d) | Draws one frame and submits it. |

### <a id="m-capture-scene3d"></a>Capture(Scene3D)

```csharp
public byte[] Capture(Scene3D scene);
```

Draws one frame and reads it back as tightly packed RGBA bytes (sRGB-encoded for the default format).

**Parameters**

- `scene` ([`Scene3D`](Scene3D.md)): What to draw.

**Returns:** `byte[]` — `Width * Height * 4` bytes, top row first.

### <a id="m-captureasync-scene3d"></a>CaptureAsync(Scene3D)

```csharp
public ValueTask<byte[]> CaptureAsync(Scene3D scene);
```

Like [`Offscreen3D.Capture()`](Offscreen3D.md#m-capture-scene3d), for backends that cannot block on a read (a browser): the frame is drawn now and the read completes later.

**Parameters**

- `scene` ([`Scene3D`](Scene3D.md)): What to draw.

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1)`<byte[]>` — `Width * Height * 4` bytes, top row first.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-render-scene3d"></a>Render(Scene3D)

```csharp
public void Render(Scene3D scene);
```

Draws one frame and submits it.

**Parameters**

- `scene` ([`Scene3D`](Scene3D.md)): What to draw.
