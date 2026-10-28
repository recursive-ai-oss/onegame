[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# BlendState struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BlendState`](BlendState.md)&gt;

```csharp
public readonly struct BlendState : IEquatable<BlendState>
```

Blending of one color target.

## Constructors

| Member | Description |
| --- | --- |
| [`BlendState(BlendComponent, BlendComponent)`](#m-ctor-blendcomponent-blendcomponent) | Blending of one color target. |

### <a id="m-ctor-blendcomponent-blendcomponent"></a>BlendState(BlendComponent, BlendComponent)

```csharp
public BlendState(BlendComponent Color, BlendComponent Alpha);
```

Blending of one color target.

**Parameters**

- `Color` ([`BlendComponent`](BlendComponent.md)): Color channels.
- `Alpha` ([`BlendComponent`](BlendComponent.md)): Alpha channel.

## Properties

| Member | Description |
| --- | --- |
| [`Additive`](#m-additive) | Additive blending: source \* alpha + destination, for glow and particles. |
| [`Alpha`](#m-alpha) | Alpha channel. |
| [`AlphaBlend`](#m-alphablend) | Straight-alpha blending: source \* alpha + destination \* (1 - alpha). |
| [`Color`](#m-color) | Color channels. |
| [`Premultiplied`](#m-premultiplied) | Premultiplied-alpha blending: source + destination \* (1 - alpha). |

### <a id="m-additive"></a>Additive

```csharp
public static BlendState Additive { get; }
```

Additive blending: source \* alpha + destination, for glow and particles.

**Type:** [`BlendState`](BlendState.md)

### <a id="m-alpha"></a>Alpha

```csharp
public BlendComponent Alpha { get; init; }
```

Alpha channel.

**Type:** [`BlendComponent`](BlendComponent.md)

### <a id="m-alphablend"></a>AlphaBlend

```csharp
public static BlendState AlphaBlend { get; }
```

Straight-alpha blending: source \* alpha + destination \* (1 - alpha).

**Type:** [`BlendState`](BlendState.md)

### <a id="m-color"></a>Color

```csharp
public BlendComponent Color { get; init; }
```

Color channels.

**Type:** [`BlendComponent`](BlendComponent.md)

### <a id="m-premultiplied"></a>Premultiplied

```csharp
public static BlendState Premultiplied { get; }
```

Premultiplied-alpha blending: source + destination \* (1 - alpha).

**Type:** [`BlendState`](BlendState.md)
