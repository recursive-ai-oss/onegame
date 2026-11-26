[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidBodyEvent struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public readonly struct FluidBodyEvent
```

A body becoming wet or dry in a fluid, from [`PhysicsSpace2D.FluidEvents`](PhysicsSpace2D.md#m-fluidevents).

## Properties

| Member | Description |
| --- | --- |
| [`Body`](#m-body) | The body. |
| [`Fluid`](#m-fluid) | The fluid. |
| [`Kind`](#m-kind) | Whether the body became wet or dry. |
| [`Submerged`](#m-submerged) | The covered fraction of the body when the event fired, 0 to 1. |

### <a id="m-body"></a>Body

```csharp
public BodyRef Body { get; }
```

The body.

**Type:** [`BodyRef`](BodyRef.md)

### <a id="m-fluid"></a>Fluid

```csharp
public FluidRef Fluid { get; }
```

The fluid.

**Type:** [`FluidRef`](FluidRef.md)

### <a id="m-kind"></a>Kind

```csharp
public FluidBodyEventKind Kind { get; }
```

Whether the body became wet or dry.

**Type:** [`FluidBodyEventKind`](FluidBodyEventKind.md)

### <a id="m-submerged"></a>Submerged

```csharp
public Fixed Submerged { get; }
```

The covered fraction of the body when the event fired, 0 to 1.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

## Methods

| Member | Description |
| --- | --- |
| [`ToString()`](#m-tostring) |  |

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`ValueType.ToString()`](https://learn.microsoft.com/dotnet/api/system.valuetype.tostring).

**Returns:** `string`
