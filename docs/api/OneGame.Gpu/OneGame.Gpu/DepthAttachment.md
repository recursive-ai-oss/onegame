[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# DepthAttachment struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DepthAttachment`](DepthAttachment.md)&gt;

```csharp
public readonly struct DepthAttachment : IEquatable<DepthAttachment>
```

The depth attachment of a render pass.

## Properties

| Member | Description |
| --- | --- |
| [`ClearDepth`](#m-cleardepth) | Depth value to clear to. |
| [`Layer`](#m-layer) | Array layer rendered into (a cascade of a shadow map array, say). |
| [`Load`](#m-load) | What happens at the start of the pass. |
| [`MipLevel`](#m-miplevel) | Mip level rendered into. |
| [`Store`](#m-store) | What happens at the end of the pass. |
| [`Target`](#m-target) | Depth texture; must have [`TextureUsage.RenderTarget`](TextureUsage.md#m-rendertarget). |

### <a id="m-cleardepth"></a>ClearDepth

```csharp
public float ClearDepth { get; init; }
```

Depth value to clear to.

**Type:** `float`

### <a id="m-layer"></a>Layer

```csharp
public uint Layer { get; init; }
```

Array layer rendered into (a cascade of a shadow map array, say).

**Type:** `uint`

### <a id="m-load"></a>Load

```csharp
public LoadOp Load { get; init; }
```

What happens at the start of the pass.

**Type:** [`LoadOp`](LoadOp.md)

### <a id="m-miplevel"></a>MipLevel

```csharp
public uint MipLevel { get; init; }
```

Mip level rendered into.

**Type:** `uint`

### <a id="m-store"></a>Store

```csharp
public StoreOp Store { get; init; }
```

What happens at the end of the pass.

**Type:** [`StoreOp`](StoreOp.md)

### <a id="m-target"></a>Target

```csharp
public GpuTexture Target { get; init; }
```

Depth texture; must have [`TextureUsage.RenderTarget`](TextureUsage.md#m-rendertarget).

**Type:** [`GpuTexture`](GpuTexture.md)
