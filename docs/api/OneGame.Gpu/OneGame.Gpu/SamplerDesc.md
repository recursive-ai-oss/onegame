[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# SamplerDesc struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SamplerDesc`](SamplerDesc.md)&gt;

```csharp
public readonly struct SamplerDesc : IEquatable<SamplerDesc>
```

Describes a sampler to create.

## Constructors

| Member | Description |
| --- | --- |
| [`SamplerDesc()`](#m-ctor) | Creates a sampler description with nearest filtering, clamped edges, no anisotropy and every mip level available. |

### <a id="m-ctor"></a>SamplerDesc()

```csharp
public SamplerDesc();
```

Creates a sampler description with nearest filtering, clamped edges, no anisotropy and every mip level available.

## Properties

| Member | Description |
| --- | --- |
| [`AddressU`](#m-addressu) | Addressing in U. |
| [`AddressV`](#m-addressv) | Addressing in V. |
| [`AddressW`](#m-addressw) | Addressing in W (3D and cube textures). |
| [`Compare`](#m-compare) | Makes this a comparison sampler for shadow lookups: `SampleCmp` returns how many of the filtered texels pass `reference <op> texel`. |
| [`Linear`](#m-linear) | Linear filtering with clamped edges. |
| [`MagFilter`](#m-magfilter) | Filter when magnifying. |
| [`MaxAnisotropy`](#m-maxanisotropy) | Maximum anisotropy; 1 disables it. |
| [`MaxLod`](#m-maxlod) | Highest mip level sampled. |
| [`MinFilter`](#m-minfilter) | Filter when minifying. |
| [`MinLod`](#m-minlod) | Lowest mip level sampled. |
| [`MipFilter`](#m-mipfilter) | Filter between mip levels. |
| [`Nearest`](#m-nearest) | Nearest filtering with clamped edges. |

### <a id="m-addressu"></a>AddressU

```csharp
public AddressMode AddressU { get; init; }
```

Addressing in U.

**Type:** [`AddressMode`](AddressMode.md)

### <a id="m-addressv"></a>AddressV

```csharp
public AddressMode AddressV { get; init; }
```

Addressing in V.

**Type:** [`AddressMode`](AddressMode.md)

### <a id="m-addressw"></a>AddressW

```csharp
public AddressMode AddressW { get; init; }
```

Addressing in W (3D and cube textures).

**Type:** [`AddressMode`](AddressMode.md)

### <a id="m-compare"></a>Compare

```csharp
public CompareFunction? Compare { get; init; }
```

Makes this a comparison sampler for shadow lookups: `SampleCmp` returns how many of the filtered texels pass `reference <op> texel`. Only depth textures can be sampled with it.

**Type:** [`CompareFunction`](CompareFunction.md)?

### <a id="m-linear"></a>Linear

```csharp
public static SamplerDesc Linear { get; }
```

Linear filtering with clamped edges.

**Type:** [`SamplerDesc`](SamplerDesc.md)

### <a id="m-magfilter"></a>MagFilter

```csharp
public FilterMode MagFilter { get; init; }
```

Filter when magnifying.

**Type:** [`FilterMode`](FilterMode.md)

### <a id="m-maxanisotropy"></a>MaxAnisotropy

```csharp
public float MaxAnisotropy { get; init; }
```

Maximum anisotropy; 1 disables it.

**Type:** `float`

### <a id="m-maxlod"></a>MaxLod

```csharp
public float MaxLod { get; init; }
```

Highest mip level sampled.

**Type:** `float`

### <a id="m-minfilter"></a>MinFilter

```csharp
public FilterMode MinFilter { get; init; }
```

Filter when minifying.

**Type:** [`FilterMode`](FilterMode.md)

### <a id="m-minlod"></a>MinLod

```csharp
public float MinLod { get; init; }
```

Lowest mip level sampled.

**Type:** `float`

### <a id="m-mipfilter"></a>MipFilter

```csharp
public FilterMode MipFilter { get; init; }
```

Filter between mip levels.

**Type:** [`FilterMode`](FilterMode.md)

### <a id="m-nearest"></a>Nearest

```csharp
public static SamplerDesc Nearest { get; }
```

Nearest filtering with clamped edges.

**Type:** [`SamplerDesc`](SamplerDesc.md)
