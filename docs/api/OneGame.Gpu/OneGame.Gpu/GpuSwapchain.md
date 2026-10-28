[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuSwapchain class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`GpuObject`](GpuObject.md)

```csharp
public abstract class GpuSwapchain : GpuObject
```

Presents rendered images to a window. Per frame: [`GpuSwapchain.TryAcquire()`](GpuSwapchain.md#m-tryacquire-gputexture), record and submit commands that render into the returned texture, then [`GpuSwapchain.Present()`](GpuSwapchain.md#m-present).

## Constructors

| Member | Description |
| --- | --- |
| [`GpuSwapchain()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuSwapchain()

```csharp
protected GpuSwapchain();
```

## Properties

| Member | Description |
| --- | --- |
| [`AcquireTimeout`](#m-acquiretimeout) | How long [`GpuSwapchain.TryAcquire()`](GpuSwapchain.md#m-tryacquire-gputexture) waits for a free backbuffer before it gives up and returns [`AcquireStatus.Skipped`](AcquireStatus.md#m-skipped). |
| [`Format`](#m-format) | Format of the backbuffers. |
| [`Height`](#m-height) | Current height in pixels. |
| [`ImageCount`](#m-imagecount) | Number of backbuffers. |
| [`Width`](#m-width) | Current width in pixels. |

### <a id="m-acquiretimeout"></a>AcquireTimeout

```csharp
public TimeSpan AcquireTimeout { get; set; }
```

How long [`GpuSwapchain.TryAcquire()`](GpuSwapchain.md#m-tryacquire-gputexture) waits for a free backbuffer before it gives up and returns [`AcquireStatus.Skipped`](AcquireStatus.md#m-skipped). A window the system never shows (or never releases images for) would otherwise block the frame loop forever. Default 2 seconds: far longer than a healthy frame, short enough that a stalled window system shows up as skipped frames.

**Type:** [`TimeSpan`](https://learn.microsoft.com/dotnet/api/system.timespan)

### <a id="m-format"></a>Format

```csharp
public abstract GpuFormat Format { get; }
```

Format of the backbuffers.

**Type:** [`GpuFormat`](GpuFormat.md)

### <a id="m-height"></a>Height

```csharp
public abstract uint Height { get; }
```

Current height in pixels.

**Type:** `uint`

### <a id="m-imagecount"></a>ImageCount

```csharp
public abstract int ImageCount { get; }
```

Number of backbuffers.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public abstract uint Width { get; }
```

Current width in pixels.

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`Present()`](#m-present) | Queues the acquired backbuffer for display. |
| [`Resize(uint, uint)`](#m-resize-uint-uint) | Tells the swapchain the window's new size in pixels. |
| [`TryAcquire(out GpuTexture)`](#m-tryacquire-gputexture) | Gets the next backbuffer to render into. |

### <a id="m-present"></a>Present()

```csharp
public abstract void Present();
```

Queues the acquired backbuffer for display. The commands that rendered into it must already be submitted.

### <a id="m-resize-uint-uint"></a>Resize(uint, uint)

```csharp
public abstract void Resize(uint width, uint height);
```

Tells the swapchain the window's new size in pixels. It is rebuilt at the next acquire.

**Parameters**

- `width` (`uint`)
- `height` (`uint`)

### <a id="m-tryacquire-gputexture"></a>TryAcquire(out GpuTexture)

```csharp
public abstract AcquireStatus TryAcquire(out GpuTexture backbuffer);
```

Gets the next backbuffer to render into. Rebuilds the swapchain first when the window changed. The texture is valid until [`GpuSwapchain.Present()`](GpuSwapchain.md#m-present), and the next command submission must render into it.

**Parameters**

- `backbuffer` ([`GpuTexture`](GpuTexture.md))

**Returns:** [`AcquireStatus`](AcquireStatus.md)
