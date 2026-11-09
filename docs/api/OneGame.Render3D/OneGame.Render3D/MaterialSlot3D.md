[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# MaterialSlot3D enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum MaterialSlot3D : int
```

The texture slots of a [`Material3D`](Material3D.md) that [`Material3D.SetTexture()`](Material3D.md#m-settexture-materialslot3d-gputexture) can replace.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-basecolor"></a>`BaseColor` | `0` | Base colour (sRGB colour; multiplied by the base colour factor). |
| <a id="m-metallicroughness"></a>`MetallicRoughness` | `1` | Metallic (blue) and roughness (green). |
| <a id="m-normal"></a>`Normal` | `2` | Tangent-space normal map. |
| <a id="m-occlusion"></a>`Occlusion` | `3` | Occlusion (red). |
| <a id="m-emissive"></a>`Emissive` | `4` | Emissive colour (multiplied by [`Material3D.Emissive`](Material3D.md#m-emissive)). |
