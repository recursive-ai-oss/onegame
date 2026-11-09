[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SceneModelsResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SceneModelsResult`](SceneModelsResult.md)&gt;

```csharp
public sealed record SceneModelsResult : IEquatable<SceneModelsResult>
```

Result of `scene_models`: what the 3D view draws this frame.

## Constructors

| Member | Description |
| --- | --- |
| [`SceneModelsResult(int, int, long, bool, SceneModelEntry[])`](#m-ctor-int-int-long-bool-scenemodelentry) | Result of `scene_models`: what the 3D view draws this frame. |

### <a id="m-ctor-int-int-long-bool-scenemodelentry"></a>SceneModelsResult(int, int, long, bool, SceneModelEntry\[\])

```csharp
public SceneModelsResult(int Count, int Dynamic, long Triangles, bool Truncated, SceneModelEntry[] Models);
```

Result of `scene_models`: what the 3D view draws this frame.

**Parameters**

- `Count` (`int`): Models drawn.
- `Dynamic` (`int`): How many of them are meshes built at run time.
- `Triangles` (`long`): Triangles across all of them.
- `Truncated` (`bool`): True when `limit` cut the list.
- `Models` ([`SceneModelEntry`](SceneModelEntry.md)\[\]): The models, in draw order.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | Models drawn. |
| [`Dynamic`](#m-dynamic) | How many of them are meshes built at run time. |
| [`Models`](#m-models) | The models, in draw order. |
| [`Triangles`](#m-triangles) | Triangles across all of them. |
| [`Truncated`](#m-truncated) | True when `limit` cut the list. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; init; }
```

Models drawn.

**Type:** `int`

### <a id="m-dynamic"></a>Dynamic

```csharp
public int Dynamic { get; init; }
```

How many of them are meshes built at run time.

**Type:** `int`

### <a id="m-models"></a>Models

```csharp
public SceneModelEntry[] Models { get; init; }
```

The models, in draw order.

**Type:** [`SceneModelEntry`](SceneModelEntry.md)\[\]

### <a id="m-triangles"></a>Triangles

```csharp
public long Triangles { get; init; }
```

Triangles across all of them.

**Type:** `long`

### <a id="m-truncated"></a>Truncated

```csharp
public bool Truncated { get; init; }
```

True when `limit` cut the list.

**Type:** `bool`
