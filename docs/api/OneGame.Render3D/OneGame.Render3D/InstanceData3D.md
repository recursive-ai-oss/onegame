[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# InstanceData3D struct

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public struct InstanceData3D
```

One copy of a model in an [`InstanceBatch3D`](InstanceBatch3D.md): where it stands and a colour that multiplies the material's base colour.

## Properties

| Member | Description |
| --- | --- |
| [`Color`](#m-color) | Linear RGBA multiplied into the base colour (white leaves it unchanged). |
| [`Transform`](#m-transform) | Placement in the world. |

### <a id="m-color"></a>Color

```csharp
public Vector4 Color { get; set; }
```

Linear RGBA multiplied into the base colour (white leaves it unchanged).

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)

### <a id="m-transform"></a>Transform

```csharp
public Matrix4x4 Transform { get; set; }
```

Placement in the world. Use rotation, uniform scale and translation; mirrored or non-uniformly scaled copies light wrongly.

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)
