[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfAlphaMode enum

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public enum GltfAlphaMode : int
```

How the alpha of the base colour is used.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-opaque"></a>`Opaque` | `0` | Alpha is ignored. |
| <a id="m-mask"></a>`Mask` | `1` | Pixels below [`GltfMaterial.AlphaCutoff`](GltfMaterial.md#m-alphacutoff) are dropped. |
| <a id="m-blend"></a>`Blend` | `2` | Alpha blends with what is behind. |
