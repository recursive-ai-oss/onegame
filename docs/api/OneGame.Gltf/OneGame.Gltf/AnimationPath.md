[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# AnimationPath enum

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public enum AnimationPath : int
```

Which node property a channel animates.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-translation"></a>`Translation` | `0` | Local translation (3 floats per key). |
| <a id="m-rotation"></a>`Rotation` | `1` | Local rotation as a quaternion (4 floats per key). |
| <a id="m-scale"></a>`Scale` | `2` | Local scale (3 floats per key). |
| <a id="m-weights"></a>`Weights` | `3` | Morph target weights (one float per target per key). |
