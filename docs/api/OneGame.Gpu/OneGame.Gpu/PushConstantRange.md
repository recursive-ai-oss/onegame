[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# PushConstantRange struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PushConstantRange`](PushConstantRange.md)&gt;

```csharp
public readonly struct PushConstantRange : IEquatable<PushConstantRange>
```

Small constant data written straight into the command stream with `SetPushConstants`.

## Constructors

| Member | Description |
| --- | --- |
| [`PushConstantRange(uint, ShaderStage)`](#m-ctor-uint-shaderstage) | Small constant data written straight into the command stream with `SetPushConstants`. |

### <a id="m-ctor-uint-shaderstage"></a>PushConstantRange(uint, ShaderStage)

```csharp
public PushConstantRange(uint Size, ShaderStage Stages);
```

Small constant data written straight into the command stream with `SetPushConstants`.

**Parameters**

- `Size` (`uint`): Bytes, a multiple of 4 and at most [`GpuDevice.MaxPushConstantBytes`](GpuDevice.md#m-maxpushconstantbytes).
- `Stages` ([`ShaderStage`](ShaderStage.md)): Stages that read it.

## Properties

| Member | Description |
| --- | --- |
| [`Size`](#m-size) | Bytes, a multiple of 4 and at most [`GpuDevice.MaxPushConstantBytes`](GpuDevice.md#m-maxpushconstantbytes). |
| [`Stages`](#m-stages) | Stages that read it. |

### <a id="m-size"></a>Size

```csharp
public uint Size { get; init; }
```

Bytes, a multiple of 4 and at most [`GpuDevice.MaxPushConstantBytes`](GpuDevice.md#m-maxpushconstantbytes).

**Type:** `uint`

### <a id="m-stages"></a>Stages

```csharp
public ShaderStage Stages { get; init; }
```

Stages that read it.

**Type:** [`ShaderStage`](ShaderStage.md)
