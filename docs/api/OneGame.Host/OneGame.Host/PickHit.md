[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# PickHit struct

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PickHit`](PickHit.md)&gt;

```csharp
public readonly struct PickHit : IEquatable<PickHit>
```

What a pick found.

## Constructors

| Member | Description |
| --- | --- |
| [`PickHit(EntityId, string, float, Vector3)`](#m-ctor-entityid-string-float-vector3) | What a pick found. |

### <a id="m-ctor-entityid-string-float-vector3"></a>PickHit(EntityId, string, float, Vector3)

```csharp
public PickHit(EntityId Entity, string Model, float Distance, Vector3 Point);
```

What a pick found.

**Parameters**

- `Entity` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The entity whose model was hit.
- `Model` (`string`): The name of the model.
- `Distance` (`float`): Distance from the ray origin to the hit.
- `Point` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The hit position in the world.

## Properties

| Member | Description |
| --- | --- |
| [`Distance`](#m-distance) | Distance from the ray origin to the hit. |
| [`Entity`](#m-entity) | The entity whose model was hit. |
| [`Model`](#m-model) | The name of the model. |
| [`Point`](#m-point) | The hit position in the world. |

### <a id="m-distance"></a>Distance

```csharp
public float Distance { get; init; }
```

Distance from the ray origin to the hit.

**Type:** `float`

### <a id="m-entity"></a>Entity

```csharp
public EntityId Entity { get; init; }
```

The entity whose model was hit.

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)

### <a id="m-model"></a>Model

```csharp
public string Model { get; init; }
```

The name of the model.

**Type:** `string`

### <a id="m-point"></a>Point

```csharp
public Vector3 Point { get; init; }
```

The hit position in the world.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)
