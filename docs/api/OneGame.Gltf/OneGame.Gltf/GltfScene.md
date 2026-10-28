[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfScene class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfScene
```

A scene: the root nodes drawn together.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfScene()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfScene()

```csharp
public GltfScene();
```

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The scene's name, or empty. |
| [`Roots`](#m-roots) | Indices of the root nodes. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The scene's name, or empty.

**Type:** `string`

### <a id="m-roots"></a>Roots

```csharp
public int[] Roots { get; }
```

Indices of the root nodes.

**Type:** `int[]`
