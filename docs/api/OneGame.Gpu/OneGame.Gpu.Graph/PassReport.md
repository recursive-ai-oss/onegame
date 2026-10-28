[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# PassReport record

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PassReport`](PassReport.md)&gt;

```csharp
public sealed record PassReport : IEquatable<PassReport>
```

One pass of the last frame.

## Constructors

| Member | Description |
| --- | --- |
| [`PassReport(string, string, bool, int, AccessReport[], AccessReport[], double?)`](#m-ctor-string-string-bool-int-accessreport-accessreport-double) | One pass of the last frame. |

### <a id="m-ctor-string-string-bool-int-accessreport-accessreport-double"></a>PassReport(string, string, bool, int, AccessReport\[\], AccessReport\[\], double?)

```csharp
public PassReport(string Name, string Type, bool Culled, int Order, AccessReport[] Reads, AccessReport[] Writes, double? GpuMicroseconds);
```

One pass of the last frame.

**Parameters**

- `Name` (`string`): Pass name.
- `Type` (`string`): `raster`, `compute` or `custom`.
- `Culled` (`bool`): True if nothing used its results, so it was not recorded.
- `Order` (`int`): Position among the passes that ran, or -1 when culled.
- `Reads` ([`AccessReport`](AccessReport.md)\[\]): What it reads (including what it loads and keeps).
- `Writes` ([`AccessReport`](AccessReport.md)\[\]): What it writes.
- `GpuMicroseconds` (`double?`): GPU time in microseconds from the newest finished frame that ran this pass; null when unknown (culled, timing off, or not finished yet).

## Properties

| Member | Description |
| --- | --- |
| [`Culled`](#m-culled) | True if nothing used its results, so it was not recorded. |
| [`GpuMicroseconds`](#m-gpumicroseconds) | GPU time in microseconds from the newest finished frame that ran this pass; null when unknown (culled, timing off, or not finished yet). |
| [`Name`](#m-name) | Pass name. |
| [`Order`](#m-order) | Position among the passes that ran, or -1 when culled. |
| [`Reads`](#m-reads) | What it reads (including what it loads and keeps). |
| [`Type`](#m-type) | `raster`, `compute` or `custom`. |
| [`Writes`](#m-writes) | What it writes. |

### <a id="m-culled"></a>Culled

```csharp
public bool Culled { get; init; }
```

True if nothing used its results, so it was not recorded.

**Type:** `bool`

### <a id="m-gpumicroseconds"></a>GpuMicroseconds

```csharp
public double? GpuMicroseconds { get; init; }
```

GPU time in microseconds from the newest finished frame that ran this pass; null when unknown (culled, timing off, or not finished yet).

**Type:** `double?`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

Pass name.

**Type:** `string`

### <a id="m-order"></a>Order

```csharp
public int Order { get; init; }
```

Position among the passes that ran, or -1 when culled.

**Type:** `int`

### <a id="m-reads"></a>Reads

```csharp
public AccessReport[] Reads { get; init; }
```

What it reads (including what it loads and keeps).

**Type:** [`AccessReport`](AccessReport.md)\[\]

### <a id="m-type"></a>Type

```csharp
public string Type { get; init; }
```

`raster`, `compute` or `custom`.

**Type:** `string`

### <a id="m-writes"></a>Writes

```csharp
public AccessReport[] Writes { get; init; }
```

What it writes.

**Type:** [`AccessReport`](AccessReport.md)\[\]
