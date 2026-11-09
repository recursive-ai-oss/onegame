[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# DebugDraw3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class DebugDraw3D
```

Reusable immediate-mode debug primitives. Colours are linear straight-alpha; sizes must be finite and positive. Clear before refilling each frame. Within each depth mode primitives blend in submission order, not distance order.

## Constructors

| Member | Description |
| --- | --- |
| [`DebugDraw3D()`](#m-ctor) |  |

### <a id="m-ctor"></a>DebugDraw3D()

```csharp
public DebugDraw3D();
```

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | Number of submitted primitives. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

Number of submitted primitives.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Append(DebugDraw3D)`](#m-append-debugdraw3d) | Appends another list, retaining its per-mode submission order. |
| [`Arrow(Vector3, Vector3, Vector4, float, DebugSize3D, DebugDepth3D)`](#m-arrow-vector3-vector3-vector4-float-debugsize3d-debugdepth3d) | Adds a line and triangular head at its destination. |
| [`Clear()`](#m-clear) | Removes all primitives, retaining storage. |
| [`Line(Vector3, Vector3, Vector4, float, DebugSize3D, DebugDepth3D)`](#m-line-vector3-vector3-vector4-float-debugsize3d-debugdepth3d) | Adds a camera-facing thick line. |
| [`Point(Vector3, Vector4, float, DebugSize3D, DebugDepth3D)`](#m-point-vector3-vector4-float-debugsize3d-debugdepth3d) | Adds a camera-facing square centred on a world position. |

### <a id="m-append-debugdraw3d"></a>Append(DebugDraw3D)

```csharp
public void Append(DebugDraw3D source);
```

Appends another list, retaining its per-mode submission order.

**Parameters**

- `source` ([`DebugDraw3D`](DebugDraw3D.md))

### <a id="m-arrow-vector3-vector3-vector4-float-debugsize3d-debugdepth3d"></a>Arrow(Vector3, Vector3, Vector4, float, DebugSize3D, DebugDepth3D)

```csharp
public void Arrow(Vector3 from, Vector3 to, Vector4 color, float width = 1f, DebugSize3D units = 0, DebugDepth3D mode = 0);
```

Adds a line and triangular head at its destination. Head length is six widths, capped at half the projected shaft length.

**Parameters**

- `from` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3))
- `to` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3))
- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4))
- `width` (`float`)
- `units` ([`DebugSize3D`](DebugSize3D.md))
- `mode` ([`DebugDepth3D`](DebugDepth3D.md))

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes all primitives, retaining storage.

### <a id="m-line-vector3-vector3-vector4-float-debugsize3d-debugdepth3d"></a>Line(Vector3, Vector3, Vector4, float, DebugSize3D, DebugDepth3D)

```csharp
public void Line(Vector3 from, Vector3 to, Vector4 color, float width = 1f, DebugSize3D units = 0, DebugDepth3D mode = 0);
```

Adds a camera-facing thick line. Coincident endpoints produce no pixels.

**Parameters**

- `from` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3))
- `to` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3))
- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4))
- `width` (`float`)
- `units` ([`DebugSize3D`](DebugSize3D.md))
- `mode` ([`DebugDepth3D`](DebugDepth3D.md))

### <a id="m-point-vector3-vector4-float-debugsize3d-debugdepth3d"></a>Point(Vector3, Vector4, float, DebugSize3D, DebugDepth3D)

```csharp
public void Point(Vector3 position, Vector4 color, float size = 4f, DebugSize3D units = 0, DebugDepth3D mode = 0);
```

Adds a camera-facing square centred on a world position.

**Parameters**

- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3))
- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4))
- `size` (`float`)
- `units` ([`DebugSize3D`](DebugSize3D.md))
- `mode` ([`DebugDepth3D`](DebugDepth3D.md))
