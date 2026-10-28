[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# PrimitiveTopology enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum PrimitiveTopology : int
```

How vertices are assembled into primitives.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-trianglelist"></a>`TriangleList` | `0` | Every three vertices form a triangle. |
| <a id="m-trianglestrip"></a>`TriangleStrip` | `1` | Each vertex after the second forms a triangle with the previous two. |
| <a id="m-linelist"></a>`LineList` | `2` | Every two vertices form a line. |
| <a id="m-pointlist"></a>`PointList` | `3` | Each vertex is a point. |
