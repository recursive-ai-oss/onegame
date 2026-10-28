[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# AnimationInterpolation enum

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public enum AnimationInterpolation : int
```

How values between two keys are computed.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-linear"></a>`Linear` | `0` | Linear; rotations use spherical interpolation. |
| <a id="m-step"></a>`Step` | `1` | The earlier key's value holds until the next key. |
| <a id="m-cubicspline"></a>`CubicSpline` | `2` | Cubic Hermite spline with in and out tangents per key. |
