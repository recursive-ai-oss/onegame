[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# SwapchainDesc struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SwapchainDesc`](SwapchainDesc.md)&gt;

```csharp
public readonly struct SwapchainDesc : IEquatable<SwapchainDesc>
```

Describes a swapchain to create.

## Constructors

| Member | Description |
| --- | --- |
| [`SwapchainDesc(uint, uint)`](#m-ctor-uint-uint) | Describes a swapchain to create. |

### <a id="m-ctor-uint-uint"></a>SwapchainDesc(uint, uint)

```csharp
public SwapchainDesc(uint Width, uint Height);
```

Describes a swapchain to create.

**Parameters**

- `Width` (`uint`): Initial width in pixels; used when the window system does not report one.
- `Height` (`uint`): Initial height in pixels.

## Properties

| Member | Description |
| --- | --- |
| [`ExtraUsage`](#m-extrausage) | Extra usage flags for the backbuffers besides [`TextureUsage.RenderTarget`](TextureUsage.md#m-rendertarget) and [`TextureUsage.CopySource`](TextureUsage.md#m-copysource). |
| [`Height`](#m-height) | Initial height in pixels. |
| [`PresentMode`](#m-presentmode) | Presentation pacing. |
| [`Srgb`](#m-srgb) | Whether images are sRGB-encoded (BGRA8 sRGB) or linear (BGRA8). |
| [`Width`](#m-width) | Initial width in pixels; used when the window system does not report one. |

### <a id="m-extrausage"></a>ExtraUsage

```csharp
public TextureUsage ExtraUsage { get; init; }
```

Extra usage flags for the backbuffers besides [`TextureUsage.RenderTarget`](TextureUsage.md#m-rendertarget) and [`TextureUsage.CopySource`](TextureUsage.md#m-copysource).

**Type:** [`TextureUsage`](TextureUsage.md)

### <a id="m-height"></a>Height

```csharp
public uint Height { get; init; }
```

Initial height in pixels.

**Type:** `uint`

### <a id="m-presentmode"></a>PresentMode

```csharp
public PresentMode PresentMode { get; init; }
```

Presentation pacing.

**Type:** [`PresentMode`](PresentMode.md)

### <a id="m-srgb"></a>Srgb

```csharp
public bool Srgb { get; init; }
```

Whether images are sRGB-encoded (BGRA8 sRGB) or linear (BGRA8). The actual format is in [`GpuSwapchain.Format`](GpuSwapchain.md#m-format).

**Type:** `bool`

### <a id="m-width"></a>Width

```csharp
public uint Width { get; init; }
```

Initial width in pixels; used when the window system does not report one.

**Type:** `uint`
