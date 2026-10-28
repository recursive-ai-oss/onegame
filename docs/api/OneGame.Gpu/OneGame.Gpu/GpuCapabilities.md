[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuCapabilities struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GpuCapabilities`](GpuCapabilities.md)&gt;

```csharp
public readonly struct GpuCapabilities : IEquatable<GpuCapabilities>
```

Optional features and limits of a device. Code that needs more than the portable baseline checks these.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuCapabilities(bool, bool, bool, bool, bool, bool, uint, uint, uint, uint, uint)`](#m-ctor-bool-bool-bool-bool-bool-bool-uint-uint-uint-uint-uint) | Optional features and limits of a device. |

### <a id="m-ctor-bool-bool-bool-bool-bool-bool-uint-uint-uint-uint-uint"></a>GpuCapabilities(bool, bool, bool, bool, bool, bool, uint, uint, uint, uint, uint)

```csharp
public GpuCapabilities(bool Timestamps, bool MultiDrawIndirect, bool IndirectFirstInstance, bool DepthClamp, bool DepthBiasClamp, bool NonUniformArrayIndexing, uint MaxSampleCount, uint MaxTextureSize, uint MaxTextureSize3D, uint MaxArrayLayers, uint MaxResourceArrayCount);
```

Optional features and limits of a device. Code that needs more than the portable baseline checks these.

**Parameters**

- `Timestamps` (`bool`): GPU timestamp queries work ([`GpuDevice.CreateTimestampSet()`](GpuDevice.md#m-createtimestampset-uint)).
- `MultiDrawIndirect` (`bool`): An indirect draw with a count above 1 is one GPU command; otherwise the backend issues them one by one with the same result.
- `IndirectFirstInstance` (`bool`): Indirect draws may use a non-zero first instance.
- `DepthClamp` (`bool`): Pipelines may set [`GraphicsPipelineDesc.DepthClamp`](GraphicsPipelineDesc.md#m-depthclamp).
- `DepthBiasClamp` (`bool`): Pipelines may set [`DepthStencilState.DepthBiasClamp`](DepthStencilState.md#m-depthbiasclamp).
- `NonUniformArrayIndexing` (`bool`): Shaders may index resource arrays with values that differ between invocations.
- `MaxSampleCount` (`uint`): Largest MSAA sample count for color and depth targets (1, 2, 4 or 8).
- `MaxTextureSize` (`uint`): Largest 2D texture width or height.
- `MaxTextureSize3D` (`uint`): Largest 3D texture size in any dimension.
- `MaxArrayLayers` (`uint`): Most layers in a texture.
- `MaxResourceArrayCount` (`uint`): Most elements in a resource array binding.

## Properties

| Member | Description |
| --- | --- |
| [`DepthBiasClamp`](#m-depthbiasclamp) | Pipelines may set [`DepthStencilState.DepthBiasClamp`](DepthStencilState.md#m-depthbiasclamp). |
| [`DepthClamp`](#m-depthclamp) | Pipelines may set [`GraphicsPipelineDesc.DepthClamp`](GraphicsPipelineDesc.md#m-depthclamp). |
| [`IndirectFirstInstance`](#m-indirectfirstinstance) | Indirect draws may use a non-zero first instance. |
| [`MaxArrayLayers`](#m-maxarraylayers) | Most layers in a texture. |
| [`MaxResourceArrayCount`](#m-maxresourcearraycount) | Most elements in a resource array binding. |
| [`MaxSampleCount`](#m-maxsamplecount) | Largest MSAA sample count for color and depth targets (1, 2, 4 or 8). |
| [`MaxTextureSize`](#m-maxtexturesize) | Largest 2D texture width or height. |
| [`MaxTextureSize3D`](#m-maxtexturesize3d) | Largest 3D texture size in any dimension. |
| [`MultiDrawIndirect`](#m-multidrawindirect) | An indirect draw with a count above 1 is one GPU command; otherwise the backend issues them one by one with the same result. |
| [`NonUniformArrayIndexing`](#m-nonuniformarrayindexing) | Shaders may index resource arrays with values that differ between invocations. |
| [`PushConstants`](#m-pushconstants) | Whether `SetPushConstants` works. |
| [`Timestamps`](#m-timestamps) | GPU timestamp queries work ([`GpuDevice.CreateTimestampSet()`](GpuDevice.md#m-createtimestampset-uint)). |

### <a id="m-depthbiasclamp"></a>DepthBiasClamp

```csharp
public bool DepthBiasClamp { get; init; }
```

Pipelines may set [`DepthStencilState.DepthBiasClamp`](DepthStencilState.md#m-depthbiasclamp).

**Type:** `bool`

### <a id="m-depthclamp"></a>DepthClamp

```csharp
public bool DepthClamp { get; init; }
```

Pipelines may set [`GraphicsPipelineDesc.DepthClamp`](GraphicsPipelineDesc.md#m-depthclamp).

**Type:** `bool`

### <a id="m-indirectfirstinstance"></a>IndirectFirstInstance

```csharp
public bool IndirectFirstInstance { get; init; }
```

Indirect draws may use a non-zero first instance.

**Type:** `bool`

### <a id="m-maxarraylayers"></a>MaxArrayLayers

```csharp
public uint MaxArrayLayers { get; init; }
```

Most layers in a texture.

**Type:** `uint`

### <a id="m-maxresourcearraycount"></a>MaxResourceArrayCount

```csharp
public uint MaxResourceArrayCount { get; init; }
```

Most elements in a resource array binding.

**Type:** `uint`

### <a id="m-maxsamplecount"></a>MaxSampleCount

```csharp
public uint MaxSampleCount { get; init; }
```

Largest MSAA sample count for color and depth targets (1, 2, 4 or 8).

**Type:** `uint`

### <a id="m-maxtexturesize"></a>MaxTextureSize

```csharp
public uint MaxTextureSize { get; init; }
```

Largest 2D texture width or height.

**Type:** `uint`

### <a id="m-maxtexturesize3d"></a>MaxTextureSize3D

```csharp
public uint MaxTextureSize3D { get; init; }
```

Largest 3D texture size in any dimension.

**Type:** `uint`

### <a id="m-multidrawindirect"></a>MultiDrawIndirect

```csharp
public bool MultiDrawIndirect { get; init; }
```

An indirect draw with a count above 1 is one GPU command; otherwise the backend issues them one by one with the same result.

**Type:** `bool`

### <a id="m-nonuniformarrayindexing"></a>NonUniformArrayIndexing

```csharp
public bool NonUniformArrayIndexing { get; init; }
```

Shaders may index resource arrays with values that differ between invocations.

**Type:** `bool`

### <a id="m-pushconstants"></a>PushConstants

```csharp
public bool PushConstants { get; init; }
```

Whether `SetPushConstants` works. False on a backend whose shader compiler cannot yet produce push-constant blocks.

**Type:** `bool`

### <a id="m-timestamps"></a>Timestamps

```csharp
public bool Timestamps { get; init; }
```

GPU timestamp queries work ([`GpuDevice.CreateTimestampSet()`](GpuDevice.md#m-createtimestampset-uint)).

**Type:** `bool`
