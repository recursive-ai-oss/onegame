[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuMemoryStats struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GpuMemoryStats`](GpuMemoryStats.md)&gt;

```csharp
public readonly struct GpuMemoryStats : IEquatable<GpuMemoryStats>
```

A backend's tally of device memory, from [`GpuDevice.MemoryStats`](GpuDevice.md#m-memorystats).

## Constructors

| Member | Description |
| --- | --- |
| [`GpuMemoryStats(ulong, ulong, int)`](#m-ctor-ulong-ulong-int) | A backend's tally of device memory, from [`GpuDevice.MemoryStats`](GpuDevice.md#m-memorystats). |

### <a id="m-ctor-ulong-ulong-int"></a>GpuMemoryStats(ulong, ulong, int)

```csharp
public GpuMemoryStats(ulong ReservedBytes, ulong UsedBytes, int Allocations);
```

A backend's tally of device memory, from [`GpuDevice.MemoryStats`](GpuDevice.md#m-memorystats).

**Parameters**

- `ReservedBytes` (`ulong`): Bytes held from the driver (blocks that resources are carved from).
- `UsedBytes` (`ulong`): Bytes occupied by live resources, with alignment padding.
- `Allocations` (`int`): Number of live resource allocations.

## Properties

| Member | Description |
| --- | --- |
| [`Allocations`](#m-allocations) | Number of live resource allocations. |
| [`ReservedBytes`](#m-reservedbytes) | Bytes held from the driver (blocks that resources are carved from). |
| [`UsedBytes`](#m-usedbytes) | Bytes occupied by live resources, with alignment padding. |

### <a id="m-allocations"></a>Allocations

```csharp
public int Allocations { get; init; }
```

Number of live resource allocations.

**Type:** `int`

### <a id="m-reservedbytes"></a>ReservedBytes

```csharp
public ulong ReservedBytes { get; init; }
```

Bytes held from the driver (blocks that resources are carved from).

**Type:** `ulong`

### <a id="m-usedbytes"></a>UsedBytes

```csharp
public ulong UsedBytes { get; init; }
```

Bytes occupied by live resources, with alignment padding.

**Type:** `ulong`
