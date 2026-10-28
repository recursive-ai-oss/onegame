[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# RenderGraphReport record

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`RenderGraphReport`](RenderGraphReport.md)&gt;

```csharp
public sealed record RenderGraphReport : IEquatable<RenderGraphReport>
```

What [`RenderGraph.Describe()`](RenderGraph.md#m-describe) returns: the structure of the last frame.

## Constructors

| Member | Description |
| --- | --- |
| [`RenderGraphReport(int, bool, int, PassReport[], ResourceReport[], GraphStats)`](#m-ctor-int-bool-int-passreport-resourcereport-graphstats) | What [`RenderGraph.Describe()`](RenderGraph.md#m-describe) returns: the structure of the last frame. |

### <a id="m-ctor-int-bool-int-passreport-resourcereport-graphstats"></a>RenderGraphReport(int, bool, int, PassReport\[\], ResourceReport\[\], GraphStats)

```csharp
public RenderGraphReport(int Frame, bool Timing, int TimingFrame, PassReport[] Passes, ResourceReport[] Resources, GraphStats Stats);
```

What [`RenderGraph.Describe()`](RenderGraph.md#m-describe) returns: the structure of the last frame.

**Parameters**

- `Frame` (`int`): Frame number.
- `Timing` (`bool`): Whether GPU timing is on.
- `TimingFrame` (`int`): Frame the GPU times come from (0 when there are none); it can trail `Frame` by a frame or two.
- `Passes` ([`PassReport`](PassReport.md)\[\]): Every pass added, in declaration order, culled ones included.
- `Resources` ([`ResourceReport`](ResourceReport.md)\[\]): Every resource declared.
- `Stats` ([`GraphStats`](GraphStats.md)): Totals.

## Properties

| Member | Description |
| --- | --- |
| [`Frame`](#m-frame) | Frame number. |
| [`Passes`](#m-passes) | Every pass added, in declaration order, culled ones included. |
| [`Resources`](#m-resources) | Every resource declared. |
| [`Stats`](#m-stats) | Totals. |
| [`Timing`](#m-timing) | Whether GPU timing is on. |
| [`TimingFrame`](#m-timingframe) | Frame the GPU times come from (0 when there are none); it can trail `Frame` by a frame or two. |

### <a id="m-frame"></a>Frame

```csharp
public int Frame { get; init; }
```

Frame number.

**Type:** `int`

### <a id="m-passes"></a>Passes

```csharp
public PassReport[] Passes { get; init; }
```

Every pass added, in declaration order, culled ones included.

**Type:** [`PassReport`](PassReport.md)\[\]

### <a id="m-resources"></a>Resources

```csharp
public ResourceReport[] Resources { get; init; }
```

Every resource declared.

**Type:** [`ResourceReport`](ResourceReport.md)\[\]

### <a id="m-stats"></a>Stats

```csharp
public GraphStats Stats { get; init; }
```

Totals.

**Type:** [`GraphStats`](GraphStats.md)

### <a id="m-timing"></a>Timing

```csharp
public bool Timing { get; init; }
```

Whether GPU timing is on.

**Type:** `bool`

### <a id="m-timingframe"></a>TimingFrame

```csharp
public int TimingFrame { get; init; }
```

Frame the GPU times come from (0 when there are none); it can trail `Frame` by a frame or two.

**Type:** `int`
