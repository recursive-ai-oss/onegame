[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# PickResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PickResult`](PickResult.md)&gt;

```csharp
public sealed record PickResult : IEquatable<PickResult>
```

Result of `pick`: what is under a pixel, or along a ray.

## Constructors

| Member | Description |
| --- | --- |
| [`PickResult(bool, string?, ulong?, string?, string?, double?, double[]?, double[], double[], long)`](#m-ctor-bool-string-ulong-string-string-double-double-double-double-long) | Result of `pick`: what is under a pixel, or along a ray. |

### <a id="m-ctor-bool-string-ulong-string-string-double-double-double-double-long"></a>PickResult(bool, string?, ulong?, string?, string?, double?, double\[\]?, double\[\], double\[\], long)

```csharp
public PickResult(bool Hit, string? Entity, ulong? Id, string? Address, string? Model, double? Distance, double[]? Point, double[] Origin, double[] Direction, long Tick);
```

Result of `pick`: what is under a pixel, or along a ray.

**Parameters**

- `Hit` (`bool`): True when a pickable model was hit.
- `Entity` (`string?`): The entity as `Type#id`, when hit.
- `Id` (`ulong?`): The entity id, when hit.
- `Address` (`string?`): The entity as `Type#n`, n counting from 1 among entities of its type: the form other operations and scenarios take.
- `Model` (`string?`): The model name, when hit.
- `Distance` (`double?`): Distance from the ray origin, when hit.
- `Point` (`double[]?`): The hit position in the world, when hit.
- `Origin` (`double[]`): The ray origin.
- `Direction` (`double[]`): The ray direction (unit length).
- `Tick` (`long`): The tick the answer is for.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | The entity as `Type#n`, n counting from 1 among entities of its type: the form other operations and scenarios take. |
| [`Direction`](#m-direction) | The ray direction (unit length). |
| [`Distance`](#m-distance) | Distance from the ray origin, when hit. |
| [`Entity`](#m-entity) | The entity as `Type#id`, when hit. |
| [`Hit`](#m-hit) | True when a pickable model was hit. |
| [`Id`](#m-id) | The entity id, when hit. |
| [`Model`](#m-model) | The model name, when hit. |
| [`Origin`](#m-origin) | The ray origin. |
| [`Point`](#m-point) | The hit position in the world, when hit. |
| [`Tick`](#m-tick) | The tick the answer is for. |

### <a id="m-address"></a>Address

```csharp
public string? Address { get; init; }
```

The entity as `Type#n`, n counting from 1 among entities of its type: the form other operations and scenarios take.

**Type:** `string?`

### <a id="m-direction"></a>Direction

```csharp
public double[] Direction { get; init; }
```

The ray direction (unit length).

**Type:** `double[]`

### <a id="m-distance"></a>Distance

```csharp
public double? Distance { get; init; }
```

Distance from the ray origin, when hit.

**Type:** `double?`

### <a id="m-entity"></a>Entity

```csharp
public string? Entity { get; init; }
```

The entity as `Type#id`, when hit.

**Type:** `string?`

### <a id="m-hit"></a>Hit

```csharp
public bool Hit { get; init; }
```

True when a pickable model was hit.

**Type:** `bool`

### <a id="m-id"></a>Id

```csharp
public ulong? Id { get; init; }
```

The entity id, when hit.

**Type:** `ulong?`

### <a id="m-model"></a>Model

```csharp
public string? Model { get; init; }
```

The model name, when hit.

**Type:** `string?`

### <a id="m-origin"></a>Origin

```csharp
public double[] Origin { get; init; }
```

The ray origin.

**Type:** `double[]`

### <a id="m-point"></a>Point

```csharp
public double[]? Point { get; init; }
```

The hit position in the world, when hit.

**Type:** `double[]?`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick the answer is for.

**Type:** `long`
