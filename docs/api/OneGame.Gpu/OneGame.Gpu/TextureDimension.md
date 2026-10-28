[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# TextureDimension enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum TextureDimension : int
```

The shape of a texture as shaders see it.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-d2"></a>`D2` | `0` | A 2D image, or a 2D array when it has more than one layer. |
| <a id="m-cube"></a>`Cube` | `1` | A cube map (6 layers) or cube array (a multiple of 6 layers). Layer order is +X, -X, +Y, -Y, +Z, -Z. |
| <a id="m-d3"></a>`D3` | `2` | A volume texture. |
