[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# LightKind enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum LightKind : int
```

The kind of a punctual light (KHR\_lights\_punctual).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-directional"></a>`Directional` | `0` | Parallel rays from infinitely far away; intensity is in lux. |
| <a id="m-point"></a>`Point` | `1` | Radiates equally in all directions from a point; intensity is in candela. |
| <a id="m-spot"></a>`Spot` | `2` | A point light limited to a cone; intensity is in candela. |
