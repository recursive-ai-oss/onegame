[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# AnimationChannel class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class AnimationChannel
```

Connects a sampler to a node property.

## Constructors

| Member | Description |
| --- | --- |
| [`AnimationChannel()`](#m-ctor) |  |

### <a id="m-ctor"></a>AnimationChannel()

```csharp
public AnimationChannel();
```

## Properties

| Member | Description |
| --- | --- |
| [`Node`](#m-node) | Index of the animated node. |
| [`Path`](#m-path) | The animated property. |
| [`Sampler`](#m-sampler) | Index into [`AnimationClip.Samplers`](AnimationClip.md#m-samplers). |

### <a id="m-node"></a>Node

```csharp
public int Node { get; }
```

Index of the animated node.

**Type:** `int`

### <a id="m-path"></a>Path

```csharp
public AnimationPath Path { get; }
```

The animated property.

**Type:** [`AnimationPath`](AnimationPath.md)

### <a id="m-sampler"></a>Sampler

```csharp
public int Sampler { get; }
```

Index into [`AnimationClip.Samplers`](AnimationClip.md#m-samplers).

**Type:** `int`
