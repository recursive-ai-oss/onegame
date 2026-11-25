[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# FogMode enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum FogMode : int
```

How [`FogSettings`](FogSettings.md) turns distance (and height) into an amount of fog.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-linear"></a>`Linear` | `0` | Rises linearly from 0 at [`FogSettings.Start`](FogSettings.md#m-start) to the maximum at [`FogSettings.End`](FogSettings.md#m-end). The default. |
| <a id="m-exponential"></a>`Exponential` | `1` | 1 - exp(-[`FogSettings.Density`](FogSettings.md#m-density) \* distance): thick fog with no hard far edge. |
| <a id="m-exponentialsquared"></a>`ExponentialSquared` | `2` | 1 - exp(-([`FogSettings.Density`](FogSettings.md#m-density) \* distance)^2): clear nearby, then closing in faster. |
| <a id="m-height"></a>`Height` | `3` | Exponential fog whose density falls off with world height: [`FogSettings.Density`](FogSettings.md#m-density) at [`FogSettings.HeightBase`](FogSettings.md#m-heightbase), shrinking by [`FogSettings.HeightFalloff`](FogSettings.md#m-heightfalloff) per unit above it (and growing below). Fog pools in low ground and a camera above it sees through it. |
