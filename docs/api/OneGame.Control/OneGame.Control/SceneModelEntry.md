[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SceneModelEntry record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SceneModelEntry`](SceneModelEntry.md)&gt;

```csharp
public sealed record SceneModelEntry : IEquatable<SceneModelEntry>
```

One model of `scene_models`.

## Constructors

| Member | Description |
| --- | --- |
| [`SceneModelEntry(string, string?, double, string?, bool, int, double[])`](#m-ctor-string-string-double-string-bool-int-double) | One model of `scene_models`. |

### <a id="m-ctor-string-string-double-string-bool-int-double"></a>SceneModelEntry(string, string?, double, string?, bool, int, double\[\])

```csharp
public SceneModelEntry(string Model, string? Label, double Opacity, string? Entity, bool Dynamic, int Triangles, double[] Position);
```

One model of `scene_models`.

**Parameters**

- `Model` (`string`): The library name.
- `Label` (`string?`): The label the game gave this draw, or null.
- `Opacity` (`double`): 1 for solid, less when faded.
- `Entity` (`string?`): The entity that drew it (`Type#id`), or null.
- `Dynamic` (`bool`): True for a mesh built at run time.
- `Triangles` (`int`): Triangles drawn.
- `Position` (`double[]`): World position.

## Properties

| Member | Description |
| --- | --- |
| [`Dynamic`](#m-dynamic) | True for a mesh built at run time. |
| [`Entity`](#m-entity) | The entity that drew it (`Type#id`), or null. |
| [`Label`](#m-label) | The label the game gave this draw, or null. |
| [`Model`](#m-model) | The library name. |
| [`Opacity`](#m-opacity) | 1 for solid, less when faded. |
| [`Position`](#m-position) | World position. |
| [`Triangles`](#m-triangles) | Triangles drawn. |

### <a id="m-dynamic"></a>Dynamic

```csharp
public bool Dynamic { get; init; }
```

True for a mesh built at run time.

**Type:** `bool`

### <a id="m-entity"></a>Entity

```csharp
public string? Entity { get; init; }
```

The entity that drew it (`Type#id`), or null.

**Type:** `string?`

### <a id="m-label"></a>Label

```csharp
public string? Label { get; init; }
```

The label the game gave this draw, or null.

**Type:** `string?`

### <a id="m-model"></a>Model

```csharp
public string Model { get; init; }
```

The library name.

**Type:** `string`

### <a id="m-opacity"></a>Opacity

```csharp
public double Opacity { get; init; }
```

1 for solid, less when faded.

**Type:** `double`

### <a id="m-position"></a>Position

```csharp
public double[] Position { get; init; }
```

World position.

**Type:** `double[]`

### <a id="m-triangles"></a>Triangles

```csharp
public int Triangles { get; init; }
```

Triangles drawn.

**Type:** `int`
