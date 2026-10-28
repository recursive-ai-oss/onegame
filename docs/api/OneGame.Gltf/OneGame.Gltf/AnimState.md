[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# AnimState class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class AnimState
```

One state of an [`Animator`](Animator.md): a motion and how it plays.

## Properties

| Member | Description |
| --- | --- |
| [`Loop`](#m-loop) | Whether the motion repeats; if not it holds its last pose. |
| [`Motion`](#m-motion) | What the state plays. |
| [`Name`](#m-name) | The state's name. |
| [`Progress`](#m-progress) | Progress through the motion in passes (1 = one full pass), not wrapped, reset when the state is entered. |
| [`Speed`](#m-speed) | Playback speed factor. |

### <a id="m-loop"></a>Loop

```csharp
public bool Loop { get; }
```

Whether the motion repeats; if not it holds its last pose.

**Type:** `bool`

### <a id="m-motion"></a>Motion

```csharp
public Motion Motion { get; }
```

What the state plays.

**Type:** [`Motion`](Motion.md)

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The state's name.

**Type:** `string`

### <a id="m-progress"></a>Progress

```csharp
public float Progress { get; }
```

Progress through the motion in passes (1 = one full pass), not wrapped, reset when the state is entered.

**Type:** `float`

### <a id="m-speed"></a>Speed

```csharp
public float Speed { get; }
```

Playback speed factor.

**Type:** `float`
