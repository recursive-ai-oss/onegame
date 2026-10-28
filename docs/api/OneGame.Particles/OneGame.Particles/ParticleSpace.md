[API reference](../../index.md) › [OneGame.Particles](../index.md) › OneGame.Particles

# ParticleSpace enum

**Namespace:** `OneGame.Particles`  
**Package:** [`RecursiveAI.OneGame.Particles`](../index.md)

```csharp
public enum ParticleSpace : int
```

Whether particles move in a flat plane (2D games) or in space.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-plane"></a>`Plane` | `0` | X and Y only. Directions spread by an angle in the plane and the spawn area is a disc. Use for `Draw2D`. |
| <a id="m-volume"></a>`Volume` | `1` | X, Y and Z. Directions spread in a cone and the spawn area is a ball. Use for 3D scenes. |
