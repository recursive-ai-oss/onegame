[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# DepthStencilState struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DepthStencilState`](DepthStencilState.md)&gt;

```csharp
public readonly struct DepthStencilState : IEquatable<DepthStencilState>
```

Depth testing of a graphics pipeline.

## Constructors

| Member | Description |
| --- | --- |
| [`DepthStencilState(GpuFormat)`](#m-ctor-gpuformat) | Depth testing of a graphics pipeline. |

### <a id="m-ctor-gpuformat"></a>DepthStencilState(GpuFormat)

```csharp
public DepthStencilState(GpuFormat Format);
```

Depth testing of a graphics pipeline.

**Parameters**

- `Format` ([`GpuFormat`](GpuFormat.md)): Depth attachment format.

## Properties

| Member | Description |
| --- | --- |
| [`DepthBias`](#m-depthbias) | Constant depth offset in units of the smallest depth step, added to every fragment (shadow maps use it against acne). |
| [`DepthBiasClamp`](#m-depthbiasclamp) | Largest absolute offset applied; 0 means no limit. |
| [`DepthBiasSlopeScale`](#m-depthbiasslopescale) | Depth offset that grows with the polygon's slope. |
| [`DepthCompare`](#m-depthcompare) | Depth comparison. |
| [`DepthWrite`](#m-depthwrite) | Whether passing fragments write depth. |
| [`Format`](#m-format) | Depth attachment format. |

### <a id="m-depthbias"></a>DepthBias

```csharp
public int DepthBias { get; init; }
```

Constant depth offset in units of the smallest depth step, added to every fragment (shadow maps use it against acne).

**Type:** `int`

### <a id="m-depthbiasclamp"></a>DepthBiasClamp

```csharp
public float DepthBiasClamp { get; init; }
```

Largest absolute offset applied; 0 means no limit. Needs [`GpuCapabilities.DepthBiasClamp`](GpuCapabilities.md#m-depthbiasclamp) when non-zero.

**Type:** `float`

### <a id="m-depthbiasslopescale"></a>DepthBiasSlopeScale

```csharp
public float DepthBiasSlopeScale { get; init; }
```

Depth offset that grows with the polygon's slope.

**Type:** `float`

### <a id="m-depthcompare"></a>DepthCompare

```csharp
public CompareFunction DepthCompare { get; init; }
```

Depth comparison.

**Type:** [`CompareFunction`](CompareFunction.md)

### <a id="m-depthwrite"></a>DepthWrite

```csharp
public bool DepthWrite { get; init; }
```

Whether passing fragments write depth.

**Type:** `bool`

### <a id="m-format"></a>Format

```csharp
public GpuFormat Format { get; init; }
```

Depth attachment format.

**Type:** [`GpuFormat`](GpuFormat.md)
