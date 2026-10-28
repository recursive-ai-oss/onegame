[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# TextureViewKind enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum TextureViewKind : int
```

How a view of a texture presents it to a shader.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-auto"></a>`Auto` | `0` | Derived from the texture: 2D, 2D array, cube, cube array or 3D; a view of exactly one layer of a 2D or cube texture is 2D. |
| <a id="m-d2"></a>`D2` | `1` | One 2D image. |
| <a id="m-d2array"></a>`D2Array` | `2` | A 2D array. |
| <a id="m-cube"></a>`Cube` | `3` | One cube map (6 layers). |
| <a id="m-cubearray"></a>`CubeArray` | `4` | A cube array. |
| <a id="m-d3"></a>`D3` | `5` | A volume. |
