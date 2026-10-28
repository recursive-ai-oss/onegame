[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# AssetKind enum

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public enum AssetKind : int
```

What kind of content an [`Asset`](Asset.md) holds.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-texture"></a>`Texture` | `0` | A raster image (PNG, JPEG, WebP and the other formats OneImage reads) as a 2D texture. |
| <a id="m-svg"></a>`Svg` | `1` | An SVG rasterized at a chosen size as a 2D texture. |
| <a id="m-data"></a>`Data` | `2` | A YAML file read into one of the game's own types. Sim code may read it, so a reload is held back (see [`Asset.IsPending`](Asset.md#m-ispending)). |
| <a id="m-sound"></a>`Sound` | `3` | An audio clip. |
| <a id="m-model"></a>`Model` | `4` | A glTF model. |
| <a id="m-font"></a>`Font` | `5` | A font file added to a [`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md). Fonts are not hot reloaded. |
