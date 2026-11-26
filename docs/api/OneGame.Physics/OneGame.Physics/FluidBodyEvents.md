[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidBodyEvents struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public readonly struct FluidBodyEvents
```

The wet and dry events of the last step, ordered by body then fluid, the same on every node. Read them in `OnTick`; the next step replaces them. Particles reaching a drain raise no event: compare [`PhysicsSpace2D.Drained()`](PhysicsSpace2D.md#m-drained-fluiddrainref) from tick to tick.

## Nested types

| Type | Description |
| --- | --- |
| [`FluidBodyEvents.Enumerator`](FluidBodyEvents.Enumerator.md) | Walks the events without allocating. |

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of events. |
| [`this[int]`](#m-item-int) | The event at an index, 0 to [`FluidBodyEvents.Count`](FluidBodyEvents.md#m-count) - 1. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of events.

**Type:** `int`

### <a id="m-item-int"></a>this\[int\]

```csharp
public FluidBodyEvent this[int index] { get; }
```

The event at an index, 0 to [`FluidBodyEvents.Count`](FluidBodyEvents.md#m-count) - 1.

**Parameters**

- `index` (`int`): The index.

**Type:** [`FluidBodyEvent`](FluidBodyEvent.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The index is outside the list.

## Methods

| Member | Description |
| --- | --- |
| [`GetEnumerator()`](#m-getenumerator) | Returns an enumerator over the events. |

### <a id="m-getenumerator"></a>GetEnumerator()

```csharp
public FluidBodyEvents.Enumerator GetEnumerator();
```

Returns an enumerator over the events.

**Returns:** `FluidBodyEvents.`[`Enumerator`](FluidBodyEvents.Enumerator.md)
