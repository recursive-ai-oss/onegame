[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# SceneModelInfo struct

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SceneModelInfo`](SceneModelInfo.md)&gt;

```csharp
public readonly struct SceneModelInfo : IEquatable<SceneModelInfo>
```

One model drawn in a frame, as the `scene_models` operation reports it.

## Constructors

| Member | Description |
| --- | --- |
| [`SceneModelInfo(string, string?, float, EntityId, bool, int, Vector3)`](#m-ctor-string-string-float-entityid-bool-int-vector3) | One model drawn in a frame, as the `scene_models` operation reports it. |

### <a id="m-ctor-string-string-float-entityid-bool-int-vector3"></a>SceneModelInfo(string, string?, float, EntityId, bool, int, Vector3)

```csharp
public SceneModelInfo(string Model, string? Label, float Opacity, EntityId Entity, bool Dynamic, int Triangles, Vector3 Position);
```

One model drawn in a frame, as the `scene_models` operation reports it.

**Parameters**

- `Model` (`string`): The library name of the model or mesh (the instance's label for a hand-filled scene).
- `Label` (`string?`): The label given with [`ModelOptions.Label`](ModelOptions.md#m-label), or null.
- `Opacity` (`float`): 1 for solid; below 1 the model is blended.
- `Entity` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The entity that drew it, or the default id for a describer or a hand-filled scene.
- `Dynamic` (`bool`): True for a mesh built at run time ([`ModelLibrary.SetMesh()`](ModelLibrary.md#m-setmesh-string-material3d-readonlyspan-meshvertex3d-readonlyspan-uint)).
- `Triangles` (`int`): Triangles drawn (0 until a mesh's first upload).
- `Position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where the model is, in world space.

## Properties

| Member | Description |
| --- | --- |
| [`Dynamic`](#m-dynamic) | True for a mesh built at run time ([`ModelLibrary.SetMesh()`](ModelLibrary.md#m-setmesh-string-material3d-readonlyspan-meshvertex3d-readonlyspan-uint)). |
| [`Entity`](#m-entity) | The entity that drew it, or the default id for a describer or a hand-filled scene. |
| [`Label`](#m-label) | The label given with [`ModelOptions.Label`](ModelOptions.md#m-label), or null. |
| [`Model`](#m-model) | The library name of the model or mesh (the instance's label for a hand-filled scene). |
| [`Opacity`](#m-opacity) | 1 for solid; below 1 the model is blended. |
| [`Position`](#m-position) | Where the model is, in world space. |
| [`Triangles`](#m-triangles) | Triangles drawn (0 until a mesh's first upload). |

### <a id="m-dynamic"></a>Dynamic

```csharp
public bool Dynamic { get; init; }
```

True for a mesh built at run time ([`ModelLibrary.SetMesh()`](ModelLibrary.md#m-setmesh-string-material3d-readonlyspan-meshvertex3d-readonlyspan-uint)).

**Type:** `bool`

### <a id="m-entity"></a>Entity

```csharp
public EntityId Entity { get; init; }
```

The entity that drew it, or the default id for a describer or a hand-filled scene.

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)

### <a id="m-label"></a>Label

```csharp
public string? Label { get; init; }
```

The label given with [`ModelOptions.Label`](ModelOptions.md#m-label), or null.

**Type:** `string?`

### <a id="m-model"></a>Model

```csharp
public string Model { get; init; }
```

The library name of the model or mesh (the instance's label for a hand-filled scene).

**Type:** `string`

### <a id="m-opacity"></a>Opacity

```csharp
public float Opacity { get; init; }
```

1 for solid; below 1 the model is blended.

**Type:** `float`

### <a id="m-position"></a>Position

```csharp
public Vector3 Position { get; init; }
```

Where the model is, in world space.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-triangles"></a>Triangles

```csharp
public int Triangles { get; init; }
```

Triangles drawn (0 until a mesh's first upload).

**Type:** `int`
