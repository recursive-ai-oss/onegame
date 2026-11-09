[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# ModelOptions struct

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ModelOptions`](ModelOptions.md)&gt;

```csharp
public readonly struct ModelOptions : IEquatable<ModelOptions>
```

Per-draw settings for `SceneWriter.Model` beyond placement: how see-through the model is, and the name tools show for it.

## Constructors

| Member | Description |
| --- | --- |
| [`ModelOptions()`](#m-ctor) | Settings that change nothing: fully opaque, no label, pickable. |

### <a id="m-ctor"></a>ModelOptions()

```csharp
public ModelOptions();
```

Settings that change nothing: fully opaque, no label, pickable.

## Properties

| Member | Description |
| --- | --- |
| [`Label`](#m-label) | A name for tools; it is the instance's `ModelInstance3D.Label`. |
| [`Opacity`](#m-opacity) | 1 (default) is solid; below 1 the whole model is multiplied by this alpha and drawn in the blended pass (far to near, no depth write, no shadow), as for `ModelInstance3D.Opacity`. |
| [`Pickable`](#m-pickable) | Whether `pick` can hit the model (meshes made at run time are never pickable). |

### <a id="m-label"></a>Label

```csharp
public string? Label { get; init; }
```

A name for tools; it is the instance's `ModelInstance3D.Label`. Null leaves it empty.

**Type:** `string?`

### <a id="m-opacity"></a>Opacity

```csharp
public float Opacity { get; init; }
```

1 (default) is solid; below 1 the whole model is multiplied by this alpha and drawn in the blended pass (far to near, no depth write, no shadow), as for `ModelInstance3D.Opacity`. At 0 nothing is drawn.

**Type:** `float`

### <a id="m-pickable"></a>Pickable

```csharp
public bool Pickable { get; init; }
```

Whether `pick` can hit the model (meshes made at run time are never pickable).

**Type:** `bool`
