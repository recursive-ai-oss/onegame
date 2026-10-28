[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# DrawIndirectArgs struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DrawIndirectArgs`](DrawIndirectArgs.md)&gt;

```csharp
public readonly struct DrawIndirectArgs : IEquatable<DrawIndirectArgs>
```

Arguments of one indirect non-indexed draw, as stored in a buffer. Same layout on every backend.

## Constructors

| Member | Description |
| --- | --- |
| [`DrawIndirectArgs(uint, uint, uint, uint)`](#m-ctor-uint-uint-uint-uint) | Arguments of one indirect non-indexed draw, as stored in a buffer. |

### <a id="m-ctor-uint-uint-uint-uint"></a>DrawIndirectArgs(uint, uint, uint, uint)

```csharp
public DrawIndirectArgs(uint VertexCount, uint InstanceCount, uint FirstVertex, uint FirstInstance);
```

Arguments of one indirect non-indexed draw, as stored in a buffer. Same layout on every backend.

**Parameters**

- `VertexCount` (`uint`): Vertices per instance.
- `InstanceCount` (`uint`): Instances.
- `FirstVertex` (`uint`): First vertex.
- `FirstInstance` (`uint`): First instance; must be 0 unless [`GpuCapabilities.IndirectFirstInstance`](GpuCapabilities.md#m-indirectfirstinstance).

## Fields

| Member | Description |
| --- | --- |
| [`Size`](#m-size) | Size in bytes. |

### <a id="m-size"></a>Size

```csharp
public const int Size = 16;
```

Size in bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`FirstInstance`](#m-firstinstance) | First instance; must be 0 unless [`GpuCapabilities.IndirectFirstInstance`](GpuCapabilities.md#m-indirectfirstinstance). |
| [`FirstVertex`](#m-firstvertex) | First vertex. |
| [`InstanceCount`](#m-instancecount) | Instances. |
| [`VertexCount`](#m-vertexcount) | Vertices per instance. |

### <a id="m-firstinstance"></a>FirstInstance

```csharp
public uint FirstInstance { get; init; }
```

First instance; must be 0 unless [`GpuCapabilities.IndirectFirstInstance`](GpuCapabilities.md#m-indirectfirstinstance).

**Type:** `uint`

### <a id="m-firstvertex"></a>FirstVertex

```csharp
public uint FirstVertex { get; init; }
```

First vertex.

**Type:** `uint`

### <a id="m-instancecount"></a>InstanceCount

```csharp
public uint InstanceCount { get; init; }
```

Instances.

**Type:** `uint`

### <a id="m-vertexcount"></a>VertexCount

```csharp
public uint VertexCount { get; init; }
```

Vertices per instance.

**Type:** `uint`
