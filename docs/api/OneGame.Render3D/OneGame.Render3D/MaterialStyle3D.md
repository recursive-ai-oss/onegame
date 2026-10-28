[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# MaterialStyle3D enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum MaterialStyle3D : int
```

How a material is lit.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-pbr"></a>`Pbr` | `0` | Physically based metallic-roughness shading, as authored. |
| <a id="m-toon"></a>`Toon` | `1` | Cel shading: light is split into a few flat bands, with a hard specular highlight and an optional outline. |
| <a id="m-flat"></a>`Flat` | `2` | Unlit: the base colour (texture and vertex colour included) as is. An outline is still available. |
