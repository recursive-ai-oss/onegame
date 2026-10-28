[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# PassTiming struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PassTiming`](PassTiming.md)&gt;

```csharp
public readonly struct PassTiming : IEquatable<PassTiming>
```

Where a group of passes writes its GPU start and end times: pass `k` of the group uses queries `First + 2k` and `First + 2k + 1`.

## Constructors

| Member | Description |
| --- | --- |
| [`PassTiming(GpuTimestampSet, uint, uint)`](#m-ctor-gputimestampset-uint-uint) | Where a group of passes writes its GPU start and end times: pass `k` of the group uses queries `First + 2k` and `First + 2k + 1`. |

### <a id="m-ctor-gputimestampset-uint-uint"></a>PassTiming(GpuTimestampSet, uint, uint)

```csharp
public PassTiming(GpuTimestampSet Set, uint First, uint MaxPasses);
```

Where a group of passes writes its GPU start and end times: pass `k` of the group uses queries `First + 2k` and `First + 2k + 1`.

**Parameters**

- `Set` ([`GpuTimestampSet`](GpuTimestampSet.md)): The query set.
- `First` (`uint`): Index of the first query used.
- `MaxPasses` (`uint`): Number of passes that get timed; later ones are not.

## Properties

| Member | Description |
| --- | --- |
| [`First`](#m-first) | Index of the first query used. |
| [`MaxPasses`](#m-maxpasses) | Number of passes that get timed; later ones are not. |
| [`Set`](#m-set) | The query set. |

### <a id="m-first"></a>First

```csharp
public uint First { get; init; }
```

Index of the first query used.

**Type:** `uint`

### <a id="m-maxpasses"></a>MaxPasses

```csharp
public uint MaxPasses { get; init; }
```

Number of passes that get timed; later ones are not.

**Type:** `uint`

### <a id="m-set"></a>Set

```csharp
public GpuTimestampSet Set { get; init; }
```

The query set.

**Type:** [`GpuTimestampSet`](GpuTimestampSet.md)
