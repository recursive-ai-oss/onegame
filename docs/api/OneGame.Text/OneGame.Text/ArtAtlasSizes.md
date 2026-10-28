[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# ArtAtlasSizes class

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)  
**Implements:** [`IArtSizes`](../../OneGame.UI/OneGame.UI/IArtSizes.md)

```csharp
public sealed class ArtAtlasSizes : IArtSizes
```

Lets the UI ask an [`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md) how big its art is: assign it to [`UiTree.ArtSizes`](../../OneGame.UI/OneGame.UI/UiTree.md#m-artsizes).

## Constructors

| Member | Description |
| --- | --- |
| [`ArtAtlasSizes(ArtAtlas)`](#m-ctor-artatlas) | Lets the UI ask an [`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md) how big its art is: assign it to [`UiTree.ArtSizes`](../../OneGame.UI/OneGame.UI/UiTree.md#m-artsizes). |

### <a id="m-ctor-artatlas"></a>ArtAtlasSizes(ArtAtlas)

```csharp
public ArtAtlasSizes(ArtAtlas atlas);
```

Lets the UI ask an [`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md) how big its art is: assign it to [`UiTree.ArtSizes`](../../OneGame.UI/OneGame.UI/UiTree.md#m-artsizes).

**Parameters**

- `atlas` ([`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md)): The atlas the display list's image ids refer to.

## Methods

| Member | Description |
| --- | --- |
| [`TryGetNaturalSize(string, out float, out float)`](#m-trygetnaturalsize-string-float-float) | Gets the natural size of the art with this asset id: pixels for a raster image, the document's own size for an SVG. |

### <a id="m-trygetnaturalsize-string-float-float"></a>TryGetNaturalSize(string, out float, out float)

```csharp
public bool TryGetNaturalSize(string id, out float width, out float height);
```

Gets the natural size of the art with this asset id: pixels for a raster image, the document's own size for an SVG. Both are in UI units at scale 1.

**Parameters**

- `id` (`string`): The asset id used by `background-image`.
- `width` (`float`): Natural width.
- `height` (`float`): Natural height.

**Returns:** `bool` — False when the id is unknown.
