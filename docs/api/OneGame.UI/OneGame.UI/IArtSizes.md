[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# IArtSizes interface

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public interface IArtSizes
```

Tells the UI how big an image or SVG is, so it can size and tile backgrounds (`background-size`, `background-repeat`) without knowing how art is stored.

## Methods

| Member | Description |
| --- | --- |
| [`TryGetNaturalSize(string, out float, out float)`](#m-trygetnaturalsize-string-float-float) | Gets the natural size of the art with this asset id: pixels for a raster image, the document's own size for an SVG. |

### <a id="m-trygetnaturalsize-string-float-float"></a>TryGetNaturalSize(string, out float, out float)

```csharp
bool TryGetNaturalSize(string id, out float width, out float height);
```

Gets the natural size of the art with this asset id: pixels for a raster image, the document's own size for an SVG. Both are in UI units at scale 1.

**Parameters**

- `id` (`string`): The asset id used by `background-image`.
- `width` (`float`): Natural width.
- `height` (`float`): Natural height.

**Returns:** `bool` — False when the id is unknown.
