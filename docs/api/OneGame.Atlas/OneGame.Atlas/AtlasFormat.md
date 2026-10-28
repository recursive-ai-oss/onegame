[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# AtlasFormat enum

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public enum AtlasFormat : int
```

The pixel layout of an atlas page.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-r8"></a>`R8` | `0` | One byte per pixel: coverage (0 = outside, 255 = fully inside). Glyph pages use it; the shader multiplies it into the text color. |
| <a id="m-rgba8premultiplied"></a>`Rgba8Premultiplied` | `1` | Four bytes per pixel, R G B A, with the color channels already multiplied by alpha. SVG and image pages use it; blend with (One, OneMinusSrcAlpha). |
