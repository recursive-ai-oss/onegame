[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# GpuPassReport record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GpuPassReport`](GpuPassReport.md)&gt;

```csharp
public sealed record GpuPassReport : IEquatable<GpuPassReport>
```

One pass of the render graph and its GPU time.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuPassReport(string, double)`](#m-ctor-string-double) | One pass of the render graph and its GPU time. |

### <a id="m-ctor-string-double"></a>GpuPassReport(string, double)

```csharp
public GpuPassReport(string Name, double Microseconds);
```

One pass of the render graph and its GPU time.

**Parameters**

- `Name` (`string`): Pass name.
- `Microseconds` (`double`): GPU time in microseconds, from the newest finished frame.

## Properties

| Member | Description |
| --- | --- |
| [`Microseconds`](#m-microseconds) | GPU time in microseconds, from the newest finished frame. |
| [`Name`](#m-name) | Pass name. |

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

Pass name.

**Type:** `string`
