[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# BillboardBatch3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class BillboardBatch3D
```

Quads that always face the camera, drawn after the opaque and blended surfaces: particles, glows, lens spots, markers. They are depth tested against the scene but write no depth, so they can be seen through each other. Blended quads of a batch are sorted far to near each frame; additive ones are order independent. The batch reuses its storage, so refilling it each frame allocates nothing once grown. Add it to [`Scene3D.Billboards`](Scene3D.md#m-billboards).

## Constructors

| Member | Description |
| --- | --- |
| [`BillboardBatch3D()`](#m-ctor) |  |

### <a id="m-ctor"></a>BillboardBatch3D()

```csharp
public BillboardBatch3D();
```

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | Number of quads. |
| [`Name`](#m-name) | A name for debugging and the render graph report. |
| [`Texture`](#m-texture) | The image the quads show; null shows the renderer's soft round spot ([`Renderer3D.SoftSpot`](Renderer3D.md#m-softspot)). |
| [`Visible`](#m-visible) | Whether the batch is drawn. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

Number of quads.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; set; }
```

A name for debugging and the render graph report.

**Type:** `string`

### <a id="m-texture"></a>Texture

```csharp
public BillboardTexture3D? Texture { get; set; }
```

The image the quads show; null shows the renderer's soft round spot ([`Renderer3D.SoftSpot`](Renderer3D.md#m-softspot)).

**Type:** [`BillboardTexture3D`](BillboardTexture3D.md)?

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; set; }
```

Whether the batch is drawn.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Add(in Billboard3D)`](#m-add-billboard3d) | Adds a quad. |
| [`Clear()`](#m-clear) | Removes every quad. |

### <a id="m-add-billboard3d"></a>Add(in Billboard3D)

```csharp
public void Add(in Billboard3D quad);
```

Adds a quad.

**Parameters**

- `quad` ([`Billboard3D`](Billboard3D.md)): The quad.

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every quad.
