[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfMinFilter enum

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public enum GltfMinFilter : int
```

Texture minification filter.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-unspecified"></a>`Unspecified` | `0` | The file does not say. |
| <a id="m-nearest"></a>`Nearest` | `1` | Nearest texel. |
| <a id="m-linear"></a>`Linear` | `2` | Bilinear. |
| <a id="m-nearestmipmapnearest"></a>`NearestMipmapNearest` | `3` | Nearest texel in the nearest mip. |
| <a id="m-linearmipmapnearest"></a>`LinearMipmapNearest` | `4` | Bilinear in the nearest mip. |
| <a id="m-nearestmipmaplinear"></a>`NearestMipmapLinear` | `5` | Nearest texel blended between mips. |
| <a id="m-linearmipmaplinear"></a>`LinearMipmapLinear` | `6` | Trilinear. |
