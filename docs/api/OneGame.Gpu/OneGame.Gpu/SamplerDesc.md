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
| [`AddressU`](#m-addressu) | What a read outside 0..1 in U (horizontal) returns. |
| [`AddressV`](#m-addressv) | What a read outside 0..1 in V (vertical) returns. |
| [`AddressW`](#m-addressw) | Addressing in W (3D and cube textures). |
| [`Compare`](#m-compare) | Makes this a comparison sampler for shadow lookups: `SampleCmp` returns how many of the filtered texels pass `reference <op> texel`. |
| [`Linear`](#m-linear) | Linear filtering with clamped edges. |
| [`MagFilter`](#m-magfilter) | Filter when a texel covers more than one pixel (the texture is magnified). |
| [`MaxAnisotropy`](#m-maxanisotropy) | Maximum anisotropy; 1 disables it. |
| [`MaxLod`](#m-maxlod) | Highest (smallest) mip level the sampler may read. |
| [`MinFilter`](#m-minfilter) | Filter when a pixel covers more than one texel (the texture is minified). |
| [`MinLod`](#m-minlod) | Lowest (largest) mip level the sampler may read; 0 by default. |
| [`MipFilter`](#m-mipfilter) | How two mip levels are blended; `Linear` is trilinear filtering, `Nearest` picks the closer level. |
| [`Nearest`](#m-nearest) | Nearest filtering with clamped edges. |

### <a id="m-addressu"></a>AddressU

```csharp
public AddressMode AddressU { get; init; }
```

What a read outside 0..1 in U (horizontal) returns.

**Type:** [`AddressMode`](AddressMode.md)

### <a id="m-addressv"></a>AddressV

```csharp
public AddressMode AddressV { get; init; }
```

What a read outside 0..1 in V (vertical) returns.

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

Filter when a texel covers more than one pixel (the texture is magnified). `Nearest` gives hard pixels, `Linear` smooths them.

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

Highest (smallest) mip level the sampler may read. The default allows every level the texture has.

**Type:** `float`

### <a id="m-minfilter"></a>MinFilter

```csharp
public FilterMode MinFilter { get; init; }
```

Filter when a pixel covers more than one texel (the texture is minified).

**Type:** [`FilterMode`](FilterMode.md)

### <a id="m-minlod"></a>MinLod

```csharp
public float MinLod { get; init; }
```

Lowest (largest) mip level the sampler may read; 0 by default.

**Type:** `float`

### <a id="m-mipfilter"></a>MipFilter

```csharp
public FilterMode MipFilter { get; init; }
```

How two mip levels are blended; `Linear` is trilinear filtering, `Nearest` picks the closer level.

**Type:** [`FilterMode`](FilterMode.md)

### <a id="m-nearest"></a>Nearest

```csharp
public static SamplerDesc Nearest { get; }
```

Nearest filtering with clamped edges.

**Type:** [`SamplerDesc`](SamplerDesc.md)
