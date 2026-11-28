[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# ViewTintZone3D enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum ViewTintZone3D : int
```

Which side of [`Material3D.ViewTintEdge`](Material3D.md#m-viewtintedge) a view-angle tint colours.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-outsidecone"></a>`OutsideCone` | `0` | Surfaces seen at a grazing angle, where the facing value is below the edge (outside a cone around the normal). |
| <a id="m-insidecone"></a>`InsideCone` | `1` | Surfaces seen head on, where the facing value is above the edge (inside the cone). |
