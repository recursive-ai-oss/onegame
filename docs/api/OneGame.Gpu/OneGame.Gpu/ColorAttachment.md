[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# ColorAttachment struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ColorAttachment`](ColorAttachment.md)&gt;

```csharp
public readonly struct ColorAttachment : IEquatable<ColorAttachment>
```

A color attachment of a render pass.

## Properties

| Member | Description |
| --- | --- |
| [`ClearA`](#m-cleara) | Clear alpha. |
| [`ClearB`](#m-clearb) | Clear blue. |
| [`ClearG`](#m-clearg) | Clear green. |
| [`ClearR`](#m-clearr) | Clear red. |
| [`Layer`](#m-layer) | Array layer rendered into. |
| [`Load`](#m-load) | What happens at the start of the pass. |
| [`MipLevel`](#m-miplevel) | Mip level rendered into. |
| [`Resolve`](#m-resolve) | Single-sample texture that receives the resolved image when [`ColorAttachment.Target`](ColorAttachment.md#m-target) is multisampled. |
| [`Store`](#m-store) | What happens at the end of the pass. |
| [`Target`](#m-target) | Texture rendered into; must have [`TextureUsage.RenderTarget`](TextureUsage.md#m-rendertarget). |

### <a id="m-cleara"></a>ClearA

```csharp
public float ClearA { get; init; }
```

Clear alpha.

**Type:** `float`

### <a id="m-clearb"></a>ClearB

```csharp
public float ClearB { get; init; }
```

Clear blue.

**Type:** `float`

### <a id="m-clearg"></a>ClearG

```csharp
public float ClearG { get; init; }
```

Clear green.

**Type:** `float`

### <a id="m-clearr"></a>ClearR

```csharp
public float ClearR { get; init; }
```

Clear red.

**Type:** `float`

### <a id="m-layer"></a>Layer

```csharp
public uint Layer { get; init; }
```

Array layer rendered into.

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

### <a id="m-resolve"></a>Resolve

```csharp
public GpuTexture? Resolve { get; init; }
```

Single-sample texture that receives the resolved image when [`ColorAttachment.Target`](ColorAttachment.md#m-target) is multisampled.

**Type:** [`GpuTexture`](GpuTexture.md)?

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

Texture rendered into; must have [`TextureUsage.RenderTarget`](TextureUsage.md#m-rendertarget).

**Type:** [`GpuTexture`](GpuTexture.md)

## Methods

| Member | Description |
| --- | --- |
| [`Clear(GpuTexture, float, float, float, float)`](#m-clear-gputexture-float-float-float-float) | A color attachment that clears to the given color and keeps the result. |

### <a id="m-clear-gputexture-float-float-float-float"></a>Clear(GpuTexture, float, float, float, float)

```csharp
public static ColorAttachment Clear(GpuTexture target, float r, float g, float b, float a);
```

A color attachment that clears to the given color and keeps the result.

**Parameters**

- `target` ([`GpuTexture`](GpuTexture.md))
- `r` (`float`)
- `g` (`float`)
- `b` (`float`)
- `a` (`float`)

**Returns:** [`ColorAttachment`](ColorAttachment.md)
