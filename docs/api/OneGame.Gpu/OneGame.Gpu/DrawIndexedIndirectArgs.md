[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# DrawIndexedIndirectArgs struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DrawIndexedIndirectArgs`](DrawIndexedIndirectArgs.md)&gt;

```csharp
public readonly struct DrawIndexedIndirectArgs : IEquatable<DrawIndexedIndirectArgs>
```

Arguments of one indirect indexed draw, as stored in a buffer. Same layout on every backend.

## Constructors

| Member | Description |
| --- | --- |
| [`DrawIndexedIndirectArgs(uint, uint, uint, int, uint)`](#m-ctor-uint-uint-uint-int-uint) | Arguments of one indirect indexed draw, as stored in a buffer. |

### <a id="m-ctor-uint-uint-uint-int-uint"></a>DrawIndexedIndirectArgs(uint, uint, uint, int, uint)

```csharp
public DrawIndexedIndirectArgs(uint IndexCount, uint InstanceCount, uint FirstIndex, int BaseVertex, uint FirstInstance);
```

Arguments of one indirect indexed draw, as stored in a buffer. Same layout on every backend.

**Parameters**

- `IndexCount` (`uint`): Indices per instance.
- `InstanceCount` (`uint`): Instances.
- `FirstIndex` (`uint`): First index.
- `BaseVertex` (`int`): Value added to every index.
- `FirstInstance` (`uint`): First instance; must be 0 unless [`GpuCapabilities.IndirectFirstInstance`](GpuCapabilities.md#m-indirectfirstinstance).

## Fields

| Member | Description |
| --- | --- |
| [`Size`](#m-size) | Size in bytes. |

### <a id="m-size"></a>Size

```csharp
public const int Size = 20;
```

Size in bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`BaseVertex`](#m-basevertex) | Value added to every index. |
| [`FirstIndex`](#m-firstindex) | First index. |
| [`FirstInstance`](#m-firstinstance) | First instance; must be 0 unless [`GpuCapabilities.IndirectFirstInstance`](GpuCapabilities.md#m-indirectfirstinstance). |
| [`IndexCount`](#m-indexcount) | Indices per instance. |
| [`InstanceCount`](#m-instancecount) | Instances. |

### <a id="m-basevertex"></a>BaseVertex

```csharp
public int BaseVertex { get; init; }
```

Value added to every index.

**Type:** `int`

### <a id="m-firstindex"></a>FirstIndex

```csharp
public uint FirstIndex { get; init; }
```

First index.

**Type:** `uint`

### <a id="m-firstinstance"></a>FirstInstance

```csharp
public uint FirstInstance { get; init; }
```

First instance; must be 0 unless [`GpuCapabilities.IndirectFirstInstance`](GpuCapabilities.md#m-indirectfirstinstance).

**Type:** `uint`

### <a id="m-indexcount"></a>IndexCount

```csharp
public uint IndexCount { get; init; }
```

Indices per instance.

**Type:** `uint`

### <a id="m-instancecount"></a>InstanceCount

```csharp
public uint InstanceCount { get; init; }
```

Instances.

**Type:** `uint`
