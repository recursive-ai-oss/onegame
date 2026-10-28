[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfTopology enum

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public enum GltfTopology : int
```

How a primitive's indices form shapes. Strips, fans and loops are expanded to lists on import.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-points"></a>`Points` | `0` | One point per index. |
| <a id="m-lines"></a>`Lines` | `1` | Two indices per line. |
| <a id="m-triangles"></a>`Triangles` | `2` | Three indices per triangle, counter-clockwise front faces. |
