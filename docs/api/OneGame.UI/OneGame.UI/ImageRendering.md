[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# ImageRendering enum

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum ImageRendering : int
```

The `image-rendering` property (CSS Images 3 §5.3): how an image is resampled when it is drawn at another size than its own pixels.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-auto"></a>`Auto` | `0` | Smooth (bilinear) filtering. The default. |
| <a id="m-pixelated"></a>`Pixelated` | `1` | Nearest-neighbour: every source pixel becomes a sharp block, for pixel art and icons that must stay crisp at any UI scale. Inherited. |
