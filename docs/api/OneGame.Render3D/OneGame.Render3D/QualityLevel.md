[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# QualityLevel enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum QualityLevel : int
```

A named rung of the quality ladder. Switch it at runtime with [`Scene3D.Quality`](Scene3D.md#m-quality).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-low"></a>`Low` | `0` | Small shadow maps with two cascades and no filtering, no ambient occlusion, no multisampling and no bloom. |
| <a id="m-medium"></a>`Medium` | `1` | 1024 texel shadow maps, three cascades, half-resolution ambient occlusion, 4x multisampling. |
| <a id="m-high"></a>`High` | `2` | 2048 texel shadow maps, four cascades with 3x3 filtering, full-resolution ambient occlusion, 4x multisampling. The default. |
| <a id="m-ultra"></a>`Ultra` | `3` | 4096 texel shadow maps with 5x5 filtering, more occlusion samples, 8x multisampling, a 512 texel environment. |
