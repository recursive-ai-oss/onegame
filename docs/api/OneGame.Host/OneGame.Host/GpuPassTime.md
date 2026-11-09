[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GpuPassTime struct

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GpuPassTime`](GpuPassTime.md)&gt;

```csharp
public readonly struct GpuPassTime : IEquatable<GpuPassTime>
```

One pass of the render graph and how long the GPU spent on it.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuPassTime(string, double)`](#m-ctor-string-double) | One pass of the render graph and how long the GPU spent on it. |

### <a id="m-ctor-string-double"></a>GpuPassTime(string, double)

```csharp
public GpuPassTime(string Name, double Microseconds);
```

One pass of the render graph and how long the GPU spent on it.

**Parameters**

- `Name` (`string`): The pass name, as `render_graph` shows it.
- `Microseconds` (`double`): GPU time in microseconds, from the newest finished frame.

## Properties

| Member | Description |
| --- | --- |
| [`Microseconds`](#m-microseconds) | GPU time in microseconds, from the newest finished frame. |
| [`Name`](#m-name) | The pass name, as `render_graph` shows it. |

### <a id="m-microseconds"></a>Microseconds

```csharp
public double Microseconds { get; init; }
```

GPU time in microseconds, from the newest finished frame.

**Type:** `double`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The pass name, as `render_graph` shows it.

**Type:** `string`
