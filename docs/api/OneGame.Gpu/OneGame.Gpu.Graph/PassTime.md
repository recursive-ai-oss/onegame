[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# PassTime struct

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PassTime`](PassTime.md)&gt;

```csharp
public readonly struct PassTime : IEquatable<PassTime>
```

The GPU time of one pass, from [`RenderGraph.ReadPassTimes()`](RenderGraph.md#m-readpasstimes-list-passtime).

## Constructors

| Member | Description |
| --- | --- |
| [`PassTime(string, double)`](#m-ctor-string-double) | The GPU time of one pass, from [`RenderGraph.ReadPassTimes()`](RenderGraph.md#m-readpasstimes-list-passtime). |

### <a id="m-ctor-string-double"></a>PassTime(string, double)

```csharp
public PassTime(string Name, double Microseconds);
```

The GPU time of one pass, from [`RenderGraph.ReadPassTimes()`](RenderGraph.md#m-readpasstimes-list-passtime).

**Parameters**

- `Name` (`string`): Pass name.
- `Microseconds` (`double`): GPU time in microseconds.

## Properties

| Member | Description |
| --- | --- |
| [`Microseconds`](#m-microseconds) | GPU time in microseconds. |
| [`Name`](#m-name) | Pass name. |

### <a id="m-microseconds"></a>Microseconds

```csharp
public double Microseconds { get; init; }
```

GPU time in microseconds.

**Type:** `double`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

Pass name.

**Type:** `string`
