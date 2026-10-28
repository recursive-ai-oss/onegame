[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# ImageStatus enum

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public enum ImageStatus : int
```

The outcome of reading an image.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-decoded"></a>`Decoded` | `0` | Decoded; [`GltfImage.Pixels`](GltfImage.md#m-pixels) holds RGBA8. |
| <a id="m-headeronly"></a>`HeaderOnly` | `1` | Only the header was read because decoding was switched off. |
| <a id="m-unsupported"></a>`Unsupported` | `2` | The format is not PNG or JPEG; there are no pixels. |
