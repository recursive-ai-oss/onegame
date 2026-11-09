[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# MaterialAlpha3D enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum MaterialAlpha3D : int
```

How a material from [`Renderer3D.CreateMaterial()`](Renderer3D.md#m-creatematerial-string-materialalpha3d-bool-vector4-float) treats alpha.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-opaque"></a>`Opaque` | `0` | Alpha is ignored. |
| <a id="m-mask"></a>`Mask` | `1` | Pixels with alpha below the cutoff are dropped (leaves, fences); still drawn and shadowed as solid. |
| <a id="m-blend"></a>`Blend` | `2` | Alpha blends with what is behind (water, glass); drawn far to near, no depth write. |
