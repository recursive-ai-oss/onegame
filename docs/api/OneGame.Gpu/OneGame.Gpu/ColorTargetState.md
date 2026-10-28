[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# ColorTargetState struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ColorTargetState`](ColorTargetState.md)&gt;

```csharp
public readonly struct ColorTargetState : IEquatable<ColorTargetState>
```

A color target of a graphics pipeline.

## Constructors

| Member | Description |
| --- | --- |
| [`ColorTargetState(GpuFormat)`](#m-ctor-gpuformat) | A color target of a graphics pipeline. |

### <a id="m-ctor-gpuformat"></a>ColorTargetState(GpuFormat)

```csharp
public ColorTargetState(GpuFormat Format);
```

A color target of a graphics pipeline.

**Parameters**

- `Format` ([`GpuFormat`](GpuFormat.md)): Format of the attachment this pipeline renders to.

## Properties

| Member | Description |
| --- | --- |
| [`Blend`](#m-blend) | Blending, or null to overwrite. |
| [`Format`](#m-format) | Format of the attachment this pipeline renders to. |
| [`WriteMask`](#m-writemask) | Channels written. |

### <a id="m-blend"></a>Blend

```csharp
public BlendState? Blend { get; init; }
```

Blending, or null to overwrite.

**Type:** [`BlendState`](BlendState.md)?

### <a id="m-format"></a>Format

```csharp
public GpuFormat Format { get; init; }
```

Format of the attachment this pipeline renders to.

**Type:** [`GpuFormat`](GpuFormat.md)

### <a id="m-writemask"></a>WriteMask

```csharp
public ColorWriteMask WriteMask { get; init; }
```

Channels written.

**Type:** [`ColorWriteMask`](ColorWriteMask.md)
