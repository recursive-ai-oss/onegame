[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# BlendComponent struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BlendComponent`](BlendComponent.md)&gt;

```csharp
public readonly struct BlendComponent : IEquatable<BlendComponent>
```

Blend settings for one of color or alpha.

## Constructors

| Member | Description |
| --- | --- |
| [`BlendComponent(BlendFactor, BlendFactor, BlendOperation)`](#m-ctor-blendfactor-blendfactor-blendoperation) | Blend settings for one of color or alpha. |

### <a id="m-ctor-blendfactor-blendfactor-blendoperation"></a>BlendComponent(BlendFactor, BlendFactor, BlendOperation)

```csharp
public BlendComponent(BlendFactor Source, BlendFactor Destination, BlendOperation Operation);
```

Blend settings for one of color or alpha.

**Parameters**

- `Source` ([`BlendFactor`](BlendFactor.md)): Factor for the source value.
- `Destination` ([`BlendFactor`](BlendFactor.md)): Factor for the destination value.
- `Operation` ([`BlendOperation`](BlendOperation.md)): How the two combine.

## Properties

| Member | Description |
| --- | --- |
| [`Destination`](#m-destination) | Factor for the destination value. |
| [`Operation`](#m-operation) | How the two combine. |
| [`Source`](#m-source) | Factor for the source value. |

### <a id="m-destination"></a>Destination

```csharp
public BlendFactor Destination { get; init; }
```

Factor for the destination value.

**Type:** [`BlendFactor`](BlendFactor.md)

### <a id="m-operation"></a>Operation

```csharp
public BlendOperation Operation { get; init; }
```

How the two combine.

**Type:** [`BlendOperation`](BlendOperation.md)

### <a id="m-source"></a>Source

```csharp
public BlendFactor Source { get; init; }
```

Factor for the source value.

**Type:** [`BlendFactor`](BlendFactor.md)
