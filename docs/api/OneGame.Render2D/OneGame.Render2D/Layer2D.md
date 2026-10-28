[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Layer2D class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public sealed class Layer2D
```

A layer of a [`Scene2D`](Scene2D.md). Layers draw in the order they were added; each follows the camera by its own parallax factor.

## Properties

| Member | Description |
| --- | --- |
| [`Index`](#m-index) | Position in draw order; 0 is the back. |
| [`Name`](#m-name) | The layer's name, for debugging and captures. |
| [`Parallax`](#m-parallax) | How much the layer follows the camera position, per axis. |
| [`Visible`](#m-visible) | Layers that are not visible are skipped. |

### <a id="m-index"></a>Index

```csharp
public int Index { get; }
```

Position in draw order; 0 is the back.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The layer's name, for debugging and captures.

**Type:** `string`

### <a id="m-parallax"></a>Parallax

```csharp
public Vector2 Parallax { get; set; }
```

How much the layer follows the camera position, per axis. 1 is the world itself; a distant backdrop uses something below 1.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; set; }
```

Layers that are not visible are skipped.

**Type:** `bool`
