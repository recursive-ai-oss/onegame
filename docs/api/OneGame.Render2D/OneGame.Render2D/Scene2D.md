[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Scene2D class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public sealed class Scene2D
```

Everything to draw in one frame of the world: sprites and tilemaps on layers. Fill it each frame (or keep entries that do not move and refill the rest), then pass it to [`Renderer2D.DrawScene()`](Renderer2D.md#m-drawscene-scene2d-camera2d-postchain). It reuses its storage, so a steady scene allocates nothing.

## Constructors

| Member | Description |
| --- | --- |
| [`Scene2D()`](#m-ctor) | Makes a scene with one layer, "default", that follows the camera exactly. |

### <a id="m-ctor"></a>Scene2D()

```csharp
public Scene2D();
```

Makes a scene with one layer, "default", that follows the camera exactly.

## Properties

| Member | Description |
| --- | --- |
| [`DefaultLayer`](#m-defaultlayer) | The first layer. |
| [`Layers`](#m-layers) | The layers, back to front. |

### <a id="m-defaultlayer"></a>DefaultLayer

```csharp
public Layer2D DefaultLayer { get; }
```

The first layer.

**Type:** [`Layer2D`](Layer2D.md)

### <a id="m-layers"></a>Layers

```csharp
public IReadOnlyList<Layer2D> Layers { get; }
```

The layers, back to front.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Layer2D`](Layer2D.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`AddLayer(string, Vector2)`](#m-addlayer-string-vector2) | Adds a layer in front of the existing ones. |
| [`Clear()`](#m-clear) | Removes everything drawn so far. |
| [`Draw(in Sprite)`](#m-draw-sprite) | Queues a sprite on the default layer. |
| [`Draw(Layer2D, in Sprite)`](#m-draw-layer2d-sprite) | Queues a sprite. |
| [`Draw(Layer2D, TileMap, float)`](#m-draw-layer2d-tilemap-float) | Queues a tilemap. |

### <a id="m-addlayer-string-vector2"></a>AddLayer(string, Vector2)

```csharp
public Layer2D AddLayer(string name, Vector2 parallax);
```

Adds a layer in front of the existing ones.

**Parameters**

- `name` (`string`): A name for debugging.
- `parallax` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): How much the layer follows the camera; see [`Layer2D.Parallax`](Layer2D.md#m-parallax).

**Returns:** [`Layer2D`](Layer2D.md) — The layer.

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes everything drawn so far. Layers stay.

### <a id="m-draw-sprite"></a>Draw(in Sprite)

```csharp
public void Draw(in Sprite sprite);
```

Queues a sprite on the default layer.

**Parameters**

- `sprite` ([`Sprite`](Sprite.md)): The sprite.

### <a id="m-draw-layer2d-sprite"></a>Draw(Layer2D, in Sprite)

```csharp
public void Draw(Layer2D layer, in Sprite sprite);
```

Queues a sprite.

**Parameters**

- `layer` ([`Layer2D`](Layer2D.md)): The layer it belongs to.
- `sprite` ([`Sprite`](Sprite.md)): The sprite.

### <a id="m-draw-layer2d-tilemap-float"></a>Draw(Layer2D, TileMap, float)

```csharp
public void Draw(Layer2D layer, TileMap map, float z = 0f);
```

Queues a tilemap.

**Parameters**

- `layer` ([`Layer2D`](Layer2D.md)): The layer it belongs to.
- `map` ([`TileMap`](TileMap.md)): The map.
- `z` (`float`): Draw order inside the layer, as for sprites.
