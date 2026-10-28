[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# FrameStats3D struct

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FrameStats3D`](FrameStats3D.md)&gt;

```csharp
public readonly struct FrameStats3D : IEquatable<FrameStats3D>
```

What one frame of 3D drew.

## Constructors

| Member | Description |
| --- | --- |
| [`FrameStats3D(int, int, long, int, int)`](#m-ctor-int-int-long-int-int) | What one frame of 3D drew. |

### <a id="m-ctor-int-int-long-int-int"></a>FrameStats3D(int, int, long, int, int)

```csharp
public FrameStats3D(int Objects, int DrawCalls, long Triangles, int Lights, int Clusters);
```

What one frame of 3D drew.

**Parameters**

- `Objects` (`int`): Mesh nodes drawn.
- `DrawCalls` (`int`): Indexed draws recorded (sky and post excluded).
- `Triangles` (`long`): Triangles submitted.
- `Lights` (`int`): Punctual lights used.
- `Clusters` (`int`): Light clusters in the grid.

## Properties

| Member | Description |
| --- | --- |
| [`Billboards`](#m-billboards) | Billboard quads drawn (see [`BillboardBatch3D`](BillboardBatch3D.md)). |
| [`Clusters`](#m-clusters) | Light clusters in the grid. |
| [`DrawCalls`](#m-drawcalls) | Indexed draws recorded (sky and post excluded). |
| [`Instances`](#m-instances) | Copies in all instance batches. |
| [`InstancesDrawn`](#m-instancesdrawn) | Copies of batches that survived culling against the camera. |
| [`Lights`](#m-lights) | Punctual lights used. |
| [`Objects`](#m-objects) | Mesh nodes drawn. |
| [`Quality`](#m-quality) | The quality level the frame was drawn at. |
| [`ShadowLights`](#m-shadowlights) | Lights that cast shadows. |
| [`ShadowTiles`](#m-shadowtiles) | Shadow map tiles rendered (cascades, spot maps, point faces). |
| [`Ssao`](#m-ssao) | Whether ambient occlusion ran. |
| [`Triangles`](#m-triangles) | Triangles submitted. |

### <a id="m-billboards"></a>Billboards

```csharp
public int Billboards { get; init; }
```

Billboard quads drawn (see [`BillboardBatch3D`](BillboardBatch3D.md)).

**Type:** `int`

### <a id="m-clusters"></a>Clusters

```csharp
public int Clusters { get; init; }
```

Light clusters in the grid.

**Type:** `int`

### <a id="m-drawcalls"></a>DrawCalls

```csharp
public int DrawCalls { get; init; }
```

Indexed draws recorded (sky and post excluded).

**Type:** `int`

### <a id="m-instances"></a>Instances

```csharp
public int Instances { get; init; }
```

Copies in all instance batches.

**Type:** `int`

### <a id="m-instancesdrawn"></a>InstancesDrawn

```csharp
public int InstancesDrawn { get; init; }
```

Copies of batches that survived culling against the camera.

**Type:** `int`

### <a id="m-lights"></a>Lights

```csharp
public int Lights { get; init; }
```

Punctual lights used.

**Type:** `int`

### <a id="m-objects"></a>Objects

```csharp
public int Objects { get; init; }
```

Mesh nodes drawn.

**Type:** `int`

### <a id="m-quality"></a>Quality

```csharp
public QualityLevel Quality { get; init; }
```

The quality level the frame was drawn at.

**Type:** [`QualityLevel`](QualityLevel.md)

### <a id="m-shadowlights"></a>ShadowLights

```csharp
public int ShadowLights { get; init; }
```

Lights that cast shadows.

**Type:** `int`

### <a id="m-shadowtiles"></a>ShadowTiles

```csharp
public int ShadowTiles { get; init; }
```

Shadow map tiles rendered (cascades, spot maps, point faces).

**Type:** `int`

### <a id="m-ssao"></a>Ssao

```csharp
public bool Ssao { get; init; }
```

Whether ambient occlusion ran.

**Type:** `bool`

### <a id="m-triangles"></a>Triangles

```csharp
public long Triangles { get; init; }
```

Triangles submitted.

**Type:** `long`
